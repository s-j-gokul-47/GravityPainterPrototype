using UnityEngine;

public class BallController : MonoBehaviour
{
    public float forceStrength = 10f;
    private Rigidbody rb;
    private TileZone currentZone;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (currentZone != null)
        {
            Vector3 force = currentZone.GetForceDirection() * forceStrength;
            rb.AddForce(force, ForceMode.Force);
            Debug.Log("Applying force: " + force);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter with: " + other.name);
        TileZone zone = other.GetComponent<TileZone>();
        if (zone != null)
        {
            currentZone = zone;
            Debug.Log("Ball entered zone: " + zone.zoneType);
        }
    }

    void OnTriggerExit(Collider other)
    {
        TileZone zone = other.GetComponent<TileZone>();
        if (zone == currentZone)
        {
            currentZone = null;
            Debug.Log("Ball exited zone");
        }
    }
}