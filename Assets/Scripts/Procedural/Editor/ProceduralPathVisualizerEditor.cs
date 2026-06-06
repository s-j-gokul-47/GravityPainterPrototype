#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralPathVisualizer))]
public class ProceduralPathVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        var visualizer = (ProceduralPathVisualizer)target;

        if (GUILayout.Button("Generate Visual Path", GUILayout.Height(28f)))
        {
            visualizer.GenerateVisualPath();
            EditorUtility.SetDirty(visualizer);
            if (visualizer.gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(visualizer.gameObject.scene);
            }
        }

        if (GUILayout.Button("Clear Spawned Tiles"))
        {
            Transform parent = visualizer.parent != null ? visualizer.parent : visualizer.transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
            }

            EditorUtility.SetDirty(visualizer);
            if (visualizer.gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(visualizer.gameObject.scene);
            }
        }
    }
}
#endif
