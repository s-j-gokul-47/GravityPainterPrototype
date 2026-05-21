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

    [Header("Fall restart")]
    [Tooltip("Reload the current level when the ball falls below the platform.")]
    [SerializeField] private bool restartOnFall = true;
    [Tooltip("World Y below this = fell off (tiles are usually near Y = 0).")]
    [SerializeField] private float fallYThreshold = -2f;
    [Tooltip("Seconds after falling before the level reloads.")]
    [SerializeField] private float fallRestartDelay = 5f;

    private Rigidbody rb;
    private TileZone currentZone;
    private float timeSinceLastZoneContact;
    private bool _restarting;
    private float _fallElapsed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.WakeUp();
        timeSinceLastZoneContact = 999f;
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
            if (_fallElapsed >= fallRestartDelay)
            {
                RestartCurrentLevel();
            }
        }
        else
        {
            _fallElapsed = 0f;
        }
    }

    private void RestartCurrentLevel()
    {
        _restarting = true;
        Time.timeScale = 1f;

        Scene active = SceneManager.GetActiveScene();
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
