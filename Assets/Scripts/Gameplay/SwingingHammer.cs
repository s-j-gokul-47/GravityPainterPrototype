using UnityEngine;

/// <summary>
/// Pendulum swing for hammer obstacles. Only animates during Play when time is running
/// (stops when the level is paused, e.g. level-complete UI).
/// </summary>
public class SwingingHammer : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private float swingAngle = 55f;
    [Tooltip("Swing rate. Positive = cycles per second (1 = one full swing per second). "
             + "Negative = slower: -2 is half speed, -4 is quarter speed, etc.")]
    [SerializeField] private float swingSpeed = -2f;
    [Tooltip("Local axis on the pivot to rotate around (default Z = side-to-side over the path).")]
    [SerializeField] private Vector3 swingAxisLocal = Vector3.forward;

    private float _swingPhase;
    private float _cachedAngle;
    private bool _hasCachedAngle;

    private void Reset()
    {
        if (pivot == null && transform.childCount > 0)
        {
            pivot = transform.GetChild(0);
        }
    }

    private void OnValidate()
    {
        if (pivot == null && transform.childCount > 0)
        {
            pivot = transform.GetChild(0);
        }
    }

    private void Update()
    {
        if (pivot == null)
        {
            return;
        }

        if (!Application.isPlaying || Time.timeScale <= 0f)
        {
            ApplyAngle(_hasCachedAngle ? _cachedAngle : 0f);
            return;
        }

        _swingPhase += Time.deltaTime * GetEffectiveSwingSpeed();
        _cachedAngle = Mathf.Sin(_swingPhase * Mathf.PI * 2f) * swingAngle;
        _hasCachedAngle = true;
        ApplyAngle(_cachedAngle);
    }

    private void ApplyAngle(float angle)
    {
        pivot.localRotation = Quaternion.AngleAxis(angle, swingAxisLocal.normalized);
    }

    private float GetEffectiveSwingSpeed()
    {
        if (Mathf.Approximately(swingSpeed, 0f))
        {
            return 0f;
        }

        if (swingSpeed < 0f)
        {
            return 1f / Mathf.Abs(swingSpeed);
        }

        return swingSpeed;
    }
}
