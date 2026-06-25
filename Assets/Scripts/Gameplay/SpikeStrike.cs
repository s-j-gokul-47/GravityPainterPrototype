using UnityEngine;

/// <summary>
/// Destroys the ball when it touches extended spikes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpikeStrike : MonoBehaviour
{
    [SerializeField] private SpikeTrap trap;

    private void Awake()
    {
        if (trap == null)
        {
            trap = GetComponentInParent<SpikeTrap>();
        }

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStrike(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStrike(other);
    }

    private void TryStrike(Collider other)
    {
        if (trap == null || !trap.IsDangerous)
        {
            return;
        }

        BallController ball = other.GetComponentInParent<BallController>();
        if (ball == null)
        {
            return;
        }

        ball.DestroyFromObstacle();
    }
}
