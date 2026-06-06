using UnityEngine;

/// <summary>
/// Korrath Beam — cycles on/off. Only the beam blinks; lasersupport stays visible.
/// </summary>
public class LaserGate : MonoBehaviour
{
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform beamRoot;
    [SerializeField] private float onDuration = 1.5f;
    [SerializeField] private float offDuration = 1.5f;
    [SerializeField] private bool startActive = true;

    private float _phaseTimer;
    private Renderer[] _beamRenderers = System.Array.Empty<Renderer>();
    private Renderer[] _supportRenderers = System.Array.Empty<Renderer>();
    private LaserGateStrike[] _strikes;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        CacheReferences();
        IsActive = startActive;
        _phaseTimer = IsActive ? onDuration : offDuration;
        ApplyActiveState();
    }

    private void Start()
    {
        CacheReferences();
        ApplyActiveState();
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.timeScale <= 0f)
        {
            return;
        }

        _phaseTimer -= Time.deltaTime;
        if (_phaseTimer > 0f)
        {
            return;
        }

        IsActive = !IsActive;
        _phaseTimer = IsActive ? onDuration : offDuration;
        ApplyActiveState();
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

        LaserGateMeshParts.ClassifyRenderers(modelRoot, beamRoot, out _beamRenderers, out _supportRenderers);
        _strikes = GetComponentsInChildren<LaserGateStrike>(true);
    }

    private void ApplyActiveState()
    {
        foreach (Renderer renderer in _supportRenderers)
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
                renderer.enabled = IsActive;
            }
        }

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

        CacheReferences();
        if (!Application.isPlaying)
        {
            IsActive = startActive;
            ApplyActiveState();
        }
    }
#endif
}

/// <summary>Identifies beam vs support renderers in the split laser GLB.</summary>
public static class LaserGateMeshParts
{
    public static Transform FindBeamTransform(Transform modelRoot)
    {
        if (modelRoot == null)
        {
            return null;
        }

        Transform direct = modelRoot.Find("beam");
        if (direct != null)
        {
            return direct;
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
            if (IsSupportName(lower))
            {
                continue;
            }

            if (lower == "beam")
            {
                return child;
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
        if (modelRoot == null)
        {
            return null;
        }

        Transform direct = modelRoot.Find("lasersupport");
        if (direct != null)
        {
            return direct;
        }

        Transform namedSupport = null;
        Transform largestRenderer = null;
        int largestVerts = 0;

        foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == modelRoot)
            {
                continue;
            }

            string lower = child.name.ToLowerInvariant();
            if (IsBeamName(lower) && !IsSupportName(lower))
            {
                continue;
            }

            if (IsSupportName(lower) && child.GetComponentInChildren<Renderer>(true) != null)
            {
                namedSupport = child;
            }

            int verts = GetVertexCount(child);
            if (verts > largestVerts)
            {
                largestVerts = verts;
                largestRenderer = child;
            }
        }

        if (namedSupport != null)
        {
            return namedSupport;
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

        Transform supportRoot = FindSupportTransform(modelRoot);

        if (beamRoot != null && supportRoot != null && beamRoot != supportRoot)
        {
            beamRenderers = beamRoot.GetComponentsInChildren<Renderer>(true);
            supportRenderers = supportRoot.GetComponentsInChildren<Renderer>(true);
            return;
        }

        Renderer[] all = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (all.Length <= 1)
        {
            beamRenderers = all;
            return;
        }

        System.Collections.Generic.List<Renderer> beamList = new System.Collections.Generic.List<Renderer>();
        System.Collections.Generic.List<Renderer> supportList = new System.Collections.Generic.List<Renderer>();

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

            if (IsSupportName(nodeName) || meshName.Contains("mesh_0.001"))
            {
                supportList.Add(renderer);
            }
            else if (IsBeamName(nodeName) || meshName.Contains("mesh_0.002"))
            {
                beamList.Add(renderer);
            }
            else
            {
                int verts = filter != null && filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0;
                if (verts > 10000)
                {
                    supportList.Add(renderer);
                }
                else
                {
                    beamList.Add(renderer);
                }
            }
        }

        beamRenderers = beamList.ToArray();
        supportRenderers = supportList.ToArray();

        if (beamRenderers.Length == 0)
        {
            beamRenderers = all;
        }
    }

    private static bool IsBeamName(string lower)
    {
        return lower == "beam" || (lower.Contains("beam") && !lower.Contains("support") && !lower.Contains("holder"));
    }

    private static bool IsSupportName(string lower)
    {
        return lower.Contains("lasersupport") || lower.Contains("support") || lower.Contains("holder");
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
