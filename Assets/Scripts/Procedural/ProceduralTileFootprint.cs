using UnityEngine;

/// <summary>
/// Verified planar footprint for procedural tiles (root scale + optional GLB layout).
/// Used for spacing, overlap checks, and path validation.
/// </summary>
public static class ProceduralTileFootprint
{
    private const float OverlapBoundsPadding = 0.08f;

    /// <summary>Full center-to-center spacing for one grid step (longest tile axis + gap).</summary>
    public static float GetPathGridSpacing(LevelGenConfig config)
    {
        return GetLongestPlanarAxis(config) + config.tileGap;
    }

    /// <summary>Longest half-extent on the XZ plane for any 90° Y rotation.</summary>
    public static float GetMaxPlanarHalfExtent(LevelGenConfig config)
    {
        float max = 0f;
        for (int i = 0; i < 4; i++)
        {
            float rotation = i * 90f;
            max = Mathf.Max(max, GetPlanarHalfExtentAlong(rotation, Vector3.right, config));
            max = Mathf.Max(max, GetPlanarHalfExtentAlong(rotation, Vector3.forward, config));
        }

        return max;
    }

    public static float GetLongestPlanarAxis(LevelGenConfig config)
    {
        return GetMaxPlanarHalfExtent(config) * 2f;
    }

    public static float GetPlanarHalfExtentAlong(float yRotationDegrees, Vector3 worldDirection, LevelGenConfig config)
    {
        if (worldDirection.sqrMagnitude < 1e-6f)
        {
            return 0f;
        }

        worldDirection.Normalize();
        Quaternion rotation = Quaternion.Euler(0f, yRotationDegrees, 0f);
        Vector3 halfScale = config.tileLocalScale * 0.5f;

        Vector3 right = rotation * Vector3.right * halfScale.x;
        Vector3 up = rotation * Vector3.up * halfScale.y;
        Vector3 forward = rotation * Vector3.forward * halfScale.z;

        return Mathf.Abs(Vector3.Dot(right, worldDirection))
            + Mathf.Abs(Vector3.Dot(up, worldDirection))
            + Mathf.Abs(Vector3.Dot(forward, worldDirection));
    }

    public static Bounds ComputeWorldBounds(Vector3 center, float yRotationDegrees, LevelGenConfig config)
    {
        Quaternion rotation = Quaternion.Euler(0f, yRotationDegrees, 0f);
        Vector3 halfExtents = config.tileLocalScale * 0.5f;
        halfExtents += Vector3.one * OverlapBoundsPadding;
        Bounds bounds = new Bounds(center, Vector3.zero);

        for (int xSign = -1; xSign <= 1; xSign += 2)
        {
            for (int ySign = -1; ySign <= 1; ySign += 2)
            {
                for (int zSign = -1; zSign <= 1; zSign += 2)
                {
                    Vector3 localCorner = new Vector3(
                        halfExtents.x * xSign,
                        halfExtents.y * ySign,
                        halfExtents.z * zSign);
                    bounds.Encapsulate(center + rotation * localCorner);
                }
            }
        }

        return bounds;
    }

    public static bool BoundsOverlap(Bounds a, Bounds b, float extraMargin = 0f)
    {
        Vector3 minA = a.min - Vector3.one * extraMargin;
        Vector3 maxA = a.max + Vector3.one * extraMargin;
        Vector3 minB = b.min - Vector3.one * extraMargin;
        Vector3 maxB = b.max + Vector3.one * extraMargin;

        return minA.x <= maxB.x && maxA.x >= minB.x
            && minA.y <= maxB.y && maxA.y >= minB.y
            && minA.z <= maxB.z && maxA.z >= minB.z;
    }

    /// <summary>
    /// Fast grid check: tiles on the same row/column closer than this many cells will overlap in world space.
    /// </summary>
    public static int GetMinimumGridSeparation(LevelGenConfig config)
    {
        float spacing = GetPathGridSpacing(config);
        float minStep = Mathf.Min(
            GetPlanarHalfExtentAlong(0f, Vector3.right, config)
                + GetPlanarHalfExtentAlong(0f, Vector3.right, config)
                + config.tileGap,
            GetPlanarHalfExtentAlong(0f, Vector3.forward, config)
                + GetPlanarHalfExtentAlong(0f, Vector3.forward, config)
                + config.tileGap);

        if (minStep < 0.001f)
        {
            return 3;
        }

        return Mathf.Max(2, Mathf.CeilToInt(spacing / minStep));
    }
}
