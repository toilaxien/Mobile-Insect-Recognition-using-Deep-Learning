using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource bgmSource;
    public AudioSource voiceSource;

    public AudioClip helloClip;
    public AudioClip guideClip;

    public float bgmNormalVolume = 0.5f;
    public float bgmDuckVolume = 0.2f;  // chỉ nhỏ lại thôi
    public float fadeSpeed = 4f;

    [Header("Intro Delay")]
    public float introDelay = 2.5f;     // nhạc nền dạo trước bao nhiêu giây

    void Awake()
    {
        var sources = GetComponents<AudioSource>();
        if (bgmSource == null) bgmSource = sources[0];
        if (voiceSource == null) voiceSource = sources[1];

        bgmSource.volume = bgmNormalVolume;

        // Đảm bảo nhạc nền chạy
        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    void Start()
    {
        StartCoroutine(PlayIntroRoutine());
    }

    IEnumerator PlayIntroRoutine()
    {
        // 0) Cho nhạc nền dạo trước
        yield return new WaitForSeconds(introDelay);

        // 1) phát chào
        yield return DuckAndPlay(helloClip);

        // nghỉ nhẹ cho tự nhiên
        yield return new WaitForSeconds(0.2f);

        // 2) phát hướng dẫn
        yield return DuckAndPlay(guideClip);
    }

    IEnumerator DuckAndPlay(AudioClip clip)
    {
        if (clip == null) yield break;

        // nhỏ nhạc nền lại (không tắt)
        yield return FadeBGM(bgmDuckVolume);

        voiceSource.clip = clip;
        voiceSource.loop = false;     // chắc chắn không lặp
        voiceSource.Play();
        yield return new WaitWhile(() => voiceSource.isPlaying);

        // tăng nhạc nền lên lại
        yield return FadeBGM(bgmNormalVolume);
    }

    IEnumerator FadeBGM(float target)
    {
        while (Mathf.Abs(bgmSource.volume - target) > 0.01f)
        {
            bgmSource.volume = Mathf.Lerp(bgmSource.volume, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        bgmSource.volume = target;
    }
}
