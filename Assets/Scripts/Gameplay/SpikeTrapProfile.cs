using UnityEngine;

/// <summary>
/// Shared spike trap look + timing used by every spike in Level 2 and the Spikes prefab.
/// Edit Spike_Master_Tile(43) on Level 2, then publish via the editor menu.
/// </summary>
[CreateAssetMenu(fileName = "SpikeTrapProfile", menuName = "Gravity Painter/Spike Trap Profile")]
public class SpikeTrapProfile : ScriptableObject
{
    public const string DefaultResourcePath = "Settings/SpikeTrapProfile";

    [Header("Spike Root")]
    public Vector3 rootLocalScale = Vector3.one;

    [Header("SpikeVisualRoot")]
    public Vector3 visualRootLocalScale = Vector3.one;

    [Header("Spike Model")]
    public Vector3 targetLocalBoundsSize = new Vector3(0.9f, 0.9f, 0.9f);
    public Vector3 modelLocalPosition = Vector3.zero;
    public Vector3 modelLocalEuler = Vector3.zero;
    public Vector3 modelLocalScale = Vector3.one;

    [Header("Timing (seconds)")]
    public float hiddenDuration = 2f;
    public float extendDuration = 0.75f;
    public float extendedDuration = 2f;
    public float retractDuration = 0.6f;

    [Header("Growth")]
    public AnimationCurve extendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Range(0.001f, 1f)] public float hiddenScaleY = 0.02f;
    [Range(0.5f, 1.5f)] public float dangerThreshold = 0.85f;

    [Header("Editor Preview")]
    [Range(0f, 1f)] public float editorPreviewExtension = 1f;
    public bool playOnStart = true;

    private static SpikeTrapProfile _cached;

    public static SpikeTrapProfile LoadOrDefault()
    {
        if (_cached != null)
        {
            return _cached;
        }

        _cached = Resources.Load<SpikeTrapProfile>(DefaultResourcePath);
        if (_cached != null)
        {
            return _cached;
        }

        _cached = CreateInstance<SpikeTrapProfile>();
        return _cached;
    }

    public static void SetCached(SpikeTrapProfile profile)
    {
        _cached = profile;
    }

    public void ApplyToTrap(SpikeTrap trap, SpikeVisual visual)
    {
        if (trap == null)
        {
            return;
        }

        trap.ApplySharedSettingsFromProfile(this);

        if (visual != null)
        {
            visual.ApplyFromProfile(this);
        }

        trap.RefreshVisualScaleFromProfile();
    }

    public void CaptureFromTrap(SpikeTrap trap, SpikeVisual visual)
    {
        if (trap == null)
        {
            return;
        }

        trap.CaptureSharedSettingsToProfile(this);

        if (visual == null)
        {
            visual = trap.GetComponent<SpikeVisual>();
        }

        if (visual != null)
        {
            visual.CaptureToProfile(this);
        }
    }
}
