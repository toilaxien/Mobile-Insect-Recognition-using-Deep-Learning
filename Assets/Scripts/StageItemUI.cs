// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class StageItemUI : MonoBehaviour
// {
//     [Header("UI refs")]
//     public TMP_Text titleText;      // "Giai đoạn 1: Trứng"
//     public TMP_Text durationText;   // "3–7 ngày"
//     public TMP_Text bodyText;       // gộp: Môi trường + Đặc điểm + Mô tả
//     public Button view3DButton;

//     private StageData data;

//     public void Bind(StageData d, System.Action<StageData> onClick3D)
//     {
//         data = d;

//         if (titleText)    titleText.text = d.title;
//         if (durationText) durationText.text = d.duration;

//         if (bodyText)
//         {
//             // GỘP chung 1 text, mỗi phần xuống dòng
//             // Bạn có thể đổi format tùy ý, miễn là 1 text.
//             bodyText.text =
//                 $"Môi trường: {d.environment}\n" +
//                 $"Đặc điểm: {d.features}\n" +
//                 $"Mô tả: {d.description}";
//         }

//         if (view3DButton)
//         {
//             view3DButton.onClick.RemoveAllListeners();
//             view3DButton.onClick.AddListener(() => onClick3D?.Invoke(data));
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StageItemUI : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_Text titleText;
    public TMP_Text durationText;
    public TMP_Text bodyText;
    public Button view3DButton;

    [Header("Scene")]
    public string scene3DName = "ARScene";   // nhớ đúng tên scene 3D của bạn

    private StageData data;

    public void Bind(StageData d, System.Action<StageData> onClick3D = null)
    {
        data = d;

        if (titleText) titleText.text = d.title;
        if (durationText) durationText.text = d.duration;

        if (bodyText)
        {
            bodyText.text =
                $"Môi trường: {d.environment}\n" +
                $"Đặc điểm: {d.features}\n" +
                $"Mô tả: {d.description}";
        }

        if (view3DButton)
        {
            view3DButton.onClick.RemoveAllListeners();

            // ƯU TIÊN callback nếu có (để ResultController điều khiển)
            if (onClick3D != null)
            {
                view3DButton.onClick.AddListener(() => onClick3D.Invoke(data));
            }
            else
            {
                // Nếu không truyền callback, StageItemUI tự chuyển scene
                view3DButton.onClick.AddListener(GoTo3DScene);
            }
        }
    }

    void GoTo3DScene()
    {
        if (data == null || string.IsNullOrEmpty(data.modelKey))
        {
            Debug.LogWarning("Không có modelKey => không thể xem 3D");
            return;
        }

        // Lưu key để scene 3D load
        PredictStore.savedImagePath = data.modelKey;

        // Chuyển scene
        SceneManager.LoadScene(scene3DName);
    }
}
