// using System.Collections;
// using System.IO;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.Networking;
// using UnityEngine.UI;
// using TensorFlowLite;
// using TMPro;

// public class PredictControllerTFLiteOptimized : MonoBehaviour
// {
//     [Header("--- CÀI ĐẶT SCENE ---")]
//     public string scanSceneName = "ScanScene";
//     public string resultSceneName = "ResultScene";

//     [Header("--- UI (Kéo TextMeshPro vào đây) ---")]
//     public TMP_Text statusText;

//     [Header("--- MODELS (Kéo file .tflite vào đây để test Editor) ---")]
//     public UnityEngine.Object detectTfLiteAsset;     // Kéo YOLO.tflite
//     public UnityEngine.Object classifyTfLiteAsset;   // Kéo MobileNet.tflite

//     [Header("--- FALLBACK (Tên file trong StreamingAssets/Models) ---")]
//     public string detectFallbackName = "YOLO.tflite";
//     public string classifyFallbackName = "MobileNet.tflite";

//     [Header("--- INPUT SIZE (Giảm xuống 320 nếu J7 Pro vẫn lag) ---")]
//     public int detectInputW = 640; 
//     public int detectInputH = 640; 
//     public int classiInputW = 224;
//     public int classiInputH = 224;

//     [Header("--- NGƯỠNG (Threshold) ---")]
//     public float detectMinScore = 0.3f; 
//     public float classiMinScore = 0.5f;

//     [Header("--- ÂM THANH ---")]
//     public AudioSource bgmSource;
//     public AudioClip bgmClip;
//     public float bgmNormalVolume = 0.5f;
//     public float bgmDuckVolume = 0.2f;
//     public float fadeSpeed = 4f;

//     public AudioSource voiceSource;
//     public AudioClip predictingClip;
//     public AudioClip failClip;

//     // Các biến nội bộ
//     private Interpreter detectInterpreter;
//     private Interpreter classifyInterpreter;
//     private float[] detectInputBuffer;
//     private float[] classiInputBuffer;

//     private InterpreterOptions options;

//     static readonly string[] LABELS = {
//         "ant", "butterfly", "cockroach", "dragonfly", "fly",
//         "grasshopper", "honeybee", "ladybug", "mosquito", "silkworm", "spider"
//     };

//     void Start()
//     {
//         SetStatus("Khởi động...");
        
//         options = new InterpreterOptions();
//         options.threads = 2; // J7 Pro dùng 2 nhân

//         if (bgmSource && bgmClip)
//         {
//             bgmSource.clip = bgmClip;
//             bgmSource.loop = true;
//             bgmSource.volume = bgmNormalVolume;
//             if (!bgmSource.isPlaying) bgmSource.Play();
//         }

//         StartCoroutine(InitAndPredict());
//     }

//     void OnDestroy()
//     {
//         detectInterpreter?.Dispose();
//         classifyInterpreter?.Dispose();
//         options?.Dispose();
//     }

//     // ==========================================================
//     // 1. QUY TRÌNH KHỞI TẠO
//     // ==========================================================
//     IEnumerator InitAndPredict()
//     {
//         string errorMsg = null; 

//         // --- LOAD YOLO ---
//         SetStatus("1. Đang tải file YOLO...");
//         byte[] detBytes = null;
//         yield return LoadModelBytesCoroutine(detectTfLiteAsset, detectFallbackName, b => detBytes = b);
//         if (detBytes == null) { yield return FailRoutine("Lỗi: Không tìm thấy YOLO"); yield break; }
//         yield return Resources.UnloadUnusedAssets();

//         // --- LOAD MOBILENET ---
//         SetStatus("2. Đang tải file MobileNet...");
//         byte[] clsBytes = null;
//         yield return LoadModelBytesCoroutine(classifyTfLiteAsset, classifyFallbackName, b => clsBytes = b);
//         if (clsBytes == null) { yield return FailRoutine("Lỗi: Không tìm thấy MobileNet"); yield break; }
//         yield return Resources.UnloadUnusedAssets();

//         // --- INIT YOLO ---
//         SetStatus("3. Khởi tạo YOLO...");
//         yield return null; 
//         try {
//             detectInterpreter = new Interpreter(detBytes, options);
//             detectInterpreter.AllocateTensors();
//         } catch (System.Exception ex) { errorMsg = $"Lỗi RAM YOLO: {ex.Message}"; }
//         if (errorMsg != null) { yield return FailRoutine(errorMsg); yield break; }

//         // --- INIT MOBILENET ---
//         SetStatus("4. Khởi tạo MobileNet...");
//         yield return null;
//         try {
//             classifyInterpreter = new Interpreter(clsBytes, options);
//             classifyInterpreter.AllocateTensors();
//         } catch (System.Exception ex) { errorMsg = $"Lỗi RAM MobileNet: {ex.Message}"; }
//         if (errorMsg != null) { yield return FailRoutine(errorMsg); yield break; }

//         // --- BUFFER ---
//         SetStatus("5. Tạo bộ đệm...");
//         try {
//             detectInputBuffer = new float[detectInputW * detectInputH * 3];
//             classiInputBuffer = new float[classiInputW * classiInputH * 3];
//         } catch (System.Exception ex) { errorMsg = $"Hết bộ nhớ Buffer: {ex.Message}"; }
//         if (errorMsg != null) { yield return FailRoutine(errorMsg); yield break; }

//         SetStatus("Sẵn sàng! Đang xử lý ảnh...");
//         yield return PredictRoutine();
//     }

//     IEnumerator LoadModelBytesCoroutine(UnityEngine.Object asset, string fallbackFileName, System.Action<byte[]> onDone)
//     {
// #if UNITY_EDITOR
//         if (asset != null) {
//             if (asset is TextAsset ta) { onDone?.Invoke(ta.bytes); yield break; }
//             string path = UnityEditor.AssetDatabase.GetAssetPath(asset);
//             if (File.Exists(path)) { onDone?.Invoke(File.ReadAllBytes(path)); yield break; }
//         }
// #endif
//         string folder = Path.Combine(Application.streamingAssetsPath, "Models");
//         string fullPath = Path.Combine(folder, fallbackFileName);
//         using (var req = UnityWebRequest.Get(fullPath)) {
//             yield return req.SendWebRequest();
//             if (req.result != UnityWebRequest.Result.Success) {
//                 Debug.LogError($"Lỗi tải: {fullPath} | {req.error}");
//                 onDone?.Invoke(null);
//             } else { onDone?.Invoke(req.downloadHandler.data); }
//         }
//     }

//     // ==========================================================
//     // 2. QUY TRÌNH DỰ ĐOÁN
//     // ==========================================================
//     IEnumerator PredictRoutine()
//     {
//         if (voiceSource && predictingClip) {
//             yield return FadeBGM(bgmDuckVolume);
//             voiceSource.PlayOneShot(predictingClip);
//         }

//         string path = PredictStore.capturedImagePath;
//         SetStatus($"6. Đọc ảnh...");

//         if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
//             yield return FailRoutine("Không tìm thấy ảnh chụp."); yield break;
//         }

//         Texture2D tex = new Texture2D(2, 2);
//         string errorMsg = null;
//         try {
//             byte[] fileData = File.ReadAllBytes(path);
//             tex.LoadImage(fileData); 
//         } catch(System.Exception ex) { errorMsg = "Lỗi đọc ảnh: " + ex.Message; }
//         if (errorMsg != null) { yield return FailRoutine(errorMsg); yield break; }

//         // --- DETECT ---
//         SetStatus("7. Đang Detect (YOLO)...");
//         yield return null;

//         DetectResult det = null;
//         try { det = RunDetect_TFLite(tex); } 
//         catch (System.Exception ex) { errorMsg = $"Lỗi YOLO: {ex.Message}"; }
        
//         if (errorMsg != null) { Destroy(tex); yield return FailRoutine(errorMsg); yield break; }

//         if (det == null || det.score < detectMinScore) {
//             Destroy(tex);
//             yield return FailRoutine($"Không tìm thấy côn trùng (Score: {det?.score ?? 0})");
//             yield break;
//         }

//         // --- CROP ---
//         SetStatus("8. Đang cắt ảnh...");
//         Texture2D crop = CropByBBox(tex, det);
//         Destroy(tex); 

//         // --- CLASSIFY ---
//         SetStatus("9. Đang phân loại...");
//         yield return null;

//         ClassifyResult cls = null;
//         try { cls = RunClassify_TFLite(crop); } 
//         catch (System.Exception ex) { errorMsg = $"Lỗi MobileNet: {ex.Message}"; }

//         if (errorMsg != null) { Destroy(crop); yield return FailRoutine(errorMsg); yield break; }

//         if (cls == null || cls.score < classiMinScore) {
//             Destroy(crop);
//             yield return FailRoutine($"Không nhận diện được loài (Label: {cls?.label})");
//             yield break;
//         }

//         // --- SAVE ---
//         SetStatus("10. Lưu kết quả...");
//         string savedPath = SaveToCollection(crop, cls.label);
//         Destroy(crop); 

//         PredictStore.predictedLabel = cls.label;
//         PredictStore.predictedScore = cls.score;
//         PredictStore.savedImagePath = savedPath;

//         SetStatus($"Xong! {cls.label}");
//         yield return new WaitForSeconds(0.5f);
//         yield return FadeBGM(bgmNormalVolume);
//         SceneManager.LoadScene(resultSceneName);
//     }

//     // ==========================================================
//     // 3. XỬ LÝ NEURAL NETWORK
//     // ==========================================================
//     public class DetectResult { public Rect bbox; public float score; }

//     DetectResult RunDetect_TFLite(Texture2D tex)
//     {
//         if (detectInterpreter == null) return null;
        
//         // YOLO THƯỜNG CẦN CHIA 255 (về 0-1) => normalize = true
//         TextureToFloatArrayNonAlloc(tex, detectInputW, detectInputH, detectInputBuffer, true); 
        
//         detectInterpreter.SetInputTensorData(0, detectInputBuffer);
//         detectInterpreter.Invoke();

//         var outInfo = detectInterpreter.GetOutputTensorInfo(0);
//         int outSize = 1; foreach (int d in outInfo.shape) outSize *= d;
//         float[] output = new float[outSize];
//         detectInterpreter.GetOutputTensorData(0, output);

//         return ParseYOLO_Output(output, outInfo.shape);
//     }

//     // (Giữ nguyên phần ParseYOLO như cũ, không đổi)
//     DetectResult ParseYOLO_Output(float[] output, int[] shape)
//     {
//         if (shape.Length == 3 && shape[1] == 5) {
//             int boxes = shape[2]; int stride = boxes;
//             int best = -1; float bestConf = 0f;
//             for (int i = 0; i < boxes; i++) {
//                 float conf = output[4 * stride + i];
//                 if (conf > bestConf) { bestConf = conf; best = i; }
//             }
//             if (best < 0) return null;
//             float cx = output[0 * stride + best]; float cy = output[1 * stride + best];
//             float w = output[2 * stride + best]; float h = output[3 * stride + best];
//             return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestConf };
//         }
//         if (shape.Length == 3 && shape[2] == 5) {
//              int boxes = shape[1]; int stride = shape[2];
//              int best = -1; float bestConf = 0f;
//              for(int i=0; i<boxes; i++) {
//                  float conf = output[i*stride + 4];
//                  if(conf > bestConf) { bestConf = conf; best = i; }
//              }
//              if(best < 0) return null;
//              float cx = output[best*stride + 0]; float cy = output[best*stride + 1];
//              float w = output[best*stride + 2]; float h = output[best*stride + 3];
//              return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestConf };
//         }
//         if (shape.Length == 3 && shape[1] > 10) {
//              int N = shape[1]; int stride = shape[2];
//              int best = -1; float bestScore = 0f;
//              for (int i = 0; i < N; i++) {
//                  float conf = output[i * stride + 4];
//                  if (conf > bestScore) { bestScore = conf; best = i; }
//              }
//              if (best < 0) return null;
//              float cx = output[best * stride + 0]; float cy = output[best * stride + 1];
//              float w = output[best * stride + 2]; float h = output[best * stride + 3];
//              return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestScore };
//         }
//         return null;
//     }

//     public class ClassifyResult { public string label; public float score; public int classId; }

//     ClassifyResult RunClassify_TFLite(Texture2D tex)
//     {
//         if (classifyInterpreter == null) return null;

//         // MOBILENET CỦA BẠN CẦN GIỮ NGUYÊN 0-255 => normalize = false
//         TextureToFloatArrayNonAlloc(tex, classiInputW, classiInputH, classiInputBuffer, false);

//         classifyInterpreter.SetInputTensorData(0, classiInputBuffer);
//         classifyInterpreter.Invoke();

//         var outInfo = classifyInterpreter.GetOutputTensorInfo(0);
//         int outSize = 1; foreach (int d in outInfo.shape) outSize *= d;
//         float[] prob = new float[outSize];
//         classifyInterpreter.GetOutputTensorData(0, prob);

//         int best = 0; float bestScore = prob[0];
//         for (int i = 1; i < prob.Length; i++) {
//             if (prob[i] > bestScore) { bestScore = prob[i]; best = i; }
//         }
//         return new ClassifyResult { classId = best, label = IndexToLabel(best), score = bestScore };
//     }

//     // ==========================================================
//     // 4. TIỆN ÍCH HÌNH ẢNH (ĐÃ SỬA normalize)
//     // ==========================================================
    
//     // Thêm tham số 'normalize0to1'
//     void TextureToFloatArrayNonAlloc(Texture2D tex, int w, int h, float[] buffer, bool normalize0to1)
//     {
//         Texture2D resized = ResizeTexture(tex, w, h);
//         Color32[] pixels = resized.GetPixels32();
//         int idx = 0;
        
//         for (int i = 0; i < pixels.Length; i++) {
//             var c = pixels[i];
            
//             if (normalize0to1)
//             {
//                 // Chia 255 (về 0.0 - 1.0) -> Dùng cho YOLO
//                 buffer[idx++] = c.r / 255f; 
//                 buffer[idx++] = c.g / 255f; 
//                 buffer[idx++] = c.b / 255f;
//             }
//             else
//             {
//                 // Giữ nguyên (0.0 - 255.0) -> Dùng cho MobileNet của bạn
//                 buffer[idx++] = (float)c.r;
//                 buffer[idx++] = (float)c.g;
//                 buffer[idx++] = (float)c.b;
//             }
//         }
//         Destroy(resized);
//     }

//     Texture2D ResizeTexture(Texture2D src, int w, int h)
//     {
//         RenderTexture rt = RenderTexture.GetTemporary(w, h);
//         Graphics.Blit(src, rt);
//         RenderTexture prev = RenderTexture.active; RenderTexture.active = rt;
//         Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
//         tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
//         RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
//         return tex;
//     }

//     Texture2D CropByBBox(Texture2D src, DetectResult det)
//     {
//         Rect b = det.bbox;
//         // Fix ngược trục Y
//         float unityY = 1.0f - (b.y + b.height); 

//         int x = Mathf.Clamp(Mathf.RoundToInt(b.x * src.width), 0, src.width - 1);
//         int y = Mathf.Clamp(Mathf.RoundToInt(unityY * src.height), 0, src.height - 1);
        
//         int w = Mathf.Clamp(Mathf.RoundToInt(b.width * src.width), 1, src.width - x);
//         int h = Mathf.Clamp(Mathf.RoundToInt(b.height * src.height), 1, src.height - y);
        
//         // Padding
//         int pad = Mathf.RoundToInt(0.1f * Mathf.Max(w, h)); 
//         x = Mathf.Clamp(x - pad, 0, src.width - 1); 
//         y = Mathf.Clamp(y - pad, 0, src.height - 1);
//         w = Mathf.Clamp(w + pad * 2, 1, src.width - x); 
//         h = Mathf.Clamp(h + pad * 2, 1, src.height - y);
        
//         Color[] pixels = src.GetPixels(x, y, w, h);
//         Texture2D cropped = new Texture2D(w, h); cropped.SetPixels(pixels); cropped.Apply();
//         return cropped;
//     }

//     string SaveToCollection(Texture2D tex, string label)
//     {
//         string folder = Path.Combine(Application.persistentDataPath, "Collection", label);
//         if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
//         string path = Path.Combine(folder, System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");
//         File.WriteAllBytes(path, tex.EncodeToJPG(90)); return path;
//     }

//     IEnumerator FailRoutine(string msg)
//     {
//         SetStatus("LỖI: " + msg); 
//         PredictStore.errorMessage = msg;
//         if (voiceSource && failClip) { yield return FadeBGM(bgmDuckVolume); voiceSource.PlayOneShot(failClip); }
//         yield return new WaitForSeconds(2.0f); 
//         yield return FadeBGM(bgmNormalVolume);
//         SceneManager.LoadScene(scanSceneName);
//     }

//     IEnumerator FadeBGM(float target)
//     {
//         if (!bgmSource) yield break;
//         while (Mathf.Abs(bgmSource.volume - target) > 0.01f) {
//             bgmSource.volume = Mathf.Lerp(bgmSource.volume, target, Time.deltaTime * fadeSpeed); yield return null;
//         } bgmSource.volume = target;
//     }

//     string IndexToLabel(int idx) { if (idx < 0 || idx >= LABELS.Length) return "unknown"; return LABELS[idx]; }

//     void SetStatus(string s) { Debug.Log("[Predict] " + s); if (statusText != null) statusText.text = s; }
// }



// using System.Collections;
// using System.IO;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.Networking;
// using TensorFlowLite;
// using TMPro;

// public class PredictControllerTFLiteOptimized : MonoBehaviour
// {
//     [Header("--- CÀI ĐẶT SCENE ---")]
//     public string scanSceneName = "ScanScene";
//     public string resultSceneName = "ResultScene";

//     [Header("--- UI ---")]
//     public TMP_Text statusText;

//     [Header("--- MODELS (Kéo file .tflite vào đây để test Editor) ---")]
//     public UnityEngine.Object detectTfLiteAsset;     // YOLO.tflite
//     public UnityEngine.Object classifyTfLiteAsset;   // MobileNet.tflite

//     [Header("--- FALLBACK (Tên file trong StreamingAssets/Models) ---")]
//     public string detectFallbackName = "YOLO.tflite";
//     public string classifyFallbackName = "MobileNet.tflite";

//     [Header("--- INPUT SIZE (Giảm xuống 320 nếu J7 Pro vẫn lag) ---")]
//     public int detectInputW = 640;
//     public int detectInputH = 640;
//     public int classiInputW = 224;
//     public int classiInputH = 224;

//     [Header("--- NGƯỠNG (Threshold) ---")]
//     public float detectMinScore = 0.3f;
//     public float classiMinScore = 0.5f;

//     [Header("--- ÂM THANH ---")]
//     public AudioSource bgmSource;
//     public AudioClip bgmClip;
//     public float bgmNormalVolume = 0.5f;
//     public float bgmDuckVolume = 0.2f;
//     public float fadeSpeed = 4f;

//     public AudioSource voiceSource;
//     public AudioClip predictingClip;
//     public AudioClip failClip;

//     // Nội bộ
//     private Interpreter detectInterpreter;
//     private Interpreter classifyInterpreter;
//     private float[] detectInputBuffer;
//     private float[] classiInputBuffer;
//     private InterpreterOptions options;

//     static readonly string[] LABELS = {
//         "ant", "butterfly", "cockroach", "dragonfly", "fly",
//         "grasshopper", "honeybee", "ladybug", "mosquito", "silkworm", "spider"
//     };

//     void Start()
//     {
//         SetStatus("B0: Khởi động...");

//         options = new InterpreterOptions { threads = 2 };

//         if (bgmSource && bgmClip)
//         {
//             bgmSource.clip = bgmClip;
//             bgmSource.loop = true;
//             bgmSource.volume = bgmNormalVolume;
//             if (!bgmSource.isPlaying) bgmSource.Play();
//         }

//         StartCoroutine(InitAndPredict());
//     }

//     void OnDestroy()
//     {
//         detectInterpreter?.Dispose();
//         classifyInterpreter?.Dispose();
//         options?.Dispose();
//     }

//     // ==========================================================
//     // 1. INIT + LOAD MODEL
//     IEnumerator InitAndPredict()
//     {
//         string errorMsg = null;

//         // --- LOAD YOLO ---
//         SetStatus("B1: Đang tải YOLO...");
//         byte[] detBytes = null;
//         yield return LoadModelBytesCoroutine(detectTfLiteAsset, detectFallbackName, b => detBytes = b);
//         if (detBytes == null)
//         {
//             yield return FailRoutine("B1 FAIL: Không tìm thấy YOLO");
//             yield break;
//         }
//         yield return Resources.UnloadUnusedAssets();

//         // --- LOAD MOBILENET ---
//         SetStatus("B2: Đang tải MobileNet...");
//         byte[] clsBytes = null;
//         yield return LoadModelBytesCoroutine(classifyTfLiteAsset, classifyFallbackName, b => clsBytes = b);
//         if (clsBytes == null)
//         {
//             yield return FailRoutine("B2 FAIL: Không tìm thấy MobileNet");
//             yield break;
//         }
//         yield return Resources.UnloadUnusedAssets();

//         // --- INIT YOLO ---
//         SetStatus("B3: Khởi tạo YOLO...");
//         yield return null;
//         try
//         {
//             detectInterpreter = new Interpreter(detBytes, options);
//             detectInterpreter.AllocateTensors();
//         }
//         catch (System.Exception ex)
//         {
//             errorMsg = $"B3 FAIL: YOLO AllocateTensors lỗi RAM: {ex.Message}";
//         }
//         if (errorMsg != null)
//         {
//             yield return FailRoutine(errorMsg);
//             yield break;
//         }

//         // --- INIT MOBILENET ---
//         SetStatus("B4: Khởi tạo MobileNet...");
//         yield return null;
//         try
//         {
//             classifyInterpreter = new Interpreter(clsBytes, options);
//             classifyInterpreter.AllocateTensors();
//         }
//         catch (System.Exception ex)
//         {
//             errorMsg = $"B4 FAIL: MobileNet AllocateTensors lỗi RAM: {ex.Message}";
//         }
//         if (errorMsg != null)
//         {
//             yield return FailRoutine(errorMsg);
//             yield break;
//         }

//         // --- BUFFERS ---
//         SetStatus("B5: Tạo buffer...");
//         try
//         {
//             detectInputBuffer = new float[detectInputW * detectInputH * 3];
//             classiInputBuffer = new float[classiInputW * classiInputH * 3];
//         }
//         catch (System.Exception ex)
//         {
//             errorMsg = $"B5 FAIL: Hết bộ nhớ buffer: {ex.Message}";
//         }
//         if (errorMsg != null)
//         {
//             yield return FailRoutine(errorMsg);
//             yield break;
//         }

//         SetStatus("B6: Sẵn sàng -> Predict...");
//         yield return PredictRoutine();
//     }

//     IEnumerator LoadModelBytesCoroutine(UnityEngine.Object asset, string fallbackFileName, System.Action<byte[]> onDone)
//     {
// #if UNITY_EDITOR
//         if (asset != null)
//         {
//             if (asset is TextAsset ta)
//             {
//                 onDone?.Invoke(ta.bytes);
//                 yield break;
//             }

//             string path = UnityEditor.AssetDatabase.GetAssetPath(asset);
//             if (!string.IsNullOrEmpty(path) && File.Exists(path))
//             {
//                 onDone?.Invoke(File.ReadAllBytes(path));
//                 yield break;
//             }
//         }
// #endif

//         string folder = Path.Combine(Application.streamingAssetsPath, "Models");
//         string fullPath = Path.Combine(folder, fallbackFileName);

//         using (var req = UnityWebRequest.Get(fullPath))
//         {
//             yield return req.SendWebRequest();
//             if (req.result != UnityWebRequest.Result.Success)
//             {
//                 Debug.LogError($"Lỗi tải model: {fullPath} | {req.error}");
//                 onDone?.Invoke(null);
//             }
//             else
//             {
//                 onDone?.Invoke(req.downloadHandler.data);
//             }
//         }
//     }

//     // ==========================================================
//     // 2. PREDICT
//     IEnumerator PredictRoutine()
//     {
//         // audio predicting
//         if (voiceSource && predictingClip)
//         {
//             SetStatus("B6.1: Phát âm predicting...");
//             yield return FadeBGM(bgmDuckVolume);
//             voiceSource.PlayOneShot(predictingClip);
//         }

//         // đọc ảnh
//         SetStatus("B6.2: Đọc ảnh chụp...");
//         string path = PredictStore.capturedImagePath;
//         Debug.Log($"[Predict] capturedImagePath={path}");

//         if (string.IsNullOrEmpty(path) || !File.Exists(path))
//         {
//             yield return FailRoutine("B6.2 FAIL: Không tìm thấy ảnh chụp");
//             yield break;
//         }

//         Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
//         string errorMsg = null;
//         try
//         {
//             tex.LoadImage(File.ReadAllBytes(path));
//         }
//         catch (System.Exception ex)
//         {
//             errorMsg = "B6.2 FAIL: Lỗi đọc ảnh: " + ex.Message;
//         }

//         if (errorMsg != null)
//         {
//             Destroy(tex);
//             yield return FailRoutine(errorMsg);
//             yield break;
//         }

//         // --- DETECT ---
//         SetStatus("B7: YOLO Detect...");
//         DetectResult det = null;
//         try
//         {
//             det = RunDetect_TFLite(tex);
//         }
//         catch (System.Exception ex)
//         {
//             errorMsg = $"B7 FAIL: Lỗi YOLO: {ex.Message}";
//         }

//         if (errorMsg != null)
//         {
//             Destroy(tex);
//             yield return FailRoutine(errorMsg);
//             yield break;
//         }

//         Debug.Log($"[YOLO] score={det?.score}");
//         if (det == null || det.score < detectMinScore)
//         {
//             Destroy(tex);
//             yield return FailRoutine($"B7 FAIL: Không tìm thấy côn trùng (score={det?.score ?? 0})");
//             yield break;
//         }

//         // --- CROP ---
//         SetStatus("B8: Crop ảnh...");
//         Texture2D crop = CropByBBox(tex, det);
//         Destroy(tex);

//         // --- CLASSIFY ---
//         SetStatus("B9: MobileNet Classify...");
//         ClassifyResult cls = null;
//         try
//         {
//             cls = RunClassify_TFLite(crop);
//         }
//         catch (System.Exception ex)
//         {
//             errorMsg = $"B9 FAIL: Lỗi MobileNet: {ex.Message}";
//         }

//         if (errorMsg != null)
//         {
//             Destroy(crop);
//             yield return FailRoutine(errorMsg);
//             yield break;
//         }

//         if (cls == null || cls.score < classiMinScore || string.IsNullOrEmpty(cls.label))
//         {
//             Destroy(crop);
//             yield return FailRoutine($"B9 FAIL: Không nhận diện được loài (label={cls?.label})");
//             yield break;
//         }

//         // --- SAVE ---
//         SetStatus("B10: Lưu kết quả + chuyển Result...");
//         string savedPath = SaveToCollection(crop, cls.label);
//         Destroy(crop);

//         // ✅ đổ dữ liệu sang PredictStore để ResultScene load JSON/Clip/Audio/3D
//         PredictStore.predictedLabel = cls.label;
//         PredictStore.predictedScore = cls.score;
//         PredictStore.savedImagePath = savedPath;
//         PredictStore.errorMessage = null; // clear lỗi thành công

//         Debug.Log($"[Predict DONE] label={cls.label}, score={cls.score}, saved={savedPath}");

//         yield return FadeBGM(bgmNormalVolume);
//         SceneManager.LoadScene(resultSceneName);
//     }

//     // ==========================================================
//     // 3. YOLO DETECT (giữ đúng như bạn)
//     public class DetectResult { public Rect bbox; public float score; }

//     DetectResult RunDetect_TFLite(Texture2D tex)
//     {
//         if (detectInterpreter == null) return null;

//         TextureToFloatArrayNonAlloc(tex, detectInputW, detectInputH, detectInputBuffer, true);
//         detectInterpreter.SetInputTensorData(0, detectInputBuffer);
//         detectInterpreter.Invoke();

//         var outInfo = detectInterpreter.GetOutputTensorInfo(0);
//         int outSize = 1; foreach (int d in outInfo.shape) outSize *= d;

//         float[] output = new float[outSize];
//         detectInterpreter.GetOutputTensorData(0, output);

//         return ParseYOLO_Output(output, outInfo.shape);
//     }

//     // ✅ giữ nguyên Parse YOLO của bạn để đảm bảo accuracy
//     DetectResult ParseYOLO_Output(float[] output, int[] shape)
//     {
//         if (shape.Length == 3 && shape[1] == 5)
//         {
//             int boxes = shape[2]; int stride = boxes;
//             int best = -1; float bestConf = 0f;
//             for (int i = 0; i < boxes; i++)
//             {
//                 float conf = output[4 * stride + i];
//                 if (conf > bestConf) { bestConf = conf; best = i; }
//             }
//             if (best < 0) return null;
//             float cx = output[0 * stride + best]; float cy = output[1 * stride + best];
//             float w = output[2 * stride + best]; float h = output[3 * stride + best];
//             return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestConf };
//         }

//         if (shape.Length == 3 && shape[2] == 5)
//         {
//             int boxes = shape[1]; int stride = shape[2];
//             int best = -1; float bestConf = 0f;
//             for (int i = 0; i < boxes; i++)
//             {
//                 float conf = output[i * stride + 4];
//                 if (conf > bestConf) { bestConf = conf; best = i; }
//             }
//             if (best < 0) return null;
//             float cx = output[best * stride + 0]; float cy = output[best * stride + 1];
//             float w = output[best * stride + 2]; float h = output[best * stride + 3];
//             return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestConf };
//         }

//         if (shape.Length == 3 && shape[1] > 10)
//         {
//             int N = shape[1]; int stride = shape[2];
//             int best = -1; float bestScore = 0f;
//             for (int i = 0; i < N; i++)
//             {
//                 float conf = output[i * stride + 4];
//                 if (conf > bestScore) { bestScore = conf; best = i; }
//             }
//             if (best < 0) return null;
//             float cx = output[best * stride + 0]; float cy = output[best * stride + 1];
//             float w = output[best * stride + 2]; float h = output[best * stride + 3];
//             return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestScore };
//         }

//         return null;
//     }

//     // ==========================================================
//     // 4. CLASSIFY
//     public class ClassifyResult { public string label; public float score; public int classId; }

//     ClassifyResult RunClassify_TFLite(Texture2D tex)
//     {
//         if (classifyInterpreter == null) return null;

//         TextureToFloatArrayNonAlloc(tex, classiInputW, classiInputH, classiInputBuffer, false);
//         classifyInterpreter.SetInputTensorData(0, classiInputBuffer);
//         classifyInterpreter.Invoke();

//         var outInfo = classifyInterpreter.GetOutputTensorInfo(0);
//         int outSize = 1; foreach (int d in outInfo.shape) outSize *= d;

//         float[] prob = new float[outSize];
//         classifyInterpreter.GetOutputTensorData(0, prob);

//         int best = 0; float bestScore = prob[0];
//         for (int i = 1; i < prob.Length; i++)
//         {
//             if (prob[i] > bestScore) { bestScore = prob[i]; best = i; }
//         }

//         return new ClassifyResult { classId = best, label = IndexToLabel(best), score = bestScore };
//     }

//     // ==========================================================
//     // 5. IMAGE HELPERS
//     void TextureToFloatArrayNonAlloc(Texture2D tex, int w, int h, float[] buffer, bool normalize0to1)
//     {
//         Texture2D resized = ResizeTexture(tex, w, h);
//         Color32[] pixels = resized.GetPixels32();
//         int idx = 0;

//         for (int i = 0; i < pixels.Length; i++)
//         {
//             var c = pixels[i];
//             if (normalize0to1)
//             {
//                 buffer[idx++] = c.r / 255f;
//                 buffer[idx++] = c.g / 255f;
//                 buffer[idx++] = c.b / 255f;
//             }
//             else
//             {
//                 buffer[idx++] = c.r;
//                 buffer[idx++] = c.g;
//                 buffer[idx++] = c.b;
//             }
//         }
//         Destroy(resized);
//     }

//     Texture2D ResizeTexture(Texture2D src, int w, int h)
//     {
//         RenderTexture rt = RenderTexture.GetTemporary(w, h);
//         Graphics.Blit(src, rt);
//         RenderTexture prev = RenderTexture.active; RenderTexture.active = rt;
//         Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
//         tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
//         tex.Apply();
//         RenderTexture.active = prev;
//         RenderTexture.ReleaseTemporary(rt);
//         return tex;
//     }

//     Texture2D CropByBBox(Texture2D src, DetectResult det)
//     {
//         Rect b = det.bbox;
//         float unityY = 1.0f - (b.y + b.height);

//         int x = Mathf.Clamp(Mathf.RoundToInt(b.x * src.width), 0, src.width - 1);
//         int y = Mathf.Clamp(Mathf.RoundToInt(unityY * src.height), 0, src.height - 1);

//         int w = Mathf.Clamp(Mathf.RoundToInt(b.width * src.width), 1, src.width - x);
//         int h = Mathf.Clamp(Mathf.RoundToInt(b.height * src.height), 1, src.height - y);

//         int pad = Mathf.RoundToInt(0.1f * Mathf.Max(w, h));
//         x = Mathf.Clamp(x - pad, 0, src.width - 1);
//         y = Mathf.Clamp(y - pad, 0, src.height - 1);
//         w = Mathf.Clamp(w + pad * 2, 1, src.width - x);
//         h = Mathf.Clamp(h + pad * 2, 1, src.height - y);

//         Color[] pixels = src.GetPixels(x, y, w, h);
//         Texture2D cropped = new Texture2D(w, h);
//         cropped.SetPixels(pixels);
//         cropped.Apply();
//         return cropped;
//     }

//     string SaveToCollection(Texture2D tex, string label)
//     {
//         string folder = Path.Combine(Application.persistentDataPath, "Collection", label);
//         if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
//         string path = Path.Combine(folder, System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");
//         File.WriteAllBytes(path, tex.EncodeToJPG(90));
//         return path;
//     }

//     IEnumerator FailRoutine(string msg)
//     {
//         SetStatus(msg);
//         PredictStore.errorMessage = msg;

//         if (voiceSource && failClip)
//         {
//             yield return FadeBGM(bgmDuckVolume);
//             voiceSource.PlayOneShot(failClip);
//         }

//         yield return new WaitForSeconds(1.5f);
//         yield return FadeBGM(bgmNormalVolume);
//         SceneManager.LoadScene(scanSceneName);
//     }

//     IEnumerator FadeBGM(float target)
//     {
//         if (!bgmSource) yield break;
//         while (Mathf.Abs(bgmSource.volume - target) > 0.01f)
//         {
//             bgmSource.volume = Mathf.Lerp(bgmSource.volume, target, Time.deltaTime * fadeSpeed);
//             yield return null;
//         }
//         bgmSource.volume = target;
//     }

//     string IndexToLabel(int idx)
//     {
//         if (idx < 0 || idx >= LABELS.Length) return "unknown";
//         return LABELS[idx];
//     }

//     void SetStatus(string s)
//     {
//         Debug.Log("[Predict] " + s);
//         if (statusText != null) statusText.text = s;
//     }
// }


using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TensorFlowLite;
using TMPro;

public class PredictControllerTFLiteOptimized : MonoBehaviour
{
    #region --- CẤU HÌNH (INSPECTOR) ---
    [Header("1. Cài đặt Scene")]
    public string scanSceneName = "ScanScene";
    public string resultSceneName = "ResultScene";

    [Header("2. Giao diện (UI)")]
    public TMP_Text statusText; 

    [Header("3. Models (Kéo file .tflite vào đây)")]
    public Object detectTfLiteAsset;    // YOLO.tflite
    public Object classifyTfLiteAsset;  // MobileNet.tflite

    [Header("4. Fallback (StreamingAssets)")]
    public string detectFallbackName = "YOLO.tflite";
    public string classifyFallbackName = "MobileNet.tflite";

    [Header("5. Input Size")]
    public int detectInputW = 640; 
    public int detectInputH = 640;
    public int classiInputW = 224; 
    public int classiInputH = 224;

    [Header("6. Ngưỡng (Threshold)")]
    [Range(0f, 1f)] public float detectMinScore = 0.3f; 
    [Range(0f, 1f)] public float classiMinScore = 0.5f; 

    [Header("7. Âm thanh")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmNormalVolume = 0.5f;
    [Range(0f, 1f)] public float bgmDuckVolume = 0.2f; 

    public AudioSource voiceSource;
    public AudioClip predictingClip; 
    public AudioClip failClip;       
    public AudioClip successClip;    
    #endregion

    // --- BIẾN NỘI BỘ ---
    private Interpreter detectInterpreter;
    private Interpreter classifyInterpreter;
    private float[] detectInputBuffer;
    private float[] classiInputBuffer;
    private InterpreterOptions options;

    static readonly string[] LABELS = {
        "ant", "butterfly", "cockroach", "dragonfly", "fly",
        "grasshopper", "honeybee", "ladybug", "mosquito", "silkworm", "spider"
    };

    void Start()
    {
        SetStatus("B0: Hệ thống khởi động...");
        options = new InterpreterOptions { threads = 2 };

        if (bgmSource && bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = bgmNormalVolume;
            if (!bgmSource.isPlaying) bgmSource.Play();
        }

        StartCoroutine(MainProcess());
    }

    void OnDestroy()
    {
        detectInterpreter?.Dispose();
        classifyInterpreter?.Dispose();
        options?.Dispose();
    }

    // ==========================================================
    // LUỒNG XỬ LÝ CHÍNH (MAIN PROCESS)
    // ==========================================================
    IEnumerator MainProcess()
    {
        // --- GIAI ĐOẠN 1: TẢI MODEL ---
        string runtimeError = null; // Biến tạm để hứng lỗi

        // 1. Load YOLO
        SetStatus("Bạn chờ xíu nhé!");
        byte[] detBytes = null;
        yield return LoadModelBytesCoroutine(detectTfLiteAsset, detectFallbackName, b => detBytes = b);
        if (detBytes == null) { yield return FailRoutine("Lỗi B1: Không tìm thấy file YOLO"); yield break; }
        
        // 2. Load MobileNet
        SetStatus("Bạn chờ xíu nhé!");
        byte[] clsBytes = null;
        yield return LoadModelBytesCoroutine(classifyTfLiteAsset, classifyFallbackName, b => clsBytes = b);
        if (clsBytes == null) { yield return FailRoutine("Lỗi B2: Không tìm thấy file MobileNet"); yield break; }

        yield return Resources.UnloadUnusedAssets();

        // 3. Khởi tạo YOLO Interpreter
        SetStatus("Bạn chờ xíu nhé!...");
        yield return null;
        try {
            detectInterpreter = new Interpreter(detBytes, options);
            detectInterpreter.AllocateTensors();
        } catch (System.Exception e) {
            runtimeError = $"Lỗi B3 (YOLO RAM): {e.Message}";
        }
        if (runtimeError != null) { yield return FailRoutine(runtimeError); yield break; }

        // 4. Khởi tạo MobileNet Interpreter
        SetStatus("Bạn chờ xíu nhé!...");
        yield return null;
        try {
            classifyInterpreter = new Interpreter(clsBytes, options);
            classifyInterpreter.AllocateTensors();
        } catch (System.Exception e) {
            runtimeError = $"Lỗi B4 (Classify RAM): {e.Message}";
        }
        if (runtimeError != null) { yield return FailRoutine(runtimeError); yield break; }

        // 5. Tạo bộ đệm dữ liệu (Buffer)
        SetStatus("Bạn chờ xíu nhé!...");
        try {
            detectInputBuffer = new float[detectInputW * detectInputH * 3];
            classiInputBuffer = new float[classiInputW * classiInputH * 3];
        } catch (System.Exception e) {
            runtimeError = $"Lỗi B5 (Buffer): {e.Message}";
        }
        if (runtimeError != null) { yield return FailRoutine(runtimeError); yield break; }

        // --- GIAI ĐOẠN 2: DỰ ĐOÁN (PREDICT) ---
        SetStatus("Bạn chờ xíu nhé!...");
        
        if (voiceSource && predictingClip)
        {
            yield return FadeBGM(bgmDuckVolume);
            voiceSource.PlayOneShot(predictingClip);
        }

        string path = PredictStore.capturedImagePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            yield return FailRoutine("Lỗi B6: Không tìm thấy ảnh chụp!");
            yield break;
        }

        // --- FIX LỖI CS1631 TẠI ĐÂY ---
        Texture2D fullTex = new Texture2D(2, 2);
        bool imageLoadSuccess = false;
        try {
            fullTex.LoadImage(File.ReadAllBytes(path)); 
            imageLoadSuccess = true;
        } catch {
            // Không làm gì ở đây, chỉ đánh dấu là lỗi
            imageLoadSuccess = false;
        }

        if (!imageLoadSuccess) {
            Destroy(fullTex);
            yield return FailRoutine("Lỗi B6: File ảnh bị hỏng."); 
            yield break; 
        }

        // 7. Chạy YOLO
        SetStatus("Bạn chờ xíu nhé!...");
        DetectResult det = null;
        runtimeError = null;
        try {
            det = RunDetect_TFLite(fullTex);
        } catch (System.Exception e) {
            runtimeError = $"Lỗi B7 (YOLO): {e.Message}";
        }

        if (runtimeError != null) { 
            Destroy(fullTex); 
            yield return FailRoutine(runtimeError); 
            yield break; 
        }

        if (det == null || det.score < detectMinScore)
        {
            Destroy(fullTex);
            yield return FailRoutine("Không tìm thấy côn trùng nào trong ảnh.");
            yield break;
        }

        // 8. Cắt ảnh (Crop)
        SetStatus("Bạn chờ xíu nhé!...");
        Texture2D cropTex = CropByBBox(fullTex, det);
        Destroy(fullTex); 

        // 9. Chạy MobileNet
        SetStatus("Bạn chờ xíu nhé!...");
        ClassifyResult cls = null;
        runtimeError = null;
        try {
            cls = RunClassify_TFLite(cropTex);
        } catch (System.Exception e) {
            runtimeError = $"Lỗi B9 (Net): {e.Message}";
        }

        if (runtimeError != null) {
            Destroy(cropTex);
            yield return FailRoutine(runtimeError);
            yield break;
        }

        if (cls == null || cls.score < classiMinScore || string.IsNullOrEmpty(cls.label))
        {
            Destroy(cropTex);
            yield return FailRoutine("Ảnh mờ quá, không nhận ra con gì.");
            yield break;
        }

        // --- GIAI ĐOẠN 3: KẾT QUẢ & CHUYỂN CẢNH ---
        
        // 10. HIỂN THỊ KẾT QUẢ
        string animalName = cls.label.ToUpper();
        string confidence = (cls.score * 100).ToString("F0") + "%";
        
        SetStatus($"TÌM THẤY: {animalName}\nĐộ chính xác: {confidence}");
        Debug.Log($"---> KẾT QUẢ: {animalName} | {confidence}");

        if (successClip && voiceSource) voiceSource.PlayOneShot(successClip);

        string savedPath = SaveToCollection(cropTex, cls.label);
        Destroy(cropTex); 

        // Đổ dữ liệu
        PredictStore.predictedLabel = cls.label;
        PredictStore.predictedScore = cls.score;
        PredictStore.savedImagePath = savedPath;
        PredictStore.errorMessage = null;

        // === ĐỢI 2 GIÂY ĐỂ NGƯỜI DÙNG ĐỌC ===
        yield return new WaitForSeconds(2.0f);

        SetStatus("Đang mở thông tin chi tiết...");
        yield return FadeBGM(bgmNormalVolume);

        if (resultSceneName == scanSceneName)
        {
            Debug.LogError("LỖI CÀI ĐẶT: Tên Scene Result trùng với Scan!");
        }
        else
        {
            SceneManager.LoadScene(resultSceneName);
        }
    }

    // ==========================================================
    // LOGIC YOLO & MOBILENET
    // ==========================================================
    public class DetectResult { public Rect bbox; public float score; }
    public class ClassifyResult { public string label; public float score; }

    DetectResult RunDetect_TFLite(Texture2D tex)
    {
        if (detectInterpreter == null) return null;
        TextureToFloatArrayNonAlloc(tex, detectInputW, detectInputH, detectInputBuffer, true);
        detectInterpreter.SetInputTensorData(0, detectInputBuffer);
        detectInterpreter.Invoke();

        var outInfo = detectInterpreter.GetOutputTensorInfo(0);
        int outSize = 1; foreach (int d in outInfo.shape) outSize *= d;
        float[] output = new float[outSize];
        detectInterpreter.GetOutputTensorData(0, output);

        return ParseYOLO_Output(output, outInfo.shape);
    }

    DetectResult ParseYOLO_Output(float[] output, int[] shape)
    {
        if (shape.Length == 3 && shape[1] == 5)
        {
            int boxes = shape[2]; int stride = boxes;
            int best = -1; float bestConf = 0f;
            for (int i = 0; i < boxes; i++)
            {
                float conf = output[4 * stride + i];
                if (conf > bestConf) { bestConf = conf; best = i; }
            }
            if (best < 0) return null;
            float cx = output[0 * stride + best]; float cy = output[1 * stride + best];
            float w = output[2 * stride + best]; float h = output[3 * stride + best];
            return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestConf };
        }
        if (shape.Length == 3 && shape[2] == 5)
        {
            int boxes = shape[1]; int stride = shape[2];
            int best = -1; float bestConf = 0f;
            for (int i = 0; i < boxes; i++)
            {
                float conf = output[i * stride + 4];
                if (conf > bestConf) { bestConf = conf; best = i; }
            }
            if (best < 0) return null;
            float cx = output[best * stride + 0]; float cy = output[best * stride + 1];
            float w = output[best * stride + 2]; float h = output[best * stride + 3];
            return new DetectResult { bbox = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), score = bestConf };
        }
        return null;
    }

    ClassifyResult RunClassify_TFLite(Texture2D tex)
    {
        if (classifyInterpreter == null) return null;
        TextureToFloatArrayNonAlloc(tex, classiInputW, classiInputH, classiInputBuffer, false);
        classifyInterpreter.SetInputTensorData(0, classiInputBuffer);
        classifyInterpreter.Invoke();

        var outInfo = classifyInterpreter.GetOutputTensorInfo(0);
        int outSize = 1; foreach (int d in outInfo.shape) outSize *= d;
        float[] prob = new float[outSize];
        classifyInterpreter.GetOutputTensorData(0, prob);

        int best = 0; float bestScore = prob[0];
        for (int i = 1; i < prob.Length; i++)
        {
            if (prob[i] > bestScore) { bestScore = prob[i]; best = i; }
        }
        return new ClassifyResult { label = IndexToLabel(best), score = bestScore };
    }

    // ==========================================================
    // UTILS
    // ==========================================================
    void TextureToFloatArrayNonAlloc(Texture2D tex, int w, int h, float[] buffer, bool normalize0to1)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h);
        Graphics.Blit(tex, rt);
        RenderTexture prev = RenderTexture.active; 
        RenderTexture.active = rt;
        
        Texture2D resized = new Texture2D(w, h, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        resized.Apply();
        
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = resized.GetPixels32();
        Destroy(resized);

        int idx = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            if (normalize0to1)
            {
                buffer[idx++] = c.r / 255f;
                buffer[idx++] = c.g / 255f;
                buffer[idx++] = c.b / 255f;
            }
            else
            {
                buffer[idx++] = c.r;
                buffer[idx++] = c.g;
                buffer[idx++] = c.b;
            }
        }
    }

    Texture2D CropByBBox(Texture2D src, DetectResult det)
    {
        Rect b = det.bbox;
        float unityY = 1.0f - (b.y + b.height);
        int x = Mathf.Clamp(Mathf.RoundToInt(b.x * src.width), 0, src.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(unityY * src.height), 0, src.height - 1);
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
        string folder = Path.Combine(Application.persistentDataPath, "Collection", label);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string filename = System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
        string path = Path.Combine(folder, filename);
        File.WriteAllBytes(path, tex.EncodeToJPG(90));
        return path;
    }

    IEnumerator FailRoutine(string msg)
    {
        SetStatus(msg);
        Debug.LogError(msg);
        PredictStore.errorMessage = msg;

        if (voiceSource && failClip)
        {
            yield return FadeBGM(bgmDuckVolume);
            voiceSource.PlayOneShot(failClip);
        }

        yield return new WaitForSeconds(2.0f);
        yield return FadeBGM(bgmNormalVolume);
        SceneManager.LoadScene(scanSceneName);
    }

    IEnumerator LoadModelBytesCoroutine(Object asset, string fallbackName, System.Action<byte[]> onDone)
    {
        #if UNITY_EDITOR
        if (asset != null && asset is TextAsset ta) {
            onDone?.Invoke(ta.bytes);
            yield break;
        }
        #endif

        string path = Path.Combine(Application.streamingAssetsPath, "Models", fallbackName);
        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    onDone?.Invoke(req.downloadHandler.data);
                else
                    onDone?.Invoke(null);
            }
        }
        else
        {
            if (File.Exists(path)) onDone?.Invoke(File.ReadAllBytes(path));
            else onDone?.Invoke(null);
        }
    }

    IEnumerator FadeBGM(float target)
    {
        if (!bgmSource) yield break;
        float start = bgmSource.volume;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            bgmSource.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        bgmSource.volume = target;
    }

    string IndexToLabel(int idx)
    {
        if (idx < 0 || idx >= LABELS.Length) return "unknown";
        return LABELS[idx];
    }

    void SetStatus(string s)
    {
        if (statusText != null) statusText.text = s;
    }
}