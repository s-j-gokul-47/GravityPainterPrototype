#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainMenu))]
public class MainMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        var mainMenu = (MainMenu)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Button Layout"))
            {
                mainMenu.ApplyButtonLayout();
                EditorUtility.SetDirty(mainMenu);
            }

            if (GUILayout.Button("Reset To Defaults"))
            {
                Undo.RecordObject(mainMenu, "Reset Main Menu Button Layout");
                ResetDefaults(mainMenu);
                mainMenu.ApplyButtonLayout();
                EditorUtility.SetDirty(mainMenu);
            }
        }

        EditorGUILayout.HelpBox(
            "Adjust each button's Anchored Position and Size Delta above, then click Apply Button Layout. "
            + "Changes preview live while Use Manual Button Layout is enabled.",
            MessageType.Info);
    }

    private static void ResetDefaults(MainMenu mainMenu)
    {
        SerializedObject serialized = new SerializedObject(mainMenu);
        serialized.FindProperty("menuRootPosition").vector2Value = new Vector2(0f, -40f);
        serialized.FindProperty("menuRootSize").vector2Value = new Vector2(560f, 720f);
        serialized.FindProperty("useManualButtonLayout").boolValue = true;

        SetButtonLayout(serialized, "playButtonLayout", "Play", new Vector2(0f, 240f), new Vector2(520f, 120f));
        SetButtonLayout(serialized, "settingsButtonLayout", "Settings", new Vector2(0f, 120f), new Vector2(520f, 120f));
        SetButtonLayout(serialized, "storeButtonLayout", "Store", new Vector2(0f, 0f), new Vector2(520f, 120f));
        SetButtonLayout(serialized, "levelsButtonLayout", "Levels", new Vector2(0f, -120f), new Vector2(520f, 120f));
        SetButtonLayout(serialized, "howToPlayButtonLayout", "HowToPlay", new Vector2(0f, -240f), new Vector2(520f, 120f));
        serialized.ApplyModifiedProperties();
    }

    private static void SetButtonLayout(
        SerializedObject serialized,
        string propertyName,
        string buttonName,
        Vector2 position,
        Vector2 size)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.FindPropertyRelative("buttonName").stringValue = buttonName;
        property.FindPropertyRelative("anchoredPosition").vector2Value = position;
        property.FindPropertyRelative("sizeDelta").vector2Value = size;
    }
}
#endif
