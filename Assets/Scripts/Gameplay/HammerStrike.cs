using UnityEngine;

/// <summary>
/// Knocks the ball downward when the hammer head collides with it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HammerStrike : MonoBehaviour
{
    [SerializeField] private float downwardSpeed = 7f;
    [SerializeField] private float outwardSpeed = 5f;
    [SerializeField] private float hitCooldown = 0.35f;

    private float _lastHitTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider col)
    {
        if (Time.time - _lastHitTime < hitCooldown)
        {
            return;
        }

        BallController ball = col.GetComponentInParent<BallController>();
        if (ball == null)
        {
            return;
        }

        _lastHitTime = Time.time;
        ball.KnockDown(transform.position, downwardSpeed, outwardSpeed);
    }
}
