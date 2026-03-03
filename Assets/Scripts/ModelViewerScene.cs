// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class ModelViewerScene : MonoBehaviour
// {
//     [Header("Spawn")]
//     public Transform spawnPoint;      // optional
//     public float uniformScale = 1f;

//     [Header("Back")]
//     public string backSceneName = "ResultScene"; // đổi đúng tên scene bạn muốn quay lại

//     GameObject current;

//     void Start()
//     {
//         string key = PredictStore.savedImagePath; // bạn đã set = st.modelKey bên ResultController

//         if (string.IsNullOrEmpty(key))
//         {
//             Debug.LogError("Model key rỗng (PredictStore.savedImagePath) => không load được!");
//             return;
//         }

//         // Load prefab từ Resources
//         GameObject prefab = Resources.Load<GameObject>(key);
//         if (prefab == null)
//         {
//             Debug.LogError($"Không tìm thấy prefab theo key: {key}. " +
//                            $"Hãy chắc chắn file nằm trong Assets/Resources/... và key đúng.");
//             return;
//         }

//         Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
//         Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

//         current = Instantiate(prefab, pos, rot);
//         current.transform.localScale *= uniformScale;

//         EnsureCollider(current);

//         // gắn xoay chạm
//         if (current.GetComponent<TouchRotate>() == null)
//             current.AddComponent<TouchRotate>();
//     }

//     void Update()
//     {
//         // Android: nút back vật lý
//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             SceneManager.LoadScene(backSceneName);
//         }
//     }

//     void EnsureCollider(GameObject go)
//     {
//         if (go.GetComponentInChildren<Collider>() != null) return;

//         var renderers = go.GetComponentsInChildren<Renderer>();
//         if (renderers.Length == 0) return;

//         Bounds b = renderers[0].bounds;
//         for (int i = 1; i < renderers.Length; i++)
//             b.Encapsulate(renderers[i].bounds);

//         var bc = go.AddComponent<BoxCollider>();
//         bc.center = go.transform.InverseTransformPoint(b.center);
//         bc.size = b.size;
//     }
// }


// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI;

// public class ModelViewerScene : MonoBehaviour
// {
//     [Header("TEST - Fixed model key in Resources (no extension)")]
//     public string fixedModelKey = "Species/ladybug/models/stage1/Imported";

//     [Header("Spawn")]
//     public Transform spawnPoint;
//     public float uniformScale = 1f;

//     [Header("Back")]
//     public string backSceneName = "ResultScene";

//     [Header("UI")]
//     public Button btnExit;

//     GameObject current;

//     void Start()
//     {
//         // Exit button
//         if (btnExit != null)
//         {
//             btnExit.onClick.RemoveAllListeners();
//             btnExit.onClick.AddListener(ExitToBackScene);
//         }

//         // ===== TEST KEY FIXED =====
//         string key = fixedModelKey;
//         Debug.Log("[ModelViewerScene] Fixed key = " + key);

//         if (string.IsNullOrEmpty(key))
//         {
//             Debug.LogError("Fixed model key rỗng => không load được!");
//             return;
//         }

//         // Load model root GameObject from Resources
//         GameObject prefab = Resources.Load<GameObject>(key);
//         if (prefab == null)
//         {
//             Debug.LogError(
//                 $"Không tìm thấy GameObject trong Resources theo key: {key}\n" +
//                 $"Hãy kiểm tra asset tên đúng là 'Imported' và nằm trong Assets/Resources/{key}.<ext>\n" +
//                 $"Tip: thử rename asset 'Imported (1)' -> 'Imported' hoặc 'Model' để tránh sai tên."
//             );
//             return;
//         }

//         Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
//         Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

//         current = Instantiate(prefab, pos, rot);
//         current.transform.localScale *= uniformScale;

//         EnsureCollider(current);

//         if (current.GetComponent<TouchRotate>() == null)
//             current.AddComponent<TouchRotate>();

//         Debug.Log("[ModelViewerScene] Spawned = " + current.name);
//     }

//     void Update()
//     {
//         // Android back button
//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             ExitToBackScene();
//         }
//     }

//     void ExitToBackScene()
//     {
//         SceneManager.LoadScene(backSceneName);
//     }

//     void EnsureCollider(GameObject go)
//     {
//         if (go.GetComponentInChildren<Collider>() != null) return;

//         var renderers = go.GetComponentsInChildren<Renderer>();
//         if (renderers.Length == 0)
//         {
//             Debug.LogWarning("Model không có Renderer => không auto tạo collider được.");
//             return;
//         }

//         Bounds b = renderers[0].bounds;
//         for (int i = 1; i < renderers.Length; i++)
//             b.Encapsulate(renderers[i].bounds);

//         var bc = go.AddComponent<BoxCollider>();
//         bc.center = go.transform.InverseTransformPoint(b.center);
//         bc.size = b.size;
//     }
// }



using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModelViewerScene : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;
    public float uniformScale = 1f;

    [Header("Back")]
    public string backSceneName = "ResultScene";

    [Header("UI")]
    public Button btnExit;

    GameObject current;

    void Start()
    {
        if (btnExit != null)
        {
            btnExit.onClick.RemoveAllListeners();
            btnExit.onClick.AddListener(ExitToBackScene);
        }

        string key = PredictStore.savedImagePath;

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("Model key rỗng (PredictStore.savedImagePath) => không load được!");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(key);
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy model theo key: {key}. " +
                           $"Key phải trỏ tới 1 asset (vd: Species/ladybug/models/stage1/Model), không phải folder.");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        current = Instantiate(prefab, pos, rot);
        current.transform.localScale *= uniformScale;

        EnsureCollider(current);

        if (current.GetComponent<TouchRotate>() == null)
            current.AddComponent<TouchRotate>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitToBackScene();
        }
    }

    void ExitToBackScene()
    {
        SceneManager.LoadScene(backSceneName);
    }

    void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        var bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(b.center);
        bc.size = b.size;
    }
}
