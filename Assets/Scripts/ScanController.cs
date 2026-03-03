// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Collections;
// using UnityEngine.SceneManagement;
// using System.IO;

// public class ARCapture : MonoBehaviour
// {
//     public ARCameraManager cameraManager;
//     public string predictLoadingScene = "PredictLoadingScene";

//     public void CaptureAndPredict()
//     {
//         if (cameraManager == null)
//         {
//             Debug.LogError("ARCameraManager chưa được gắn!");
//             return;
//         }

//         if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//         {
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, image.width, image.height),
//                 outputDimensions = new Vector2Int(image.width, image.height),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorY
//             };

//             int size = image.GetConvertedDataSize(conversionParams);
//             var buffer = new NativeArray<byte>(size, Allocator.Temp);
//             image.Convert(conversionParams, buffer);
//             image.Dispose();

//             Texture2D tex = new Texture2D(
//                 conversionParams.outputDimensions.x,
//                 conversionParams.outputDimensions.y,
//                 conversionParams.outputFormat,
//                 false
//             );
//             tex.LoadRawTextureData(buffer);
//             tex.Apply();
//             buffer.Dispose();

//             // Lưu ảnh ra file
//             string path = SaveTextureToFile(tex);
//             PredictStore.capturedImagePath = path;

//             SceneManager.LoadScene(predictLoadingScene);
//         }
//         else
//         {
//             Debug.Log("Không lấy được ảnh từ AR Camera");
//         }
//     }

//     string SaveTextureToFile(Texture2D tex)
//     {
//         string folder = Path.Combine(Application.persistentDataPath, "Captured");
//         if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

//         string fileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
//         string path = Path.Combine(folder, fileName);

//         File.WriteAllBytes(path, tex.EncodeToJPG(90));
//         return path;
//     }
// }
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.IO;

public class ARCapture : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ---")]
    public ARCameraManager cameraManager;
    public string predictLoadingScene = "PredictLoadingScene";

    [Header("--- UI KHUNG NHẮM (Bắt buộc) ---")]
    public RectTransform focusFrame; // Kéo Image FocusFrame (640x640) vào đây

    [Header("--- ĐẦU RA MODEL ---")]
    public int modelInputSize = 640; // Ảnh cuối cùng sẽ resize về cỡ này

    public void CaptureAndPredict()
    {
        if (cameraManager == null) return;
        StartCoroutine(CaptureRoutine());
    }

    System.Collections.IEnumerator CaptureRoutine()
    {
        // 1. Lấy ảnh thô
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) yield break;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();

        Texture2D rawTex = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false
        );
        rawTex.LoadRawTextureData(buffer);
        rawTex.Apply();
        buffer.Dispose();

        // 2. XOAY ẢNH CHO ĐÚNG CHIỀU (Fix Samsung J7)
        if (rawTex.width > rawTex.height)
        {
            Texture2D rotated = RotateTexture90(rawTex);
            Destroy(rawTex);
            rawTex = rotated;
        }

        // 3. CẮT ĐÚNG VỊ TRÍ KHUNG UI (MAGIC STEP)
        Texture2D croppedTex = CropToUIFrameExact(rawTex);
        Destroy(rawTex); 

        // 4. RESIZE VỀ CHUẨN 640x640 (Nếu khung UI to/nhỏ hơn 640)
        Texture2D finalTex = ResizeTexture(croppedTex, modelInputSize, modelInputSize);
        Destroy(croppedTex);

        // 5. LƯU
        string path = SaveTextureToFile(finalTex);
        Destroy(finalTex);

        PredictStore.capturedImagePath = path;
        SceneManager.LoadScene(predictLoadingScene);
    }

    // --- HÀM CẮT ẢNH CHÍNH XÁC THEO UI ---
    Texture2D CropToUIFrameExact(Texture2D bgTexture)
    {
        if (focusFrame == null) return bgTexture;

        // 1. Lấy kích thước màn hình & Texture
        float screenW = Screen.width;
        float screenH = Screen.height;
        float texW = bgTexture.width;
        float texH = bgTexture.height;

        // 2. Tính toán tỉ lệ Zoom của Camera nền (Aspect Fill)
        // Unity AR luôn phóng to Camera để lấp đầy màn hình mà không bị đen viền
        float screenAspect = screenW / screenH;
        float texAspect = texW / texH;

        float scaleFactor; // 1 pixel màn hình = bao nhiêu pixel texture?
        
        if (screenAspect > texAspect) 
        {
            // Màn hình bè hơn Texture (Cắt trên/dưới texture)
            scaleFactor = texW / screenW;
        }
        else 
        {
            // Màn hình dài hơn Texture (Cắt trái/phải texture - Thường gặp ở Mobile)
            scaleFactor = texH / screenH;
        }

        // 3. Tính kích thước cần cắt trên Texture dựa vào kích thước UI
        float cropW = focusFrame.rect.width * scaleFactor;
        float cropH = focusFrame.rect.height * scaleFactor;

        // 4. Tính tọa độ tâm để bắt đầu cắt
        float centerX = texW / 2f;
        float centerY = texH / 2f;

        float startX = centerX - (cropW / 2f);
        float startY = centerY - (cropH / 2f);

        // 5. Cắt an toàn (Clamp)
        int x = Mathf.Clamp(Mathf.RoundToInt(startX), 0, (int)texW);
        int y = Mathf.Clamp(Mathf.RoundToInt(startY), 0, (int)texH);
        int w = Mathf.Clamp(Mathf.RoundToInt(cropW), 1, (int)texW - x);
        int h = Mathf.Clamp(Mathf.RoundToInt(cropH), 1, (int)texH - y);

        // 6. Thực hiện cắt
        Color[] pixels = bgTexture.GetPixels(x, y, w, h);
        Texture2D result = new Texture2D(w, h);
        result.SetPixels(pixels);
        result.Apply();

        return result;
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

    Texture2D RotateTexture90(Texture2D original)
    {
        int w = original.width;
        int h = original.height;
        Texture2D rotated = new Texture2D(h, w);
        Color32[] origPixels = original.GetPixels32();
        Color32[] newPixels = new Color32[origPixels.Length];
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                newPixels[x * h + (h - y - 1)] = origPixels[y * w + x];
            }
        }
        rotated.SetPixels32(newPixels);
        rotated.Apply();
        return rotated;
    }

    string SaveTextureToFile(Texture2D tex)
    {
        string folder = Path.Combine(Application.persistentDataPath, "Captured");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");
        File.WriteAllBytes(path, tex.EncodeToJPG(90));
        return path;
    }
}