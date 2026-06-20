using UnityEngine;

public class BallPreviewRenderer : MonoBehaviour
{
    public Camera previewCamera;
    public Transform spawnPoint;
    public float rotationSpeed = 30f;
    public float previewScale = 1f;

    private GameObject _currentPreview;

    private void Awake()
    {
        Debug.Log("[BallPreviewRenderer] Awake");
        if (previewCamera == null)
        {
            previewCamera = GetComponentInChildren<Camera>();
            Debug.Log("[BallPreviewRenderer] Auto-found camera: " + (previewCamera != null));
        }
        if (spawnPoint == null)
        {
            spawnPoint = transform;
            Debug.Log("[BallPreviewRenderer] Using self as spawn point");
        }
    }

    private void Update()
    {
        if (_currentPreview != null)
            _currentPreview.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void ShowSkin(BallSkinData skin)
    {
        Debug.Log("[BallPreviewRenderer] ShowSkin: " + (skin != null ? skin.skinName : "null"));

        ClearPreview();

        if (skin == null || string.IsNullOrEmpty(skin.prefabResourcePath))
        {
            Debug.LogWarning("[BallPreviewRenderer] Invalid skin data");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(skin.prefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError("[BallPreviewRenderer] Skin prefab not found at: " + skin.prefabResourcePath);
            return;
        }

        _currentPreview = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        Debug.Log("[BallPreviewRenderer] Instantiated skin: " + skin.skinName);

        foreach (Collider col in _currentPreview.GetComponentsInChildren<Collider>(true))
            Destroy(col);
        foreach (Rigidbody rb in _currentPreview.GetComponentsInChildren<Rigidbody>(true))
            Destroy(rb);

        FitToUniformScale(_currentPreview);
        _currentPreview.transform.localRotation = Quaternion.identity;
    }

    public void ClearPreview()
    {
        if (_currentPreview != null)
        {
            Debug.Log("[BallPreviewRenderer] Clearing previous preview");
            Destroy(_currentPreview);
            _currentPreview = null;
        }
    }

    private void FitToUniformScale(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            obj.transform.localScale = Vector3.one * previewScale;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float scale = previewScale / Mathf.Max(maxExtent, 0.0001f);
        obj.transform.localScale = Vector3.one * scale;

        Vector3 center = bounds.center;
        obj.transform.position = spawnPoint.position - (center - obj.transform.position) * (1f - 1f / scale);
        Debug.Log("[BallPreviewRenderer] Scaled to: " + scale);
    }

    private void OnDestroy()
    {
        ClearPreview();
    }
}
