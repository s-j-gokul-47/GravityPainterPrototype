using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a connected tile path from a seed using a backtracking random walk.
/// Pure data logic — no scene objects or instantiation.
/// </summary>
public class ProceduralPathGenerator
{
    private static readonly Vector2Int Right = new Vector2Int(1, 0);
    private static readonly Vector2Int Up = new Vector2Int(0, 1);
    private static readonly Vector2Int Left = new Vector2Int(-1, 0);
    private static readonly Vector2Int Down = new Vector2Int(0, -1);

    /// <summary>
    /// Builds a reproducible list of <see cref="LevelCell"/> values for the given config and seed.
    /// </summary>
    /// <param name="config">Path length and grid bounds.</param>
    /// <param name="seed">RNG seed; the same seed always produces the same path.</param>
    /// <returns>Ordered path from start (index 0) to finish (last index).</returns>
    public List<LevelCell> Generate(LevelGenConfig config, int seed)
    {
        Random.InitState(seed);

        int pathLength = Random.Range(config.minPathLength, config.maxPathLength + 1);
        var path = new List<Vector2Int>(pathLength);
        var used = new HashSet<Vector2Int>();

        Vector2Int current = Vector2Int.zero;
        path.Add(current);
        used.Add(current);

        Vector2Int[] directions = { Right, Up, Left, Down };
        int maxAttempts = pathLength * 20;
        int attempts = 0;

        while (path.Count < pathLength && attempts < maxAttempts)
        {
            attempts++;
            ShuffleDirections(directions);

            bool moved = false;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (used.Contains(next))
                {
                    continue;
                }

                if (!IsWithinBounds(next, config))
                {
                    continue;
                }

                path.Add(next);
                used.Add(next);
                current = next;
                moved = true;
                break;
            }

            if (!moved && path.Count > 1)
            {
                used.Remove(current);
                path.RemoveAt(path.Count - 1);
                current = path[path.Count - 1];
            }
        }

        return BuildCells(path);
    }

    private static bool IsWithinBounds(Vector2Int gridPos, LevelGenConfig config)
    {
        return Mathf.Abs(gridPos.x) <= config.gridWidth / 2
            && Mathf.Abs(gridPos.y) <= config.gridDepth / 2;
    }

    private static void ShuffleDirections(Vector2Int[] directions)
    {
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Vector2Int temp = directions[i];
            directions[i] = directions[swapIndex];
            directions[swapIndex] = temp;
        }
    }

    private static List<LevelCell> BuildCells(List<Vector2Int> path)
    {
        var cells = new List<LevelCell>(path.Count);
        float lastRotation = 0f;

        for (int i = 0; i < path.Count; i++)
        {
            float yRotation = lastRotation;
            if (i < path.Count - 1)
            {
                Vector2Int direction = path[i + 1] - path[i];
                yRotation = DirectionToYRotation(direction);
                lastRotation = yRotation;
            }

            cells.Add(new LevelCell
            {
                GridPos = path[i],
                PathIndex = i,
                IsMainPath = true,
                Obstacle = ObstacleType.None,
                PresetZone = PaintZone.None,
                YRotation = yRotation
            });
        }

        return cells;
    }

    private static float DirectionToYRotation(Vector2Int direction)
    {
        if (direction == Right)
        {
            return 90f;
        }

        if (direction == Up)
        {
            return 0f;
        }

        if (direction == Left)
        {
            return 270f;
        }

        if (direction == Down)
        {
            return 180f;
        }

        return 0f;
    }
}
