#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SetupMainMenuButtons
{
    private const string ScenePath = "Assets/Scenes/Menus/MainMenu.unity";

    private static readonly (string name, string spritePath)[] Buttons =
    {
        ("Play", "Assets/Art/Sprites/UI/play_button.png"),
        ("Settings", "Assets/Art/Sprites/UI/settings_button.png"),
        ("Store", "Assets/Art/Sprites/UI/store_button.png"),
        ("Levels", "Assets/Art/Sprites/UI/levels_button.png"),
        ("HowToPlay", "Assets/Art/Sprites/UI/howtoplay_button.png"),
    };

    [MenuItem("Gravity Painter/Setup Main Menu Buttons")]
    public static void SetupFromMenu()
    {
        Setup();
        EditorUtility.DisplayDialog(
            "Main menu buttons",
            "MainMenu now has Play, Settings, Store, Levels, and How To Play.\n\n"
            + "Quit was removed. Select Canvas > MainMenu to adjust layout.",
            "OK");
    }

    public static void RunFromBatch()
    {
        Setup();
        EditorSceneManager.SaveOpenScenes();
        EditorApplication.Exit(0);
    }

    public static void Setup()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        bool openedTempScene = false;
        if (scene.path != ScenePath)
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("SetupMainMenuButtons: scene not found at " + ScenePath);
                return;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            openedTempScene = true;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("SetupMainMenuButtons: Canvas not found.");
            return;
        }

        Transform mainMenuTransform = canvas.transform.Find("MainMenu");
        if (mainMenuTransform == null)
        {
            Debug.LogError("SetupMainMenuButtons: MainMenu object not found under Canvas.");
            return;
        }

        GameObject mainMenuRoot = mainMenuTransform.gameObject;
        RemoveLegacyButtons(mainMenuRoot.transform);

        VerticalLayoutGroup layout = mainMenuRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = mainMenuRoot.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        RectTransform menuRect = mainMenuRoot.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = new Vector2(0f, -40f);
        menuRect.sizeDelta = new Vector2(560f, 720f);

        for (int i = 0; i < Buttons.Length; i++)
        {
            (string name, string spritePath) = Buttons[i];
            Sprite sprite = LoadSprite(spritePath);
            CreateOrUpdateButton(mainMenuRoot.transform, name, sprite);
        }

        EnsureOverlayPanel(canvas.transform, "Settings", "Settings", "Audio and controls coming soon.");
        EnsureHowToPanel(canvas.transform);

        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenu != null)
        {
            SerializedObject serialized = new SerializedObject(mainMenu);
            serialized.FindProperty("mainMenuRoot").objectReferenceValue = mainMenuRoot;

            Transform levelsPanel = canvas.transform.Find("Levels Panel");
            if (levelsPanel != null)
                serialized.FindProperty("levelsPanel").objectReferenceValue = levelsPanel.gameObject;

            Transform howTo = canvas.transform.Find("HowTo");
            if (howTo != null)
                serialized.FindProperty("howToPanel").objectReferenceValue = howTo.gameObject;

            Transform settings = canvas.transform.Find("Settings");
            if (settings != null)
                serialized.FindProperty("settingsPanel").objectReferenceValue = settings.gameObject;

            Transform storeButton = mainMenuRoot.transform.Find("Store");
            if (storeButton != null && storeButton.TryGetComponent(out Button button))
                serialized.FindProperty("storeButton").objectReferenceValue = button;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mainMenu);
        }

        EditorUtility.SetDirty(mainMenuRoot);
        EditorSceneManager.MarkSceneDirty(scene);

        if (openedTempScene)
            EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveLegacyButtons(Transform mainMenuRoot)
    {
        string[] legacyNames = { "Quit" };
        foreach (string legacyName in legacyNames)
        {
            Transform legacy = mainMenuRoot.Find(legacyName);
            if (legacy != null)
                Object.DestroyImmediate(legacy.gameObject);
        }

        Transform store = mainMenuRoot.Find("Store");
        if (store != null)
        {
            for (int i = store.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(store.GetChild(i).gameObject);
        }
    }

    private static Sprite LoadSprite(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void CreateOrUpdateButton(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject;
        if (existing == null)
        {
            buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = parent.gameObject.layer;
        }
        else
        {
            buttonObject = existing.gameObject;
            if (buttonObject.GetComponent<Image>() == null)
                buttonObject.AddComponent<Image>();
            if (buttonObject.GetComponent<Button>() == null)
                buttonObject.AddComponent<Button>();
        }

        for (int i = buttonObject.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(buttonObject.transform.GetChild(i).gameObject);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = buttonObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = 520f;
        layoutElement.preferredHeight = 120f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 120f);
    }

    private static void EnsureHowToPanel(Transform canvas)
    {
        Transform howTo = canvas.Find("HowTo");
        if (howTo == null)
            return;

        Button backButton = FindButton(howTo, "cancle", "Back", "Close");
        if (backButton != null)
            WireBackButton(backButton, "CloseHowToPlay");
    }

    private static void EnsureOverlayPanel(Transform canvas, string panelName, string title, string body)
    {
        Transform panelTransform = canvas.Find(panelName);
        GameObject panelObject;
        if (panelTransform == null)
        {
            panelObject = new GameObject(panelName, typeof(RectTransform));
            panelObject.layer = canvas.gameObject.layer;
            panelObject.transform.SetParent(canvas, false);
            panelObject.SetActive(false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }
        else
        {
            panelObject = panelTransform.gameObject;
        }

        if (panelObject.transform.Find("Panel") == null)
        {
            GameObject backdrop = CreateImageChild(panelObject.transform, "Panel", null);
            RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            Image backdropImage = backdrop.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.72f);
        }

        if (panelObject.transform.Find("Title") == null)
        {
            GameObject titleObject = CreateTextChild(panelObject.transform, "Title", title, 48);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 180f);
            titleRect.sizeDelta = new Vector2(800f, 80f);
        }

        if (panelObject.transform.Find("Body") == null)
        {
            GameObject bodyObject = CreateTextChild(panelObject.transform, "Body", body, 32);
            RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, 40f);
            bodyRect.sizeDelta = new Vector2(800f, 160f);
        }

        if (panelObject.transform.Find("Back") == null)
        {
            GameObject backObject = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backObject.layer = panelObject.layer;
            backObject.transform.SetParent(panelObject.transform, false);

            RectTransform backRect = backObject.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.5f);
            backRect.anchorMax = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(0f, -180f);
            backRect.sizeDelta = new Vector2(220f, 72f);

            Image backImage = backObject.GetComponent<Image>();
            backImage.color = new Color(0.2f, 0.45f, 0.85f, 1f);

            Button backButton = backObject.GetComponent<Button>();
            backButton.targetGraphic = backImage;
            CreateTextChild(backObject.transform, "Label", "Back", 28);
            WireBackButton(backButton, "Close" + panelName);
        }
        else
        {
            Button backButton = panelObject.transform.Find("Back").GetComponent<Button>();
            WireBackButton(backButton, "Close" + panelName);
        }
    }

    private static GameObject CreateImageChild(Transform parent, string name, Sprite sprite)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = true;
        return child;
    }

    private static GameObject CreateTextChild(Transform parent, string name, string text, int fontSize)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = child.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        return child;
    }

    private static Button FindButton(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Transform child = root.Find(name);
            if (child != null && child.TryGetComponent(out Button button))
                return button;
        }

        return null;
    }

    private static void WireBackButton(Button button, string methodName)
    {
        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenu == null || button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            var method = typeof(MainMenu).GetMethod(methodName);
            method?.Invoke(mainMenu, null);
        });
    }
}
#endif
