using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public const string OpenLevelSelectKey = "OpenLevelSelect";

    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject levelsPanel;

    private void Start()
    {
        if (ConsumeOpenLevelSelectFlag())
        {
            ShowLevelSelect();
        }
    }

    public static void RequestOpenLevelSelect()
    {
        PlayerPrefs.SetInt(OpenLevelSelectKey, 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeOpenLevelSelectFlag()
    {
        if (PlayerPrefs.GetInt(OpenLevelSelectKey, 0) != 1)
        {
            return false;
        }

        PlayerPrefs.DeleteKey(OpenLevelSelectKey);
        PlayerPrefs.Save();
        return true;
    }

    public void ShowLevelSelect()
    {
        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }

        if (levelsPanel != null)
        {
            levelsPanel.SetActive(true);
            LevelMenu levelMenu = levelsPanel.GetComponent<LevelMenu>();
            if (levelMenu != null)
            {
                levelMenu.RefreshLevelList();
            }
        }
    }

    public void PlayGame()
    {
        ShowLevelSelect();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
