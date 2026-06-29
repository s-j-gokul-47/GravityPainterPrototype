using UnityEngine;

/// <summary>
/// Designer-tunable parameters for procedural level generation.
/// </summary>
[CreateAssetMenu(fileName = "LevelGenConfig", menuName = "Gravity Painter/Level Gen Config")]
public class LevelGenConfig : ScriptableObject
{
    [Header("Path Settings")]
    [Tooltip("Runtime min path length (set by DifficultyScaler before generation).")]
    public int minPathLength = 8;

    [Tooltip("Runtime max path length (set by DifficultyScaler before generation).")]
    public int maxPathLength = 16;

    [Tooltip("Runtime grid spread along X (set by DifficultyScaler).")]
    public int gridWidth = 7;

    [Tooltip("Runtime grid spread along Z (set by DifficultyScaler).")]
    public int gridDepth = 7;

    [Header("Difficulty Progression")]
    [Tooltip("Manual difficulty 0–1 when progression is disabled on the level builder.")]
    [Range(0f, 1f)]
    public float difficulty = 0f;

    [Tooltip("Shortest path at difficulty 0.")]
    public int absoluteMinPath = 8;

    [Tooltip("Longest path at difficulty 1.")]
    public int absoluteMaxPath = 30;

    [Tooltip("Probability of turning per step at Expert (0.1 = mostly straight at Easy).")]
    [Range(0f, 1f)]
    public float turnFrequency = 0.2f;

    [Tooltip("Minimum start-to-finish grid distance (set by DifficultyScaler).")]
    public float minFinishDistance = 2f;

    [Tooltip("Controls how quickly obstacle slots fill as difficulty rises.")]
    public AnimationCurve obstacleDensityCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.2f, 0f),
        new Keyframe(0.25f, 0.25f),
        new Keyframe(0.5f, 0.6f),
        new Keyframe(0.75f, 0.85f),
        new Keyframe(1f, 1f));

    [Tooltip("Runtime cap on how many obstacles a procedural level may spawn.")]
    public int maxObstaclesPerLevel = 0;

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
