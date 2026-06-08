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
        SerializedObject so = serializedObject;
        so.Update();
        int seed = so.FindProperty("seed").intValue;

        EditorGUILayout.LabelField("Last Built Seed", builder.LastBuiltSeed >= 0 ? builder.LastBuiltSeed.ToString() : "(none)");

        if (GUILayout.Button("Build From Seed", GUILayout.Height(28f)))
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first, then click Build From Seed.");
            }
            else
            {
                so.ApplyModifiedProperties();
                builder.BuildFromSeed(seed);
            }
        }

        if (GUILayout.Button("Next Random Seed", GUILayout.Height(24f)))
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first.");
            }
            else
            {
                builder.RebuildNextSeed();
                so.Update();
                Repaint();
            }
        }

        if (GUILayout.Button("Clear Spawned Tiles", GUILayout.Height(24f)))
        {
            if (Application.isPlaying)
            {
                builder.ClearLevel();
            }
        }

        EditorGUILayout.HelpBox(
            "Change Seed on ProceduralLevel (not PathTester) while playing — the level rebuilds automatically.\n" +
            "Or stop Play, change seed, press Play again.\n\n" +
            "Console shows: requested seed vs used seed (may differ if the layout needed a retry).",
            MessageType.Info);
    }
}
#endif
