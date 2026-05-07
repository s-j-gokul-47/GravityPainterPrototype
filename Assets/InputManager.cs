using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
{
    if (mainCamera == null) return;

    bool inputDetected = false;
    Vector2 inputPosition = Vector2.zero;

    // Mouse (PC)
    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
    {
        inputDetected = true;
        inputPosition = Mouse.current.position.ReadValue();
    }

    // Touch (Android)
    if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
    {
        inputDetected = true;
        inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
    }

    if (inputDetected)
    {
        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            TileZone tile = hit.collider.GetComponent<TileZone>();
            if (tile != null)
            {
                tile.CycleZone();
            }
        }
    }
}
}