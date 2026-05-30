using UnityEngine;

/// <summary>
/// Destroys the ball when the parent <see cref="LaserGate"/> is active.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LaserGateStrike : MonoBehaviour
{
    [SerializeField] private LaserGate gate;

    private bool _gateActive = true;

    private void Awake()
    {
        if (gate == null)
        {
            gate = GetComponentInParent<LaserGate>();
        }

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void SetGateActive(bool active)
    {
        _gateActive = active;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStrike(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStrike(other);
    }

    private void TryStrike(Collider col)
    {
        if (!_gateActive)
        {
            return;
        }

        if (gate != null && !gate.IsActive)
        {
            return;
        }

        BallController ball = col.GetComponentInParent<BallController>();
        if (ball == null)
        {
            return;
        }

        ball.DestroyFromObstacle();
    }
}
