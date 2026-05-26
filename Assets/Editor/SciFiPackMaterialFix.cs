#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sci Fi Modular Pack materials use Built-in Standard; this project uses URP (pink = broken shader).
/// </summary>
public static class SciFiPackMaterialFix
{
    private const string PackMaterialsFolder = "Assets/ThirdParty/Sci Fi Modular Pack/Materials";

    [MenuItem("Gravity Painter/Fix Sci-Fi Pack Materials (URP Pink)")]
    public static void FixMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "URP shader missing",
                "Could not find \"Universal Render Pipeline/Lit\". Is URP installed?",
                "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { PackMaterialsFolder });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == urpLit)
            {
                continue;
            }

            Undo.RecordObject(mat, "Convert Sci-Fi material to URP");
            mat.shader = urpLit;

            if (mat.HasProperty("_BaseMap") && mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
            {
                mat.SetTexture("_BaseMap", mat.GetTexture("_MainTex"));
            }

            if (mat.HasProperty("_BaseColor") && mat.HasProperty("_Color"))
            {
                mat.SetColor("_BaseColor", mat.GetColor("_Color"));
            }

            if (mat.HasProperty("_MetallicGlossMap") && mat.GetTexture("_MetallicGlossMap") != null)
            {
                mat.SetTexture("_MetallicGlossMap", mat.GetTexture("_MetallicGlossMap"));
            }

            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Sci-Fi materials fixed",
            $"Updated {fixedCount} materials in\n{PackMaterialsFolder}\n\nto URP/Lit.",
            "OK");

        Debug.Log($"Sci-Fi pack: converted {fixedCount} materials to URP/Lit.");
    }
}
#endif
