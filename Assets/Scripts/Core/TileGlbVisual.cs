using UnityEngine;

/// <summary>
/// Combined tiles.glb on one tile: old grey tile always stays; tap overlays blue / red / yellow on that region only.
/// Tune TileGlbVisualRoot + TilesGlbMesh on one tile, then use the editor menu to copy to all tiles.
/// </summary>
[ExecuteAlways]
public class TileGlbVisual : MonoBehaviour
{
    public const string VisualRootName = "TileGlbVisualRoot";
    public const string VisualChildName = "TilesGlbMesh";
    public const string DefaultPrefabResourcePath = "Visuals/Tiles/TilesGlbMesh";

    private const string BlueMeshName = "Mesh_0";
    private const string RedMeshName = "Mesh_0.001";
    private const string YellowMeshName = "Mesh_0.004";

    [SerializeField] private GameObject tilesMeshPrefab;
    [Tooltip("When off (default), your scene transforms are kept. Turn on only if you want script auto-fit.")]
    [SerializeField] private bool useAutomaticLayout = false;
    [SerializeField] private bool autoAlignStripLayoutToTile = true;
    [SerializeField] private Vector3 modelEuler = new Vector3(0f, 270f, 0f);

    private Transform visualRoot;
    private GameObject visualInstance;
    private GameObject blueStrip;
    private GameObject redStrip;
    private GameObject yellowStrip;
    private GameObject leftStrip;
    private GameObject rightStrip;

    public GameObject TilesMeshPrefab => tilesMeshPrefab;

    private void Start()
    {
        if (Application.isPlaying)
        {
            EnsureVisualHierarchy();
            ResolveStripMeshes();
            TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(visualInstance);

            TileZone zone = TileZone.GetPrimaryZone(gameObject);
            ApplyZoneVisual(zone != null ? zone.zoneType : ZoneType.None);
        }
    }

    /// <summary>
    /// Ensures hierarchy + materials. Does not move transforms unless useAutomaticLayout is on.
    /// </summary>
    public void RefreshVisual()
    {
        if (tilesMeshPrefab == null)
        {
            tilesMeshPrefab = Resources.Load<GameObject>(DefaultPrefabResourcePath);
        }

        if (tilesMeshPrefab == null)
        {
            return;
        }

        EnsureVisualHierarchy();
        ResolveStripMeshes();
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(visualInstance);

        if (useAutomaticLayout)
        {
            AlignAndFitVisual();
        }

        TileZone zone = TileZone.GetPrimaryZone(gameObject);
        ApplyZoneVisual(zone != null ? zone.zoneType : ZoneType.None);

#if UNITY_EDITOR
        MarkVisualTransformsDirty();
#endif
    }

    public bool TryCaptureLayout(out TileGlbVisualLayout layout)
    {
        layout = default;
        if (!TryResolveVisualTransforms(out Transform root, out Transform mesh))
        {
            return false;
        }

        layout = new TileGlbVisualLayout
        {
            rootLocalPosition = root.localPosition,
            rootLocalRotation = root.localRotation,
            rootLocalScale = root.localScale,
            meshLocalPosition = mesh.localPosition,
            meshLocalRotation = mesh.localRotation,
            meshLocalScale = mesh.localScale,
            autoAlignStripLayoutToTile = autoAlignStripLayoutToTile,
            modelEuler = modelEuler
        };
        return true;
    }

    public void ApplyLayout(TileGlbVisualLayout layout, bool compensateRootYScale = true)
    {
        autoAlignStripLayoutToTile = layout.autoAlignStripLayoutToTile;
        modelEuler = layout.modelEuler;

        EnsureVisualHierarchy();

        if (!TryResolveVisualTransforms(out Transform root, out Transform mesh))
        {
            return;
        }

        root.localPosition = layout.rootLocalPosition;
        root.localRotation = layout.rootLocalRotation;
        if (compensateRootYScale)
        {
            float parentY = transform.localScale.y;
            float compensateY = parentY > 0.01f ? 1f / parentY : 1f;
            root.localScale = new Vector3(layout.rootLocalScale.x, compensateY, layout.rootLocalScale.z);
        }
        else
        {
            root.localScale = layout.rootLocalScale;
        }

        mesh.localPosition = layout.meshLocalPosition;
        mesh.localRotation = layout.meshLocalRotation;
        mesh.localScale = layout.meshLocalScale;

        ResolveStripMeshes();
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(visualInstance);

        TileZone zone = TileZone.GetPrimaryZone(gameObject);
        ApplyZoneVisual(zone != null ? zone.zoneType : ZoneType.None);

#if UNITY_EDITOR
        MarkVisualTransformsDirty();
#endif
    }

    /// <summary>
    /// Old tile always visible. None = no GLB overlay. Blue / Red / Yellow = that strip only.
    /// </summary>
    public void ApplyZoneVisual(ZoneType zoneType)
    {
        EnsureVisualHierarchy();
        ResolveStripMeshes();
        ResolveStripSidesFromLayout();

        SetLegacyTileVisible(true);

        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(true);
        }

        // Tap left/right (TileZone) → physically left/right strip on this tile.
        SetStripActive(leftStrip, zoneType == ZoneType.Blue);
        SetStripActive(redStrip, zoneType == ZoneType.Red);
        SetStripActive(rightStrip, zoneType == ZoneType.Yellow);
    }

    /// <summary>
    /// Finds which GLB piece is on the tap-left vs tap-right side (matches TileZone, not model +Z).
    /// </summary>
    private void ResolveStripSidesFromLayout()
    {
        leftStrip = blueStrip;
        rightStrip = yellowStrip;

        if (blueStrip == null || yellowStrip == null)
        {
            return;
        }

        Vector3 planarLeft = GetTilePlanarLeftWorld();
        if (planarLeft.sqrMagnitude < 1e-8f)
        {
            return;
        }

        Vector3 reference = GetStripSideReferencePoint();
        Vector3 blueOffset = blueStrip.transform.position - reference;
        Vector3 yellowOffset = yellowStrip.transform.position - reference;
        blueOffset.y = 0f;
        yellowOffset.y = 0f;

        if (blueOffset.sqrMagnitude < 1e-8f && yellowOffset.sqrMagnitude < 1e-8f)
        {
            return;
        }

        float blueOnLeft = blueOffset.sqrMagnitude > 1e-8f
            ? Vector3.Dot(blueOffset.normalized, planarLeft.normalized)
            : -1f;
        float yellowOnLeft = yellowOffset.sqrMagnitude > 1e-8f
            ? Vector3.Dot(yellowOffset.normalized, planarLeft.normalized)
            : 1f;

        if (yellowOnLeft > blueOnLeft)
        {
            leftStrip = yellowStrip;
            rightStrip = blueStrip;
        }
    }

    private Vector3 GetStripSideReferencePoint()
    {
        if (redStrip != null)
        {
            return redStrip.transform.position;
        }

        if (TryGetComponent(out BoxCollider box))
        {
            return transform.TransformPoint(box.center);
        }

        return transform.position;
    }

    private Vector3 GetTilePlanarLeftWorld()
    {
        TileZone zone = TileZone.GetPrimaryZone(gameObject);
        if (zone != null)
        {
            zone.GetPlanarBasis(out _, out Vector3 planarLeft, out _);
            planarLeft.y = 0f;
            if (planarLeft.sqrMagnitude > 1e-8f)
            {
                return planarLeft.normalized;
            }
        }

        Vector3 fallback = Vector3.Cross(Vector3.up, transform.forward);
        fallback.y = 0f;
        return fallback.sqrMagnitude > 1e-8f ? fallback.normalized : Vector3.left;
    }

    public void SetTilesMeshPrefab(GameObject prefab)
    {
        tilesMeshPrefab = prefab;
        blueStrip = null;
        redStrip = null;
        yellowStrip = null;
        DestroyVisualInstance();
        RefreshVisual();
    }

    private void EnsureVisualHierarchy()
    {
        if (visualRoot == null)
        {
            Transform existing = transform.Find(VisualRootName);
            visualRoot = existing != null ? existing : CreateVisualRoot().transform;
        }

        Transform child = visualRoot.Find(VisualChildName);
        if (child != null)
        {
            visualInstance = child.gameObject;
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            visualInstance = UnityEditor.PrefabUtility.InstantiatePrefab(tilesMeshPrefab, visualRoot) as GameObject;
        }
        else
#endif
        {
            visualInstance = Instantiate(tilesMeshPrefab, visualRoot);
        }

        if (visualInstance == null)
        {
            visualInstance = Instantiate(tilesMeshPrefab, visualRoot);
        }

        visualInstance.name = VisualChildName;
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.Euler(modelEuler);
        visualInstance.transform.localScale = Vector3.one;
    }

    private bool TryResolveVisualTransforms(out Transform root, out Transform mesh)
    {
        root = transform.Find(VisualRootName);
        mesh = root != null ? root.Find(VisualChildName) : null;
        if (root != null)
        {
            visualRoot = root;
        }

        if (mesh != null)
        {
            visualInstance = mesh.gameObject;
        }

        return root != null && mesh != null;
    }

    private void ResolveStripMeshes()
    {
        if (visualInstance == null)
        {
            return;
        }

        if (blueStrip != null && redStrip != null && yellowStrip != null)
        {
            return;
        }

        blueStrip = null;
        redStrip = null;
        yellowStrip = null;

        foreach (Transform child in visualInstance.GetComponentsInChildren<Transform>(true))
        {
            if (child == visualInstance.transform)
            {
                continue;
            }

            string name = child.name;
            if (name == BlueMeshName)
            {
                blueStrip = child.gameObject;
            }
            else if (name == RedMeshName)
            {
                redStrip = child.gameObject;
            }
            else if (name == YellowMeshName)
            {
                yellowStrip = child.gameObject;
            }
        }
    }

    private void SetLegacyTileVisible(bool visible)
    {
        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = visible;
            TileZone zone = TileZone.GetPrimaryZone(gameObject);
            if (visible && zone != null && zone.noneMat != null)
            {
                rootRenderer.sharedMaterial = zone.noneMat;
            }
        }

        Transform floor = transform.Find("Floor5Visual");
        if (floor != null)
        {
            floor.gameObject.SetActive(visible);
        }

        TileZoneIndicator indicator = GetComponentInChildren<TileZoneIndicator>(true);
        if (indicator != null)
        {
            indicator.gameObject.SetActive(false);
        }
    }

    private static void SetStripActive(GameObject strip, bool active)
    {
        if (strip != null && strip.activeSelf != active)
        {
            strip.SetActive(active);
        }
    }

    private GameObject CreateVisualRoot()
    {
        GameObject root = new GameObject(VisualRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        visualRoot = root.transform;
        return root;
    }

    private void AlignAndFitVisual()
    {
        if (visualInstance == null || visualRoot == null)
        {
            return;
        }

        ResolveStripMeshes();
        SetStripActive(blueStrip, true);
        SetStripActive(redStrip, true);
        SetStripActive(yellowStrip, true);

        Quaternion layoutRotation = Quaternion.Euler(modelEuler);
        if (autoAlignStripLayoutToTile)
        {
            Vector3 planarLeftLocal = GetTilePlanarLeftInVisualRoot();
            planarLeftLocal.y = 0f;
            if (planarLeftLocal.sqrMagnitude > 1e-8f)
            {
                layoutRotation = Quaternion.FromToRotation(Vector3.forward, planarLeftLocal.normalized);
            }
        }

        GetTileFootprint(out Vector3 targetSize, out Vector3 targetCenter);
        FitMeshToFootprint(targetSize, targetCenter, layoutRotation);

        TileZone zone = TileZone.GetPrimaryZone(gameObject);
        ApplyZoneVisual(zone != null ? zone.zoneType : ZoneType.None);
    }

    private void FitMeshToFootprint(Vector3 targetSize, Vector3 targetCenter, Quaternion layoutRotation)
    {
        visualInstance.transform.localRotation = layoutRotation;
        visualInstance.transform.localScale = Vector3.one;
        visualInstance.transform.localPosition = Vector3.zero;

        Bounds meshInTile = GetRendererBoundsInSpace(visualInstance.transform, transform);
        if (meshInTile.size.sqrMagnitude < 1e-8f)
        {
            return;
        }

        visualInstance.transform.localScale = new Vector3(
            targetSize.x / Mathf.Max(meshInTile.size.x, 0.001f),
            targetSize.y / Mathf.Max(meshInTile.size.y, 0.001f),
            targetSize.z / Mathf.Max(meshInTile.size.z, 0.001f));

        meshInTile = GetRendererBoundsInSpace(visualInstance.transform, transform);
        Vector3 centerOffset = targetCenter - meshInTile.center;
        visualInstance.transform.localPosition = visualRoot.InverseTransformVector(
            transform.TransformVector(centerOffset));
        visualInstance.transform.localRotation = layoutRotation;
    }

    private Vector3 GetTilePlanarLeftInVisualRoot()
    {
        TileZone zone = TileZone.GetPrimaryZone(gameObject);
        Vector3 planarLeft;
        if (zone != null)
        {
            zone.GetPlanarBasis(out _, out planarLeft, out _);
        }
        else
        {
            planarLeft = Vector3.Cross(Vector3.up, transform.forward);
        }

        planarLeft.y = 0f;
        return visualRoot.InverseTransformDirection(
            planarLeft.sqrMagnitude > 1e-8f ? planarLeft.normalized : Vector3.left);
    }

    private void GetTileFootprint(out Vector3 size, out Vector3 center)
    {
        Transform floor = transform.Find("Floor5Visual");
        if (floor != null)
        {
            Bounds bounds = GetRendererBoundsInSpace(floor, transform);
            if (bounds.size.sqrMagnitude > 1e-8f)
            {
                size = bounds.size;
                center = bounds.center;
                return;
            }
        }

        if (TryGetComponent(out BoxCollider box))
        {
            size = box.size;
            center = box.center;
            return;
        }

        size = Vector3.one;
        center = Vector3.zero;
    }

    private static Bounds GetRendererBoundsInSpace(Transform meshRoot, Transform space)
    {
        Renderer[] renderers = meshRoot.GetComponentsInChildren<Renderer>(true);
        Bounds result = default;
        bool initialized = false;

        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            Vector3 min = space.InverseTransformPoint(world.min);
            Vector3 max = space.InverseTransformPoint(world.max);

            if (!initialized)
            {
                result = new Bounds((min + max) * 0.5f, max - min);
                initialized = true;
            }
            else
            {
                result.Encapsulate(min);
                result.Encapsulate(max);
            }
        }

        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh GLB Visual (keep manual transforms)")]
    private void RefreshGlbVisualContextMenu()
    {
        RefreshVisual();
    }

    private void MarkVisualTransformsDirty()
    {
        if (visualRoot != null)
        {
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(visualRoot);
        }

        if (visualInstance != null)
        {
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(visualInstance.transform);
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void DestroyVisualInstance()
    {
        if (visualRoot != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(visualRoot.gameObject);
            }
            else
#endif
            {
                Destroy(visualRoot.gameObject);
            }
        }

        visualRoot = null;
        visualInstance = null;
        blueStrip = null;
        redStrip = null;
        yellowStrip = null;
        leftStrip = null;
        rightStrip = null;
    }
}
