using UnityEngine;
using TMPro;

public class KidTextEffect : MonoBehaviour
{
    public TMP_Text targetText;

    [Header("Scale Pulse")]
    public float scaleUp = 1.25f;
    public float scaleSpeed = 6f;

    [Header("Glow/Color Pulse")]
    public Color normalColor = Color.black;
    public Color glowColor = new Color(1f, 0.95f, 0.2f);
    public float colorSpeed = 6f;

    Vector3 originalScale;
    bool isHighlighting = false;
    float t;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        originalScale = targetText.rectTransform.localScale;
        targetText.color = normalColor;
    }

    void Update()
    {
        if (!isHighlighting) return;

        t += Time.deltaTime;

        float pulse = Mathf.Sin(t * scaleSpeed) * 0.5f + 0.5f;
        targetText.rectTransform.localScale =
            Vector3.Lerp(originalScale, originalScale * scaleUp, pulse);

        float cPulse = Mathf.Sin(t * colorSpeed) * 0.5f + 0.5f;
        targetText.color = Color.Lerp(normalColor, glowColor, cPulse);
    }

    public void StartHighlight()
    {
        isHighlighting = true;
        t = 0f;
    }

    public void StopHighlight()
    {
        isHighlighting = false;
        targetText.rectTransform.localScale = originalScale;
        targetText.color = normalColor;
    }
}
