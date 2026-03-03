using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    public AudioSource bgmSource;
    public AudioClip bgmClip; // clip mặc định (scene đầu tiên)

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource != null) bgmSource.loop = true;

        // Auto play clip mặc định nếu có
        if (bgmSource != null && bgmClip != null)
        {
            Play(bgmClip);
        }
    }

    // Gọi hàm này ở mỗi scene để đổi nhạc nền theo scene
    public void Play(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        // Nếu đang phát đúng bài rồi thì không làm gì
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // (Tuỳ chọn) Dừng nhạc
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
}
