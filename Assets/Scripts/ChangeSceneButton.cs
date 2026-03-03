using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneButton : MonoBehaviour
{
    public string sceneName;

    // Gọi trực tiếp từ Button
    public void ChangeScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Chưa nhập tên scene!");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
