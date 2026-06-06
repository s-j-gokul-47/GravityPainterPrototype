using UnityEngine;

/// <summary>
/// Designer-tunable parameters for procedural level generation.
/// </summary>
[CreateAssetMenu(fileName = "LevelGenConfig", menuName = "Gravity Painter/Level Gen Config")]
public class LevelGenConfig : ScriptableObject
{
    [Header("Path Settings")]
    [Tooltip("Minimum number of connected tiles in the generated path.")]
    public int minPathLength = 8;

    [Tooltip("Maximum number of connected tiles in the generated path.")]
    public int maxPathLength = 16;

    [Tooltip("Maximum grid spread along the X axis (centered on origin).")]
    public int gridWidth = 7;

    [Tooltip("Maximum grid spread along the Z axis (centered on origin).")]
    public int gridDepth = 7;

    [Tooltip("World-space spacing between tile centers.")]
    public float tileSpacing = 1.05f;

    [Header("Prefab References")]
    [Tooltip("Tile prefab instantiated by the level builder.")]
    public GameObject tilePrefab;

    [Tooltip("Optional finish-line prefab (FinishLine is added to the last tile if empty).")]
    public GameObject finishLinePrefab;

    [Header("Visual Layout")]
    [Tooltip("Reference GLB layout applied to spawned tiles.")]
    public TileGlbReferenceLayoutAsset glbLayout;
}
