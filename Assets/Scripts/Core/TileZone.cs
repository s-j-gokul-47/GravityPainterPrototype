using UnityEngine;

public enum ZoneType { None, Red, Blue, Yellow }

public class TileZone : MonoBehaviour
{
    public ZoneType zoneType = ZoneType.None;

    [Tooltip("Off (default): directions follow this tile’s yaw on the floor (see Scene gizmo when selected).\nOn: fixed world axes (+Z / −X / +X) — only for an unrotated grid.")]
    [SerializeField] private bool useWorldSpaceDirections = false;

    [Tooltip("Draw planar force arrows in Scene view when this object is selected.")]
    [SerializeField] private bool drawDirectionGizmos = true;

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material noneMat;

    [Tooltip("When Floor5Visual exists: keep floor texture and show colored edge strips for red/blue/yellow.")]
    [SerializeField] private bool useEdgeIndicatorsWithFloorMesh = true;

    [Tooltip("Tap on the left/right third of the tile (along its width). Center third sets forward (red).")]
    [SerializeField] [Range(0.2f, 0.45f)] private float sideRegionThreshold = 0.33f;

    private Renderer tileRenderer;
    private Renderer floorRenderer;
    private Material floorBaseMaterial;
    private TileZoneIndicator zoneIndicator;

    private void Awake()
    {
        ResolveRenderer();
        ResolveFloorVisual();

        if (HasAllMaterials())
        {
            SyncZoneTypeFromPrimary();
            UpdateVisual();
            return;
        }

        foreach (TileZone other in GetComponents<TileZone>())
        {
            if (other == this)
            {
                continue;
            }

            if (other.HasAllMaterials())
            {
                Destroy(this);
                return;
            }
        }

        SyncZoneTypeFromPrimary();
        UpdateVisual();
    }

    private bool HasAllMaterials()
    {
        return redMat != null && blueMat != null && yellowMat != null && noneMat != null;
    }

    public static TileZone GetPrimaryZone(GameObject go)
    {
        if (go == null)
        {
            return null;
        }

        TileZone[] zones = go.GetComponents<TileZone>();
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].HasAllMaterials())
            {
                return zones[i];
            }
        }

        return zones.Length > 0 ? zones[0] : null;
    }

    private void Start()
    {
        ResolveRenderer();
        ResolveFloorVisual();

        SyncZoneTypeFromPrimary();
        UpdateVisual();
    }

    private void SyncZoneTypeFromPrimary()
    {
        TileZone primary = GetPrimaryZone(gameObject);
        if (primary == null)
        {
            return;
        }

        ZoneType t = primary.zoneType;
        foreach (TileZone z in GetComponents<TileZone>())
        {
            z.zoneType = t;
        }
    }

    /// <summary>
    /// Sets the zone from where the player tapped on the tile surface:
    /// left third → left (blue), center third → forward (red), right third → right (yellow).
    /// Tapping the same region again clears the tile (None).
    /// </summary>
    public void SetZoneFromWorldPoint(Vector3 worldPoint)
    {
        ZoneType tapped = GetZoneFromTap(worldPoint);
        TileZone primary = GetPrimaryZone(gameObject);
        ZoneType current = primary != null ? primary.zoneType : zoneType;

        if (tapped == current)
        {
            ApplyZoneType(ZoneType.None);
        }
        else
        {
            ApplyZoneType(tapped);
        }
    }

    private ZoneType GetZoneFromTap(Vector3 worldPoint)
    {
        if (!TryGetNormalizedTapAlongWidth(worldPoint, out float normalizedAlongRight))
        {
            return ZoneType.Red;
        }

        if (normalizedAlongRight < -sideRegionThreshold)
        {
            return ZoneType.Blue;
        }

        if (normalizedAlongRight > sideRegionThreshold)
        {
            return ZoneType.Yellow;
        }

        return ZoneType.Red;
    }

    private void ApplyZoneType(ZoneType type)
    {
        TileZone[] zones = GetComponents<TileZone>();
        for (int i = 0; i < zones.Length; i++)
        {
            zones[i].zoneType = type;
            zones[i].UpdateVisual();
        }
    }

    /// <summary>
    /// Maps a world hit to -1 (planar left edge) … +1 (planar right edge), using the same
    /// left/right axes as <see cref="GetForceDirection"/> so tap zones match push direction at any Y rotation.
    /// </summary>
    private bool TryGetNormalizedTapAlongWidth(Vector3 worldPoint, out float normalizedAlongRight)
    {
        normalizedAlongRight = 0f;

        GetPlanarForwardLeftRight(out _, out _, out Vector3 planarRight);
        if (planarRight.sqrMagnitude < 1e-8f)
        {
            return false;
        }

        Vector3 center = GetTapReferenceCenter();
        Vector3 toHit = worldPoint - center;
        toHit.y = 0f;

        float halfWidth = GetHalfWidthAlongPlanarAxis(planarRight);
        if (halfWidth < 1e-6f)
        {
            return false;
        }

        normalizedAlongRight = Vector3.Dot(toHit, planarRight) / halfWidth;
        return true;
    }

    private Vector3 GetTapReferenceCenter()
    {
        Transform floor = transform.Find("Floor5Visual");
        if (floor != null)
        {
            Renderer floorRenderer = floor.GetComponentInChildren<Renderer>();
            if (floorRenderer != null)
            {
                Vector3 center = floorRenderer.bounds.center;
                center.y = transform.position.y;
                return center;
            }
        }

        if (TryGetComponent(out BoxCollider box))
        {
            Vector3 center = transform.TransformPoint(box.center);
            center.y = transform.position.y;
            return center;
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }

        Vector3 fallback = col != null ? col.bounds.center : transform.position;
        fallback.y = transform.position.y;
        return fallback;
    }

    private float GetHalfWidthAlongPlanarAxis(Vector3 planarAxis)
    {
        planarAxis.y = 0f;
        if (planarAxis.sqrMagnitude < 1e-8f)
        {
            return 0.5f;
        }

        planarAxis.Normalize();

        if (TryGetComponent(out BoxCollider box))
        {
            Vector3 extentX = transform.TransformVector(new Vector3(box.size.x * 0.5f, 0f, 0f));
            Vector3 extentZ = transform.TransformVector(new Vector3(0f, 0f, box.size.z * 0.5f));
            float halfWidth = Mathf.Abs(Vector3.Dot(extentX, planarAxis)) + Mathf.Abs(Vector3.Dot(extentZ, planarAxis));
            return Mathf.Max(halfWidth, 1e-4f);
        }

        Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col != null)
        {
            Vector3 extents = col.bounds.extents;
            extents.y = 0f;
            return Mathf.Max(Vector3.Dot(extents, planarAxis), 1e-4f);
        }

        return 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
    }

    public void UpdateVisual()
    {
        TileGlbVisual glbVisual = GetComponent<TileGlbVisual>();
        if (glbVisual != null)
        {
            glbVisual.ApplyZoneVisual(zoneType);
            return;
        }

        ResolveRenderer();
        ResolveFloorVisual();

        if (useEdgeIndicatorsWithFloorMesh && floorRenderer != null)
        {
            if (floorBaseMaterial != null)
            {
                floorRenderer.sharedMaterial = floorBaseMaterial;
            }

            if (zoneIndicator == null)
            {
                zoneIndicator = TileZoneIndicator.Ensure(transform);
            }

            GetPlanarForwardLeftRight(out Vector3 planarForward, out Vector3 planarLeft, out Vector3 planarRight);
            zoneIndicator.AlignStripsToTile(transform, planarForward, planarLeft, planarRight);
            zoneIndicator.SetZone(zoneType, redMat, blueMat, yellowMat);
            return;
        }

        Material next = zoneType switch
        {
            ZoneType.Red => redMat,
            ZoneType.Blue => blueMat,
            ZoneType.Yellow => yellowMat,
            _ => noneMat,
        };

        if (next != null && tileRenderer != null)
        {
            tileRenderer.material = next;
        }
    }

    public Vector3 GetForceDirection()
    {
        if (useWorldSpaceDirections)
        {
            switch (zoneType)
            {
                case ZoneType.Red:
                    return Vector3.forward;
                case ZoneType.Blue:
                    return Vector3.left;
                case ZoneType.Yellow:
                    return Vector3.right;
                default:
                    return Vector3.zero;
            }
        }

        GetPlanarForwardLeftRight(out Vector3 planarForward, out Vector3 planarLeft, out Vector3 planarRight);

        switch (zoneType)
        {
            case ZoneType.Red:
                return planarForward;
            case ZoneType.Blue:
                return planarLeft;
            case ZoneType.Yellow:
                return planarRight;
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// World-space tap/force basis on the XZ plane (same axes as <see cref="GetForceDirection"/>).
    /// </summary>
    public void GetPlanarBasis(out Vector3 planarForward, out Vector3 planarLeft, out Vector3 planarRight)
    {
        GetPlanarForwardLeftRight(out planarForward, out planarLeft, out planarRight);
    }

    /// <summary>Tile path direction on the XZ plane (same as red / forward force).</summary>
    public Vector3 GetPlanarForward()
    {
        GetPlanarForwardLeftRight(out Vector3 planarForward, out _, out _);
        return planarForward;
    }

    /// <summary>True when the tap is in the left or right third (not the forward center band).</summary>
    public bool IsSideRegionTap(Vector3 worldPoint)
    {
        if (!TryGetNormalizedTapAlongWidth(worldPoint, out float normalizedAlongRight))
        {
            return false;
        }

        return Mathf.Abs(normalizedAlongRight) >= sideRegionThreshold;
    }

    /// <summary>True when the tap is in the left (blue) third of the tile.</summary>
    public bool IsLeftSideTap(Vector3 worldPoint)
    {
        if (!TryGetNormalizedTapAlongWidth(worldPoint, out float normalizedAlongRight))
        {
            return false;
        }

        return normalizedAlongRight < -sideRegionThreshold;
    }

    /// <summary>True when the tap is in the right (yellow) third of the tile.</summary>
    public bool IsRightSideTap(Vector3 worldPoint)
    {
        if (!TryGetNormalizedTapAlongWidth(worldPoint, out float normalizedAlongRight))
        {
            return false;
        }

        return normalizedAlongRight > sideRegionThreshold;
    }

    /// <summary>
    /// World position for the ball center at the northwest or northeast corner of the tile
    /// (forward + lateral edge), derived from the tile collider dimensions.
    /// </summary>
    public Vector3 GetCrossEdgeTarget(Vector3 ballWorldPosition, bool toNorthWestCorner, float ballRadius)
    {
        GetPlanarForwardLeftRight(out Vector3 planarForward, out Vector3 planarLeft, out Vector3 planarRight);
        Vector3 center = GetTapReferenceCenter();
        float halfWidth = GetHalfWidthAlongPlanarAxis(planarRight);
        float halfDepth = GetHalfWidthAlongPlanarAxis(planarForward);

        float inset = Mathf.Max(ballRadius + 0.02f, 0.01f);
        float forwardReach = Mathf.Max(halfDepth - inset, 0f);
        float sideReach = Mathf.Max(halfWidth - inset, 0f);

        Vector3 lateral = toNorthWestCorner ? planarLeft : planarRight;
        Vector3 cornerPoint = center + planarForward * forwardReach + lateral * sideReach;
        cornerPoint.y = ballWorldPosition.y;
        return cornerPoint;
    }

    /// <summary>
    /// Build a right-handed basis on the XZ plane: forward from tile, then left/right via cross with world up.
    /// This keeps Blue = geometric left of Red and Yellow = geometric right for any Y rotation (fixes Y=90 swap from using raw transform.right).
    /// </summary>
    private void GetPlanarForwardLeftRight(out Vector3 planarForward, out Vector3 planarLeft, out Vector3 planarRight)
    {
        planarForward = GetPlanarDirection(transform.forward);

        if (planarForward.sqrMagnitude < 1e-8f)
        {
            // Tilted or degenerate: try mesh “up” projected, else world +Z
            planarForward = GetPlanarDirection(transform.up);
            if (planarForward.sqrMagnitude < 1e-8f)
            {
                planarForward = Vector3.forward;
            }
        }

        planarLeft = Vector3.Cross(Vector3.up, planarForward);
        if (planarLeft.sqrMagnitude < 1e-8f)
        {
            planarLeft = Vector3.left;
        }
        else
        {
            planarLeft.Normalize();
        }

        planarRight = Vector3.Cross(planarForward, Vector3.up);
        if (planarRight.sqrMagnitude < 1e-8f)
        {
            planarRight = Vector3.right;
        }
        else
        {
            planarRight.Normalize();
        }
    }

    private void ResolveRenderer()
    {
        if (tileRenderer != null)
        {
            return;
        }

        tileRenderer = GetComponent<Renderer>();
    }

    private void ResolveFloorVisual()
    {
        if (floorRenderer != null)
        {
            return;
        }

        Transform floor = transform.Find("Floor5Visual");
        if (floor == null)
        {
            return;
        }

        floorRenderer = floor.GetComponentInChildren<Renderer>();
        if (floorRenderer != null && floorBaseMaterial == null)
        {
            floorBaseMaterial = floorRenderer.sharedMaterial;
        }
    }

    private static Vector3 GetPlanarDirection(Vector3 direction)
    {
        Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (planar.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return planar.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDirectionGizmos || useWorldSpaceDirections)
        {
            return;
        }

        GetPlanarForwardLeftRight(out Vector3 f, out Vector3 l, out Vector3 r);
        Vector3 o = transform.position + Vector3.up * 0.15f;
        const float len = 0.85f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(o, o + f * len);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(o, o + l * len);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o, o + r * len);

        DrawTapRegionGizmos();
    }

    private void DrawTapRegionGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            return;
        }

        GetPlanarForwardLeftRight(out _, out _, out Vector3 planarRight);
        Vector3 surface = transform.position + Vector3.up * 0.12f;
        Vector3 widthAxis = planarRight.sqrMagnitude > 1e-8f ? planarRight : transform.right;
        float halfWidth = GetHalfWidthAlongPlanarAxis(widthAxis);
        Vector3 center = GetTapReferenceCenter();
        center.y = surface.y;
        float split = halfWidth * sideRegionThreshold;

        Vector3 leftEdge = center - widthAxis * halfWidth;
        Vector3 rightEdge = center + widthAxis * halfWidth;
        Vector3 leftSplit = center - widthAxis * split;
        Vector3 rightSplit = center + widthAxis * split;

        Gizmos.color = new Color(0.2f, 0.45f, 1f, 0.35f);
        Gizmos.DrawLine(leftEdge, leftSplit);
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.35f);
        Gizmos.DrawLine(leftSplit, rightSplit);
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawLine(rightSplit, rightEdge);
    }
}
