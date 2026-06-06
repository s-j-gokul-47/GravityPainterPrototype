#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralLevelBuilder))]
public class ProceduralLevelBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        var builder = (ProceduralLevelBuilder)target;
        SerializedObject so = new SerializedObject(builder);
        int seed = so.FindProperty("seed").intValue;

        if (GUILayout.Button("Build From Seed (Play Mode Only)", GUILayout.Height(28f)))
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode to build a runtime procedural level.");
                return;
            }

            builder.BuildFromSeed(seed);
        }

        if (GUILayout.Button("Clear Spawned Tiles (Play Mode Only)", GUILayout.Height(24f)))
        {
            if (Application.isPlaying)
            {
                builder.ClearLevel();
            }
        }

        EditorGUILayout.HelpBox(
            "Step 2: Press Play to auto-build. Paint tiles, roll the ball to the last tile.\n" +
            "Run Gravity Painter → Setup Procedural Level Scene if Ball or camera input is missing.",
            MessageType.Info);
    }
}
#endif
