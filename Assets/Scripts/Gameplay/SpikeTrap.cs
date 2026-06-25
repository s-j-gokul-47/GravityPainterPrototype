using UnityEngine;

/// <summary>
/// Spikes that grow out of a tile, stay up briefly, then sink back into the tile surface.
/// Edit Spike_Master_Tile(43) on Level 2 and publish to sync all spikes.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class SpikeTrap : MonoBehaviour
{
    private enum Phase
    {
        Hidden,
        Extending,
        Extended,
        Retracting
    }

    public const string MasterSpikeName = "Spike_Master_Tile(43)";

    [Header("Profile")]
    [SerializeField] private SpikeTrapProfile profile;
    [SerializeField] private bool publishChangesToProfile;

    [Header("Per-Tile Timing")]
    [SerializeField] private float startDelay = 0f;

    [Header("Timing (seconds)")]
    [SerializeField] private float hiddenDuration = 2f;
    [SerializeField] private float extendDuration = 0.75f;
    [SerializeField] private float extendedDuration = 2f;
    [SerializeField] private float retractDuration = 0.6f;

    [Header("Growth")]
    [SerializeField] private AnimationCurve extendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.001f, 1f)] private float hiddenScaleY = 0.02f;
    [SerializeField, Range(0.5f, 1.5f)] private float dangerThreshold = 0.85f;

    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private BoxCollider strikeCollider;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField, Range(0f, 1f)] private float editorPreviewExtension = 1f;

    private Phase _phase = Phase.Hidden;
    private float _phaseTimer;
    private float _extensionProgress;
    private Vector3 _fullLocalScale = Vector3.one;
    private bool _initialized;

    private static bool _isPublishing;
    private static bool _syncInProgress;

    public bool IsMaster => publishChangesToProfile;
    public SpikeTrapProfile Profile => profile;
    public float StartDelay => startDelay;

    public static bool SyncInProgress => _syncInProgress;

    public static void SetSyncInProgress(bool inProgress)
    {
        _syncInProgress = inProgress;
    }

    public float ExtensionProgress => _extensionProgress;
    public bool IsDangerous => _extensionProgress >= dangerThreshold;
    public string CurrentPhaseName => _phase.ToString();

    private void Awake()
    {
        EnsureProfileReference();

        if (!publishChangesToProfile)
        {
            ApplyFromProfile();
        }

        ResetInitialization();
        Initialize();
        if (playOnStart && Application.isPlaying)
        {
            BeginHidden();
        }
    }

    private void OnEnable()
    {
        if (_syncInProgress)
        {
            return;
        }

        EnsureProfileReference();

        if (!publishChangesToProfile)
        {
            ApplyFromProfile();
        }

        ResetInitialization();
        Initialize();
        if (!Application.isPlaying)
        {
            ApplyExtension(editorPreviewExtension);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.timeScale <= 0f)
        {
            return;
        }

        TickPhase(Time.deltaTime);
    }

    public void EnsureProfileReference()
    {
        if (profile != null)
        {
            SpikeTrapProfile.SetCached(profile);
            return;
        }

        profile = SpikeTrapProfile.LoadOrDefault();
    }

    public void ApplyFromProfile()
    {
        if (_isPublishing || _syncInProgress || profile == null)
        {
            return;
        }

        profile.ApplyToTrap(this, GetComponent<SpikeVisual>());
        ResetInitialization();
    }

    public void ApplyFromProfile(SpikeTrapProfile sourceProfile)
    {
        profile = sourceProfile;
        ApplyFromProfile();
    }

    public void ApplySharedSettingsFromProfile(SpikeTrapProfile source)
    {
        if (source == null)
        {
            return;
        }

        transform.localScale = source.rootLocalScale;
        hiddenDuration = source.hiddenDuration;
        extendDuration = source.extendDuration;
        extendedDuration = source.extendedDuration;
        retractDuration = source.retractDuration;
        extendCurve = source.extendCurve;
        hiddenScaleY = source.hiddenScaleY;
        dangerThreshold = source.dangerThreshold;
        editorPreviewExtension = source.editorPreviewExtension;
        playOnStart = source.playOnStart;
    }

    public void CaptureSharedSettingsToProfile(SpikeTrapProfile destination)
    {
        if (destination == null)
        {
            return;
        }

        destination.rootLocalScale = transform.localScale;
        destination.hiddenDuration = hiddenDuration;
        destination.extendDuration = extendDuration;
        destination.extendedDuration = extendedDuration;
        destination.retractDuration = retractDuration;
        destination.extendCurve = extendCurve;
        destination.hiddenScaleY = hiddenScaleY;
        destination.dangerThreshold = dangerThreshold;
        destination.editorPreviewExtension = editorPreviewExtension;
        destination.playOnStart = playOnStart;
    }

    public void RefreshVisualScaleFromProfile()
    {
        ResetInitialization();
        Initialize();

        if (!Application.isPlaying)
        {
            ApplyExtension(editorPreviewExtension);
        }
    }

    public void ResetInitialization()
    {
        _initialized = false;
    }

    public void BindVisualRoot()
    {
        SpikeVisual visual = GetComponent<SpikeVisual>();
        if (visual != null)
        {
            visual.PruneExtraVisualRoots();
            visualRoot = visual.VisualRoot;
        }
        else if (visualRoot == null)
        {
            visualRoot = transform.Find(SpikeVisual.VisualRootName);
        }
    }

    public void RestartCycle()
    {
        ResetInitialization();
        Initialize();
        if (Application.isPlaying)
        {
            BeginHidden();
        }
        else
        {
            ApplyExtension(editorPreviewExtension);
        }
    }

#if UNITY_EDITOR
    public static event System.Action<SpikeTrap, SpikeTrapProfile> ProfilePublished;

    public void PublishFromHierarchy()
    {
        if (profile == null || !publishChangesToProfile)
        {
            return;
        }

        _isPublishing = true;
        try
        {
            profile.CaptureFromTrap(this, GetComponent<SpikeVisual>());
            UnityEditor.EditorUtility.SetDirty(profile);
            ProfilePublished?.Invoke(this, profile);
        }
        finally
        {
            _isPublishing = false;
        }
    }

    public static SpikeTrap FindMasterInOpenScenes()
    {
        SpikeTrap[] traps = Object.FindObjectsByType<SpikeTrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SpikeTrap trap in traps)
        {
            if (trap != null && trap.IsMaster)
            {
                return trap;
            }
        }

        return null;
    }
#endif

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        SpikeVisual visual = GetComponent<SpikeVisual>();
        if (visual != null)
        {
            visual.EnsureVisual();
            visual.PruneExtraVisualRoots();
        }

        BindVisualRoot();

        if (visualRoot != null && profile != null)
        {
            _fullLocalScale = profile.visualRootLocalScale;
            if (_fullLocalScale.y <= 0.0001f)
            {
                _fullLocalScale.y = 1f;
            }
        }
        else if (visualRoot != null)
        {
            _fullLocalScale = visualRoot.localScale;
            if (_fullLocalScale.y <= 0.0001f)
            {
                _fullLocalScale.y = 1f;
            }
        }

        EnsureStrikeCollider();
        _initialized = true;
    }

    private void EnsureStrikeCollider()
    {
        if (strikeCollider == null)
        {
            strikeCollider = GetComponent<BoxCollider>();
        }

        if (strikeCollider == null)
        {
            strikeCollider = gameObject.AddComponent<BoxCollider>();
        }

        strikeCollider.isTrigger = true;
        if (GetComponent<SpikeStrike>() == null)
        {
            gameObject.AddComponent<SpikeStrike>();
        }

        UpdateStrikeColliderShape(1f);
        strikeCollider.enabled = false;
    }

    private void BeginHidden()
    {
        _phase = Phase.Hidden;
        _phaseTimer = Mathf.Max(startDelay, 0f) + Mathf.Max(hiddenDuration, 0f);
        ApplyExtension(0f);
    }

    private void BeginExtending()
    {
        _phase = Phase.Extending;
        _phaseTimer = Mathf.Max(extendDuration, 0.001f);
    }

    private void BeginExtended()
    {
        _phase = Phase.Extended;
        _phaseTimer = Mathf.Max(extendedDuration, 0f);
        ApplyExtension(1f);
    }

    private void BeginRetracting()
    {
        _phase = Phase.Retracting;
        _phaseTimer = Mathf.Max(retractDuration, 0.001f);
    }

    private void TickPhase(float deltaTime)
    {
        _phaseTimer -= deltaTime;

        switch (_phase)
        {
            case Phase.Hidden:
                ApplyExtension(0f);
                if (_phaseTimer <= 0f)
                {
                    BeginExtending();
                }

                break;

            case Phase.Extending:
            {
                float duration = Mathf.Max(extendDuration, 0.001f);
                float t = 1f - Mathf.Clamp01(_phaseTimer / duration);
                ApplyExtension(extendCurve.Evaluate(t));
                if (_phaseTimer <= 0f)
                {
                    BeginExtended();
                }

                break;
            }

            case Phase.Extended:
                ApplyExtension(1f);
                if (_phaseTimer <= 0f)
                {
                    BeginRetracting();
                }

                break;

            case Phase.Retracting:
            {
                float duration = Mathf.Max(retractDuration, 0.001f);
                float t = Mathf.Clamp01(_phaseTimer / duration);
                ApplyExtension(extendCurve.Evaluate(t));
                if (_phaseTimer <= 0f)
                {
                    BeginHidden();
                }

                break;
            }
        }
    }

    private void ApplyExtension(float progress)
    {
        _extensionProgress = Mathf.Clamp01(progress);

        if (visualRoot != null)
        {
            float scaleY = Mathf.Lerp(hiddenScaleY, _fullLocalScale.y, _extensionProgress);
            visualRoot.localScale = new Vector3(_fullLocalScale.x, scaleY, _fullLocalScale.z);
        }

        UpdateStrikeColliderShape(_extensionProgress);
        if (strikeCollider != null)
        {
            strikeCollider.enabled = IsDangerous;
        }
    }

    private void UpdateStrikeColliderShape(float progress)
    {
        if (strikeCollider == null || visualRoot == null)
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            strikeCollider.center = new Vector3(0f, 0.35f * progress, 0f);
            strikeCollider.size = new Vector3(0.8f, 0.7f * progress, 0.8f);
            return;
        }

        Bounds localBounds = RendererLocalBounds(visualRoot, renderers);
        float clamped = Mathf.Clamp01(progress);
        Vector3 size = localBounds.size;
        size.y = Mathf.Max(size.y * clamped, 0.01f);
        strikeCollider.center = new Vector3(
            localBounds.center.x,
            localBounds.min.y + size.y * 0.5f,
            localBounds.center.z);
        strikeCollider.size = size;
    }

    private static Bounds RendererLocalBounds(Transform root, Renderer[] renderers)
    {
        Bounds bounds = new Bounds(root.InverseTransformPoint(renderers[0].bounds.min), Vector3.zero);
        bounds.Encapsulate(root.InverseTransformPoint(renderers[0].bounds.max));

        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            bounds.Encapsulate(root.InverseTransformPoint(renderers[i].bounds.min));
            bounds.Encapsulate(root.InverseTransformPoint(renderers[i].bounds.max));
        }

        return bounds;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_isPublishing || _syncInProgress)
        {
            return;
        }

        EnsureProfileReference();

        if (!publishChangesToProfile)
        {
            ApplyFromProfile();
        }
    }
#endif
}
