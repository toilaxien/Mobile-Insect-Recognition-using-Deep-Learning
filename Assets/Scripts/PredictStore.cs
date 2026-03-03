using UnityEngine;

public static class PredictStore
{
    public static string capturedImagePath;   // đường dẫn ảnh đã chụp (jpg/png)
    public static string predictedLabel;
    public static float predictedScore;
    public static string errorMessage;
    public static string savedImagePath; 

    public static void Clear()
    {
        capturedImagePath = null;
        predictedLabel = null;
        predictedScore = 0f;
        errorMessage = null;
    }
}
