#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpikeTrap))]
public class SpikeTrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpikeTrap trap = (SpikeTrap)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Phase", trap.CurrentPhaseName);
        EditorGUILayout.LabelField("Extension", trap.ExtensionProgress.ToString("P0"));
        EditorGUILayout.LabelField("Dangerous", trap.IsDangerous ? "Yes" : "No");

        if (GUILayout.Button("Restart Cycle"))
        {
            trap.RestartCycle();
            EditorUtility.SetDirty(trap);
        }

        if (GUILayout.Button("Fix Duplicate Spike Visual Roots"))
        {
            SpikeTrapSync.FixAllTrapsInOpenScene();
            trap.RestartCycle();
            EditorUtility.SetDirty(trap);
        }

        if (trap.IsMaster)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This is the master spike. Edit size and timing here, then publish to update every spike.",
                MessageType.Info);

            if (GUILayout.Button("Publish Master Spike To All Spikes"))
            {
                trap.PublishFromHierarchy();
                EditorUtility.DisplayDialog(
                    "Spikes updated",
                    "Published the master spike settings to all spikes and the Spikes prefab.",
                    "OK");
            }
        }
    }
}
#endif
