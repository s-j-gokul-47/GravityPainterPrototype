using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Edge-aligned placement for procedural tiles. Rectangular GLB tiles collide at turns
/// when centers share grid corners, so each step advances by half-extents plus a
/// perpendicular offset when the path changes direction.
/// </summary>
public static class ProceduralTilePlacement
{
    public static Vector3 ComputeCenterPosition(int index, IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        if (index <= 0 || cells == null || cells.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 position = Vector3.zero;
        for (int i = 1; i <= index; i++)
        {
            position += ComputeStepOffset(i, cells, config);
        }

        return position;
    }

    public static void ApplyPathTransform(
        Transform tile,
        int index,
        IReadOnlyList<LevelCell> cells,
        LevelGenConfig config)
    {
        tile.localPosition = ComputeCenterPosition(index, cells, config);
        tile.localRotation = Quaternion.Euler(0f, cells[index].YRotation, 0f);
        tile.localScale = config.tileLocalScale;
    }

    public static bool IsTurnIndex(int index, IReadOnlyList<LevelCell> cells)
    {
        if (cells == null || index < 2 || index >= cells.Count)
        {
            return false;
        }

        Vector2Int step = cells[index].GridPos - cells[index - 1].GridPos;
        Vector2Int previousStep = cells[index - 1].GridPos - cells[index - 2].GridPos;
        return step != previousStep;
    }

    public static int CountTurns(IReadOnlyList<LevelCell> cells)
    {
        int count = 0;
        if (cells == null)
        {
            return count;
        }

        for (int i = 2; i < cells.Count; i++)
        {
            if (IsTurnIndex(i, cells))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Extra tiles at a turn: same orientation as the incoming (forward) tile, each placed
    /// one more step along that direction so the corner gap is bridged without clipping.
    /// </summary>
    public static Vector3 ComputeCornerPadPosition(
        int turnIndex,
        int padIndex,
        IReadOnlyList<LevelCell> cells,
        LevelGenConfig config)
    {
        Vector2Int previousLeg = cells[turnIndex - 1].GridPos - cells[turnIndex - 2].GridPos;
        LevelCell forwardTile = cells[turnIndex - 1];
        float halfForward = GetHalfExtentAlong(forwardTile.YRotation, previousLeg, config.tileLocalScale);
        float halfPad = halfForward;

        Vector3 position = ComputeCenterPosition(turnIndex - 1, cells, config);
        for (int k = 0; k <= padIndex; k++)
        {
            float halfFrom = k == 0 ? halfForward : halfPad;
            position += GridStepToWorld(previousLeg) * (halfFrom + halfPad + config.tileGap);
        }

        return position;
    }

    public static void ApplyCornerPadTransform(
        Transform tile,
        int turnIndex,
        int padIndex,
        IReadOnlyList<LevelCell> cells,
        LevelGenConfig config)
    {
        LevelCell forwardTile = cells[turnIndex - 1];
        tile.localPosition = ComputeCornerPadPosition(turnIndex, padIndex, cells, config);
        tile.localRotation = Quaternion.Euler(0f, forwardTile.YRotation, 0f);
        tile.localScale = config.tileLocalScale;
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

    private static Vector3 ComputeStepOffset(int index, IReadOnlyList<LevelCell> cells, LevelGenConfig config)
    {
        Vector2Int step = cells[index].GridPos - cells[index - 1].GridPos;
        LevelCell previous = cells[index - 1];
        LevelCell current = cells[index];

        float halfPrevious = GetHalfExtentAlong(previous.YRotation, step, config.tileLocalScale);
        float halfCurrent = GetHalfExtentAlong(current.YRotation, step, config.tileLocalScale);
        Vector3 delta = GridStepToWorld(step) * (halfPrevious + halfCurrent + config.tileGap);

        if (index >= 2)
        {
            Vector2Int previousStep = cells[index - 1].GridPos - cells[index - 2].GridPos;
            if (previousStep != step)
            {
                float exitAlongPrevious = GetHalfExtentAlong(previous.YRotation, previousStep, config.tileLocalScale);
                float enterAlongPrevious = GetHalfExtentAlong(current.YRotation, previousStep, config.tileLocalScale);
                delta += GridStepToWorld(previousStep) * (exitAlongPrevious + enterAlongPrevious);
            }
        }

        return delta;
    }

    private static Vector3 GridStepToWorld(Vector2Int gridStep)
    {
        return new Vector3(gridStep.x, 0f, gridStep.y);
    }

    private static float GetHalfExtentAlong(float yRotationDegrees, Vector2Int gridStep, Vector3 tileScale)
    {
        if (gridStep == Vector2Int.zero)
        {
            return 0f;
        }

        Vector3 worldDirection = GridStepToWorld(gridStep).normalized;
        Quaternion rotation = Quaternion.Euler(0f, yRotationDegrees, 0f);

        Vector3 right = rotation * Vector3.right * (tileScale.x * 0.5f);
        Vector3 up = rotation * Vector3.up * (tileScale.y * 0.5f);
        Vector3 forward = rotation * Vector3.forward * (tileScale.z * 0.5f);

        return Mathf.Abs(Vector3.Dot(right, worldDirection))
            + Mathf.Abs(Vector3.Dot(up, worldDirection))
            + Mathf.Abs(Vector3.Dot(forward, worldDirection));
    }
}
