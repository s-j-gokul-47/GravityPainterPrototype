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

    [Header("Tile Footprint")]
    [Tooltip("Local scale applied to spawned tiles (matches Level 2 GLB tiles).")]
    public Vector3 tileLocalScale = new Vector3(9.764219f, 0.20627001f, 4.5902023f);

    [Tooltip("Center-to-center spacing along world X when the path steps left/right.")]
    public float tileSpacingX = 9.764219f;

    [Tooltip("Center-to-center spacing along world Z when the path steps forward/back.")]
    public float tileSpacingZ = 4.85f;

    [Tooltip("Small gap added between edge-aligned tiles.")]
    public float tileGap = 0.05f;

    [Tooltip("Spawn extra filler tiles at every 90° turn so the ball cannot fall through the corner gap.")]
    public bool addCornerPads = true;

    [Tooltip("How many forward-aligned tiles to add before each turn.")]
    public int cornerPadTileCount = 2;

    [Header("Prefab References")]
    [Tooltip("Tile prefab instantiated by the level builder.")]
    public GameObject tilePrefab;

    [Tooltip("Optional finish-line prefab (FinishLine is added to the last tile if empty).")]
    public GameObject finishLinePrefab;

    [Header("Visual Layout")]
    [Tooltip("Reference GLB layout applied to spawned tiles.")]
    public TileGlbReferenceLayoutAsset glbLayout;
}
