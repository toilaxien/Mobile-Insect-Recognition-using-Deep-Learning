using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    // Hàm gọi khi bấm nút "Chơi ngay"
    public void OnPlayNow()
    {
        SceneManager.LoadScene("ScanScene");
    }
    public void OnCollection()
    {
        SceneManager.LoadScene("ColectionScene");
    }
}
