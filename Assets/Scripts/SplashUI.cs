using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashUI : MonoBehaviour
{
    public TMPro.TMP_Text loadingText;
    public float splashTime = 2.0f;

    void Start()
    {
        StartCoroutine(AnimateLoading());
        StartCoroutine(GoNext());
    }

    IEnumerator AnimateLoading()
    {
        string[] frames = { "●○○○○", "●●○○○", "●●●○○", "●●●●○", "●●●●●" };
        int i = 0;
        while (true)
        {
            if (loadingText != null)
                loadingText.text = frames[i % frames.Length];
            i++;
            yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator GoNext()
    {
        yield return new WaitForSeconds(splashTime);
        SceneManager.LoadScene("HomeScene");
    }
}
