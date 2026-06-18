using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool _activated;

    private void Awake()
    {
        BuildFlagVisual();
        BoxCollider col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(4f, 4f, 5f);
    }

    private void BuildFlagVisual()
    {
        GameObject pole = CreatePrimitiveChild(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.08f, 1.5f, 0.08f));
        SetColor(pole, Color.white);

        GameObject flagWhite = CreatePrimitiveChild(PrimitiveType.Cube, new Vector3(0.6f, 1.4f, 0f), new Vector3(0.7f, 0.5f, 0.04f));
        SetColor(flagWhite, Color.white);

        GameObject flagBlack = CreatePrimitiveChild(PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(0.7f, 0.5f, 0.04f));
        SetColor(flagBlack, Color.black);
    }

    private GameObject CreatePrimitiveChild(PrimitiveType type, Vector3 localPos, Vector3 localScale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        return go;
    }

    private void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        r.sharedMaterial = mat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        BallController ball = other.GetComponentInParent<BallController>();
        if (ball != null)
        {
            _activated = true;
            ball.SetCheckpoint(transform.position);
        }
    }
}
