using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds a playable procedural level at runtime from a seed:
/// tiles, GLB visuals, ball spawn, and finish line.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ProceduralLevelBuilder : MonoBehaviour
{
    private const int CheckpointObstacleBreatherRadius = 2;
    private const int CheckpointPowerUpBreatherRadius = 1;
    private const int CheckpointCoinBreatherRadius = 2;

    private const int FinishObstacleBreatherRadius = 2;
    private const int FinishPowerUpBreatherRadius = 1;
    private const int FinishCoinBreatherRadius = 1;

    private enum ObstacleKind
    {
        Hammer,
        Laser,
        Spikes,
    }

    private readonly struct ObstacleTypeDefinition
    {
        public readonly ObstacleKind Kind;
        public readonly GameObject Prefab;
        public readonly float IntroDifficulty;

        public ObstacleTypeDefinition(ObstacleKind kind, GameObject prefab, float introDifficulty)
        {
            Kind = kind;
            Prefab = prefab;
            IntroDifficulty = introDifficulty;
        }
    }

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
    [SerializeField] private Material campaignSkybox;

    [Header("Spawn Tuning")]
    [SerializeField] private float ballSpawnHeight = 2f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField, Range(0f, 1f)] private float coinSpawnChance = 0.25f;
    [SerializeField] private float coinSpawnHeight = 0.8f;

    [Header("Obstacles")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private GameObject spikesPrefab;

    [Header("PowerUps")]
    [SerializeField] private GameObject speedCorePrefab;
    [SerializeField] private GameObject magnetPrefab;
    [SerializeField, Range(0f, 1f)] private float powerUpSpawnChance = 0.15f;
    [SerializeField] private float powerUpSpawnHeight = 1.2f;

    private readonly List<GameObject> _spawnedTiles = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Reset DifficultyManager.LevelsCompleted to 0 each time Play mode starts.")]
    [SerializeField] private bool resetDifficultyOnPlay = false;

    private LevelGenConfig _activeConfig;
    private int _watchedSeed = int.MinValue;
    private Vector3 _ballSpawnPosition;
    private bool _hasBallSpawn;
    private Coroutine _releaseBallCoroutine;
    private ProceduralPathGenerator _generator;

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
        TryLoadPowerUpPrefabs();
        ApplyCampaignEnvironment();
        HoldBallUntilPlaced();
        if (buildOnStart && Application.isPlaying)
        {
            if (resetDifficultyOnPlay)
            {
                DifficultyManager.ResetProgress();
                Debug.Log("[Builder] Difficulty reset to Easy (0) via resetDifficultyOnPlay");
            }
            else
            {
                int completed = DifficultyManager.LevelsCompleted;
                Debug.Log("[Builder] Difficulty: " + DifficultyManager.GetTierName(DifficultyManager.CurrentDifficulty)
                    + " (" + DifficultyManager.CurrentDifficulty.ToString("F2")
                    + ", " + completed + " completed)"
                    + " — use Tools > Gravity Painter to reset");
            }
            BuildFromSeed(ResolveStartSeed());
        }
    }

    private void TryLoadPowerUpPrefabs()
    {
        if (speedCorePrefab != null && magnetPrefab != null)
            return;
#if UNITY_EDITOR
        if (speedCorePrefab == null)
            speedCorePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUps/SpeedCore.prefab");
        if (magnetPrefab == null)
            magnetPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUps/Magnet.prefab");
#endif
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
        if (campaignSkybox == null)
        {
            campaignSkybox = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/SkyCitySkybox.mat");
        }

        if (hammerPrefab == null) hammerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/Hammer.prefab");
        if (laserBeamPrefab == null) laserBeamPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/KorrathBeam.prefab");
        if (spikesPrefab == null) spikesPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/Spikes.prefab");
        if (speedCorePrefab == null) speedCorePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUps/SpeedCore.prefab");
        if (magnetPrefab == null) magnetPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUps/Magnet.prefab");

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

        if (ball != null)
            ball.ClearCheckpoint();

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
        int checkpointTileIndex = cells.Count / 2;
        int finishTileIndex = cells.Count - 1;

        HashSet<int> powerUpIndices = new HashSet<int>();
        int lastPowerUpIndex = -1;
        // End at cells.Count - 2 to ensure at least 1 tile for a coin after a powerup
        for (int i = 1; i < cells.Count - 2; i++)
        {
            if (IsWithinDistance(i, checkpointTileIndex, CheckpointPowerUpBreatherRadius)
                || IsWithinFinishDistance(i, finishTileIndex, FinishPowerUpBreatherRadius))
            {
                continue;
            }
            
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(actualSeed + i * 97);
            
            float chance = powerUpSpawnChance;
            if (IsWithinDistance(i, checkpointTileIndex, CheckpointPowerUpBreatherRadius + 1))
            {
                chance *= 0.35f;
            }

            if (IsWithinFinishDistance(i, finishTileIndex, FinishPowerUpBreatherRadius + 1))
            {
                chance *= 0.35f;
            }

            if (UnityEngine.Random.value <= chance)
            {
                powerUpIndices.Add(i);
                lastPowerUpIndex = i;
            }
            UnityEngine.Random.state = oldState;
        }

        Dictionary<int, ObstacleKind> obstaclesByIndex = BuildObstaclePlan(
            cells,
            checkpointTileIndex,
            finishTileIndex,
            powerUpIndices,
            actualSeed,
            difficulty);
        HashSet<int> obstacleIndices = new HashSet<int>(obstaclesByIndex.Keys);

        HashSet<int> coinIndices = new HashSet<int>();
        for (int i = 1; i < cells.Count - 1; i++)
        {
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(actualSeed + i * 73);
            
            bool isTurnTile = ProceduralTilePlacement.IsTurnIndex(i + 1, cells);
            float chance = isTurnTile ? 0.85f : 0.05f;
            if (IsWithinDistance(i, checkpointTileIndex, CheckpointCoinBreatherRadius))
            {
                chance *= 0.2f;
            }
            else if (IsWithinDistance(i, checkpointTileIndex, CheckpointCoinBreatherRadius + 1))
            {
                chance *= 0.5f;
            }

            if (IsWithinFinishDistance(i, finishTileIndex, FinishCoinBreatherRadius))
            {
                chance *= 0.2f;
            }
            else if (IsWithinFinishDistance(i, finishTileIndex, FinishCoinBreatherRadius + 1))
            {
                chance *= 0.5f;
            }

            if (lastPowerUpIndex >= cells.Count - 6 && (i == lastPowerUpIndex + 1 || i == lastPowerUpIndex + 2))
            {
                chance = 1.0f; // Guarantee coins immediately after a late powerup
            }

            if (UnityEngine.Random.value <= chance)
            {
                int placement = i;
                while (powerUpIndices.Contains(placement) || coinIndices.Contains(placement) || obstacleIndices.Contains(placement)
                    || placement == checkpointTileIndex || placement == finishTileIndex)
                {
                    placement++;
                }
                if (placement < cells.Count - 1)
                {
                    coinIndices.Add(placement);
                }
            }
            UnityEngine.Random.state = oldState;
        }

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
            else if (obstaclesByIndex.TryGetValue(i, out ObstacleKind obstacleKind))
            {
                SpawnObstacle(obstacleKind, tile, i);
                obstacleIndices.Add(i);
            }
            else if (coinIndices.Contains(i) && coinPrefab != null)
            {
                Vector3 coinPos = tile.transform.position;
                Quaternion startingRot = CampaignCoinPlacement.RandomSpawnRotation(actualSeed, i);
                GameObject coinObj = Instantiate(coinPrefab, coinPos, startingRot, levelRoot);
                coinObj.name = "Coin_" + i;
                CampaignCoinPlacement.SnapCoinToTile(coinObj.transform, tile.transform);
            }
        }

        SpawnCornerPads(cells);
        SpawnPowerUps(cells, actualSeed, powerUpIndices);
        SpawnCheckpoint();

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
            + ", obstacles=" + obstaclesByIndex.Count
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

    /// <summary>
    /// Match campaign Levels 1–2 visuals: panoramic city skybox and flat ambient lighting.
    /// </summary>
    private void ApplyCampaignEnvironment()
    {
        if (campaignSkybox == null)
        {
            return;
        }

        RenderSettings.skybox = campaignSkybox;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.24f, 1f);
        RenderSettings.ambientIntensity = 0.85f;
        RenderSettings.reflectionIntensity = 0.35f;
        DynamicGI.UpdateEnvironment();
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

                if (coinPrefab != null)
                {
                    Vector3 coinPos = pad.transform.position;
                    Quaternion startingRot = CampaignCoinPlacement.RandomSpawnRotation(Seed, i * 100 + padIndex);
                    GameObject coinObj = Instantiate(coinPrefab, coinPos, startingRot, levelRoot);
                    coinObj.name = "Coin_CornerPad_" + i + "_" + padIndex;
                    CampaignCoinPlacement.SnapCoinToTile(coinObj.transform, pad.transform);
                }

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

    private Dictionary<int, ObstacleKind> BuildObstaclePlan(
        IReadOnlyList<LevelCell> cells,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices,
        int seedForLevel,
        float difficulty)
    {
        var plan = new Dictionary<int, ObstacleKind>();
        if (cells == null || cells.Count < 5 || _activeConfig == null)
        {
            return plan;
        }

        List<ObstacleTypeDefinition> unlockedTypes = GetUnlockedObstacleTypes(difficulty);
        if (unlockedTypes.Count == 0)
        {
            return plan;
        }

        List<int> validIndices = CollectValidObstacleIndices(
            cells.Count,
            checkpointTileIndex,
            finishTileIndex,
            powerUpIndices);
        if (validIndices.Count == 0)
        {
            return plan;
        }

        int minGap = ResolveObstacleGap(difficulty);
        int maxBySpace = Mathf.Max(0, (validIndices.Count + minGap - 1) / minGap);
        int densityTarget = ResolveObstacleCountFromDensity(difficulty);
        int targetCount = Mathf.Min(_activeConfig.maxObstaclesPerLevel, Mathf.Min(densityTarget, maxBySpace));
        if (targetCount <= 0)
        {
            return plan;
        }

        Shuffle(validIndices, seedForLevel + 1234);
        var picked = new List<int>();
        EnsureAdvancedObstaclePresence(
            plan,
            picked,
            unlockedTypes,
            validIndices,
            minGap,
            cells,
            checkpointTileIndex,
            finishTileIndex,
            powerUpIndices,
            difficulty);

        for (int i = 0; i < validIndices.Count && plan.Count < targetCount; i++)
        {
            int candidate = validIndices[i];
            if (!HasRequiredGap(candidate, picked, minGap))
            {
                continue;
            }

            ObstacleTypeDefinition definition = PickObstacleType(
                unlockedTypes,
                candidate,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty,
                seedForLevel + 7000 + candidate * 97);
            if (definition.Prefab == null)
            {
                continue;
            }

            picked.Add(candidate);
            plan[candidate] = definition.Kind;
        }

        return plan;
    }

    private void EnsureAdvancedObstaclePresence(
        Dictionary<int, ObstacleKind> plan,
        List<int> picked,
        List<ObstacleTypeDefinition> unlockedTypes,
        List<int> validIndices,
        int minGap,
        IReadOnlyList<LevelCell> cells,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices,
        float difficulty)
    {
        if (difficulty >= 0.55f)
        {
            TryPlaceRequiredObstacle(
                ObstacleKind.Laser,
                plan,
                picked,
                unlockedTypes,
                validIndices,
                minGap,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
        }

        if (difficulty >= 0.85f)
        {
            TryPlaceRequiredObstacle(
                ObstacleKind.Spikes,
                plan,
                picked,
                unlockedTypes,
                validIndices,
                minGap,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
        }
    }

    private void TryPlaceRequiredObstacle(
        ObstacleKind requiredKind,
        Dictionary<int, ObstacleKind> plan,
        List<int> picked,
        List<ObstacleTypeDefinition> unlockedTypes,
        List<int> validIndices,
        int minGap,
        IReadOnlyList<LevelCell> cells,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices,
        float difficulty)
    {
        bool isUnlocked = false;
        for (int i = 0; i < unlockedTypes.Count; i++)
        {
            if (unlockedTypes[i].Kind == requiredKind && unlockedTypes[i].Prefab != null)
            {
                isUnlocked = true;
                break;
            }
        }

        if (!isUnlocked)
        {
            return;
        }

        int bestIndex = -1;
        float bestWeight = 0f;
        for (int i = 0; i < validIndices.Count; i++)
        {
            int candidate = validIndices[i];
            if (plan.ContainsKey(candidate) || !HasRequiredGap(candidate, picked, minGap))
            {
                continue;
            }

            float weight = ResolveObstacleSpawnWeight(
                requiredKind,
                candidate,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
            if (weight > bestWeight)
            {
                bestWeight = weight;
                bestIndex = candidate;
            }
        }

        if (bestIndex < 0 || bestWeight <= 0f)
        {
            return;
        }

        picked.Add(bestIndex);
        plan[bestIndex] = requiredKind;
    }

    private List<ObstacleTypeDefinition> GetUnlockedObstacleTypes(float difficulty)
    {
        var types = new List<ObstacleTypeDefinition>(3);

        if (hammerPrefab != null && difficulty >= 0.25f)
        {
            types.Add(new ObstacleTypeDefinition(ObstacleKind.Hammer, hammerPrefab, 0.25f));
        }

        if (laserBeamPrefab != null && difficulty >= 0.40f)
        {
            types.Add(new ObstacleTypeDefinition(ObstacleKind.Laser, laserBeamPrefab, 0.40f));
        }

        if (spikesPrefab != null && difficulty >= 0.80f)
        {
            types.Add(new ObstacleTypeDefinition(ObstacleKind.Spikes, spikesPrefab, 0.80f));
        }

        return types;
    }

    private List<int> CollectValidObstacleIndices(
        int cellCount,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices)
    {
        var valid = new List<int>();
        for (int i = 2; i < cellCount - 2; i++)
        {
            if (IsWithinDistance(i, checkpointTileIndex, CheckpointObstacleBreatherRadius)
                || IsWithinFinishDistance(i, finishTileIndex, FinishObstacleBreatherRadius))
            {
                continue;
            }

            if (powerUpIndices != null && powerUpIndices.Contains(i))
            {
                continue;
            }

            valid.Add(i);
        }

        return valid;
    }

    private static int ResolveObstacleGap(float difficulty)
    {
        if (difficulty >= 0.75f)
        {
            return 3;
        }

        return 4;
    }

    private int ResolveObstacleCountFromDensity(float difficulty)
    {
        AnimationCurve curve = _activeConfig.obstacleDensityCurve;
        float density = curve != null && curve.length > 0
            ? Mathf.Clamp01(curve.Evaluate(difficulty))
            : Mathf.Clamp01(Mathf.InverseLerp(0.2f, 1f, difficulty));

        int count = Mathf.CeilToInt(_activeConfig.maxObstaclesPerLevel * density);
        return Mathf.Clamp(count, 0, _activeConfig.maxObstaclesPerLevel);
    }

    private static bool HasRequiredGap(int candidate, List<int> picked, int minGap)
    {
        for (int i = 0; i < picked.Count; i++)
        {
            if (Mathf.Abs(candidate - picked[i]) < minGap)
            {
                return false;
            }
        }

        return true;
    }

    private ObstacleTypeDefinition PickObstacleType(
        List<ObstacleTypeDefinition> types,
        int tileIndex,
        IReadOnlyList<LevelCell> cells,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices,
        float difficulty,
        int randomSeed)
    {
        if (types.Count == 1)
        {
            float fit = ResolveObstacleFitWeight(
                types[0].Kind,
                tileIndex,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
            return fit > 0f ? types[0] : default;
        }

        UnityEngine.Random.State oldState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(randomSeed);

        float totalWeight = 0f;
        for (int i = 0; i < types.Count; i++)
        {
            totalWeight += ResolveObstacleSpawnWeight(
                types[i].Kind,
                tileIndex,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
        }

        if (totalWeight <= 0.0001f)
        {
            UnityEngine.Random.state = oldState;
            return default;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        UnityEngine.Random.state = oldState;

        float cumulative = 0f;
        for (int i = 0; i < types.Count; i++)
        {
            cumulative += ResolveObstacleSpawnWeight(
                types[i].Kind,
                tileIndex,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
            if (roll <= cumulative)
            {
                return types[i];
            }
        }

        return types[types.Count - 1];
    }

    private static float ResolveObstacleSpawnWeight(
        ObstacleKind kind,
        int tileIndex,
        IReadOnlyList<LevelCell> cells,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices,
        float difficulty)
    {
        return ResolveObstacleBaseWeight(kind, difficulty)
            * ResolveObstacleFitWeight(
                kind,
                tileIndex,
                cells,
                checkpointTileIndex,
                finishTileIndex,
                powerUpIndices,
                difficulty);
    }

    private static float ResolveObstacleBaseWeight(ObstacleKind kind, float difficulty)
    {
        switch (kind)
        {
            case ObstacleKind.Hammer:
                if (difficulty < 0.40f)
                {
                    return 1f;
                }

                if (difficulty < 0.80f)
                {
                    return 0.65f;
                }

                return 0.4f;

            case ObstacleKind.Laser:
                if (difficulty < 0.40f)
                {
                    return 0.01f;
                }

                if (difficulty < 0.65f)
                {
                    return 0.6f;
                }

                if (difficulty < 0.80f)
                {
                    return 0.95f;
                }

                return 1f;

            case ObstacleKind.Spikes:
                if (difficulty < 0.80f)
                {
                    return 0.01f;
                }

                if (difficulty < 0.90f)
                {
                    return 0.45f;
                }

                return 0.9f;

            default:
                return 0.1f;
        }
    }

    private static float ResolveObstacleFitWeight(
        ObstacleKind kind,
        int tileIndex,
        IReadOnlyList<LevelCell> cells,
        int checkpointTileIndex,
        int finishTileIndex,
        ICollection<int> powerUpIndices,
        float difficulty)
    {
        int straightRunLength = MeasureStraightRunLength(tileIndex, cells);
        int turnDistance = DistanceToNearestTurn(tileIndex, cells);
        bool isTurnTile = IsObstacleTurnTile(tileIndex, cells);
        int checkpointDistance = Mathf.Abs(tileIndex - checkpointTileIndex);
        int finishDistance = Mathf.Abs(tileIndex - finishTileIndex);
        int powerUpDistance = DistanceToNearestIndex(tileIndex, powerUpIndices);
        float pathProgress = cells != null && cells.Count > 1
            ? tileIndex / (float)(cells.Count - 1)
            : 0.5f;
        bool nearFinish = IsWithinFinishDistance(tileIndex, finishTileIndex, FinishObstacleBreatherRadius);

        switch (kind)
        {
            case ObstacleKind.Hammer:
                if (isTurnTile)
                {
                    return 1f;
                }

                if (turnDistance <= 1)
                {
                    return 0.95f;
                }

                if (straightRunLength >= 4)
                {
                    return 0.75f;
                }

                return 0.85f;

            case ObstacleKind.Laser:
                if (isTurnTile)
                {
                    return 0.2f;
                }

                if (checkpointDistance <= 1 || powerUpDistance <= 1 || nearFinish)
                {
                    return 0f;
                }

                if (checkpointDistance == 2 || powerUpDistance == 2 || finishDistance == FinishObstacleBreatherRadius + 1)
                {
                    return 0.35f;
                }

                if (straightRunLength >= 4)
                {
                    return 1.25f;
                }

                if (straightRunLength >= 3)
                {
                    return 1.05f;
                }

                if (turnDistance <= 1)
                {
                    return 0.45f;
                }

                return 0.75f;

            case ObstacleKind.Spikes:
                if (difficulty < 0.80f || isTurnTile)
                {
                    return 0f;
                }

                if (nearFinish || checkpointDistance <= 1 || powerUpDistance <= 1)
                {
                    return 0f;
                }

                float midPathBias = 1f - Mathf.Clamp01(Mathf.Abs(pathProgress - 0.5f) / 0.5f);
                if (midPathBias < 0.25f)
                {
                    return 0.15f;
                }

                if (straightRunLength >= 4)
                {
                    return 0.9f + midPathBias * 0.5f;
                }

                if (straightRunLength >= 3)
                {
                    return 0.75f + midPathBias * 0.45f;
                }

                return 0.2f + midPathBias * 0.25f;

            default:
                return 1f;
        }
    }

    private static bool IsObstacleTurnTile(int tileIndex, IReadOnlyList<LevelCell> cells)
    {
        return ProceduralTilePlacement.IsTurnIndex(tileIndex, cells)
            || ProceduralTilePlacement.IsTurnIndex(tileIndex + 1, cells);
    }

    private static int MeasureStraightRunLength(int tileIndex, IReadOnlyList<LevelCell> cells)
    {
        if (cells == null || tileIndex <= 0 || tileIndex >= cells.Count)
        {
            return 1;
        }

        int length = 1;

        for (int i = tileIndex; i >= 2; i--)
        {
            if (ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                break;
            }

            length++;
        }

        for (int i = tileIndex + 1; i < cells.Count; i++)
        {
            if (ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                break;
            }

            length++;
        }

        return length;
    }

    private static int DistanceToNearestTurn(int tileIndex, IReadOnlyList<LevelCell> cells)
    {
        if (cells == null || cells.Count < 3)
        {
            return int.MaxValue;
        }

        int best = int.MaxValue;
        for (int i = 2; i < cells.Count; i++)
        {
            if (!ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                continue;
            }

            best = Mathf.Min(best, Mathf.Abs(tileIndex - i));
        }

        return best;
    }

    private static int DistanceToNearestIndex(int tileIndex, ICollection<int> indices)
    {
        if (indices == null || indices.Count == 0)
        {
            return int.MaxValue;
        }

        int best = int.MaxValue;
        foreach (int index in indices)
        {
            best = Mathf.Min(best, Mathf.Abs(tileIndex - index));
        }

        return best;
    }

    private static bool IsWithinDistance(int tileIndex, int targetIndex, int radius)
    {
        return Mathf.Abs(tileIndex - targetIndex) <= radius;
    }

    private static bool IsWithinFinishDistance(int tileIndex, int finishTileIndex, int radius)
    {
        return IsWithinDistance(tileIndex, finishTileIndex, radius);
    }

    private static void Shuffle(List<int> values, int randomSeed)
    {
        UnityEngine.Random.State oldState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(randomSeed);

        for (int i = 0; i < values.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, values.Count);
            int temp = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = temp;
        }

        UnityEngine.Random.state = oldState;
    }

    private void SpawnObstacle(ObstacleKind obstacleKind, GameObject tile, int index)
    {
        switch (obstacleKind)
        {
            case ObstacleKind.Hammer:
            {
                if (hammerPrefab == null)
                {
                    return;
                }

                Vector3 hammerPos = tile.transform.position + Vector3.up * 3f;
                Quaternion hammerRot = tile.transform.rotation * Quaternion.Euler(0f, 0f, -75f);
                GameObject hammer = Instantiate(hammerPrefab, hammerPos, hammerRot, levelRoot);
                hammer.name = "Hammer_" + index;
                return;
            }

            case ObstacleKind.Laser:
            {
                if (laserBeamPrefab == null)
                {
                    return;
                }

                GameObject laser = Instantiate(laserBeamPrefab, tile.transform.position, tile.transform.rotation, levelRoot);
                laser.name = "LaserBeam_" + index;
                FitObstacleToTileSpan(laser, tile);
                return;
            }

            case ObstacleKind.Spikes:
            {
                if (spikesPrefab == null)
                {
                    return;
                }

                GameObject spikes = Instantiate(spikesPrefab, levelRoot);
                spikes.name = "Spike_" + index;
                Vector3 position = tile.transform.position;
                position.y = GetTileTopY(tile) + 0.01f;
                spikes.transform.SetPositionAndRotation(position, tile.transform.rotation);
                return;
            }
        }
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

    private void FitObstacleToTileSpan(GameObject obstacle, GameObject tile)
    {
        Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float span = Mathf.Max(bounds.size.x, bounds.size.z, 0.001f);
        float targetSpan = _activeConfig.tileSpacingX;
        float uniform = targetSpan / span;

        obstacle.transform.localScale = Vector3.one * uniform;

        // Recompute bounds after scale
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Raise obstacle so its bottom rests exactly on the tile
        float tileTop = GetTileTopY(tile);
        if (tileTop > float.NegativeInfinity)
        {
            float offset = tileTop - bounds.min.y;
            // Sink it by 0.05f so the legs don't float
            obstacle.transform.position += Vector3.up * (offset - 0.05f);
        }
    }

    private void SpawnPowerUps(IReadOnlyList<LevelCell> cells, int actualSeed, HashSet<int> powerUpIndices)
    {
        if (cells == null || cells.Count < 3)
            return;

        foreach (int i in powerUpIndices)
        {
            GameObject prefab = SelectRandomPowerUpPrefab(actualSeed + i * 31);
            if (prefab == null) continue;

            Vector3 pos = _spawnedTiles[i].transform.position + Vector3.up * powerUpSpawnHeight;
            GameObject powerUp = Instantiate(prefab, pos, Quaternion.identity, levelRoot);
            powerUp.name = prefab.name + "_" + i;
        }
    }

    private void SpawnCheckpoint()
    {
        if (_spawnedTiles.Count < 3) return;

        int mainCount = 0;
        for (int i = 0; i < _spawnedTiles.Count; i++)
        {
            if (_spawnedTiles[i] != null && !_spawnedTiles[i].name.Contains("corner"))
                mainCount++;
        }

        int targetMain = mainCount / 2;
        int currentMain = 0;
        GameObject midTile = null;
        for (int i = 0; i < _spawnedTiles.Count; i++)
        {
            if (_spawnedTiles[i] != null && !_spawnedTiles[i].name.Contains("corner"))
            {
                if (currentMain == targetMain)
                {
                    midTile = _spawnedTiles[i];
                    break;
                }
                currentMain++;
            }
        }

        if (midTile == null) return;

        Vector3 pos = midTile.transform.position + Vector3.up * 0.15f;
        GameObject checkpoint = new GameObject("Checkpoint", typeof(Checkpoint));
        checkpoint.transform.position = pos;
        checkpoint.transform.SetParent(levelRoot);
    }

    private GameObject SelectRandomPowerUpPrefab(int seed)
    {
        if (speedCorePrefab == null && magnetPrefab == null)
            return null;

        UnityEngine.Random.State oldState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);

        var available = new System.Collections.Generic.List<GameObject>();
        if (speedCorePrefab != null) available.Add(speedCorePrefab);
        if (magnetPrefab != null) available.Add(magnetPrefab);

        GameObject selected = available[UnityEngine.Random.Range(0, available.Count)];
        UnityEngine.Random.state = oldState;
        return selected;
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
