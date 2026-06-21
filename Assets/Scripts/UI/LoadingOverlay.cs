using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GravityPainter.UI
{
    public class LoadingOverlay : MonoBehaviour
    {
        [Header("Loading UI Elements")]
        [SerializeField] private GameObject loadingScreenContainer; // Holds everything: Background, Bar, Texts
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject tapToContinueText;

        [Header("Settings")]
        [SerializeField] private float loadingTime = 2.0f; // Fake loading time for the overlay

        private bool _isReadyToContinue = false;

        private void Start()
        {
            // Ensure initial state
            if (tapToContinueText != null) tapToContinueText.SetActive(false);
            if (loadingBar != null) loadingBar.value = 0f;
            if (progressText != null) progressText.text = "0%";

            StartCoroutine(SimulateLoading());
        }

        private void Update()
        {
            if (_isReadyToContinue)
            {
                // Wait for any screen tap or click
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    FinishLoading();
                }
                else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.touches.Count > 0)
                {
                    var touch = UnityEngine.InputSystem.Touchscreen.current.touches[0];
                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        FinishLoading();
                    }
                }
            }
        }

        private IEnumerator SimulateLoading()
        {
            float elapsedTime = 0f;

            // Animate the fake loading bar
            while (elapsedTime < loadingTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / loadingTime);

                if (loadingBar != null) loadingBar.value = progress;
                if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

                yield return null;
            }

            // Loading complete! Hide the bar and show Tap to Continue
            if (loadingBar != null) loadingBar.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (tapToContinueText != null) tapToContinueText.SetActive(true);

            _isReadyToContinue = true;
        }

        private void FinishLoading()
        {
            // Hide the entire loading overlay
            if (loadingScreenContainer != null)
            {
                loadingScreenContainer.SetActive(false);
            }
            
            // Remove the script so it doesn't run anymore
            Destroy(this);
        }
    }
}
