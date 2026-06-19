#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CoinAppearance))]
public class CoinAppearanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CoinAppearance appearance = (CoinAppearance)target;

        if (appearance.IsMaster)
        {
            EditorGUILayout.HelpBox(
                "Master coin: edit CoinVisualRoot / CoinVisual in the Hierarchy, then click Publish below "
                + "(or use Gravity Painter → Publish Master Coin To All Coins).",
                MessageType.Info);

            if (GUILayout.Button("Publish To All Coins"))
            {
                appearance.PublishFromHierarchy();
            }
        }

        DrawDefaultInspector();
    }
}
#endif
