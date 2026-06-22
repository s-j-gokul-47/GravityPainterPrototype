using UnityEngine;

/// <summary>
/// Converts glTF materials to URP while keeping imported model colors and textures.
/// </summary>
public static class TileMeshMaterialUtility
{
    private static Shader _urpLit;

    public static void FixRenderersToUrpPreservingModelLook(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            FixRendererToUrpPreservingModelLook(renderer);
        }
    }

    public static void FixRendererToUrpPreservingModelLook(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material[] source = renderer.sharedMaterials;
        Material[] converted = new Material[source.Length];
        bool changed = false;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
            {
                converted[i] = CreateUrpMaterialPreservingSource(null);
                changed = true;
                continue;
            }

            if (MaterialUsesUrp(source[i]))
            {
                converted[i] = source[i];
                continue;
            }

            converted[i] = CreateUrpMaterialPreservingSource(source[i]);
            changed = true;
        }

        if (changed)
        {
            renderer.sharedMaterials = converted;
        }
    }

    public static Material CreateUrpMaterialPreservingSource(Material source)
    {
        Shader urpLit = GetUrpLit();
        Material result = new Material(urpLit);

        if (source == null)
        {
            return result;
        }

        if (result.HasProperty("_BaseColor"))
        {
            result.SetColor("_BaseColor", ExtractBaseColor(source));
        }

        Texture albedo = ExtractAlbedoTexture(source);
        if (albedo != null && result.HasProperty("_BaseMap"))
        {
            result.SetTexture("_BaseMap", albedo);
        }

        return result;
    }

    private static bool MaterialUsesUrp(Material mat)
    {
        if (mat == null || mat.shader == null)
        {
            return false;
        }

        string name = mat.shader.name;
        if (name.Contains("Hidden") || name.Contains("Error"))
        {
            return false;
        }

        return name.Contains("Universal Render Pipeline");
    }

    private static Shader GetUrpLit()
    {
        if (_urpLit == null)
        {
            _urpLit = Shader.Find("Universal Render Pipeline/Lit");
        }

        return _urpLit;
    }

    private static Color ExtractBaseColor(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            return mat.GetColor("_BaseColor");
        }

        if (mat.HasProperty("_Color"))
        {
            return mat.GetColor("_Color");
        }

        return mat.color;
    }

    private static Texture ExtractAlbedoTexture(Material mat)
    {
        if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
        {
            return mat.GetTexture("_BaseMap");
        }

        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
        {
            return mat.GetTexture("_MainTex");
        }

        if (mat.HasProperty("baseColorTexture") && mat.GetTexture("baseColorTexture") != null)
        {
            return mat.GetTexture("baseColorTexture");
        }

        return mat.mainTexture;
    }
}
