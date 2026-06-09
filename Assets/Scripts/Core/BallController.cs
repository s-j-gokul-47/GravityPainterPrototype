using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BallController : MonoBehaviour
{
    public float forceStrength = 20f;
    public float maxPlanarSpeed = 4f;
    [Tooltip("Horizontal drag on grey tiles / in air. Lower = freer roll; 0 = no extra drag.")]
    public float planarDrag = 0.55f;
    public bool dampWhenNoZone = false;
    public float idlePlanarDamping = 8f;
    public float zoneProbeRadius = 0.48f;
    public float zoneRetentionTime = 0.15f;
    [Tooltip("On red only: if the tile’s push opposes current horizontal motion, flip so the ball keeps going forward along travel (forgiving wrong-side placement).")]
    public bool redAlignWithMotion = true;

    [Header("Side double-tap cross slide")]
    [Tooltip("Speed when double-tapping a tile side to auto-slide the ball to that edge.")]
    [SerializeField] private float crossSlideSpeed = 3.5f;

    [Header("Fall restart")]
    [Tooltip("Reload the current level when the ball falls below the platform.")]
    [SerializeField] private bool restartOnFall = true;
    [Tooltip("World Y below this = fell off (tiles are usually near Y = 0).")]
    [SerializeField] private float fallYThreshold = -2f;
    [Tooltip("Seconds after falling before the level reloads.")]
    [SerializeField] private float fallRestartDelay = 5f;
    [Tooltip("Faster reset when the ball falls in a procedural level.")]
    [SerializeField] private float proceduralFallRestartDelay = 0.6f;

    [Header("Ball visual (GLB mesh)")]
    [SerializeField] private bool useSciFiBallVisual = true;
    [SerializeField] private GameObject sciFiBallVisualPrefab;
    private const string SciFiVisualChildName = "SciFiBallVisual";
    private const string DefaultVisualPrefabResource = "Prefabs/SciFiBallVisual";

    private Rigidbody rb;
    private TileZone currentZone;
    private float timeSinceLastZoneContact;
    private bool _restarting;
    private float _fallElapsed;
    private bool _crossSlideActive;
    private TileZone _crossSlideZone;
    private bool _crossSlideToLeft;

    private void Awake()
    {
        if (!useSciFiBallVisual)
        {
            return;
        }

        if (sciFiBallVisualPrefab == null)
        {
            sciFiBallVisualPrefab = Resources.Load<GameObject>(DefaultVisualPrefabResource);
        }

        if (sciFiBallVisualPrefab != null)
        {
            EnsureSciFiBallVisual();
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.WakeUp();
        timeSinceLastZoneContact = 999f;
    }

    /// <summary>Moves the ball to a spawn point and clears physics velocity.</summary>
    public void PlaceAt(Vector3 worldPosition)
    {
        SuspendAt(worldPosition);
        ReleasePhysics();
    }

    /// <summary>Holds the ball at a spawn point without enabling physics yet.</summary>
    public void SuspendAt(Vector3 worldPosition)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        EndCrossSlide();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        transform.position = worldPosition;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _fallElapsed = 0f;
        _restarting = false;
    }

    /// <summary>Enables physics after the level and colliders are ready.</summary>
    public void ReleasePhysics()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
        }

        _fallElapsed = 0f;
        _restarting = false;
    }

    /// <summary>
    /// Hides the Unity sphere mesh and parents the GLB visual, scaled to the SphereCollider diameter.
    /// </summary>
    public void EnsureSciFiBallVisual(bool replaceExisting = false)
    {
        if (sciFiBallVisualPrefab == null)
        {
            return;
        }

        Transform existing = transform.Find(SciFiVisualChildName);
        if (existing != null)
        {
            if (!replaceExisting)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        GameObject visual = Instantiate(sciFiBallVisualPrefab, transform);
        visual.name = SciFiVisualChildName;

        foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
        {
            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

        foreach (Rigidbody childBody in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            if (Application.isPlaying)
            {
                Destroy(childBody);
            }
            else
            {
                DestroyImmediate(childBody);
            }
        }

        FitVisualToSphereCollider(visual);
    }

    private void FitVisualToSphereCollider(GameObject visual)
    {
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        SphereCollider sphere = GetComponent<SphereCollider>();
        float targetDiameter = 1f;
        if (sphere != null)
        {
            float scale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z));
            targetDiameter = sphere.radius * 2f * scale;
        }

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float uniformScale = targetDiameter / Mathf.Max(maxExtent, 0.0001f);
        visual.transform.localScale = Vector3.one * uniformScale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 centerOffsetLocal = transform.InverseTransformPoint(bounds.center);
        visual.transform.localPosition -= centerOffsetLocal;
    }

    private void Update()
    {
        if (!restartOnFall || _restarting || Time.timeScale <= 0f)
        {
            return;
        }

        if (transform.position.y < fallYThreshold)
        {
            _fallElapsed += Time.deltaTime;
            float delay = GetFallRestartDelay();
            if (_fallElapsed >= delay)
            {
                RestartCurrentLevel();
            }
        }
        else
        {
            _fallElapsed = 0f;
        }
    }

    /// <summary>
    /// Instant restart when the ball hits an active hazard (e.g. Korrath Beam laser gate).
    /// </summary>
    public void DestroyFromObstacle()
    {
        if (_restarting)
        {
            return;
        }

        RestartCurrentLevel();
    }

    /// <summary>Whether the ball is currently resolved to the given tile zone.</summary>
    public bool IsStandingOnTile(TileZone zone)
    {
        if (zone == null || currentZone == null)
        {
            return false;
        }

        TileZone standing = TileZone.GetPrimaryZone(currentZone.gameObject);
        TileZone target = TileZone.GetPrimaryZone(zone.gameObject);
        return standing != null && standing == target;
    }

    /// <summary>Whether the ball is on or over the given tile (for double-tap cross).</summary>
    public bool IsOnTileForCross(TileZone zone)
    {
        if (zone == null)
        {
            return false;
        }

        if (IsStandingOnTile(zone))
        {
            return true;
        }

        Collider[] colliders = zone.GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
        {
            return false;
        }

        Vector3 ballPos = transform.position;
        float maxDistance = GetSphereRadius() + 0.35f;
        float maxDistanceSq = maxDistance * maxDistance;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.isTrigger)
            {
                continue;
            }

            Vector3 closest = col.ClosestPoint(ballPos);
            closest.y = ballPos.y;
            if ((closest - ballPos).sqrMagnitude <= maxDistanceSq)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsCrossSliding => _crossSlideActive;

    /// <summary>Auto-slide the ball diagonally to the northwest or northeast tile corner (double-tap).</summary>
    public void StartCrossSlideToEdge(TileZone zone, bool toNorthWestCorner)
    {
        if (zone == null)
        {
            return;
        }

        _crossSlideActive = true;
        _crossSlideZone = zone;
        _crossSlideToLeft = toNorthWestCorner;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb?.WakeUp();
    }

    /// <summary>Stop cross slide and bring the ball to rest on the XZ plane.</summary>
    public void EndCrossSlide()
    {
        _crossSlideActive = false;
        _crossSlideZone = null;

        if (rb == null)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    private void ApplyCrossSlideMovement()
    {
        if (_crossSlideZone == null || rb == null)
        {
            return;
        }

        float radius = GetSphereRadius();
        Vector3 target = _crossSlideZone.GetCrossEdgeTarget(transform.position, _crossSlideToLeft, radius);
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= 0.02f)
        {
            transform.position = new Vector3(target.x, transform.position.y, target.z);
            EndCrossSlide();
            return;
        }

        Vector3 direction = toTarget / dist;
        float stepSpeed = Mathf.Min(crossSlideSpeed, dist / Time.fixedDeltaTime);
        Vector3 planar = direction * stepSpeed;
        Vector3 current = rb.linearVelocity;
        rb.linearVelocity = new Vector3(planar.x, current.y, planar.z);
    }

    private float GetSphereRadius()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
        {
            return 0.5f;
        }

        Vector3 scale = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return sphere.radius * maxScale;
    }

    /// <summary>
    /// Called by obstacles (e.g. hammer) to knock the ball off the platform.
    /// </summary>
    public void KnockDown(Vector3 fromWorldPoint, float downwardSpeed, float outwardSpeed)
    {
        rb.WakeUp();

        Vector3 velocity = rb.linearVelocity;
        Vector3 away = transform.position - fromWorldPoint;
        away.y = 0f;

        if (away.sqrMagnitude > 0.0001f)
        {
            velocity += away.normalized * outwardSpeed;
        }

        velocity.y = Mathf.Min(velocity.y, -Mathf.Abs(downwardSpeed));
        rb.linearVelocity = velocity;
    }

    private float GetFallRestartDelay()
    {
        Scene active = SceneManager.GetActiveScene();
        if (LevelProgress.IsProceduralScene(active))
        {
            return proceduralFallRestartDelay;
        }

        return fallRestartDelay;
    }

    private void RestartCurrentLevel()
    {
        _restarting = true;
        Time.timeScale = 1f;

        Scene active = SceneManager.GetActiveScene();
        if (LevelProgress.IsProceduralScene(active))
        {
            ProceduralLevelBuilder builder = FindFirstObjectByType<ProceduralLevelBuilder>();
            if (builder != null && builder.ResetBallToStart())
            {
                _restarting = false;
                return;
            }

            if (builder != null)
            {
                builder.RebuildSameSeed();
                _restarting = false;
                return;
            }
        }

        if (active.buildIndex >= 0)
        {
            SceneManager.LoadScene(active.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(active.name);
        }
    }

    private void FixedUpdate()
    {
        ResolveCurrentTileZone();

        if (_crossSlideActive)
        {
            ApplyCrossSlideMovement();
            return;
        }

        bool onPaintedTile = currentZone != null && currentZone.zoneType != ZoneType.None;
        bool inRetention = timeSinceLastZoneContact <= zoneRetentionTime;

        if (onPaintedTile)
        {
            Vector3 direction = currentZone.GetForceDirection();
            direction = AdjustRedDirectionForContinuity(direction);
            if (direction.sqrMagnitude > 0.0001f)
            {
                rb.AddForce(direction * forceStrength, ForceMode.Acceleration);
            }
            else if (dampWhenNoZone)
            {
                ApplyIdleDamping();
            }
        }
        else if (dampWhenNoZone && (currentZone != null || inRetention))
        {
            ApplyIdleDamping();
        }

        if (!onPaintedTile)
        {
            ApplyPlanarDrag();
        }

        ClampPlanarSpeed();
    }

    /// <summary>
    /// Resolve the tile under the ball. Ray down through solid colliders first (skips ball + triggers)
    /// so we get the real floor tile — not a neighbor chosen by "closest tile center" on XZ.
    /// </summary>
    private void ResolveCurrentTileZone()
    {
        TileZone best = TryResolveTileByGroundRay();

        if (best == null)
        {
            best = TryResolveTileBySolidOverlap();
        }

        if (best != null)
        {
            currentZone = best;
            timeSinceLastZoneContact = 0f;
        }
        else
        {
            timeSinceLastZoneContact += Time.fixedDeltaTime;
            if (timeSinceLastZoneContact > zoneRetentionTime)
            {
                currentZone = null;
            }
        }
    }

    private TileZone TryResolveTileByGroundRay()
    {
        // Start well above the ball so the ray is stable; skip self and trigger volumes.
        Vector3 origin = transform.position + Vector3.up * 2.5f;
        const float maxDist = 6f;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            maxDist,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
            {
                continue;
            }

            if (col.isTrigger)
            {
                continue;
            }

            if (col.attachedRigidbody == rb || col.GetComponentInParent<BallController>() != null)
            {
                continue;
            }

            TileZone z = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            if (z != null)
            {
                return TileZone.GetPrimaryZone(z.gameObject) ?? z;
            }
        }

        return null;
    }

    private TileZone TryResolveTileBySolidOverlap()
    {
        float probeR = Mathf.Max(0.35f, zoneProbeRadius);
        Vector3 sphereCenter = transform.position + Vector3.down * 0.35f;

        Collider[] cols = Physics.OverlapSphere(
            sphereCenter,
            probeR,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        TileZone best = null;
        float bestDistSq = float.MaxValue;
        Vector3 ballPos = transform.position;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null || col.isTrigger)
            {
                continue;
            }

            if (col.attachedRigidbody == rb || col.GetComponentInParent<BallController>() != null)
            {
                continue;
            }

            TileZone hit = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            if (hit == null)
            {
                continue;
            }

            TileZone primary = TileZone.GetPrimaryZone(hit.gameObject) ?? hit;
            Vector3 closest = col.ClosestPoint(ballPos);
            float d = (closest - ballPos).sqrMagnitude;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = primary;
            }
        }

        return best;
    }

    private Vector3 AdjustRedDirectionForContinuity(Vector3 zoneDirection)
    {
        if (!redAlignWithMotion || currentZone == null || currentZone.zoneType != ZoneType.Red)
        {
            return zoneDirection;
        }

        if (zoneDirection.sqrMagnitude < 1e-8f)
        {
            return zoneDirection;
        }

        Vector3 planarVel = rb.linearVelocity;
        planarVel.y = 0f;
        if (planarVel.sqrMagnitude < 0.02f)
        {
            return zoneDirection;
        }

        if (Vector3.Dot(zoneDirection, planarVel.normalized) < 0f)
        {
            return -zoneDirection;
        }

        return zoneDirection;
    }

    private void ApplyPlanarDrag()
    {
        if (planarDrag <= 0f)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        if (planar.sqrMagnitude < 1e-8f)
        {
            return;
        }

        rb.AddForce(-planar * planarDrag, ForceMode.Acceleration);
    }

    private void ClampPlanarSpeed()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);

        float speed = planarVelocity.magnitude;
        if (speed > maxPlanarSpeed)
        {
            planarVelocity = planarVelocity.normalized * maxPlanarSpeed;
            rb.linearVelocity = new Vector3(planarVelocity.x, velocity.y, planarVelocity.z);
        }
    }

    private void ApplyIdleDamping()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 dampedPlanar = Vector3.Lerp(planarVelocity, Vector3.zero, idlePlanarDamping * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(dampedPlanar.x, velocity.y, dampedPlanar.z);
    }
}
