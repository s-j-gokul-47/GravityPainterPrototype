using UnityEngine;

/// <summary>
/// Attach this script to a Coin prefab with a Trigger Collider.
/// Handles the visual rotation and collection logic.
/// </summary>
public class Coin : MonoBehaviour
{
    [Tooltip("How fast the coin spins around its Y axis (degrees per second).")]
    public float rotationSpeed = 750f;

    [Tooltip("The axis the coin spins around. If it spins like a wheel, try changing this to X=0, Y=0, Z=1 or X=1, Y=0, Z=0.")]
    public Vector3 spinAxis = new Vector3(0f, 1f, 0f);

    private void Awake()
    {
        // Force the speed to 750 if it accidentally got set to 0 in the Unity Inspector!
        if (rotationSpeed <= 0f || rotationSpeed == 120f)
        {
            rotationSpeed = 750f;
        }
    }

    private void Update()
    {
        // Spin the coin on the chosen axis
        transform.Rotate(spinAxis.normalized * (rotationSpeed * Time.deltaTime), Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched the coin has a BallController
        if (other.GetComponentInParent<BallController>() != null)
        {
            // Register that the player grabbed a coin this session
            CoinManager.AddSessionCoin();
            
            // TODO: (Optional) Play a sound effect or particle effect here!
            
            // Destroy the coin so it disappears from the level
            Destroy(gameObject);
        }
    }
}
