using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class ZeroDelayVideoUI : MonoBehaviour
{
    public RawImage raw;          // kéo RawImage vào
    public bool loop = true;

    VideoPlayer vp;
    bool warmedUp = false;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = loop;
        vp.waitForFirstFrame = false;
        vp.skipOnDrop = true;          // giảm giật khi decode chậm

        raw.enabled = false;           // ẩn lúc preload
        StartCoroutine(PreloadAndWarmup());
    }

    IEnumerator PreloadAndWarmup()
    {
        vp.Prepare();
        while (!vp.isPrepared) yield return null;

        raw.texture = vp.texture;

        // warm-up: play 0.05–0.1s để Unity cache decoder
        vp.Play();
        yield return new WaitForSeconds(0.08f);
        vp.Pause();

        warmedUp = true;
        // vẫn ẩn RawImage cho đến khi bạn gọi ShowAndPlay()
    }

    // Gọi hàm này khi bạn muốn hiện video
    public void ShowAndPlay()
    {
        if (!warmedUp) return;
        raw.enabled = true;
        vp.Play();
    }

    public void HideAndPause()
    {
        raw.enabled = false;
        vp.Pause();
    }
}
