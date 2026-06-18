using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PowerUpType
{
    Speed,
    Magnet
}

public class PowerUpManager : MonoBehaviour
{
    [Header("Duration")]
    public float powerUpDuration = 7f;

    [Header("Speed")]
    public float speedMultiplier = 1.5f;

    [Header("Magnet")]
    public float magnetRadius = 15f;
    public float magnetAttractSpeed = 18f;

    public bool HasMagnet { get; private set; }
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    private Dictionary<PowerUpType, float> _timers = new();
    private GameObject _speedIndicator;
    private GameObject _magnetIndicator;
    private BallController _ball;
    private int _powerUpsLayer = -1;

    private TextMeshProUGUI _timerText;
    private GameObject _timerCanvas;
    private StringBuilder _sb = new StringBuilder();

    private void Awake()
    {
        _ball = GetComponent<BallController>();
        if (_ball == null)
            enabled = false;
        CreateIndicators();
    }

    private void Start()
    {
        CreateTimerUI();
    }

    private void CreateTimerUI()
    {
        GameObject canvasObj = GameObject.Find("PowerUpHUD");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("PowerUpHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
        }

        GameObject textObj = new GameObject("PowerUpTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(400f, 200f);

        _timerText = textObj.GetComponent<TextMeshProUGUI>();
        _timerText.fontSize = 38f;
        _timerText.fontStyle = FontStyles.Bold;
        _timerText.alignment = TextAlignmentOptions.TopLeft;
        _timerText.text = "";
        _timerCanvas = canvasObj;
    }

    private void CreateIndicators()
    {
        _speedIndicator = CreateIndicatorSphere(new Color(1f, 0.6f, 0f, 0.25f), 1.15f, "SpeedIndicator");
        _magnetIndicator = CreateIndicatorSphere(new Color(0.2f, 0.5f, 1f, 0.25f), 1.4f, "MagnetIndicator");
    }

    private GameObject CreateIndicatorSphere(Color color, float radius, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * radius * 2f;
        Destroy(go.GetComponent<Collider>());

        Renderer r = go.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_DstBlend", 10f);
        mat.SetFloat("_SrcBlend", 5f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.renderQueue = 3000;
        r.sharedMaterial = mat;
        go.SetActive(false);
        return go;
    }

    private void UpdateTimerDisplay()
    {
        if (_timerText == null) return;

        if (_timers.Count == 0)
        {
            if (_timerText.gameObject.activeSelf)
                _timerText.gameObject.SetActive(false);
            return;
        }

        if (!_timerText.gameObject.activeSelf)
            _timerText.gameObject.SetActive(true);

        _sb.Clear();
        bool first = true;
        foreach (var kvp in _timers)
        {
            if (!first) _sb.AppendLine();
            first = false;

            _sb.Append("<color=#");
            switch (kvp.Key)
            {
                case PowerUpType.Speed: _sb.Append("FF8000"); break;
                case PowerUpType.Magnet: _sb.Append("3380FF"); break;
            }
            _sb.Append(">");
            _sb.Append(kvp.Key.ToString().ToUpperInvariant());
            _sb.Append(" ");
            _sb.Append(kvp.Value.ToString("F1"));
            _sb.Append("s</color>");
        }
        _timerText.text = _sb.ToString();
    }

    public void Activate(PowerUpType type)
    {
        _timers[type] = powerUpDuration;

        switch (type)
        {
            case PowerUpType.Speed:
                CurrentSpeedMultiplier = speedMultiplier;
                _speedIndicator.SetActive(true);
                break;
            case PowerUpType.Magnet:
                HasMagnet = true;
                _magnetIndicator.SetActive(true);
                break;
        }
    }

    private void Update()
    {
        TickTimers();
        UpdateTimerDisplay();

        if (HasMagnet)
            AttractCoins();
    }

    private void TickTimers()
    {
        if (_timers.Count == 0) return;

        List<PowerUpType> expired = null;
        List<PowerUpType> keys = new List<PowerUpType>(_timers.Keys);

        foreach (var type in keys)
        {
            _timers[type] -= Time.deltaTime;
            if (_timers[type] <= 0f)
            {
                expired ??= new List<PowerUpType>();
                expired.Add(type);
            }
        }

        if (expired == null) return;
        foreach (var type in expired)
        {
            _timers.Remove(type);
            Deactivate(type);
        }
    }

    private void Deactivate(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Speed:
                CurrentSpeedMultiplier = 1f;
                _speedIndicator.SetActive(false);
                break;
            case PowerUpType.Magnet:
                HasMagnet = false;
                _magnetIndicator.SetActive(false);
                break;
        }
    }

    private void AttractCoins()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            Coin coin = col.GetComponent<Coin>();
            if (coin == null) continue;

            Vector3 dir = transform.position - col.transform.position;
            float dist = dir.magnitude;
            if (dist < 0.5f) continue;

            float speed = magnetAttractSpeed * (1f + (magnetRadius - dist) / magnetRadius);
            col.transform.position = Vector3.MoveTowards(
                col.transform.position, transform.position, speed * Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        if (_timers.Count > 0)
        {
            var keys = new List<PowerUpType>(_timers.Keys);
            foreach (var type in keys)
                Deactivate(type);
            _timers.Clear();
        }
    }

    private void OnDestroy()
    {
        if (_timerCanvas != null && _timerCanvas.name == "PowerUpHUD")
            Destroy(_timerCanvas);
    }
}
