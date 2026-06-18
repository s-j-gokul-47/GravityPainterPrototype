using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Level-complete overlay on LevelCompleteCanvas: Restart, Next Level, Home (image buttons).
/// Hierarchy: Panel (background) → "Level Completed" title → ActionButtons (just below title).
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    private const string ButtonsRootName = "ActionButtons";
    private const string TitleObjectName = "Text (TMP)";
    private const string RestartResource = "UI/LevelCompleteUI/restart";
    private const string NextLevelResource = "UI/LevelCompleteUI/nextlevel";
    private const string HomeResource = "UI/LevelCompleteUI/home";

    [SerializeField] private int currentLevel;
    [SerializeField] private ProceduralLevelBuilder proceduralBuilder;
    [SerializeField] private Sprite restartButtonSprite;
    [SerializeField] private Sprite nextLevelButtonSprite;
    [SerializeField] private Sprite homeButtonSprite;

    [Header("Layout (1080×1920 portrait canvas)")]
    [SerializeField] private float buttonHeight = 200f;
    [SerializeField] private float buttonSpacing = 48f;
    [Tooltip("Y position of the button row — higher = nearer top of screen.")]
    [SerializeField] private float buttonsAnchoredY = 520f;
    [Tooltip("Gap between the title text and the button row.")]
    [SerializeField] private float titleGap = 36f;

    private Button _restartButton;
    private Button _nextLevelButton;
    private Button _homeButton;

    public void ConfigureProcedural(ProceduralLevelBuilder builder)
    {
        proceduralBuilder = builder;
        UpdateNextLevelButton();
    }

    private bool IsProceduralMode => proceduralBuilder != null;

    private void Awake()
    {
        if (currentLevel < 1)
        {
            currentLevel = LevelProgress.GetActiveLevelNumber();
        }

        LoadDefaultSpritesIfNeeded();
        BuildActionButtons();
        LayoutTitleText();
    }

    private void OnEnable()
    {
        if (currentLevel < 1)
        {
            currentLevel = LevelProgress.GetActiveLevelNumber();
        }

        if (!IsProceduralMode)
        {
            LevelProgress.UnlockThrough(currentLevel);
        }

        EnsureBackgroundVisible();
        UpdateNextLevelButton();
        UpdateProceduralTitle();
    }

    private void EnsureBackgroundVisible()
    {
        Transform panel = transform.Find("Panel");
        if (panel == null)
        {
            return;
        }

        Image image = panel.GetComponent<Image>();
        if (image == null || image.sprite != null)
        {
            return;
        }

        Sprite background = LevelCompleteCanvasFactory.LoadBackgroundSprite();
        if (background == null)
        {
            return;
        }

        image.sprite = background;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private void LoadDefaultSpritesIfNeeded()
    {
        restartButtonSprite = EnsureSprite(restartButtonSprite, RestartResource);
        nextLevelButtonSprite = EnsureSprite(nextLevelButtonSprite, NextLevelResource);
        homeButtonSprite = EnsureSprite(homeButtonSprite, HomeResource);
    }

    private static Sprite EnsureSprite(Sprite assigned, string resourcesPath)
    {
        if (assigned != null)
        {
            return assigned;
        }

        return Resources.Load<Sprite>(resourcesPath);
    }

    private void LayoutTitleText()
    {
        Transform title = transform.Find(TitleObjectName);
        if (title is not RectTransform titleRect)
        {
            return;
        }

        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);

        float titleHeight = titleRect.sizeDelta.y;
        float titleY = buttonsAnchoredY + buttonHeight * 0.5f + titleGap + titleHeight * 0.5f;
        titleRect.anchoredPosition = new Vector2(0f, titleY);

        // Draw order: background → buttons → title on top (readable over artwork).
        titleRect.SetAsLastSibling();
    }

    private void UpdateProceduralTitle()
    {
        if (!IsProceduralMode)
        {
            return;
        }

        Transform title = transform.Find(TitleObjectName);
        TextMeshProUGUI label = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
        if (label == null)
        {
            return;
        }

        int nextMenuLevel = LevelProgress.GetSelectedMenuLevel() + 1;
        float nextDifficulty = LevelProgress.GetDifficultyForMenuLevel(nextMenuLevel);
        label.text =
            "Level Completed!\nNext: "
            + DifficultyManager.GetTierName(nextDifficulty)
            + " ("
            + nextDifficulty.ToString("F2")
            + ")";
    }

    private void BuildActionButtons()
    {
        Transform existing = transform.Find(ButtonsRootName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject root = new GameObject(ButtonsRootName, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, buttonsAnchoredY);
        rootRect.sizeDelta = new Vector2(1080f, buttonHeight + 40f);

        Vector2 restartSize = GetButtonSize(restartButtonSprite);
        Vector2 nextSize = GetButtonSize(nextLevelButtonSprite);
        Vector2 homeSize = GetButtonSize(homeButtonSprite);

        float totalWidth = restartSize.x + nextSize.x + homeSize.x + buttonSpacing * 2f;
        float x = -totalWidth * 0.5f;

        _restartButton = PlaceButton(root.transform, "Restart", RestartLevel, restartButtonSprite, restartSize, ref x);
        _nextLevelButton = PlaceButton(root.transform, "Next Level", GoToNextLevel, nextLevelButtonSprite, nextSize, ref x);
        _homeButton = PlaceButton(root.transform, "Home", GoHome, homeButtonSprite, homeSize, ref x);

        UpdateNextLevelButton();
    }

    private Button PlaceButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction onClick,
        Sprite sprite,
        Vector2 size,
        ref float x)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(x + size.x * 0.5f, 0f);

        x += size.x + buttonSpacing;

        Image image = buttonObject.GetComponent<Image>();
        image.raycastTarget = true;
        image.color = Color.white;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("LevelCompleteUI: missing sprite for " + label);
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.2f, 0.55f, 0.95f, 1f);
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return button;
    }

    private void UpdateNextLevelButton()
    {
        if (_nextLevelButton == null)
        {
            return;
        }

        if (IsProceduralMode)
        {
            _nextLevelButton.gameObject.SetActive(true);
            _nextLevelButton.interactable = true;
            Image proceduralImage = _nextLevelButton.GetComponent<Image>();
            if (proceduralImage != null)
            {
                proceduralImage.color = Color.white;
            }

            return;
        }

        bool hasNext = LevelProgress.HasBuiltLevel(currentLevel + 1);
        _nextLevelButton.gameObject.SetActive(true);
        _nextLevelButton.interactable = hasNext;

        Image image = _nextLevelButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = hasNext ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
    }

    public void RestartLevel()
    {
        if (IsProceduralMode || LevelProgress.IsProceduralScene(SceneManager.GetActiveScene()))
        {
            if (proceduralBuilder == null)
            {
                proceduralBuilder = FindFirstObjectByType<ProceduralLevelBuilder>();
            }

            if (proceduralBuilder == null)
            {
                Debug.LogWarning("LevelCompleteUI: no ProceduralLevelBuilder found for Restart.");
                return;
            }

            HideAndResume();
            proceduralBuilder.RebuildSameSeed();
            return;
        }

        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
        {
            SceneManager.LoadScene(active.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(active.name);
        }
    }

    public void GoToNextLevel()
    {
        if (IsProceduralMode || LevelProgress.IsProceduralScene(SceneManager.GetActiveScene()))
        {
            if (proceduralBuilder == null)
            {
                proceduralBuilder = FindFirstObjectByType<ProceduralLevelBuilder>();
            }

            if (proceduralBuilder == null)
            {
                Debug.LogWarning("LevelCompleteUI: no ProceduralLevelBuilder found for Next Level.");
                return;
            }

            HideAndResume();
            int nextMenuLevel = LevelProgress.GetSelectedMenuLevel() + 1;
            LevelProgress.SetSelectedMenuLevel(nextMenuLevel);
            currentLevel = nextMenuLevel;
            proceduralBuilder.RebuildNextSeed();
            UpdateProceduralTitle();
            return;
        }

        Time.timeScale = 1f;
        int next = currentLevel + 1;

        if (!LevelProgress.HasBuiltLevel(next))
        {
            GoHome();
            return;
        }

        LevelProgress.SetSelectedMenuLevel(next);
        if (LevelProgress.IsProceduralMenuLevel(next))
        {
            ProceduralSession.MarkFreshRunFromMenu();
        }

        SceneManager.LoadScene(LevelProgress.GetSceneNameForLevel(next));
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        MainMenu.RequestOpenLevelSelect();
        SceneManager.LoadScene("MainMenu");
    }

    private void HideAndResume()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private Vector2 GetButtonSize(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Vector2(buttonHeight, buttonHeight);
        }

        float w = Mathf.Max(sprite.rect.width, 1f);
        float h = Mathf.Max(sprite.rect.height, 1f);
        float aspect = w / h;
        aspect = Mathf.Clamp(aspect, 0.65f, 1.35f);
        return new Vector2(buttonHeight * aspect, buttonHeight);
    }
}
