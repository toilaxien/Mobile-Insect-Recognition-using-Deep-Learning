using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TensorFlowLite;

public class PredictControllerTFLite : MonoBehaviour
{
    [Header("Scene names")]
    public string scanSceneName = "ScanScene";
    public string resultSceneName = "ResultScene";

    [Header("Models (.tflite kéo vào Inspector để chạy trong Editor)")]
    public UnityEngine.Object detectTfLiteAsset;     // kéo YOLO.tflite vào đây (Editor)
    public UnityEngine.Object classifyTfLiteAsset;   // kéo MobileNet.tflite vào đây (Editor)

    [Header("Fallback names trong StreamingAssets (chạy trên điện thoại)")]
    public string detectFallbackName = "YOLO.tflite";
    public string classifyFallbackName = "MobileNet.tflite";

    [Header("Input size")]
    public int detectInputW = 640;
    public int detectInputH = 640;
    public int classiInputW = 224;
    public int classiInputH = 224;

    [Header("Threshold")]
    public float detectMinScore = 0.5f;
    public float classiMinScore = 0.6f;

    [Header("Audio (nhạc nền + đang dự đoán)")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    public float bgmNormalVolume = 0.5f;
    public float bgmDuckVolume = 0.2f;
    public float fadeSpeed = 4f;

    public AudioSource voiceSource;
    public AudioClip predictingClip;
    public AudioClip failClip;

    private Interpreter detectInterpreter;
    private Interpreter classifyInterpreter;

    void Start()
    {
        // Nhạc nền
        if (bgmSource && bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = bgmNormalVolume;
            if (!bgmSource.isPlaying) bgmSource.Play();
        }

        // Load model + predict
        StartCoroutine(InitAndPredict());
    }

    void OnDestroy()
    {
        detectInterpreter?.Dispose();
        classifyInterpreter?.Dispose();
    }

    IEnumerator InitAndPredict()
    {
        // 1) Load YOLO bytes
        byte[] detBytes = null;
        yield return LoadModelBytesCoroutine(detectTfLiteAsset, detectFallbackName, b => detBytes = b);

        // 2) Load MobileNet bytes
        byte[] clsBytes = null;
        yield return LoadModelBytesCoroutine(classifyTfLiteAsset, classifyFallbackName, b => clsBytes = b);

        if (detBytes == null)
        {
            Debug.LogError("Không load được YOLO model trên thiết bị!");
            yield return FailRoutine("Không load được model YOLO.");
            yield break;
        }
        if (clsBytes == null)
        {
            Debug.LogError("Không load được MobileNet model trên thiết bị!");
            yield return FailRoutine("Không load được model phân loại.");
            yield break;
        }

        detectInterpreter = new Interpreter(detBytes);
        detectInterpreter.AllocateTensors();

        classifyInterpreter = new Interpreter(clsBytes);
        classifyInterpreter.AllocateTensors();

        // bắt đầu predict
        yield return PredictRoutine();
    }

    // =========================
    // LOAD BYTES (Editor + Android)
    // =========================
    IEnumerator LoadModelBytesCoroutine(UnityEngine.Object asset, string fallbackFileName, System.Action<byte[]> onDone)
    {
        // A) Nếu bạn kéo TextAsset/bytes
        if (asset is TextAsset ta)
        {
            onDone?.Invoke(ta.bytes);
            yield break;
        }

#if UNITY_EDITOR
        // B) Trong Editor: kéo .tflite -> đọc file trực tiếp
        if (asset != null)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                onDone?.Invoke(File.ReadAllBytes(path));
                yield break;
            }
        }
#endif

        // C) Trên Android/iOS: đọc từ StreamingAssets bằng UnityWebRequest
        string saPath = Path.Combine(Application.streamingAssetsPath, fallbackFileName);
        using (var req = UnityWebRequest.Get(saPath))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Không đọc được StreamingAssets: {saPath} | {req.error}");
                onDone?.Invoke(null);
            }
            else
            {
                onDone?.Invoke(req.downloadHandler.data);
            }
        }
    }

    // =========================
    // MAIN ROUTINE
    // =========================
    IEnumerator PredictRoutine()
    {
        if (voiceSource && predictingClip)
        {
            yield return FadeBGM(bgmDuckVolume);
            voiceSource.PlayOneShot(predictingClip);
        }

        string path = PredictStore.capturedImagePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            yield return FailRoutine("Không tìm thấy ảnh để dự đoán.");
            yield break;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        // 1) DETECT
        DetectResult det = RunDetect_TFLite(tex);
        if (det == null || det.score < detectMinScore)
        {
            Destroy(tex);
            yield return FailRoutine("Bé Đốm không nhận diện được bạn côn trùng này!");
            yield break;
        }

        // 2) CROP
        Texture2D crop = CropByBBox(tex, det);
        Destroy(tex);

        // 3) CLASSIFY
        ClassifyResult cls = RunClassify_TFLite(crop);
        if (cls == null || cls.score < classiMinScore || string.IsNullOrEmpty(cls.label))
        {
            Destroy(crop);
            yield return FailRoutine("Bé Đốm không nhận diện được bạn côn trùng này!");
            yield break;
        }

        // 4) SAVE
        string savedPath = SaveToCollection(crop, cls.label);
        Destroy(crop);

        PredictStore.predictedLabel = cls.label;
        PredictStore.predictedScore = cls.score;
        PredictStore.savedImagePath = savedPath;

        yield return FadeBGM(bgmNormalVolume);
        SceneManager.LoadScene(resultSceneName);
    }

    // =========================
    // DETECT (YOLO)
    // =========================
    public class DetectResult
    {
        public Rect bbox;   // normalized xMin,yMin,w,h
        public float score; // obj * bestClassProb
        public int classId;
    }

    DetectResult RunDetect_TFLite(Texture2D tex)
    {
        if (detectInterpreter == null) return null;

        float[] input = TextureToFloatArray(tex, detectInputW, detectInputH);
        detectInterpreter.SetInputTensorData(0, input);
        detectInterpreter.Invoke();

        var outInfo = detectInterpreter.GetOutputTensorInfo(0); // shape [1, N, 11] chẳng hạn
        int outSize = 1;
        foreach (int d in outInfo.shape) outSize *= d;

        float[] output = new float[outSize];
        detectInterpreter.GetOutputTensorData(0, output);

        return ParseYOLO_Output(output, outInfo.shape);
    }

    DetectResult ParseYOLO_Output(float[] output, int[] shape)
    {
        // hỗ trợ [1,N,stride] hoặc [N,stride]
        int rank = shape.Length;

        int N, stride;
        if (rank == 3) { N = shape[1]; stride = shape[2]; }
        else if (rank == 2) { N = shape[0]; stride = shape[1]; }
        else return null;

        int numClasses = stride - 5;   // stride=11 => 6 class
        if (numClasses <= 0) return null;

        int best = -1;
        int bestCls = -1;
        float bestScore = 0f;

        for (int i = 0; i < N; i++)
        {
            int baseIdx = i * stride;
            float obj = output[baseIdx + 4];

            // tìm class prob lớn nhất
            float bestProb = 0f;
            int clsId = 0;
            for (int c = 0; c < numClasses; c++)
            {
                float p = output[baseIdx + 5 + c];
                if (p > bestProb)
                {
                    bestProb = p;
                    clsId = c;
                }
            }

            float score = obj * bestProb;
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
                bestCls = clsId;
            }
        }

        if (best < 0) return null;

        int b0 = best * stride;

        // YOLO thường output cx,cy,w,h normalized 0..1
        float cx = output[b0 + 0];
        float cy = output[b0 + 1];
        float w  = output[b0 + 2];
        float h  = output[b0 + 3];

        float xMin = cx - w * 0.5f;
        float yMin = cy - h * 0.5f;

        return new DetectResult
        {
            bbox = new Rect(xMin, yMin, w, h),
            score = bestScore,
            classId = bestCls
        };
    }

    // =========================
    // CLASSIFY (MobileNet)
    // =========================
    public class ClassifyResult
    {
        public string label;
        public float score;
        public int classId;
    }

    ClassifyResult RunClassify_TFLite(Texture2D tex)
    {
        if (classifyInterpreter == null) return null;

        float[] input = TextureToFloatArray(tex, classiInputW, classiInputH);
        classifyInterpreter.SetInputTensorData(0, input);
        classifyInterpreter.Invoke();

        var outInfo = classifyInterpreter.GetOutputTensorInfo(0); // shape (?,11) => thực ra là [1,11]
        int outSize = 1;
        foreach (int d in outInfo.shape) outSize *= d;

        float[] prob = new float[outSize];
        classifyInterpreter.GetOutputTensorData(0, prob);

        int best = 0;
        float bestScore = prob[0];
        for (int i = 1; i < prob.Length; i++)
        {
            if (prob[i] > bestScore)
            {
                bestScore = prob[i];
                best = i;
            }
        }

        return new ClassifyResult
        {
            classId = best,
            label = IndexToLabel(best),
            score = bestScore
        };
    }

    // =========================
    // HELPERS
    // =========================
    float[] TextureToFloatArray(Texture2D tex, int w, int h)
    {
        Texture2D resized = ResizeTexture(tex, w, h);
        Color32[] pixels = resized.GetPixels32();

        float[] input = new float[w * h * 3];
        int idx = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            input[idx++] = c.r / 255f;
            input[idx++] = c.g / 255f;
            input[idx++] = c.b / 255f;
        }

        Destroy(resized);
        return input;
    }

    Texture2D ResizeTexture(Texture2D src, int w, int h)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h);
        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    Texture2D CropByBBox(Texture2D src, DetectResult det)
    {
        Rect b = det.bbox;

        int x = Mathf.Clamp(Mathf.RoundToInt(b.x * src.width), 0, src.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(b.y * src.height), 0, src.height - 1);
        int w = Mathf.Clamp(Mathf.RoundToInt(b.width * src.width), 1, src.width - x);
        int h = Mathf.Clamp(Mathf.RoundToInt(b.height * src.height), 1, src.height - y);

        int pad = Mathf.RoundToInt(0.1f * Mathf.Max(w, h));
        x = Mathf.Clamp(x - pad, 0, src.width - 1);
        y = Mathf.Clamp(y - pad, 0, src.height - 1);
        w = Mathf.Clamp(w + pad * 2, 1, src.width - x);
        h = Mathf.Clamp(h + pad * 2, 1, src.height - y);

        Color[] pixels = src.GetPixels(x, y, w, h);
        Texture2D cropped = new Texture2D(w, h);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }

    string SaveToCollection(Texture2D tex, string label)
    {
        string root = Path.Combine(Application.persistentDataPath, "Collection");
        string folder = Path.Combine(root, label);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string fileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
        string savePath = Path.Combine(folder, fileName);
        File.WriteAllBytes(savePath, tex.EncodeToJPG(90));
        return savePath;
    }

    IEnumerator FailRoutine(string msg)
    {
        PredictStore.errorMessage = msg;

        if (voiceSource && failClip)
        {
            yield return FadeBGM(bgmDuckVolume);
            voiceSource.PlayOneShot(failClip);
        }

        yield return new WaitForSeconds(0.3f);
        yield return FadeBGM(bgmNormalVolume);
        SceneManager.LoadScene(scanSceneName);
    }

    IEnumerator FadeBGM(float target)
    {
        if (bgmSource == null) yield break;
        while (Mathf.Abs(bgmSource.volume - target) > 0.01f)
        {
            bgmSource.volume = Mathf.Lerp(bgmSource.volume, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        bgmSource.volume = target;
    }

    string IndexToLabel(int idx)
    {
        // thay bằng label thật
        string[] labels = { "ant",
    "butterfly",
    "cockroach",
    "dragonfly",
    "fly",
    "grasshopper",
    "honeybee",
    "ladybug",
    "mosquito",
    "silkworm",
    "spider" };
        if (idx < 0 || idx >= labels.Length) return "";
        return labels[idx];
    }
}
