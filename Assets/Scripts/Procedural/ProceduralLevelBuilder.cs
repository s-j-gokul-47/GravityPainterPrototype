using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a playable procedural level at runtime from a seed:
/// tiles, GLB visuals, ball spawn, and finish line.
/// </summary>
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

    private readonly List<GameObject> _spawnedTiles = new List<GameObject>();
    private ProceduralPathGenerator _generator;
    private int _watchedSeed = int.MinValue;

    public int Seed => seed;
    public int LastBuiltSeed { get; private set; } = -1;
    public int LastBuiltTileCount { get; private set; }
    public float LastBuiltDifficulty { get; private set; } = -1f;
    public string LastBuiltTier =>
        LastBuiltDifficulty >= 0f ? DifficultyManager.GetTierName(LastBuiltDifficulty) : "(none)";
    public IReadOnlyList<GameObject> SpawnedTiles => _spawnedTiles;

    public event Action<int, int> OnLevelBuilt;

    private void Start()
    {
        if (buildOnStart)
        {
            BuildFromSeed(seed);
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

        config.SyncFootprintFromTileScale();
        float difficulty = ResolveDifficulty();
        DifficultyScaler.Apply(config, difficulty);
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
        List<LevelCell> cells = _generator.GenerateWithRetry(config, buildSeed, out int actualSeed);
        if (cells == null || cells.Count == 0)
        {
            Debug.LogError("ProceduralLevelBuilder: path generation failed for seed " + buildSeed);
            return false;
        }

        if (ProceduralTilePlacement.HasMainTileOverlaps(cells, config))
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
            + ", path=" + config.minPathLength + "-" + config.maxPathLength
            + ", grid=" + config.gridWidth + "x" + config.gridDepth
            + ", turnFreq=" + config.turnFrequency.ToString("F2")
            + ", requested seed=" + buildSeed
            + ", used seed=" + actualSeed
            + ", tiles=" + _spawnedTiles.Count
            + ", finish at " + cells[cells.Count - 1].GridPos);

        OnLevelBuilt?.Invoke(actualSeed, _spawnedTiles.Count);
        return true;
    }

    private float ResolveDifficulty()
    {
        if (useDifficultyProgression)
        {
            return DifficultyManager.CurrentDifficulty;
        }

        return Mathf.Clamp01(config.difficulty);
    }

    /// <summary>Rebuilds the current procedural layout using the last seed.</summary>
    public void RebuildSameSeed()
    {
        int rebuildSeed = LastBuiltSeed >= 0 ? LastBuiltSeed : seed;
        BuildFromSeed(rebuildSeed);
    }

    /// <summary>Builds a new procedural layout from a random seed.</summary>
    public void RebuildNextSeed()
    {
        BuildFromSeed(UnityEngine.Random.Range(1, int.MaxValue));
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
            config.tilePrefab,
            levelRoot.transform);
        ProceduralTilePlacement.ApplyPathTransform(tile.transform, index, cells, config);
        tile.name = "Tile_" + cell.PathIndex + "_" + cell.GridPos.x + "_" + cell.GridPos.y;

        ApplyTileVisual(tile);
        return tile;
    }

    private void SpawnCornerPads(IReadOnlyList<LevelCell> cells)
    {
        if (!config.addCornerPads || cells == null)
        {
            return;
        }

        var placed = ProceduralTilePlacement.BuildPlacementPlan(cells, config);

        for (int i = 2; i < cells.Count; i++)
        {
            if (!ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                continue;
            }

            int padCount = ProceduralTilePlacement.CountCornerPadsForTurn(i, cells, config);
            for (int padIndex = 0; padIndex < padCount; padIndex++)
            {
                Vector3 padCenter = ProceduralTilePlacement.ComputeCornerPadPosition(
                    i, padIndex, padCount, cells, config);
                float padRotation = cells[i - 1].YRotation;
                Bounds padBounds = ProceduralTileFootprint.ComputeWorldBounds(padCenter, padRotation, config);

                // Allow pads to meet the incoming/outgoing turn tiles; block parallel-path stacking.
                if (WouldOverlapNonNeighborMainTiles(padBounds, placed, config, i))
                {
                    continue;
                }

                GameObject pad = Instantiate(config.tilePrefab, levelRoot.transform);
                ProceduralTilePlacement.ApplyCornerPadTransform(
                    pad.transform, i, padIndex, padCount, cells, config);
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

        if (config.glbLayout != null)
        {
            visual.ApplyLayout(config.glbLayout.layout);
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

        Vector3 worldPos = levelRoot.TransformPoint(
            ProceduralTilePlacement.ComputeCenterPosition(0, cells, config));
        worldPos.y = ballSpawnHeight;
        ball.PlaceAt(worldPos);
    }

    private void SetupFinishLine(GameObject goalTile)
    {
        if (goalTile == null)
        {
            return;
        }

        FinishLine finish = goalTile.GetComponent<FinishLine>();
        if (finish == null)
        {
            finish = goalTile.AddComponent<FinishLine>();
        }

        finish.Configure(levelCompletePanel, pause: true);
        EnsureFinishTrigger(goalTile);
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
