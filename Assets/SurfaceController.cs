using UnityEngine;

public class SurfaceController : MonoBehaviour
{
    public float tiltSpeed = 30f;
    public float maxTiltAngle = 20f;
    public Rigidbody ballRigidbody; // drag the ball here in Inspector

    void Update()
    {
        float horizontal = UnityEngine.InputSystem.Keyboard.current.dKey.isPressed ? 1f :
                           UnityEngine.InputSystem.Keyboard.current.aKey.isPressed ? -1f : 0f;
        float vertical   = UnityEngine.InputSystem.Keyboard.current.wKey.isPressed ? 1f :
                           UnityEngine.InputSystem.Keyboard.current.sKey.isPressed ? -1f : 0f;

        Vector3 currentRotation = transform.rotation.eulerAngles;

        float tiltX = (currentRotation.x > 180) ? currentRotation.x - 360 : currentRotation.x;
        float tiltZ = (currentRotation.z > 180) ? currentRotation.z - 360 : currentRotation.z;

        tiltX -= vertical   * tiltSpeed * Time.deltaTime;
        tiltZ += horizontal * tiltSpeed * Time.deltaTime;

        tiltX = Mathf.Clamp(tiltX, -maxTiltAngle, maxTiltAngle);
        tiltZ = Mathf.Clamp(tiltZ, -maxTiltAngle, maxTiltAngle);

        transform.rotation = Quaternion.Euler(tiltX, 0f, tiltZ);

        // Wake up the ball so physics kicks in every frame
        if (ballRigidbody != null)
            ballRigidbody.WakeUp();
    }
}