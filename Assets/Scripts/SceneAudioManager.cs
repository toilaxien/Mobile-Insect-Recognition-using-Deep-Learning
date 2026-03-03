using System.Collections;
using UnityEngine;

public class SceneAudioManager : MonoBehaviour
{
    public static SceneAudioManager Instance;

    [Header("Background Music (Loop + Across Scenes)")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;

    [Header("Guide / Instruction (Play once on scene start)")]
    public AudioSource guideSource;
    public AudioClip guideClip;

    [Header("Ducking (Lower BGM while guide plays)")]
    [Range(0f, 1f)] public float bgmNormalVolume = 1f;
    [Range(0f, 1f)] public float bgmDuckedVolume = 0.25f; // nhỏ xuống khi guide chạy
    public float fadeTime = 0.25f;

    Coroutine duckRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource != null) bgmSource.volume = bgmNormalVolume;
    }

    void Start()
    {
        PlayBGM();
        PlayGuideOnce();
    }

    void PlayBGM()
    {
        if (bgmSource == null || bgmClip == null) return;

        if (bgmSource.clip != bgmClip)
            bgmSource.clip = bgmClip;

        bgmSource.loop = true;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    void PlayGuideOnce()
    {
        if (guideSource == null || guideClip == null) return;

        guideSource.loop = false;
        guideSource.PlayOneShot(guideClip);

        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckBGMWhileGuide());
    }

    IEnumerator DuckBGMWhileGuide()
    {
        // Fade down
        yield return FadeVolume(bgmSource, bgmSource.volume, bgmDuckedVolume, fadeTime);

        // Chờ guide phát xong (PlayOneShot không set clip, nên ta chờ theo length)
        yield return new WaitForSecondsRealtime(guideClip.length);

        // Fade up
        yield return FadeVolume(bgmSource, bgmSource.volume, bgmNormalVolume, fadeTime);
    }

    IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
    {
        if (src == null) yield break;
        if (time <= 0f) { src.volume = to; yield break; }

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        src.volume = to;
    }
}
