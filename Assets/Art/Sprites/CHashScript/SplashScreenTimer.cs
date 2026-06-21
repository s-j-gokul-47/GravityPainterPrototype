using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreenTimer : MonoBehaviour
{
    // This will create the empty box slot you need!
    public CanvasGroup fadeCanvasGroup; 
    
    public float waitDuration = 3f; 
    public float fadeDuration = 1f; 

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }
        StartCoroutine(WaitThenDissolve());
    }

    IEnumerator WaitThenDissolve()
    {
        yield return new WaitForSeconds(waitDuration);

        float counter = 0f;
        while (counter < fadeDuration)
        {
            counter += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, counter / fadeDuration);
            }
            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
