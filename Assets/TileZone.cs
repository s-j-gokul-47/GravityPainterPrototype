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

    public void CycleZone()
    {
        TileZone[] zones = GetComponents<TileZone>();
        TileZone primary = GetPrimaryZone(gameObject);
        if (primary == null && zones.Length > 0)
        {
            primary = zones[0];
        }

        ZoneType t = primary != null ? primary.zoneType : zoneType;
        if (t == ZoneType.None)
        {
            t = ZoneType.Red;
        }
        else if (t == ZoneType.Red)
        {
            t = ZoneType.Blue;
        }
        else if (t == ZoneType.Blue)
        {
            t = ZoneType.Yellow;
        }
        else
        {
            t = ZoneType.None;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            zones[i].zoneType = t;
            zones[i].UpdateVisual();
        }
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
    }
}
