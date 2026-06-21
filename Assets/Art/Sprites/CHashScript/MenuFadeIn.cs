using System.Collections;
using UnityEngine;

public class MenuFadeIn : MonoBehaviour
{
    // Drag your MenuFadePanel's CanvasGroup here
    public CanvasGroup fadeCanvasGroup; 
    
    // How fast the main menu dissolves into view (e.g., 1.5 seconds)
    public float fadeInDuration = 1.5f; 

    void Start()
    {
        // Force the screen to start pitch black
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeInRoutine());
        }
    }

    IEnumerator FadeInRoutine()
    {
        float counter = 0f;
        while (counter < fadeInDuration)
        {
            counter += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                // Gradually decrease alpha from 1 to 0 (making it transparent)
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, counter / fadeInDuration);
            }
            yield return null;
        }

        // Completely turn off the panel when done so you can click menu buttons
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}
