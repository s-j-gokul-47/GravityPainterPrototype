using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    public Button[] buttons;

    private void OnEnable()
    {
        ApplyUnlockState();
    }

    private void Awake()
    {
        ApplyUnlockState();
    }

    private void ApplyUnlockState()
    {
        if (buttons == null || buttons.Length == 0)
        {
            return;
        }

        int unlockedLevel = LevelProgress.GetUnlockedLevel();

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            buttons[i].interactable = i < unlockedLevel;
        }
    }

    public void OpenLevel(int levelId)
    {
        if (!LevelProgress.IsLevelUnlocked(levelId))
        {
            return;
        }

        string sceneName = LevelProgress.GetSceneNameForLevel(levelId);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("Level scene not in build settings: " + sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
