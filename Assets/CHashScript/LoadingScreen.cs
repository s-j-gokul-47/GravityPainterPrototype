using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject tapToContinue;

    private AsyncOperation loadingOperation;
    private bool loadingFinished = false;

    void Start()
    {
        if (tapToContinue != null)
            tapToContinue.SetActive(false);

        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        loadingOperation = SceneManager.LoadSceneAsync("GameScene");
        loadingOperation.allowSceneActivation = false;

        while (loadingOperation.progress < 0.9f)
        {
            yield return null;
        }

        loadingFinished = true;

        if (tapToContinue != null)
            tapToContinue.SetActive(true);
    }

    void Update()
    {
        if (loadingFinished && Input.GetMouseButtonDown(0))
        {
            loadingOperation.allowSceneActivation = true;
        }
    }
}