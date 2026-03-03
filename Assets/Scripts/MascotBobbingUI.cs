using UnityEngine;

public class MascotBobbingUI : MonoBehaviour
{
    [Header("Bobbing Settings")]
    public float amplitude = 20f;   // biên độ nhún (px)
    public float speed = 2f;        // tốc độ nhún (chu kỳ/giây)

    private RectTransform rt;
    private Vector2 startPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    void OnEnable()
    {
        // mỗi lần bật lại loading thì reset vị trí gốc
        startPos = rt.anchoredPosition;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) * amplitude;
        rt.anchoredPosition = startPos + new Vector2(0f, yOffset);
    }
}
