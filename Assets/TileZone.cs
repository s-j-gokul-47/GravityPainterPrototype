using UnityEngine;

public enum ZoneType { None, Red, Blue, Yellow }

public class TileZone : MonoBehaviour
{
    public ZoneType zoneType = ZoneType.None;

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material noneMat;

    private Renderer tileRenderer;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        UpdateVisual();
    }

    public void CycleZone()
    {
        if (zoneType == ZoneType.None)        zoneType = ZoneType.Red;
        else if (zoneType == ZoneType.Red)    zoneType = ZoneType.Blue;
        else if (zoneType == ZoneType.Blue)   zoneType = ZoneType.Yellow;
        else if (zoneType == ZoneType.Yellow) zoneType = ZoneType.None;

        UpdateVisual();
        Debug.Log("Tile changed to: " + zoneType);
    }

    public void UpdateVisual()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<Renderer>();

        switch (zoneType)
        {
            case ZoneType.Red:    tileRenderer.material = redMat;    break;
            case ZoneType.Blue:   tileRenderer.material = blueMat;   break;
            case ZoneType.Yellow: tileRenderer.material = yellowMat; break;
            default:              tileRenderer.material = noneMat;   break;
        }
    }

    public Vector3 GetForceDirection()
    {
        switch (zoneType)
        {
            case ZoneType.Red:    return Vector3.forward;
            case ZoneType.Blue:   return Vector3.left;
            case ZoneType.Yellow: return Vector3.right;
            default:              return Vector3.zero;
        }
    }
}