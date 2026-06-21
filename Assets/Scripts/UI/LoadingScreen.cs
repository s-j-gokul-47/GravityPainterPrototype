using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GravityPainter.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("Scene to Load")]
        [SerializeField] private string nextSceneName = "MainMenu";

        [Header("UI Elements")]
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject tapToContinueText;
        
        [Header("Settings")]
        [SerializeField] private float minimumLoadingTime = 2.0f;

        private AsyncOperation _asyncOperation;
        private bool _isReadyToContinue = false;

        private void Start()
        {
            // Ensure initial UI state
            if (tapToContinueText != null) tapToContinueText.SetActive(false);
            if (loadingBar != null) loadingBar.value = 0f;
            if (progressText != null) progressText.text = "0%";

            StartCoroutine(LoadSceneAsync());
        }

        private void Update()
        {
            if (_isReadyToContinue)
            {
                // Check for user input (Touch or Mouse Click)
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    ProceedToNextScene();
                }
                else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.touches.Count > 0)
                {
                    var touch = UnityEngine.InputSystem.Touchscreen.current.touches[0];
                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        ProceedToNextScene();
                    }
                }
            }
        }

        private IEnumerator LoadSceneAsync()
        {
            float elapsedTime = 0f;

            // Start loading the scene asynchronously
            _asyncOperation = SceneManager.LoadSceneAsync(nextSceneName);
            
            // Prevent it from activating automatically
            _asyncOperation.allowSceneActivation = false;

            while (!_asyncOperation.isDone)
            {
                elapsedTime += Time.deltaTime;

                // Unity's async load progress stops at 0.9. We map it to 0-1.
                float loadProgress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);
                
                // We enforce a minimum loading time for visual polish
                float timeProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);
                
                // Display whichever progress is slower
                float displayProgress = Mathf.Min(loadProgress, timeProgress);

                if (loadingBar != null) loadingBar.value = displayProgress;
                if (progressText != null) progressText.text = Mathf.RoundToInt(displayProgress * 100f) + "%";

                // Check if both the scene is loaded and our minimum time has passed
                if (_asyncOperation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
                {
                    // Loading complete!
                    if (loadingBar != null) loadingBar.gameObject.SetActive(false);
                    if (progressText != null) progressText.gameObject.SetActive(false);
                    if (tapToContinueText != null) tapToContinueText.SetActive(true);

                    _isReadyToContinue = true;
                    break;
                }

                yield return null;
            }
        }

        private void ProceedToNextScene()
        {
            if (_asyncOperation != null)
            {
                _asyncOperation.allowSceneActivation = true;
            }
        }
    }
}
