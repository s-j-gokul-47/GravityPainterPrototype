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
    /// </summary>
    public void SetZoneFromWorldPoint(Vector3 worldPoint)
    {
        ApplyZoneType(GetZoneFromTap(worldPoint));
    }

    private ZoneType GetZoneFromTap(Vector3 worldPoint)
    {
        if (!TryGetNormalizedTapX(worldPoint, out float normalizedX))
        {
            return ZoneType.Red;
        }

        if (normalizedX < -sideRegionThreshold)
        {
            return ZoneType.Blue;
        }

        if (normalizedX > sideRegionThreshold)
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
    /// Maps a world-space hit on this tile to -1 (left edge) … +1 (right edge) along the tile width.
    /// </summary>
    private bool TryGetNormalizedTapX(Vector3 worldPoint, out float normalizedX)
    {
        normalizedX = 0f;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        if (col is BoxCollider box)
        {
            float halfWidth = box.size.x * 0.5f;
            if (halfWidth < 1e-6f)
            {
                return false;
            }

            normalizedX = (localPoint.x - box.center.x) / halfWidth;
            return true;
        }

        if (col != null)
        {
            Vector3 localCenter = transform.InverseTransformPoint(col.bounds.center);
            float halfWidth = col.bounds.extents.x / Mathf.Max(transform.lossyScale.x, 1e-6f);
            if (halfWidth < 1e-6f)
            {
                return false;
            }

            normalizedX = (localPoint.x - localCenter.x) / halfWidth;
            return true;
        }

        normalizedX = Mathf.Clamp(localPoint.x, -1f, 1f);
        return true;
    }

    public void UpdateVisual()
    {
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

        GetPlanarForwardLeftRight(out _, out Vector3 planarLeft, out Vector3 planarRight);
        Vector3 surface = transform.position + Vector3.up * 0.12f;
        float halfWidth = box.size.x * 0.5f;
        Vector3 widthAxis = planarRight.sqrMagnitude > 1e-8f ? planarRight : transform.right;
        Vector3 center = transform.TransformPoint(box.center);
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
