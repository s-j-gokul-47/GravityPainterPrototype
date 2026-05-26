using UnityEngine;

/// <summary>
/// Follows the ball from behind its movement direction and looks at a point ahead of it
/// so the player can see upcoming tiles and read red / blue / yellow directions.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Offset from the ball in movement space: X = right, Y = up, Z = back (negative = behind).")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -14f);
    [Tooltip("How far ahead of the ball the camera looks (shows more path in front).")]
    [SerializeField] private float lookAhead = 9f;
    [Tooltip("When the ball is almost still, use this world forward for framing.")]
    [SerializeField] private Vector3 defaultForward = Vector3.forward;
    [Tooltip("Higher = snappier follow.")]
    [SerializeField] private float smoothTime = 0.25f;
    [Tooltip("Ball speed below this uses defaultForward instead of velocity.")]
    [SerializeField] private float minSpeedForVelocityHeading = 0.35f;

    private Rigidbody _targetBody;
    private Vector3 _lastPlanarForward;
    private Vector3 _velocity;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("CameraFollow must be on the Main Camera, not on tiles or other objects.", this);
            enabled = false;
            return;
        }

        ResolveTarget();
        _lastPlanarForward = GetDefaultPlanarForward();
    }

    private void LateUpdate()
    {
        if (!enabled || _camera == null)
        {
            return;
        }

        if (target == null)
        {
            ResolveTarget();
            if (target == null)
            {
                return;
            }
        }

        Vector3 planarForward = GetPlanarForward();
        Quaternion heading = Quaternion.LookRotation(planarForward, Vector3.up);

        Vector3 desiredPosition = target.position + heading * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime);

        Vector3 lookTarget = target.position + planarForward * lookAhead + Vector3.up * 0.5f;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
            Time.deltaTime / Mathf.Max(smoothTime, 0.01f));
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            _targetBody = target.GetComponent<Rigidbody>();
            return;
        }

        BallController ball = FindFirstObjectByType<BallController>();
        if (ball != null)
        {
            target = ball.transform;
            _targetBody = ball.GetComponent<Rigidbody>();
        }
    }

    private Vector3 GetPlanarForward()
    {
        if (_targetBody != null)
        {
            Vector3 vel = _targetBody.linearVelocity;
            vel.y = 0f;
            if (vel.sqrMagnitude >= minSpeedForVelocityHeading * minSpeedForVelocityHeading)
            {
                _lastPlanarForward = vel.normalized;
                return _lastPlanarForward;
            }
        }

        if (_lastPlanarForward.sqrMagnitude > 0.0001f)
        {
            return _lastPlanarForward;
        }

        return GetDefaultPlanarForward();
    }

    private Vector3 GetDefaultPlanarForward()
    {
        Vector3 fwd = defaultForward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f)
        {
            fwd = Vector3.forward;
        }

        return fwd.normalized;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
    }
}
