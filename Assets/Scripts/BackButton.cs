using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour
{
    public string backSceneName = "HomeScene";

    [Header("Voice / Notification")]
    public AudioSource sfxSource;     // AudioSource để phát voice/notification
    public AudioClip noticeClip;      // Clip “đọc thông báo”
    public bool waitForClipEnd = true;

    bool isLoading = false;

    public void GoBack()
    {
        if (isLoading) return;
        StartCoroutine(GoBackRoutine());
    }

    IEnumerator GoBackRoutine()
    {
        isLoading = true;

        if (sfxSource != null && noticeClip != null)
        {
            sfxSource.PlayOneShot(noticeClip);

            if (waitForClipEnd)
                yield return new WaitForSecondsRealtime(noticeClip.length);
        }

        SceneManager.LoadScene(backSceneName);
    }
}
