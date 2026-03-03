using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(AspectRatioFitter))]
public class AutoRawBGFit : MonoBehaviour
{
    public bool cover = true; // true = phủ kín (crop nhẹ), false = vừa khung (có thể hở viền)

    void Awake()
    {
        var raw = GetComponent<RawImage>();
        var fitter = GetComponent<AspectRatioFitter>();
        if (raw.texture == null) return;

        float w = raw.texture.width;
        float h = raw.texture.height;
        if (h <= 0) return;

        fitter.aspectRatio = w / h;
        fitter.aspectMode = cover
            ? AspectRatioFitter.AspectMode.EnvelopeParent
            : AspectRatioFitter.AspectMode.FitInParent;
    }
}
