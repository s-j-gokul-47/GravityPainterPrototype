using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a connected tile path from a seed using a biased backtracking random walk.
/// Pure data logic — no scene objects or instantiation.
/// </summary>
public class ProceduralPathGenerator
{
    private static readonly Vector2Int Right = new Vector2Int(1, 0);
    private static readonly Vector2Int Up = new Vector2Int(0, 1);
    private static readonly Vector2Int Left = new Vector2Int(-1, 0);
    private static readonly Vector2Int Down = new Vector2Int(0, -1);

    private const int MaxRetryAttempts = 50;

    /// <summary>
    /// Builds a path, retrying with seed + 1 when layout validation fails.
    /// Falls back to a guaranteed snake path if all retries fail.
    /// </summary>
    public List<LevelCell> GenerateWithRetry(LevelGenConfig config, int seed, int maxAttempts = MaxRetryAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            List<LevelCell> cells = Generate(config, seed + attempt);
            if (cells != null)
            {
                return cells;
            }
        }

        Debug.LogWarning(
            "Biased walk failed after " + maxAttempts + " seeds starting at " + seed +
            ". Using fallback snake path.");
        return GenerateFallbackPath(config, seed);
    }

    /// <summary>
    /// Builds a reproducible list of <see cref="LevelCell"/> values for the given config and seed.
    /// Returns null when the path could not reach target length or fails journey validation.
    /// </summary>
    public List<LevelCell> Generate(LevelGenConfig config, int seed)
    {
        Random.InitState(seed);

        int pathLength = Random.Range(config.minPathLength, config.maxPathLength + 1);
        var path = new List<Vector2Int>(pathLength);
        var used = new HashSet<Vector2Int>();

        Vector2Int current = Vector2Int.zero;
        path.Add(current);
        used.Add(current);

        Vector2Int preferredDir = GetPreferredDirection(seed);
        int straightSteps = 0;
        Vector2Int lastDir = preferredDir;

        int maxAttempts = pathLength * 100;
        int attempts = 0;

        while (path.Count < pathLength && attempts < maxAttempts)
        {
            attempts++;

            bool relaxBackwardBlock = attempts > pathLength * 20;
            Vector2Int[] directions = GetBiasedDirections(preferredDir, straightSteps);
            bool moved = false;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int direction = directions[i];
                Vector2Int next = current + direction;

                if (used.Contains(next))
                {
                    continue;
                }

                if (!IsWithinBounds(next, config))
                {
                    continue;
                }

                if (!relaxBackwardBlock
                    && path.Count < pathLength / 2
                    && GoesBackward(next, current, preferredDir))
                {
                    continue;
                }

                path.Add(next);
                used.Add(next);
                straightSteps = direction == lastDir ? straightSteps + 1 : 1;
                lastDir = direction;
                current = next;
                moved = true;
                break;
            }

            if (!moved && path.Count > 1)
            {
                used.Remove(current);
                path.RemoveAt(path.Count - 1);
                current = path[path.Count - 1];
                straightSteps = 0;
            }
        }

        if (path.Count < config.minPathLength)
        {
            return null;
        }

        if (!MeetsMinimumJourney(path, config))
        {
            return null;
        }

        return BuildCells(path);
    }

    private static bool IsWithinBounds(Vector2Int gridPos, LevelGenConfig config)
    {
        return Mathf.Abs(gridPos.x) <= config.gridWidth / 2
            && Mathf.Abs(gridPos.y) <= config.gridDepth / 2;
    }

    private static bool MeetsMinimumJourney(List<Vector2Int> path, LevelGenConfig config)
    {
        Vector2Int start = path[0];
        Vector2Int finish = path[path.Count - 1];
        int manhattan = Mathf.Abs(finish.x - start.x) + Mathf.Abs(finish.y - start.y);
        int minManhattan = Mathf.Max(3, config.minPathLength / 3);
        return manhattan >= minManhattan;
    }

    private static Vector2Int GetPreferredDirection(int seed)
    {
        int choice = Mathf.Abs(seed) % 3;
        switch (choice)
        {
            case 0:
                return Up;
            case 1:
                return Right;
            case 2:
                return Left;
            default:
                return Up;
        }
    }

    private static Vector2Int[] GetBiasedDirections(Vector2Int preferred, int straightSteps)
    {
        var directions = new List<Vector2Int>(4) { Right, Up, Left, Down };

        for (int i = directions.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Vector2Int temp = directions[i];
            directions[i] = directions[swapIndex];
            directions[swapIndex] = temp;
        }

        directions.Remove(preferred);

        if (straightSteps > 3)
        {
            directions.Add(preferred);
        }
        else if (Random.value < 0.65f)
        {
            directions.Insert(0, preferred);
        }
        else
        {
            int insertAt = Random.Range(0, directions.Count + 1);
            directions.Insert(insertAt, preferred);
        }

        return directions.ToArray();
    }

    private static bool GoesBackward(Vector2Int next, Vector2Int current, Vector2Int preferred)
    {
        Vector2Int delta = next - current;
        int dot = delta.x * preferred.x + delta.y * preferred.y;
        return dot < 0;
    }

    private List<LevelCell> GenerateFallbackPath(LevelGenConfig config, int seed)
    {
        Random.InitState(seed);

        int pathLength = Random.Range(config.minPathLength, config.maxPathLength + 1);
        var path = new List<Vector2Int>(pathLength);
        var used = new HashSet<Vector2Int>();

        Vector2Int current = Vector2Int.zero;
        path.Add(current);
        used.Add(current);

        Vector2Int preferred = GetPreferredDirection(seed);
        Vector2Int turn = new Vector2Int(preferred.y, -preferred.x);
        Vector2Int dir = preferred;

        while (path.Count < pathLength)
        {
            Vector2Int next = current + dir;
            if (!used.Contains(next) && IsWithinBounds(next, config))
            {
                path.Add(next);
                used.Add(next);
                current = next;
                continue;
            }

            Vector2Int alt = dir == preferred ? turn : preferred;
            next = current + alt;
            if (!used.Contains(next) && IsWithinBounds(next, config))
            {
                path.Add(next);
                used.Add(next);
                current = next;
                dir = alt;
                continue;
            }

            break;
        }

        if (path.Count < config.minPathLength)
        {
            path = BuildSimpleSnake(config.minPathLength, config);
        }

        return BuildCells(path);
    }

    private static List<Vector2Int> BuildSimpleSnake(int length, LevelGenConfig config)
    {
        var path = new List<Vector2Int>(length);
        var used = new HashSet<Vector2Int>();
        Vector2Int current = Vector2Int.zero;
        path.Add(current);
        used.Add(current);

        Vector2Int[] dirs = { Right, Up, Left, Down };
        int dirIndex = 0;

        while (path.Count < length)
        {
            bool moved = false;
            for (int attempt = 0; attempt < dirs.Length; attempt++)
            {
                Vector2Int next = current + dirs[(dirIndex + attempt) % dirs.Length];
                if (used.Contains(next) || !IsWithinBounds(next, config))
                {
                    continue;
                }

                path.Add(next);
                used.Add(next);
                current = next;
                dirIndex = (dirIndex + attempt) % dirs.Length;
                moved = true;
                break;
            }

            if (!moved)
            {
                break;
            }
        }

        return path;
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
