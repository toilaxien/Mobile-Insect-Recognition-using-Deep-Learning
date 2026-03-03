using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUIBST : MonoBehaviour
{
    // Hàm gọi khi bấm nút "Chơi ngay"
    public void OnPlayNow()
    {
        SceneManager.LoadScene("ColectionScene");
    }
}
