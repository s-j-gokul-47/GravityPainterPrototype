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

    [Tooltip("Verified longest tile axis (auto-derived from tileLocalScale).")]
    public float tileSpacingX = 9.764219f;

    [Tooltip("Verified longest tile axis used for path grid clearance.")]
    public float tileSpacingZ = 9.814219f;

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

    /// <summary>Syncs spacing fields from tile scale (call from editor or before generation).</summary>
    public void SyncFootprintFromTileScale()
    {
        float longest = ProceduralTileFootprint.GetLongestPlanarAxis(this);
        tileSpacingX = longest;
        tileSpacingZ = longest;
    }

    private void OnValidate()
    {
        SyncFootprintFromTileScale();
    }
}
