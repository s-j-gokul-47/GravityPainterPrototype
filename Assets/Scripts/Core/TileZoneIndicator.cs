using UnityEngine;

/// <summary>
/// Colored edge strips on a tile: red = forward, blue = left, yellow = right.
/// Floor mesh keeps its material; only these lights show the active zone.
/// </summary>
public class TileZoneIndicator : MonoBehaviour
{
    private Renderer forwardStrip;
    private Renderer leftStrip;
    private Renderer rightStrip;

    public static TileZoneIndicator Ensure(Transform tileRoot)
    {
        Transform existing = tileRoot.Find("ZoneIndicators");
        if (existing != null)
        {
            TileZoneIndicator indicator = existing.GetComponent<TileZoneIndicator>();
            if (indicator != null)
            {
                return indicator;
            }
        }

        GameObject root = new GameObject("ZoneIndicators");
        root.transform.SetParent(tileRoot, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        TileZoneIndicator created = root.AddComponent<TileZoneIndicator>();
        created.forwardStrip = CreateStrip(root.transform, "Forward", new Vector3(0f, 0.55f, 0.42f), new Vector3(0.82f, 0.1f, 0.14f));
        created.leftStrip = CreateStrip(root.transform, "Left", new Vector3(-0.42f, 0.55f, 0f), new Vector3(0.14f, 0.1f, 0.82f));
        created.rightStrip = CreateStrip(root.transform, "Right", new Vector3(0.42f, 0.55f, 0f), new Vector3(0.14f, 0.1f, 0.82f));
        return created;
    }

    public void AlignStripsToTile(Transform tileRoot, Vector3 planarForward, Vector3 planarLeft, Vector3 planarRight)
    {
        if (tileRoot == null)
        {
            return;
        }

        const float edgeDistance = 0.42f;
        const float stripHeight = 0.55f;

        Vector3 localForward = tileRoot.InverseTransformDirection(planarForward.normalized) * edgeDistance;
        Vector3 localLeft = tileRoot.InverseTransformDirection(planarLeft.normalized) * edgeDistance;
        Vector3 localRight = tileRoot.InverseTransformDirection(planarRight.normalized) * edgeDistance;

        if (forwardStrip != null)
        {
            forwardStrip.transform.localPosition = new Vector3(localForward.x, stripHeight, localForward.z);
        }

        if (leftStrip != null)
        {
            leftStrip.transform.localPosition = new Vector3(localLeft.x, stripHeight, localLeft.z);
        }

        if (rightStrip != null)
        {
            rightStrip.transform.localPosition = new Vector3(localRight.x, stripHeight, localRight.z);
        }
    }

    public void SetZone(ZoneType zone, Material redMat, Material blueMat, Material yellowMat)
    {
        SetStrip(forwardStrip, false, null);
        SetStrip(leftStrip, false, null);
        SetStrip(rightStrip, false, null);

        switch (zone)
        {
            case ZoneType.Red:
                SetStrip(forwardStrip, true, redMat);
                break;
            case ZoneType.Blue:
                SetStrip(leftStrip, true, blueMat);
                break;
            case ZoneType.Yellow:
                SetStrip(rightStrip, true, yellowMat);
                break;
        }
    }

    private static void SetStrip(Renderer strip, bool visible, Material mat)
    {
        if (strip == null)
        {
            return;
        }

        strip.gameObject.SetActive(visible);
        if (visible && mat != null)
        {
            strip.sharedMaterial = mat;
        }
    }

    private static Renderer CreateStrip(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localRotation = Quaternion.identity;

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        return go.GetComponent<Renderer>();
    }
}
