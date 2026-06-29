#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

[CustomEditor(typeof(MainMenuVideoBackground))]
public class MainMenuVideoBackgroundEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        var background = (MainMenuVideoBackground)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Settings"))
            {
                background.ApplySettings();
                EditorUtility.SetDirty(background);
            }

            if (GUILayout.Button("Play"))
            {
                background.ApplySettings();
                background.Play();
            }

            if (GUILayout.Button("Stop"))
            {
                background.Stop();
            }
        }

        if (background.VideoClip != null)
        {
            EditorGUILayout.HelpBox(
                "Source: " + background.VideoClip.width + " x " + background.VideoClip.height
                + "  |  Duration: " + background.VideoClip.length.ToString("F1") + "s",
                MessageType.Info);
        }
    }
}
#endif
