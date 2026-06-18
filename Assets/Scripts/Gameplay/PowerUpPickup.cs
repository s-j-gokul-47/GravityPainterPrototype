using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public PowerUpType powerUpType;

    [Header("Float Animation")]
    public float floatAmplitude = 0.35f;
    public float floatFrequency = 1.5f;
    public float rotationSpeed = 120f;

    [Header("Visual")]
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private Light pointLight;

    private Vector3 _basePosition;

    private void Start()
    {
        _basePosition = transform.position;

        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<Renderer>();

        if (pointLight == null)
            pointLight = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        FloatAnimation();
        RotateAnimation();
    }

    private void FloatAnimation()
    {
        Vector3 pos = _basePosition;
        pos.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = pos;
    }

    private void RotateAnimation()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponentInParent<BallController>();
        if (ball == null) return;

        PowerUpManager manager = ball.GetComponent<PowerUpManager>();
        if (manager == null) return;

        manager.Activate(powerUpType);
        SpawnPickupEffect();
        Destroy(gameObject);
    }

    private void SpawnPickupEffect()
    {
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.transform.position = transform.position;
        burst.transform.localScale = Vector3.one * 0.3f;
        Destroy(burst.GetComponent<Collider>());

        Renderer r = burst.GetComponent<Renderer>();
        Color color = powerUpType switch
        {
            PowerUpType.Speed => new Color(1f, 0.5f, 0f),
            PowerUpType.Magnet => new Color(0.2f, 0.5f, 1f),
            _ => Color.white
        };
        color.a = 0.7f;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_DstBlend", 10f);
        mat.SetFloat("_SrcBlend", 5f);
        mat.renderQueue = 3000;
        r.sharedMaterial = mat;

        burst.AddComponent<AutoDestroy>().lifetime = 0.6f;
        burst.transform.localScale = Vector3.zero;

        // Animate scale up + fade out
        burst.AddComponent<PickupBurstAnimation>();
    }
}

internal class AutoDestroy : MonoBehaviour
{
    public float lifetime = 0.6f;
    private void Start() => Destroy(gameObject, lifetime);
}

internal class PickupBurstAnimation : MonoBehaviour
{
    private float _elapsed;
    private Renderer _r;
    private Material _mat;

    private void Start()
    {
        _r = GetComponent<Renderer>();
        if (_r != null) _mat = _r.material;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / 0.6f;

        float scale = Mathf.Lerp(0f, 1.5f, Mathf.Min(t * 3f, 1f));
        transform.localScale = Vector3.one * scale;

        if (_mat != null)
        {
            Color c = _mat.color;
            c.a = Mathf.Lerp(0.7f, 0f, t);
            _mat.color = c;
        }
    }
}
