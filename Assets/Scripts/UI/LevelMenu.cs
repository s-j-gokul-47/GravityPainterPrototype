using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a scrollable text list of unlocked levels (1..n) and focuses the newest unlocked level.
/// </summary>
public class LevelMenu : MonoBehaviour
{
    private const string LegacyButtonsChildName = "Level Buttons";

    [SerializeField] private float rowHeight = 96f;
    [SerializeField] private float rowWidth = 520f;
    [SerializeField] private float rowSpacing = 16f;
    [SerializeField] private float scrollPadding = 24f;
    [SerializeField] private float focusScale = 1.04f;
    [SerializeField] private Vector2 scrollAreaSize = new Vector2(560f, 720f);
    [SerializeField] private Vector2 scrollAreaPosition = new Vector2(0f, -80f);
    [SerializeField] private Color rowColor = new Color(0.12f, 0.14f, 0.2f, 0.92f);
    [SerializeField] private Color focusColor = new Color(0.2f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color textColor = Color.white;

    private ScrollRect _scrollRect;
    private RectTransform _content;
    private readonly List<Button> _spawnedButtons = new List<Button>();
    private bool _uiBuilt;
    private Coroutine _scrollCoroutine;
    private int _lastBuiltCount;

    private void OnEnable()
    {
        RefreshLevelList();
    }

    public void RefreshLevelList()
    {
        EnsureScrollUi();
        RebuildLevelButtons();
        ScrollToFocusLevel();
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

        LevelProgress.SetSelectedMenuLevel(levelId);
        if (LevelProgress.IsProceduralMenuLevel(levelId))
        {
            ProceduralSession.MarkFreshRunFromMenu();
        }

        SceneManager.LoadScene(sceneName);
    }

    private void EnsureScrollUi()
    {
        if (_uiBuilt)
        {
            return;
        }

        RemoveLegacyButtons();
        BuildScrollView();
        _uiBuilt = true;
    }

    private void RemoveLegacyButtons()
    {
        Transform legacyRoot = transform.Find(LegacyButtonsChildName);
        if (legacyRoot != null)
        {
            Destroy(legacyRoot.gameObject);
        }
    }

    private void BuildScrollView()
    {
        GameObject scrollRoot = new GameObject("Level List", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollRoot.transform.SetParent(transform, false);

        RectTransform scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.sizeDelta = scrollAreaSize;
        scrollRectTransform.anchoredPosition = scrollAreaPosition;

        Image scrollBackground = scrollRoot.GetComponent<Image>();
        scrollBackground.color = new Color(0f, 0f, 0f, 0f);
        scrollBackground.raycastTarget = true;

        _scrollRect = scrollRoot.GetComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 30f;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollRoot.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchToParent(viewportRect);

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        _content = contentObject.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0.5f, 1f);
        _content.anchorMax = new Vector2(0.5f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(rowWidth, 0f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = rowSpacing;
        layout.padding = new RectOffset(
            Mathf.RoundToInt(scrollPadding),
            Mathf.RoundToInt(scrollPadding),
            Mathf.RoundToInt(scrollPadding),
            Mathf.RoundToInt(scrollPadding));
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = _content;
    }

    private void RebuildLevelButtons()
    {
        if (_content == null)
        {
            return;
        }

        int unlockedCount = LevelProgress.GetUnlockedLevel();
        int focusLevel = LevelProgress.GetFocusLevel();

        if (_lastBuiltCount == unlockedCount && _spawnedButtons.Count == unlockedCount)
        {
            ApplyButtonStates(unlockedCount, focusLevel);
            return;
        }

        ClearSpawnedButtons();

        for (int level = 1; level <= unlockedCount; level++)
        {
            Button button = CreateLevelRow(level, focusLevel);
            _spawnedButtons.Add(button);
        }

        _lastBuiltCount = unlockedCount;
    }

    private void ClearSpawnedButtons()
    {
        for (int i = _spawnedButtons.Count - 1; i >= 0; i--)
        {
            if (_spawnedButtons[i] != null)
            {
                Destroy(_spawnedButtons[i].gameObject);
            }
        }

        _spawnedButtons.Clear();
    }

    private Button CreateLevelRow(int levelNumber, int focusLevel)
    {
        bool isFocused = levelNumber == focusLevel;

        GameObject rowObject = new GameObject(
            "Level " + levelNumber,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        rowObject.transform.SetParent(_content, false);

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = rowHeight;
        layoutElement.minHeight = rowHeight;

        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rowWidth, rowHeight);
        rect.localScale = isFocused ? Vector3.one * focusScale : Vector3.one;

        Image image = rowObject.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = isFocused ? focusColor : rowColor;
        image.raycastTarget = true;

        Button button = rowObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = isFocused ? focusColor : rowColor;
        colors.highlightedColor = focusColor * 1.1f;
        colors.pressedColor = focusColor * 0.85f;
        colors.selectedColor = focusColor;
        button.colors = colors;
        button.onClick.RemoveAllListeners();

        int capturedLevel = levelNumber;
        button.onClick.AddListener(() => OpenLevel(capturedLevel));

        CreateRowLabel(rowObject.transform, levelNumber);
        return button;
    }

    private void CreateRowLabel(Transform parent, int levelNumber)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchToParent(labelRect);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.text = "Level " + levelNumber;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 42f;
        label.fontStyle = FontStyles.Bold;
        label.color = textColor;
        label.raycastTarget = false;
    }

    private void ApplyButtonStates(int unlockedCount, int focusLevel)
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            Button button = _spawnedButtons[i];
            if (button == null)
            {
                continue;
            }

            int levelNumber = i + 1;
            bool isFocused = levelNumber == focusLevel;
            button.interactable = levelNumber <= unlockedCount;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isFocused ? focusColor : rowColor;
            }

            button.transform.localScale = isFocused ? Vector3.one * focusScale : Vector3.one;
        }
    }

    private void ScrollToFocusLevel()
    {
        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
        }

        _scrollCoroutine = StartCoroutine(ScrollToFocusLevelCoroutine());
    }

    private IEnumerator ScrollToFocusLevelCoroutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (_content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        if (_scrollRect == null || _spawnedButtons.Count == 0)
        {
            yield break;
        }

        int focusLevel = LevelProgress.GetFocusLevel();
        int focusIndex = Mathf.Clamp(focusLevel - 1, 0, _spawnedButtons.Count - 1);
        Button focusButton = _spawnedButtons[focusIndex];
        if (focusButton == null)
        {
            yield break;
        }

        RectTransform focusRect = focusButton.transform as RectTransform;
        RectTransform viewport = _scrollRect.viewport;
        if (focusRect == null || viewport == null || _content == null)
        {
            yield break;
        }

        float contentHeight = _content.rect.height;
        float viewportHeight = viewport.rect.height;
        if (contentHeight <= viewportHeight)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
            yield break;
        }

        float rowTop = -focusRect.anchoredPosition.y;
        float rowCenter = rowTop + focusRect.rect.height * 0.5f;
        float scrollable = contentHeight - viewportHeight;
        float target = rowCenter - viewportHeight * 0.5f;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - target / scrollable);
        _scrollCoroutine = null;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
