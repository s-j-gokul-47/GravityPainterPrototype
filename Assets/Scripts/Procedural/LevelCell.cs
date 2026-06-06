using UnityEngine;

/// <summary>Obstacle placed on a procedural level tile.</summary>
public enum ObstacleType
{
    None,
    Hammer,
    Laser,
    MovingPlatform
}

/// <summary>Preset paint zone on a procedural level tile.</summary>
public enum PaintZone
{
    None,
    Blue,
    Red,
    Yellow
}

/// <summary>
/// One tile in a procedurally generated path.
/// Grid X maps to world right; grid Y maps to world forward (Z).
/// </summary>
public struct LevelCell
{
    /// <summary>Position on the 2D generation grid (x = right, y = forward/Z).</summary>
    public Vector2Int GridPos;

    /// <summary>Tile Y-axis rotation in degrees (0, 90, 180, or 270).</summary>
    public float YRotation;

    /// <summary>Index along the main path (0 = start, last = finish).</summary>
    public int PathIndex;

    /// <summary>True when this cell belongs to the primary route (always true in Step 1).</summary>
    public bool IsMainPath;

    /// <summary>Obstacle type for this tile (None until obstacle placement is added).</summary>
    public ObstacleType Obstacle;

    /// <summary>Preset zone color (None = player paints during play).</summary>
    public PaintZone PresetZone;
}
