using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Legacy helper used by older level scenes. Prefer LevelCompleteUI on the finish-line overlay.
/// </summary>
public class LevelComplete : MonoBehaviour
{
    public int currentLevel;

    public void CompleteLevel()
    {
        if (currentLevel < 1)
        {
            currentLevel = LevelProgress.GetActiveLevelNumber();
        }

        LevelProgress.UnlockThrough(currentLevel);
        Time.timeScale = 1f;
        MainMenu.RequestOpenLevelSelect();
        SceneManager.LoadScene("MainMenu");
    }
}
