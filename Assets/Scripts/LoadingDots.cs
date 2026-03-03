using UnityEngine;
using TMPro;

public class LoadingDots : MonoBehaviour
{
    public TMP_Text loadingText;
    public float speed = 0.5f; // tốc độ đổi chấm

    private float timer;
    private int dotCount;

    void Reset()
    {
        loadingText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (loadingText == null) return;

        timer += Time.deltaTime;
        if (timer >= speed)
        {
            timer = 0f;
            dotCount = (dotCount + 1) % 4; // 0..3 chấm
            loadingText.text = "Đang dự đoán" + new string('.', dotCount);
        }
    }
}
