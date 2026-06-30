using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenu : MonoBehaviour
{
    public const string OpenLevelSelectKey = "OpenLevelSelect";

    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private GameObject howToPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button storeButton;
    public StoreUI storeUI;

    [Header("Menu Root Layout")]
    [SerializeField] private Vector2 menuRootPosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 menuRootSize = new Vector2(1100f, 820f);

    [Header("Button Layout")]
    [SerializeField] private bool useManualButtonLayout = true;
    [SerializeField] private MainMenuButtonLayout playButtonLayout = new MainMenuButtonLayout("Play", new Vector2(0f, 290f), new Vector2(640f, 140f));
    [SerializeField] private MainMenuButtonLayout levelsButtonLayout = new MainMenuButtonLayout("Levels", new Vector2(-339f, -50f), new Vector2(420f, 240f));
    [SerializeField] private MainMenuButtonLayout howToPlayButtonLayout = new MainMenuButtonLayout("HowToPlay", new Vector2(-116f, -50f), new Vector2(420f, 240f));
    [SerializeField] private MainMenuButtonLayout storeButtonLayout = new MainMenuButtonLayout("Store", new Vector2(122f, -50f), new Vector2(420f, 240f));
    [SerializeField] private MainMenuButtonLayout settingsButtonLayout = new MainMenuButtonLayout("Settings", new Vector2(354f, -50f), new Vector2(420f, 240f));

    private void OnValidate()
    {
        if (!useManualButtonLayout)
            return;

        ApplyButtonLayout();
    }

    private void Start()
    {
        if (mainMenuRoot == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform menu = canvas.transform.Find("MainMenu");
                if (menu != null)
                    mainMenuRoot = menu.gameObject;
            }
        }

        if (levelsPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("Levels Panel");
                if (panel != null)
                    levelsPanel = panel.gameObject;
            }
        }

        if (howToPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("HowTo");
                if (panel != null)
                    howToPanel = panel.gameObject;
            }
        }

        if (settingsPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("SettingsPanel");
                if (panel != null)
                    settingsPanel = panel.gameObject;
            }
        }

        if (storeUI == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform store = canvas.transform.Find("StorePanel");
                if (store != null)
                    storeUI = store.GetComponent<StoreUI>();
            }
        }

        if (storeUI != null && storeUI.storePanel != null)
            storeUI.storePanel.SetActive(false);

        if (howToPanel != null)
            howToPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        ApplyButtonLayout();
        WireMenuButtons();

        if (ConsumeOpenLevelSelectFlag())
            ShowLevelSelect();
    }

    [ContextMenu("Apply Button Layout")]
    public void ApplyButtonLayout()
    {
        if (!useManualButtonLayout)
            return;

        Transform root = ResolveMainMenuRoot();
        if (root == null)
            return;

        RectTransform menuRect = root as RectTransform;
        if (menuRect != null)
        {
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.anchoredPosition = menuRootPosition;
            menuRect.sizeDelta = menuRootSize;
        }

        if (root.TryGetComponent(out VerticalLayoutGroup layoutGroup))
            layoutGroup.enabled = false;

        EnsureChildOfRoot(root, "Store");
        EnsureChildOfRoot(root, "Settings");

        ApplyButtonLayoutEntry(root, playButtonLayout);
        ApplyButtonLayoutEntry(root, levelsButtonLayout);
        ApplyButtonLayoutEntry(root, howToPlayButtonLayout);
        ApplyButtonLayoutEntry(root, storeButtonLayout);
        ApplyButtonLayoutEntry(root, settingsButtonLayout);

        string[] stretchButtons = { "Levels", "HowToPlay", "Store", "Settings" };
        foreach (string name in stretchButtons)
            SetButtonStretch(root, name);
    }

    private Transform ResolveMainMenuRoot()
    {
        if (mainMenuRoot != null)
            return mainMenuRoot.transform;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return null;

        Transform menu = canvas.transform.Find("MainMenu");
        return menu;
    }

    private static void ApplyButtonLayoutEntry(Transform root, MainMenuButtonLayout layout)
    {
        if (layout == null || string.IsNullOrWhiteSpace(layout.buttonName))
            return;

        Transform buttonTransform = root.Find(layout.buttonName);
        if (buttonTransform == null || !buttonTransform.TryGetComponent(out RectTransform rect))
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = layout.anchoredPosition;
        rect.sizeDelta = layout.sizeDelta;

        if (buttonTransform.TryGetComponent(out Image image))
            image.preserveAspect = true;
    }

    private static void EnsureChildOfRoot(Transform root, string childName)
    {
        Transform existing = root.Find(childName);
        if (existing != null) return;

        Transform canvas = root.parent;
        if (canvas == null) return;

        Transform childOnCanvas = canvas.Find(childName);
        if (childOnCanvas != null)
            childOnCanvas.SetParent(root, false);
    }

    private static void SetButtonStretch(Transform root, string buttonName)
    {
        Transform child = root.Find(buttonName);
        if (child == null || !child.TryGetComponent(out Image image))
            return;

        image.preserveAspect = false;
    }

    private static void ApplyCornerButtonLayout(Transform searchRoot, Transform canvas, MainMenuButtonLayout layout, bool leftCorner)
    {
        if (layout == null || string.IsNullOrWhiteSpace(layout.buttonName))
            return;

        Transform child = searchRoot.Find(layout.buttonName);
        if (child == null)
            child = canvas.Find(layout.buttonName);

        if (child == null || !child.TryGetComponent(out RectTransform rt))
            return;

        rt.SetParent(canvas, false);

        if (leftCorner)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
        }

        rt.anchoredPosition = layout.anchoredPosition;
        rt.sizeDelta = layout.sizeDelta;

        if (child.TryGetComponent(out Image image))
            image.preserveAspect = true;
    }

    private void WireMenuButtons()
    {
        Transform root = mainMenuRoot != null ? mainMenuRoot.transform : null;
        if (root == null)
            return;

        WireButton(root, "Play", PlayGame);
        WireButton(root, "Levels", ShowLevelSelect);
        WireButton(root, "HowToPlay", OpenHowToPlay);
        WireButton(root, "Store", OpenStore);
        WireButton(root, "Settings", OpenSettings);
    }

    private void WireButton(Transform root, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = root.Find(childName);
        if (child == null || !child.TryGetComponent(out Button button))
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public static void RequestOpenLevelSelect()
    {
        PlayerPrefs.SetInt(OpenLevelSelectKey, 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeOpenLevelSelectFlag()
    {
        if (PlayerPrefs.GetInt(OpenLevelSelectKey, 0) != 1)
            return false;

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

    public void OpenHowToPlay()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (howToPanel != null)
            howToPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPanel != null)
            howToPanel.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);
    }

    public void OpenSettings()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);
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
        int level = LevelProgress.GetFocusLevel();
        if (!LevelProgress.IsLevelUnlocked(level))
            level = 1;

        string sceneName = LevelProgress.GetSceneNameForLevel(level);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            ShowLevelSelect();
            return;
        }

        LevelProgress.SetSelectedMenuLevel(level);
        if (LevelProgress.IsProceduralMenuLevel(level))
            ProceduralSession.MarkFreshRunFromMenu();

        SceneManager.LoadScene(sceneName);
    }
}
