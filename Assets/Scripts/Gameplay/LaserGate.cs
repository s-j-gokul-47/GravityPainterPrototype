using UnityEngine;

/// <summary>
/// Korrath Beam — static frame with a beam that sweeps left-to-right, holds on, then turns off.
/// </summary>
public class LaserGate : MonoBehaviour
{
    private enum Phase
    {
        Off,
        Sweep,
        Hold
    }

    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform beamRoot;
    [SerializeField] private float sweepDuration = 0.6f;
    [SerializeField] private float onDuration = 1.5f;
    [SerializeField] private float offDuration = 1.5f;
    [SerializeField] private bool startActive = true;

    private Phase _phase;
    private float _phaseTimer;
    private Renderer[] _beamRenderers = System.Array.Empty<Renderer>();
    private Renderer[] _frameRenderers = System.Array.Empty<Renderer>();
    private LaserGateStrike[] _strikes;
    private BoxCollider _strikeCollider;

    private Vector3 _beamRestLocalPosition;
    private Vector3 _beamRestLocalScale;
    private float _beamMeshMinX;
    private float _beamMeshMaxX;
    private Vector3 _strikeRestCenter;
    private Vector3 _strikeRestSize;
    private bool _beamBoundsCached;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        CacheReferences();
        BeginCycle(startActive);
    }

    private void Start()
    {
        CacheReferences();
        ApplyVisualState();
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.timeScale <= 0f)
        {
            return;
        }

        _phaseTimer -= Time.deltaTime;
        switch (_phase)
        {
            case Phase.Off:
                ApplyBeamSweep(0f);
                IsActive = false;
                if (_phaseTimer <= 0f)
                {
                    BeginSweep();
                }

                break;

            case Phase.Sweep:
                float sweepTime = Mathf.Max(sweepDuration, 0.001f);
                float sweepProgress = 1f - Mathf.Clamp01(_phaseTimer / sweepTime);
                ApplyBeamSweep(sweepProgress);
                IsActive = sweepProgress > 0.02f;
                if (_phaseTimer <= 0f)
                {
                    BeginHold();
                }

                break;

            case Phase.Hold:
                ApplyBeamSweep(1f);
                IsActive = true;
                if (_phaseTimer <= 0f)
                {
                    BeginOff();
                }

                break;
        }

        ApplyStrikeState();
    }

    private void BeginCycle(bool active)
    {
        if (active)
        {
            BeginHold();
        }
        else
        {
            BeginOff();
        }
    }

    private void BeginOff()
    {
        _phase = Phase.Off;
        _phaseTimer = Mathf.Max(offDuration, 0f);
        ApplyBeamSweep(0f);
        IsActive = false;
        ApplyStrikeState();
    }

    private void BeginSweep()
    {
        _phase = Phase.Sweep;
        _phaseTimer = Mathf.Max(sweepDuration, 0.001f);
    }

    private void BeginHold()
    {
        _phase = Phase.Hold;
        _phaseTimer = Mathf.Max(onDuration, 0f);
        ApplyBeamSweep(1f);
        IsActive = true;
        ApplyStrikeState();
    }

    private void CacheReferences()
    {
        if (modelRoot == null)
        {
            Transform found = transform.Find("LaserModel");
            modelRoot = found != null ? found : transform;
        }

        if (beamRoot == null && modelRoot != null)
        {
            beamRoot = LaserGateMeshParts.FindBeamTransform(modelRoot);
        }

        LaserGateMeshParts.ClassifyRenderers(modelRoot, beamRoot, out _beamRenderers, out _frameRenderers);
        _strikes = GetComponentsInChildren<LaserGateStrike>(true);

        if (beamRoot != null)
        {
            _strikeCollider = beamRoot.GetComponent<BoxCollider>();
        }

        foreach (Renderer frameRenderer in _frameRenderers)
        {
            if (frameRenderer != null && frameRenderer.GetComponent<Collider>() == null)
            {
                frameRenderer.gameObject.AddComponent<MeshCollider>();
            }
        }

        CacheBeamSweepBounds();
    }

    private void CacheBeamSweepBounds()
    {
        if (beamRoot == null || _beamBoundsCached)
        {
            return;
        }

        _beamRestLocalPosition = beamRoot.localPosition;
        _beamRestLocalScale = beamRoot.localScale;

        Bounds meshBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer renderer in _beamRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Bounds localBounds = renderer.localBounds;
            if (!hasBounds)
            {
                meshBounds = localBounds;
                hasBounds = true;
            }
            else
            {
                meshBounds.Encapsulate(localBounds);
            }
        }

        if (!hasBounds)
        {
            meshBounds = new Bounds(Vector3.zero, Vector3.one);
        }

        _beamMeshMinX = meshBounds.min.x;
        _beamMeshMaxX = meshBounds.max.x;

        if (_strikeCollider != null)
        {
            _strikeRestCenter = _strikeCollider.center;
            _strikeRestSize = _strikeCollider.size;
            
            // Extend the trigger box vertically downwards so the ball cannot simply roll under the beam
            _strikeRestSize.y = Mathf.Max(_strikeRestSize.y, 5f);
            _strikeRestCenter.y = 0f;
        }

        _beamBoundsCached = true;
    }

    private void ApplyBeamSweep(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        bool visible = clamped > 0.001f;

        foreach (Renderer renderer in _frameRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        foreach (Renderer renderer in _beamRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        if (beamRoot == null)
        {
            return;
        }

        if (!visible)
        {
            beamRoot.localScale = new Vector3(
                _beamRestLocalScale.x * 0.001f,
                _beamRestLocalScale.y,
                _beamRestLocalScale.z);
            UpdateStrikeCollider(0f);
            return;
        }

        float width = Mathf.Max(_beamMeshMaxX - _beamMeshMinX, 0.001f);
        float scaleX = Mathf.Max(clamped, 0.001f);
        beamRoot.localScale = new Vector3(
            _beamRestLocalScale.x * scaleX,
            _beamRestLocalScale.y,
            _beamRestLocalScale.z);
        beamRoot.localPosition = new Vector3(
            _beamRestLocalPosition.x + _beamMeshMinX * (1f - scaleX),
            _beamRestLocalPosition.y,
            _beamRestLocalPosition.z);

        UpdateStrikeCollider(clamped);
    }

    private void UpdateStrikeCollider(float progress)
    {
        if (_strikeCollider == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(progress);
        if (clamped <= 0.001f)
        {
            _strikeCollider.enabled = false;
            return;
        }

        _strikeCollider.enabled = true;
        float width = Mathf.Max(_beamMeshMaxX - _beamMeshMinX, 0.001f) * clamped;
        float centerX = _beamMeshMinX + width * 0.5f;
        _strikeCollider.center = new Vector3(centerX, _strikeRestCenter.y, _strikeRestCenter.z);
        _strikeCollider.size = new Vector3(
            Mathf.Max(width, 0.01f),
            _strikeRestSize.y,
            _strikeRestSize.z);
    }

    private void ApplyVisualState()
    {
        foreach (Renderer renderer in _frameRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        ApplyBeamSweep(IsActive ? 1f : 0f);
        ApplyStrikeState();
    }

    private void ApplyStrikeState()
    {
        foreach (LaserGateStrike strike in _strikes)
        {
            if (strike != null)
            {
                strike.SetGateActive(IsActive);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (modelRoot == null && transform.childCount > 0)
        {
            Transform found = transform.Find("LaserModel");
            modelRoot = found != null ? found : transform.GetChild(0);
        }

        _beamBoundsCached = false;
        CacheReferences();
        if (!Application.isPlaying)
        {
            BeginCycle(startActive);
            ApplyVisualState();
        }
    }
#endif
}

/// <summary>Identifies beam vs frame renderers in the split laser GLB.</summary>
public static class LaserGateMeshParts
{
    public static Transform FindBeamTransform(Transform modelRoot)
    {
        if (modelRoot == null)
        {
            return null;
        }

        Transform[] candidates =
        {
            modelRoot.Find("LaserBeam"),
            modelRoot.Find("beam")
        };

        foreach (Transform candidate in candidates)
        {
            if (candidate != null)
            {
                return candidate;
            }
        }

        Transform namedBeam = null;
        Transform smallestRenderer = null;
        int smallestVerts = int.MaxValue;

        foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == modelRoot)
            {
                continue;
            }

            string lower = child.name.ToLowerInvariant();
            if (IsFrameName(lower))
            {
                continue;
            }

            if (IsBeamName(lower) && child.GetComponentInChildren<Renderer>(true) != null)
            {
                namedBeam = child;
            }

            int verts = GetVertexCount(child);
            if (verts > 0 && verts < smallestVerts)
            {
                smallestVerts = verts;
                smallestRenderer = child;
            }
        }

        if (namedBeam != null)
        {
            return namedBeam;
        }

        if (smallestRenderer != null && smallestVerts < 50000)
        {
            return smallestRenderer;
        }

        return null;
    }

    public static Transform FindSupportTransform(Transform modelRoot)
    {
        return FindFrameTransform(modelRoot);
    }

    public static Transform FindFrameTransform(Transform modelRoot)
    {
        if (modelRoot == null)
        {
            return null;
        }

        Transform[] candidates =
        {
            modelRoot.Find("LaserGate_Frame"),
            modelRoot.Find("lasersupport")
        };

        foreach (Transform candidate in candidates)
        {
            if (candidate != null)
            {
                return candidate;
            }
        }

        Transform namedFrame = null;
        Transform largestRenderer = null;
        int largestVerts = 0;

        foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == modelRoot)
            {
                continue;
            }

            string lower = child.name.ToLowerInvariant();
            if (IsBeamName(lower))
            {
                continue;
            }

            if (IsFrameName(lower) && child.GetComponentInChildren<Renderer>(true) != null)
            {
                namedFrame = child;
            }

            int verts = GetVertexCount(child);
            if (verts > largestVerts)
            {
                largestVerts = verts;
                largestRenderer = child;
            }
        }

        if (namedFrame != null)
        {
            return namedFrame;
        }

        return largestVerts > 0 ? largestRenderer : null;
    }

    public static void ClassifyRenderers(
        Transform modelRoot,
        Transform beamRoot,
        out Renderer[] beamRenderers,
        out Renderer[] supportRenderers)
    {
        beamRenderers = System.Array.Empty<Renderer>();
        supportRenderers = System.Array.Empty<Renderer>();

        if (modelRoot == null)
        {
            return;
        }

        if (beamRoot == null)
        {
            beamRoot = FindBeamTransform(modelRoot);
        }

        Transform frameRoot = FindFrameTransform(modelRoot);

        if (beamRoot != null && frameRoot != null && beamRoot != frameRoot)
        {
            beamRenderers = beamRoot.GetComponentsInChildren<Renderer>(true);
            supportRenderers = frameRoot.GetComponentsInChildren<Renderer>(true);
            return;
        }

        Renderer[] all = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (all.Length <= 1)
        {
            beamRenderers = all;
            return;
        }

        System.Collections.Generic.List<Renderer> beamList = new System.Collections.Generic.List<Renderer>();
        System.Collections.Generic.List<Renderer> frameList = new System.Collections.Generic.List<Renderer>();

        foreach (Renderer renderer in all)
        {
            if (renderer == null)
            {
                continue;
            }

            string nodeName = renderer.transform.name.ToLowerInvariant();
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            string meshName = filter != null && filter.sharedMesh != null
                ? filter.sharedMesh.name.ToLowerInvariant()
                : string.Empty;

            if (IsFrameName(nodeName) || meshName == "mesh_0")
            {
                frameList.Add(renderer);
            }
            else if (IsBeamName(nodeName) || meshName.Contains("mesh_0.001"))
            {
                beamList.Add(renderer);
            }
            else
            {
                int verts = filter != null && filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0;
                if (verts > 10000)
                {
                    frameList.Add(renderer);
                }
                else
                {
                    beamList.Add(renderer);
                }
            }
        }

        beamRenderers = beamList.ToArray();
        supportRenderers = frameList.ToArray();

        if (beamRenderers.Length == 0)
        {
            beamRenderers = all;
        }
    }

    private static bool IsBeamName(string lower)
    {
        return lower == "beam"
            || lower == "laserbeam"
            || (lower.Contains("beam") && !lower.Contains("frame") && !lower.Contains("support") && !lower.Contains("holder"));
    }

    private static bool IsFrameName(string lower)
    {
        return lower.Contains("lasergate_frame")
            || lower.Contains("lasersupport")
            || (lower.Contains("frame") && !lower.Contains("beam"))
            || lower.Contains("support")
            || lower.Contains("holder");
    }

    private static int GetVertexCount(Transform t)
    {
        MeshFilter filter = t.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            return filter.sharedMesh.vertexCount;
        }

        filter = t.GetComponentInChildren<MeshFilter>(true);
        return filter != null && filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0;
    }
}
