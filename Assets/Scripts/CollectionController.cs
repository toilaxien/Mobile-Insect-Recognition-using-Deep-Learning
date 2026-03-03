using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CollectionController : MonoBehaviour
{
    [Header("UI")]
    public Transform contentParent;           // kéo Content (Grid) vào đây
    public CollectionItemUI itemPrefab;       // kéo prefab item vào đây

    [Header("Navigation")]
    public string resultSceneName = "ResultScene";

    void Start()
    {
        LoadCollection();
    }

    void LoadCollection()
    {
        // Xoá item cũ
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        string root = Path.Combine(Application.persistentDataPath, "SavedImages");
        if (!Directory.Exists(root))
        {
            Debug.Log("[Collection] Chưa có thư mục SavedImages: " + root);
            return;
        }

        // Mỗi folder con = 1 label
        string[] labelDirs = Directory.GetDirectories(root);

        foreach (var dir in labelDirs)
        {
            string label = Path.GetFileName(dir);

            // Bạn đang lưu cố định: <label>.png
            string imgPath = Path.Combine(dir, $"{label}.png");

            if (!File.Exists(imgPath))
            {
                // nếu trước đó bạn từng lưu jpg thì có thể fallback:
                string jpg = Path.Combine(dir, $"{label}.jpg");
                if (File.Exists(jpg)) imgPath = jpg;
                else continue;
            }

            Sprite sprite = LoadSprite(imgPath);
            if (sprite == null) continue;

            var item = Instantiate(itemPrefab, contentParent);
            item.Bind(label, sprite, OnClickItem);
        }
    }

    Sprite LoadSprite(string filePath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;

            // Tạo sprite
            return Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Collection] LoadSprite error: " + e.Message);
            return null;
        }
    }

    void OnClickItem(string label)
    {
        // Set label để ResultScene load đúng JSON/hiển thị đúng loài
        PredictStore.predictedLabel = label;
        PredictStore.errorMessage = null;

        SceneManager.LoadScene(resultSceneName);
    }
}
