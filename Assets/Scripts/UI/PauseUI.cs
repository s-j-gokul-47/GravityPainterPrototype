using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseUI : MonoBehaviour
{
    public static PauseUI Instance { get; private set; }

    private bool _isPaused = false;
    private GameObject _pauseOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only show in actual gameplay levels
        if (scene.name.StartsWith("Level ") || LevelProgress.IsProceduralScene(scene))
        {
            CreatePauseCanvas();
        }
    }

    private static void CreatePauseCanvas()
    {
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        canvasObj.AddComponent<GraphicRaycaster>();
        
        PauseUI pauseUI = canvasObj.AddComponent<PauseUI>();
        pauseUI.SetupUI(canvasObj.transform);
    }

    private void SetupUI(Transform parent)
    {
        // 1. Pause Overlay (hidden by default)
        _pauseOverlay = new GameObject("PauseOverlay");
        _pauseOverlay.transform.SetParent(parent, false);
        
        RectTransform overlayRect = _pauseOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = _pauseOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.75f);
        _pauseOverlay.SetActive(false);

        // Pause Panel (Golden Frame Background)
        GameObject panelObj = new GameObject("PausePanel");
        panelObj.transform.SetParent(_pauseOverlay.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800f, 1400f);

        Image panelImage = panelObj.AddComponent<Image>();
        Sprite bgSprite = Resources.Load<Sprite>("UI/PauseBackground");
        if (bgSprite == null)
        {
            Texture2D bgTex = Resources.Load<Texture2D>("UI/PauseBackground");
            if (bgTex != null)
            {
                bgSprite = Sprite.Create(bgTex, new Rect(0f, 0f, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            }
        }
        
        if (bgSprite != null)
        {
            panelImage.sprite = bgSprite;
            // Optionally preserve aspect ratio if needed, but for a frame we usually slice or stretch. 
            // We'll preserve aspect so the frame doesn't look distorted.
            panelImage.preserveAspect = true;
        }
        else
        {
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        // Resume Button inside PausePanel
        GameObject resumeBtnObj = new GameObject("ResumeButton");
        resumeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform resumeRect = resumeBtnObj.AddComponent<RectTransform>();
        resumeRect.sizeDelta = new Vector2(500f, 180f);
        resumeRect.anchoredPosition = new Vector2(0f, 120f); // Shifted up

        Image resumeImg = resumeBtnObj.AddComponent<Image>();
        resumeImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        Button resumeBtn = resumeBtnObj.AddComponent<Button>();
        resumeBtn.onClick.AddListener(TogglePause);

        GameObject resumeTextObj = new GameObject("Text (TMP)");
        resumeTextObj.transform.SetParent(resumeBtnObj.transform, false);
        RectTransform rTextRect = resumeTextObj.AddComponent<RectTransform>();
        rTextRect.anchorMin = Vector2.zero;
        rTextRect.anchorMax = Vector2.one;
        rTextRect.offsetMin = Vector2.zero;
        rTextRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI resumeText = resumeTextObj.AddComponent<TextMeshProUGUI>();
        resumeText.text = "Resume";
        resumeText.alignment = TextAlignmentOptions.Center;
        resumeText.fontSize = 70f;
        resumeText.color = Color.white;

        // Restart Button inside PausePanel
        GameObject restartBtnObj = new GameObject("RestartButton");
        restartBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform restartRect = restartBtnObj.AddComponent<RectTransform>();
        restartRect.sizeDelta = new Vector2(500f, 180f);
        restartRect.anchoredPosition = new Vector2(0f, -120f); // Shifted down

        Image restartImg = restartBtnObj.AddComponent<Image>();
        restartImg.color = new Color(1f, 0.4f, 0.2f, 1f);
        Button restartBtn = restartBtnObj.AddComponent<Button>();
        restartBtn.onClick.AddListener(RestartLevel);

        GameObject restartTextObj = new GameObject("Text (TMP)");
        restartTextObj.transform.SetParent(restartBtnObj.transform, false);
        RectTransform resTextRect = restartTextObj.AddComponent<RectTransform>();
        resTextRect.anchorMin = Vector2.zero;
        resTextRect.anchorMax = Vector2.one;
        resTextRect.offsetMin = Vector2.zero;
        resTextRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI restartText = restartTextObj.AddComponent<TextMeshProUGUI>();
        restartText.text = "Restart";
        restartText.alignment = TextAlignmentOptions.Center;
        restartText.fontSize = 70f;
        restartText.color = Color.white;


        // 2. Pause Button (Top Right)
        GameObject btnObj = new GameObject("PauseButton");
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-50f, -50f);
        btnRect.sizeDelta = new Vector2(150f, 150f);

        Image btnImg = btnObj.AddComponent<Image>();
        Sprite pauseSprite = Resources.Load<Sprite>("UI/PauseIcon");
        if (pauseSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UI/PauseIcon");
            if (tex != null)
            {
                pauseSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (pauseSprite != null)
        {
            btnImg.sprite = pauseSprite;
        }
        else
        {
            // Fallback appearance if the sprite is missing
            btnImg.color = Color.white; 
            Debug.LogWarning("Pause icon sprite not found at Resources/UI/PauseIcon. Please place the image there and set its Texture Type to Sprite (2D and UI).");
        }

        Button pauseBtn = btnObj.AddComponent<Button>();
        pauseBtn.onClick.AddListener(TogglePause);
        
        EnsureEventSystem();
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        _pauseOverlay.SetActive(_isPaused);
    }
    
    private void RestartLevel()
    {
        TogglePause(); // Unpause and hide overlay

        Scene active = SceneManager.GetActiveScene();
        if (LevelProgress.IsProceduralScene(active))
        {
            ProceduralLevelBuilder builder = FindFirstObjectByType<ProceduralLevelBuilder>();
            if (builder != null)
            {
                builder.RebuildSameSeed();
            }
            return;
        }

        // Standard level restart
        if (active.buildIndex >= 0)
        {
            SceneManager.LoadScene(active.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(active.name);
        }
    }
    
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
