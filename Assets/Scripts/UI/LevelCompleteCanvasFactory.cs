using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Creates the same level-complete overlay used in campaign levels (Level 1/2).
/// </summary>
public static class LevelCompleteCanvasFactory
{
    public const string CanvasObjectName = "LevelCompleteCanvas";
    private const string BackgroundResource = "UI/LevelCompleteUI/level_completed";
    private const string BackgroundAssetPath = "Assets/Art/Sprites/UI/Level_Completed.jpeg";

    public static GameObject EnsureCanvas(ProceduralLevelBuilder builder)
    {
        GameObject existing = GameObject.Find(CanvasObjectName);
        if (existing != null)
        {
            WireProceduralBuilder(existing, builder);
            existing.SetActive(false);
            return existing;
        }

        EnsureEventSystem();
        GameObject canvasObject = CreateCanvasHierarchy();
        WireProceduralBuilder(canvasObject, builder);
        canvasObject.SetActive(false);
        return canvasObject;
    }

    private static void WireProceduralBuilder(GameObject canvasObject, ProceduralLevelBuilder builder)
    {
        LevelCompleteUI ui = canvasObject.GetComponent<LevelCompleteUI>();
        if (ui == null)
        {
            ui = canvasObject.AddComponent<LevelCompleteUI>();
        }

        ui.ConfigureProcedural(builder);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject CreateCanvasHierarchy()
    {
        GameObject canvasObject = new GameObject(
            CanvasObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CreateBackgroundPanel(canvasRect);
        CreateTitleText(canvasRect);

        return canvasObject;
    }

    private static void CreateBackgroundPanel(RectTransform parent)
    {
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = new Vector3(1f, 1.4f, 1f);

        Image image = panel.GetComponent<Image>();
        image.raycastTarget = true;
        image.preserveAspect = true;
        Sprite background = LoadBackgroundSprite();
        image.sprite = background;
        image.type = Image.Type.Simple;
        image.color = background != null ? Color.white : new Color(0.08f, 0.1f, 0.16f, 0.95f);
    }

    private static void CreateTitleText(RectTransform parent)
    {
        GameObject title = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        title.transform.SetParent(parent, false);

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 140f);
        rect.anchoredPosition = new Vector2(0f, 658f);

        TextMeshProUGUI text = title.GetComponent<TextMeshProUGUI>();
        text.text = "Level Completed";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 72f;
        text.color = new Color(1f, 0.84f, 0f, 1f);
        text.raycastTarget = true;
    }

    public static Sprite LoadBackgroundSprite()
    {
        Sprite sprite = Resources.Load<Sprite>(BackgroundResource);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(BackgroundResource);
        if (texture != null)
        {
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

#if UNITY_EDITOR
        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPath);
        if (sprite != null)
        {
            return sprite;
        }
#endif

        Debug.LogWarning("LevelCompleteCanvasFactory: background sprite missing at Resources/" + BackgroundResource);
        return null;
    }
}
