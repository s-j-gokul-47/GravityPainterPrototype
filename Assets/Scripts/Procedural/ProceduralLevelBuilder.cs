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

    [Header("Scene References")]
    [SerializeField] private Transform levelRoot;
    [SerializeField] private BallController ball;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Spawn Tuning")]
    [SerializeField] private float ballSpawnHeight = 2f;

    private readonly List<GameObject> _spawnedTiles = new List<GameObject>();
    private ProceduralPathGenerator _generator;

    public int LastBuiltSeed { get; private set; } = -1;
    public int LastBuiltTileCount { get; private set; }
    public IReadOnlyList<GameObject> SpawnedTiles => _spawnedTiles;

    public event Action<int, int> OnLevelBuilt;

    private void Start()
    {
        if (buildOnStart)
        {
            BuildFromSeed(seed);
        }
    }

    /// <summary>
    /// Clears the current level and builds a new path from the given seed.
    /// </summary>
    public bool BuildFromSeed(int buildSeed)
    {
        if (!ValidateConfig())
        {
            return false;
        }

        EnsureLevelRoot();
        ProceduralTilePlacement.HidePathPreview();
        ClearLevel();

        _generator ??= new ProceduralPathGenerator();
        List<LevelCell> cells = _generator.GenerateWithRetry(config, buildSeed);
        if (cells == null || cells.Count == 0)
        {
            Debug.LogError("ProceduralLevelBuilder: path generation failed for seed " + buildSeed);
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

        LastBuiltSeed = buildSeed;
        LastBuiltTileCount = _spawnedTiles.Count;
        seed = buildSeed;

        Debug.Log(
            "Procedural level built: seed=" + buildSeed +
            ", tiles=" + _spawnedTiles.Count +
            ", finish at " + cells[cells.Count - 1].GridPos);

        OnLevelBuilt?.Invoke(buildSeed, _spawnedTiles.Count);
        return true;
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

        int padCount = Mathf.Max(0, config.cornerPadTileCount);
        for (int i = 2; i < cells.Count; i++)
        {
            if (!ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                continue;
            }

            for (int padIndex = 0; padIndex < padCount; padIndex++)
            {
                GameObject pad = Instantiate(config.tilePrefab, levelRoot.transform);
                ProceduralTilePlacement.ApplyCornerPadTransform(pad.transform, i, padIndex, cells, config);
                pad.name = "Tile_corner_" + i + "_" + padIndex + "_" + cells[i - 1].GridPos.x + "_" + cells[i - 1].GridPos.y;
                ApplyTileVisual(pad);
                _spawnedTiles.Add(pad);
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

        Collider col = goalTile.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
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
