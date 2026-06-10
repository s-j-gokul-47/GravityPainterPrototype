using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a playable procedural level at runtime from a seed:
/// tiles, GLB visuals, ball spawn, and finish line.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ProceduralLevelBuilder : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private LevelGenConfig config;
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool buildOnStart = true;
    [Tooltip("When enabled, changing Seed during Play mode rebuilds the level immediately.")]
    [SerializeField] private bool rebuildWhenSeedChanges = true;

    [Header("Difficulty")]
    [Tooltip("Scale path length, grid size, and turn rate from completed procedural levels.")]
    [SerializeField] private bool useDifficultyProgression = true;

    [Header("Scene References")]
    [SerializeField] private Transform levelRoot;
    [SerializeField] private BallController ball;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Spawn Tuning")]
    [SerializeField] private float ballSpawnHeight = 2f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField, Range(0f, 1f)] private float coinSpawnChance = 0.25f;
    [SerializeField] private float coinSpawnHeight = 0.8f;

    private readonly List<GameObject> _spawnedTiles = new List<GameObject>();
    private ProceduralPathGenerator _generator;
    private LevelGenConfig _activeConfig;
    private int _watchedSeed = int.MinValue;
    private Vector3 _ballSpawnPosition;
    private bool _hasBallSpawn;
    private Coroutine _releaseBallCoroutine;

    public int Seed => seed;
    public int LastBuiltSeed { get; private set; } = -1;
    public int LastBuiltTileCount { get; private set; }
    public float LastBuiltDifficulty { get; private set; } = -1f;
    public string LastBuiltTier =>
        LastBuiltDifficulty >= 0f ? DifficultyManager.GetTierName(LastBuiltDifficulty) : "(none)";
    public IReadOnlyList<GameObject> SpawnedTiles => _spawnedTiles;

    public event Action<int, int> OnLevelBuilt;

    private void Awake()
    {
#if !UNITY_EDITOR
        rebuildWhenSeedChanges = false;
#endif
        HoldBallUntilPlaced();
        if (buildOnStart && Application.isPlaying)
        {
            BuildFromSeed(ResolveStartSeed());
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || !buildOnStart || !rebuildWhenSeedChanges)
        {
            return;
        }

        if (_watchedSeed == int.MinValue)
        {
            _watchedSeed = seed;
            return;
        }

        if (seed != _watchedSeed)
        {
            _watchedSeed = seed;
            BuildFromSeed(seed);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying || !buildOnStart || !rebuildWhenSeedChanges)
        {
            return;
        }

        if (_watchedSeed != int.MinValue && seed != _watchedSeed)
        {
            _watchedSeed = seed;
            BuildFromSeed(seed);
        }
    }
#endif

    /// <summary>
    /// Clears the current level and builds a new path from the given seed.
    /// </summary>
    public bool BuildFromSeed(int buildSeed)
    {
        if (!ValidateConfig())
        {
            return false;
        }

        // Reset session coins at the very start of level generation
        CoinManager.ResetSessionCoins();

        HoldBallUntilPlaced();
        if (_activeConfig != null)
        {
            Destroy(_activeConfig);
        }

        _activeConfig = Instantiate(config);
        _activeConfig.SyncFootprintFromTileScale();
        float difficulty = ResolveDifficulty();
        DifficultyScaler.Apply(_activeConfig, difficulty);
        LastBuiltDifficulty = difficulty;

        EnsureLevelRoot();
        EnsureLevelCompleteUi();
        Time.timeScale = 1f;
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        ProceduralTilePlacement.HidePathPreview();
        ClearLevel();

        _generator ??= new ProceduralPathGenerator();
        List<LevelCell> cells = _generator.GenerateWithRetry(_activeConfig, buildSeed, out int actualSeed);
        if (cells == null || cells.Count == 0)
        {
            Debug.LogError("ProceduralLevelBuilder: path generation failed for seed " + buildSeed);
            return false;
        }

        if (ProceduralTilePlacement.HasMainTileOverlaps(cells, _activeConfig))
        {
            Debug.LogError(
                "ProceduralLevelBuilder: overlap-free path generation failed for seed "
                + buildSeed + " (used " + actualSeed + ").");
            return false;
        }

        GameObject goalTile = null;

        for (int i = 0; i < cells.Count; i++)
        {
            LevelCell cell = cells[i];
            GameObject tile = SpawnTile(cell, i, cells);
            if (tile == null)
            {
                continue;
            }

            _spawnedTiles.Add(tile);

            if (i == cells.Count - 1)
            {
                goalTile = tile;
            }
            else if (i > 0 && coinPrefab != null)
            {
                // Deterministic random so the same level seed always has coins in the same spots
                UnityEngine.Random.State oldState = UnityEngine.Random.state;
                UnityEngine.Random.InitState(actualSeed + i * 73);
                
                if (UnityEngine.Random.value <= coinSpawnChance)
                {
                    Vector3 coinPos = tile.transform.position + Vector3.up * coinSpawnHeight;
                    
                    // Set the specific starting tilt the user requested
                    Quaternion startingRot = Quaternion.Euler(-267.281f, UnityEngine.Random.Range(0f, 360f), 47f);
                    GameObject coinObj = Instantiate(coinPrefab, coinPos, startingRot, levelRoot);
                    
                    // Force the scale to exactly what the user requested
                    coinObj.transform.localScale = new Vector3(1.5f, 0.125f, 1.5f);
                    coinObj.name = "Coin_" + i;
                }
                
                UnityEngine.Random.state = oldState;
            }
        }

        SpawnCornerPads(cells);

        PlaceBall(cells);
        SetupFinishLine(goalTile);

        LastBuiltSeed = actualSeed;
        LastBuiltTileCount = _spawnedTiles.Count;
        _watchedSeed = actualSeed;
        seed = actualSeed;

        Debug.Log(
            "Procedural level built: tier=" + DifficultyManager.GetTierName(difficulty)
            + " (" + difficulty.ToString("F2") + ")"
            + ", path=" + _activeConfig.minPathLength + "-" + _activeConfig.maxPathLength
            + ", grid=" + _activeConfig.gridWidth + "x" + _activeConfig.gridDepth
            + ", turnFreq=" + _activeConfig.turnFrequency.ToString("F2")
            + ", menuLevel=" + LevelProgress.GetSelectedMenuLevel()
            + ", requested seed=" + buildSeed
            + ", used seed=" + actualSeed
            + ", tiles=" + _spawnedTiles.Count
            + ", finish at " + cells[cells.Count - 1].GridPos);

        OnLevelBuilt?.Invoke(actualSeed, _spawnedTiles.Count);
        return true;
    }

    private int ResolveStartSeed()
    {
        return ProceduralSession.GetDeterministicSeedForMenuLevel(
            Mathf.Max(LevelProgress.GetSelectedMenuLevel(), LevelProgress.ProceduralCampaignLevel));
    }

    private void HoldBallUntilPlaced()
    {
        if (ball == null)
        {
            ball = FindFirstObjectByType<BallController>();
        }

        if (ball == null)
        {
            return;
        }

        Rigidbody body = ball.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
        }
    }

    private float ResolveDifficulty()
    {
        if (useDifficultyProgression)
        {
            return LevelProgress.GetSelectedMenuDifficulty();
        }

        return Mathf.Clamp01(config.difficulty);
    }

    /// <summary>Resets the ball to the first tile without rebuilding the layout.</summary>
    public bool ResetBallToStart()
    {
        if (!_hasBallSpawn || ball == null)
        {
            return false;
        }

        Time.timeScale = 1f;
        StopReleaseBallCoroutine();
        Vector3 spawnPosition = ResolveBallSpawnPosition(_ballSpawnPosition);
        _ballSpawnPosition = spawnPosition;
        ball.SuspendAt(spawnPosition);
        _releaseBallCoroutine = StartCoroutine(ReleaseBallWhenReady());
        return true;
    }

    /// <summary>Rebuilds the current procedural layout using the last seed.</summary>
    public void RebuildSameSeed()
    {
        int rebuildSeed = LastBuiltSeed >= 0 ? LastBuiltSeed : seed;
        BuildFromSeed(rebuildSeed);
    }

    /// <summary>Builds the procedural layout for the currently selected menu level.</summary>
    public void RebuildNextSeed()
    {
        BuildFromSeed(ProceduralSession.GetDeterministicSeedForMenuLevel(LevelProgress.GetSelectedMenuLevel()));
    }

    /// <summary>Destroys all tiles spawned by the builder.</summary>
    public void ClearLevel()
    {
        for (int i = _spawnedTiles.Count - 1; i >= 0; i--)
        {
            if (_spawnedTiles[i] != null)
            {
                DestroyTile(_spawnedTiles[i]);
            }
        }

        _spawnedTiles.Clear();

        if (levelRoot != null)
        {
            for (int i = levelRoot.childCount - 1; i >= 0; i--)
            {
                DestroyTile(levelRoot.GetChild(i).gameObject);
            }
        }
    }

    private bool ValidateConfig()
    {
        if (config == null)
        {
            Debug.LogError("ProceduralLevelBuilder: LevelGenConfig is not assigned.");
            return false;
        }

        if (config.tilePrefab == null)
        {
            Debug.LogError("ProceduralLevelBuilder: tilePrefab is missing on " + config.name);
            return false;
        }

        return true;
    }

    private void EnsureLevelRoot()
    {
        if (levelRoot == null)
        {
            Transform existing = transform.Find("LevelRoot");
            if (existing == null)
            {
                GameObject rootObject = new GameObject("LevelRoot");
                rootObject.transform.SetParent(transform, false);
                levelRoot = rootObject.transform;
            }
            else
            {
                levelRoot = existing;
            }
        }
    }

    private GameObject SpawnTile(LevelCell cell, int index, IReadOnlyList<LevelCell> cells)
    {
        GameObject tile = Instantiate(
            _activeConfig.tilePrefab,
            levelRoot.transform);
        ProceduralTilePlacement.ApplyPathTransform(tile.transform, index, cells, _activeConfig);
        tile.name = "Tile_" + cell.PathIndex + "_" + cell.GridPos.x + "_" + cell.GridPos.y;

        ApplyTileVisual(tile);
        return tile;
    }

    private void SpawnCornerPads(IReadOnlyList<LevelCell> cells)
    {
        if (!_activeConfig.addCornerPads || cells == null)
        {
            return;
        }

        var placed = ProceduralTilePlacement.BuildPlacementPlan(cells, _activeConfig);

        for (int i = 2; i < cells.Count; i++)
        {
            if (!ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                continue;
            }

            int padCount = ProceduralTilePlacement.CountCornerPadsForTurn(i, cells, _activeConfig);
            for (int padIndex = 0; padIndex < padCount; padIndex++)
            {
                Vector3 padCenter = ProceduralTilePlacement.ComputeCornerPadPosition(
                    i, padIndex, padCount, cells, _activeConfig);
                float padRotation = cells[i - 1].YRotation;
                Bounds padBounds = ProceduralTileFootprint.ComputeWorldBounds(padCenter, padRotation, _activeConfig);

                // Allow pads to meet the incoming/outgoing turn tiles; block parallel-path stacking.
                if (WouldOverlapNonNeighborMainTiles(padBounds, placed, _activeConfig, i))
                {
                    continue;
                }

                GameObject pad = Instantiate(_activeConfig.tilePrefab, levelRoot.transform);
                ProceduralTilePlacement.ApplyCornerPadTransform(
                    pad.transform, i, padIndex, padCount, cells, _activeConfig);
                pad.name = "Tile_corner_" + i + "_" + padIndex + "_" + cells[i - 1].GridPos.x + "_" + cells[i - 1].GridPos.y;
                ApplyTileVisual(pad);
                _spawnedTiles.Add(pad);

                placed.Add(new ProceduralTilePlacement.PlacedTile
                {
                    Center = padCenter,
                    YRotation = padRotation,
                    IsCornerPad = true
                });
            }
        }
    }

    private static bool WouldOverlapNonNeighborMainTiles(
        Bounds candidate,
        IReadOnlyList<ProceduralTilePlacement.PlacedTile> placed,
        LevelGenConfig levelConfig,
        int turnMainIndex)
    {
        const float margin = 0.04f;
        int incomingMainIndex = turnMainIndex - 1;

        for (int i = 0; i < placed.Count; i++)
        {
            ProceduralTilePlacement.PlacedTile other = placed[i];
            if (other.IsCornerPad || i == incomingMainIndex || i == turnMainIndex)
            {
                continue;
            }

            Bounds otherBounds = ProceduralTileFootprint.ComputeWorldBounds(
                other.Center,
                other.YRotation,
                levelConfig);

            if (ProceduralTileFootprint.BoundsOverlap(candidate, otherBounds, margin))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyTileVisual(GameObject tile)
    {
        TileGlbVisual visual = tile.GetComponent<TileGlbVisual>();
        if (visual == null)
        {
            visual = tile.AddComponent<TileGlbVisual>();
        }

        visual.RefreshVisual();

        if (_activeConfig != null && _activeConfig.glbLayout != null)
        {
            visual.ApplyLayout(_activeConfig.glbLayout.layout);
        }
    }

    private void PlaceBall(IReadOnlyList<LevelCell> cells)
    {
        if (ball == null)
        {
            ball = FindFirstObjectByType<BallController>();
        }

        if (ball == null)
        {
            Debug.LogWarning("ProceduralLevelBuilder: no BallController found in scene.");
            return;
        }

        Vector3 planarCenter = levelRoot.TransformPoint(
            ProceduralTilePlacement.ComputeCenterPosition(0, cells, _activeConfig));
        Vector3 worldPos = ResolveBallSpawnPosition(planarCenter);
        _ballSpawnPosition = worldPos;
        _hasBallSpawn = true;

        StopReleaseBallCoroutine();
        ball.SuspendAt(worldPos);
        _releaseBallCoroutine = StartCoroutine(ReleaseBallWhenReady());
    }

    private Vector3 ResolveBallSpawnPosition(Vector3 worldPlanarCenter)
    {
        Vector3 spawn = worldPlanarCenter;
        spawn.y = ComputeBallSpawnY(worldPlanarCenter);
        return spawn;
    }

    private float ComputeBallSpawnY(Vector3 worldPlanarCenter)
    {
        GameObject firstMainTile = FindFirstMainTile();
        if (firstMainTile != null)
        {
            float colliderTop = GetTileTopY(firstMainTile);
            if (colliderTop > float.NegativeInfinity)
            {
                return colliderTop + GetBallRadius() + 0.08f;
            }

            Renderer[] renderers = firstMainTile.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return bounds.max.y + GetBallRadius() + 0.08f;
            }
        }

        Physics.SyncTransforms();
        if (Physics.Raycast(
                worldPlanarCenter + Vector3.up * 8f,
                Vector3.down,
                out RaycastHit hit,
                16f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point.y + GetBallRadius() + 0.08f;
        }

        return ballSpawnHeight;
    }

    private static float GetTileTopY(GameObject tile)
    {
        float maxY = float.NegativeInfinity;
        Collider[] colliders = tile.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            maxY = Mathf.Max(maxY, collider.bounds.max.y);
        }

        return maxY;
    }

    private IEnumerator ReleaseBallWhenReady()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        if (ball != null && _hasBallSpawn)
        {
            Vector3 spawnPosition = ResolveBallSpawnPosition(_ballSpawnPosition);
            _ballSpawnPosition = spawnPosition;
            ball.SuspendAt(spawnPosition);
        }

        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        if (ball != null)
        {
            ball.ReleasePhysics();
        }

        _releaseBallCoroutine = null;
    }

    private void StopReleaseBallCoroutine()
    {
        if (_releaseBallCoroutine != null)
        {
            StopCoroutine(_releaseBallCoroutine);
            _releaseBallCoroutine = null;
        }
    }

    private GameObject FindFirstMainTile()
    {
        for (int i = 0; i < _spawnedTiles.Count; i++)
        {
            GameObject tile = _spawnedTiles[i];
            if (tile != null && !tile.name.Contains("_corner_"))
            {
                return tile;
            }
        }

        return _spawnedTiles.Count > 0 ? _spawnedTiles[0] : null;
    }

    private float GetBallRadius()
    {
        if (ball == null)
        {
            return 0.5f;
        }

        SphereCollider sphere = ball.GetComponent<SphereCollider>();
        if (sphere == null)
        {
            return 0.5f;
        }

        Vector3 scale = ball.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return sphere.radius * maxScale;
    }

    private void SetupFinishLine(GameObject goalTile)
    {
        if (goalTile == null)
        {
            return;
        }

        SetupFinishLineVisual(goalTile);

        FinishLine finish = goalTile.GetComponent<FinishLine>();
        if (finish == null)
        {
            finish = goalTile.AddComponent<FinishLine>();
        }

        finish.Configure(levelCompletePanel, pause: true);
        EnsureFinishTrigger(goalTile);
    }

    private void SetupFinishLineVisual(GameObject goalTile)
    {
        FinishLineVisual visual = goalTile.GetComponent<FinishLineVisual>();
        if (visual == null)
        {
            visual = goalTile.AddComponent<FinishLineVisual>();
        }

        GameObject prefab = ResolveFinishLinePrefab();
        if (prefab != null)
        {
            visual.ConfigurePrefab(prefab);
        }

        Transform existingRoot = goalTile.transform.Find(FinishLineVisual.VisualRootName);
        if (existingRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }
        }

        visual.EnsureVisual();
    }

    private GameObject ResolveFinishLinePrefab()
    {
        if (config != null && config.finishLinePrefab != null)
        {
            return config.finishLinePrefab;
        }

        return Resources.Load<GameObject>(FinishLineVisual.DefaultPrefabResourcePath);
    }

    private void EnsureLevelCompleteUi()
    {
        if (levelCompletePanel == null)
        {
            levelCompletePanel = LevelCompleteCanvasFactory.EnsureCanvas(this);
        }
        else
        {
            LevelCompleteUI ui = levelCompletePanel.GetComponent<LevelCompleteUI>();
            if (ui != null)
            {
                ui.ConfigureProcedural(this);
            }
        }
    }

    private static void EnsureFinishTrigger(GameObject goalTile)
    {
        Collider[] colliders = goalTile.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].isTrigger)
            {
                return;
            }
        }

        BoxCollider trigger = goalTile.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1f, 1f, 1f);
        trigger.center = new Vector3(0f, 0.5f, 0f);
    }

    private static void DestroyTile(GameObject tile)
    {
        if (Application.isPlaying)
        {
            Destroy(tile);
        }
        else
        {
            DestroyImmediate(tile);
        }
    }
}
