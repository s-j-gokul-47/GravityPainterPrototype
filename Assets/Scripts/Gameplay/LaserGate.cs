using UnityEngine;

/// <summary>
/// Korrath Beam — cycles on/off. When active, <see cref="LaserGateStrike"/> destroys the ball on contact.
/// </summary>
public class LaserGate : MonoBehaviour
{
    [SerializeField] private Transform modelRoot;
    [SerializeField] private float onDuration = 1.5f;
    [SerializeField] private float offDuration = 1.5f;
    [SerializeField] private bool startActive = true;

    private float _phaseTimer;
    private Renderer[] _renderers;
    private LaserGateStrike[] _strikes;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        CacheReferences();
        IsActive = startActive;
        _phaseTimer = IsActive ? onDuration : offDuration;
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

        _renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        _strikes = GetComponentsInChildren<LaserGateStrike>(true);
    }

    private void ApplyActiveState()
    {
        foreach (Renderer renderer in _renderers)
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
