using UnityEngine;

/// <summary>
/// Canonical grid placement for procedural tiles. Level 2 scene tiles use a much
/// larger baked scale; spawned tiles must reset to this footprint or they overlap.
/// </summary>
public static class ProceduralTilePlacement
{
    public static readonly Vector3 CanonicalLocalScale = new Vector3(1f, 0.1f, 1f);

    public static Vector3 GridToLocalPosition(Vector2Int gridPos, LevelGenConfig config)
    {
        return new Vector3(
            gridPos.x * config.tileSpacing,
            0f,
            gridPos.y * config.tileSpacing);
    }

    public static void ApplyGridTransform(Transform tile, LevelCell cell, LevelGenConfig config)
    {
        tile.localPosition = GridToLocalPosition(cell.GridPos, config);
        tile.localRotation = Quaternion.Euler(0f, cell.YRotation, 0f);
        tile.localScale = CanonicalLocalScale;
    }

    public static void ApplyGridTransform(Transform tile, Vector2Int gridPos, float yRotation, LevelGenConfig config)
    {
        tile.localPosition = GridToLocalPosition(gridPos, config);
        tile.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        tile.localScale = CanonicalLocalScale;
    }

    /// <summary>
    /// Hides edit-mode path previews so they do not stack on runtime builds.
    /// </summary>
    public static void HidePathPreview()
    {
        ProceduralPathVisualizer[] visualizers = Object.FindObjectsByType<ProceduralPathVisualizer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < visualizers.Length; i++)
        {
            ProceduralPathVisualizer visualizer = visualizers[i];
            if (visualizer == null)
            {
                continue;
            }

            Transform parent = visualizer.parent != null ? visualizer.parent : visualizer.transform;
            for (int c = parent.childCount - 1; c >= 0; c--)
            {
                Object.Destroy(parent.GetChild(c).gameObject);
            }

            visualizer.gameObject.SetActive(false);
        }
    }
}
