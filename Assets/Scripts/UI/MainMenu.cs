using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public const string OpenLevelSelectKey = "OpenLevelSelect";

    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private Button storeButton;
    public StoreUI storeUI;

    private void Start()
    {
        if (storeUI == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("StorePanel");
                if (t != null) storeUI = t.GetComponent<StoreUI>();
            }
        }
        if (storeUI != null && storeUI.storePanel != null)
            storeUI.storePanel.SetActive(false);

        WireStoreButton();

        if (ConsumeOpenLevelSelectFlag())
            ShowLevelSelect();
    }

    private void WireStoreButton()
    {
        Button btn = storeButton;
        if (btn == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("MainMenu/Store");
                if (t != null) btn = t.GetComponent<Button>();
            }
        }
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OpenStore);
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
            mainMenuRoot.SetActive(false);

        if (levelsPanel != null)
        {
            levelsPanel.SetActive(true);
            LevelMenu levelMenu = levelsPanel.GetComponent<LevelMenu>();
            if (levelMenu != null)
                levelMenu.RefreshLevelList();
        }
    }

    public void OpenStore()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (storeUI != null)
            storeUI.Open();
    }

    public void CloseStore()
    {
        if (storeUI != null)
            storeUI.Close();

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);
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
