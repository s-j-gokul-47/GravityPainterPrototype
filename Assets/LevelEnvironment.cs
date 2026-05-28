using UnityEngine;

/// <summary>
/// Spawns visual land under floating tile platforms (no colliders — gameplay tiles stay as-is).
/// Add to an empty GameObject centered on your level and assign materials.
/// </summary>
public class LevelEnvironment : MonoBehaviour
{
    [Header("Planet surface (far below tiles — world Y)")]
    [SerializeField] private bool createPlanetSurface = true;
    [Tooltip("World Y of the planet ground. Tiles are ~0; use -30 or lower so land looks far below.")]
    [SerializeField] private float planetSurfaceY = -40f;
    [SerializeField] private Vector2 planetSize = new Vector2(120f, 120f);
    [SerializeField] private Material planetMaterial;

    [Header("Platform deck (optional thin slab — usually off)")]
    [SerializeField] private bool createPlatformDeck = false;
    [SerializeField] private Vector3 deckLocalPosition = new Vector3(0f, -0.55f, 0f);
    [SerializeField] private Vector3 deckScale = new Vector3(28f, 0.12f, 40f);
    [SerializeField] private Material deckMaterial;

    private void Start()
    {
        if (createPlanetSurface)
        {
            CreatePlanetSurface();
        }

        if (createPlatformDeck)
        {
            CreatePlatformDeck();
        }
    }

    private void CreatePlanetSurface()
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.name = "PlanetSurface";
        surface.transform.SetParent(transform, false);

        Vector3 worldCenter = transform.TransformPoint(Vector3.zero);
        surface.transform.position = new Vector3(worldCenter.x, planetSurfaceY, worldCenter.z);
        surface.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        surface.transform.localScale = new Vector3(planetSize.x, planetSize.y, 1f);

        ApplyMaterial(surface, planetMaterial);
        RemoveCollider(surface);
    }

    private void CreatePlatformDeck()
    {
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "PlatformDeck";
        deck.transform.SetParent(transform, false);
        deck.transform.localPosition = deckLocalPosition;
        deck.transform.localScale = deckScale;

        ApplyMaterial(deck, deckMaterial);
        RemoveCollider(deck);
    }

    private static void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat == null)
        {
            return;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = mat;
        }
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }
    }
}
