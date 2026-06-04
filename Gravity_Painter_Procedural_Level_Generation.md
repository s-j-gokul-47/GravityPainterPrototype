# Gravity Painter — Procedural Level Generation Plan

## Overview

This document outlines a complete procedural level generation system for **Gravity Painter**, a 3D mobile puzzle game built in Unity. The system generates connected tile paths at runtime using seeded random walk algorithms, validates solvability, and integrates seamlessly with existing systems: `TileGlbVisual`, `BallController`, `FinishLine`, and `LevelEnvironment`.

Campaign Levels 1–5 remain hand-authored for tutorial pacing. A single `LevelProcedural` scene handles **Procedural**, **Daily Challenge**, and **Replay** modes.

---

## What "Procedural" Means in This Project

| Layer | Hand-Built Today | Procedural Version |
|---|---|---|
| Tile path | Placed manually in scene | Algorithm picks connected grid cells |
| Tile transform | Position, rotation, scale set by hand | Rules + seeded noise per cell |
| Paint zones | Player paints during play | Default all `None`; player still paints |
| Obstacles | Hammer, laser on specific tiles | Spawn from difficulty rules per tile |
| Sky / environment | `LevelEnvironment` + clouds placed by hand | Scaled to generated tile bounds |
| Win condition | `FinishLine` at hand-placed position | Auto-placed at last path tile |

Core gameplay stays identical: **tap tiles → ball gets force → reach the goal.**

---

## Architecture

The system uses a clean three-layer pipeline so each concern is isolated and independently testable.

```
┌─────────────────────────────────────────────────┐
│                   INPUT LAYER                   │
│  int Seed  +  LevelGenConfig (ScriptableObject) │
└──────────────┬──────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────┐
│              GENERATION LAYER                   │
│                                                 │
│  ProceduralPathGenerator                        │
│   └─ Random walk with backtrack safety          │
│   └─ Returns List<LevelCell>                    │
│                                                 │
│  ObstaclePlacer                                 │
│   └─ Reads difficulty curve from Config         │
│   └─ Never places obstacle on tiles 0,1 or      │
│      last 2 (safe zone at start and finish)     │
│                                                 │
│  LevelValidator                                 │
│   └─ BFS connectivity check                     │
│   └─ Solvability check (obstacle density cap)   │
│   └─ If fail → seed+1, retry (max 5 attempts)   │
└──────────────┬──────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────┐
│               BUILD LAYER                       │
│                                                 │
│  ProceduralLevelBuilder                         │
│   └─ Pulls tiles from ObjectPool (no GC spike)  │
│   └─ Sets position, Y rotation, scale           │
│   └─ Applies TileGlbVisual reference layout     │
│   └─ Places FinishLine, Ball spawn, Obstacles   │
│   └─ Fires OnLevelBuilt(bounds) event           │
│                                                 │
│  LevelEnvironmentScaler                         │
│   └─ Resizes clouds + skybox to tile bounds     │
└──────────────┬──────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────┐
│                SCENE LAYER                      │
│  Ball  Camera  UI  FinishLine  Tiles  Obstacles │
│  LevelEnvironment  SkyCloudVisuals              │
└─────────────────────────────────────────────────┘
```

---

## LevelGenConfig — ScriptableObject

`LevelGenConfig` is the single source of truth for all generation parameters. Designers can tune difficulty without touching code.

```csharp
[CreateAssetMenu(fileName = "LevelGenConfig",
                 menuName = "GravityPainter/LevelGenConfig")]
public class LevelGenConfig : ScriptableObject
{
    [Header("Path")]
    public int   minPathLength  = 8;
    public int   maxPathLength  = 24;
    public int   gridWidth      = 7;      // max X spread
    public int   gridDepth      = 7;      // max Z spread
    public float tileSpacing    = 1.05f;  // slight gap between tiles

    [Header("Difficulty (0 = Easy, 1 = Hard)")]
    [Range(0f, 1f)] public float difficulty = 0.3f;
    public AnimationCurve obstacleDensityCurve; // x=difficulty, y=spawnChance
    public int maxObstaclesPerLevel = 4;

    [Header("Obstacle Prefabs")]
    public GameObject hammerPrefab;
    public GameObject laserPrefab;
    public GameObject movingPlatformPrefab;

    [Header("Tile and Goal")]
    public GameObject          tilePrefab;
    public GameObject          finishLinePrefab;
    public TileGlbReferenceLayout glbLayout;

    [Header("Optional Features")]
    public bool allowDeadEndBranches  = true;
    public int  maxBranchLength       = 3;
    public bool allowElevationChange  = false; // future: Y-axis steps
}
```

Create instances via **Assets → Create → GravityPainter → LevelGenConfig**. Name them `Config_Easy`, `Config_Medium`, `Config_Hard` for the three difficulty tiers.

---

## LevelCell Data Struct

```csharp
public struct LevelCell
{
    public Vector2Int  GridPos;      // position on 2D grid
    public float       YRotation;    // 0, 90, 180, or 270 degrees
    public bool        IsMainPath;   // false = dead-end branch tile
    public int         PathIndex;    // 0 = start tile, N = finish tile
    public ObstacleType Obstacle;    // None / Hammer / Laser / Platform
    public PaintZone   PresetZone;   // None / Blue / Red / Yellow
}

public enum ObstacleType { None, Hammer, Laser, MovingPlatform }
public enum PaintZone    { None, Blue, Red, Yellow }
```

---

## ProceduralPathGenerator — Algorithm

The generator uses a **backtracking random walk**. Unlike a simple random walk, it never permanently gets stuck: when every direction is blocked, it steps backward and tries again. This guarantees a path of the requested length is always found.

```csharp
public class ProceduralPathGenerator
{
    public List<LevelCell> Generate(LevelGenConfig cfg, int seed)
    {
        Random.InitState(seed);

        int length = Random.Range(cfg.minPathLength, cfg.maxPathLength + 1);
        var path   = new List<Vector2Int>();
        var used   = new HashSet<Vector2Int>();

        Vector2Int cur = Vector2Int.zero;
        path.Add(cur);
        used.Add(cur);

        // Four cardinal directions
        Vector2Int[] dirs = {
            Vector2Int.right, Vector2Int.up,
            Vector2Int.left,  Vector2Int.down
        };

        int maxAttempts = length * 20;
        int attempts    = 0;

        while (path.Count < length && attempts < maxAttempts)
        {
            attempts++;
            Shuffle(dirs);   // seeded shuffle for reproducibility
            bool moved = false;

            foreach (var d in dirs)
            {
                Vector2Int next = cur + d;
                bool inBounds =
                    Mathf.Abs(next.x) <= cfg.gridWidth / 2 &&
                    Mathf.Abs(next.y) <= cfg.gridDepth / 2;

                if (!used.Contains(next) && inBounds)
                {
                    path.Add(next);
                    used.Add(next);
                    cur   = next;
                    moved = true;
                    break;
                }
            }

            // Dead-end: backtrack one step instead of giving up
            if (!moved && path.Count > 1)
            {
                used.Remove(cur);
                path.RemoveAt(path.Count - 1);
                cur = path[path.Count - 1];
            }
        }

        return BuildCells(path, cfg);
    }
}
```

### Path Rotation Rules

Each tile's `YRotation` is calculated from the direction vector between consecutive path cells:

| Direction | YRotation |
|---|---|
| `Vector2Int.right` (+X) | 90° |
| `Vector2Int.up` (+Z) | 0° |
| `Vector2Int.left` (−X) | 270° |
| `Vector2Int.down` (−Z) | 180° |

This ensures the **red Forward arrow** on each tile always points toward the next tile in the path.

---

## LevelValidator

After generation, the validator runs two checks before allowing the level to be built.

```csharp
public class LevelValidator
{
    // Check 1: BFS — every tile reachable from start?
    public bool IsConnected(List<LevelCell> cells)
    {
        // BFS from cells[0]; mark visited; return cells.All(visited)
    }

    // Check 2: Obstacle density — is the level solvable?
    public bool IsSolvable(List<LevelCell> cells, LevelGenConfig cfg)
    {
        int obstacleCount = cells.Count(c => c.Obstacle != ObstacleType.None);
        return obstacleCount <= cfg.maxObstaclesPerLevel;
    }

    // Full validation with automatic retry
    public List<LevelCell> ValidateOrRetry(LevelGenConfig cfg, int seed)
    {
        for (int i = 0; i < 5; i++)
        {
            var cells = new ProceduralPathGenerator().Generate(cfg, seed + i);
            if (IsConnected(cells) && IsSolvable(cells, cfg))
                return cells;
        }
        // Fallback: return a safe minimal straight path
        return GenerateSafeFallback(cfg, seed);
    }
}
```

---

## ProceduralLevelBuilder

```csharp
public class ProceduralLevelBuilder : MonoBehaviour
{
    [SerializeField] LevelGenConfig config;
    [SerializeField] Transform      levelRoot;
    [SerializeField] Transform      ballSpawnPoint;

    public event Action<Bounds> OnLevelBuilt;

    public IEnumerator BuildAsync(List<LevelCell> cells)
    {
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        int batchSize = 5; // spawn 5 tiles per frame — no frame spike

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            Vector3 worldPos = new Vector3(
                cell.GridPos.x * config.tileSpacing,
                0f,
                cell.GridPos.y * config.tileSpacing);

            // Pull from pool instead of Instantiate
            var tile = TilePool.Instance.Get();
            tile.transform.SetParent(levelRoot);
            tile.transform.position = worldPos;
            tile.transform.rotation = Quaternion.Euler(0, cell.YRotation, 0);

            // Apply existing GLB visual layout
            TileGlbVisual.ApplyLayout(tile, config.glbLayout);

            // Place obstacles (never on first 2 or last 2 tiles)
            if (cell.Obstacle != ObstacleType.None
                && i > 1 && i < cells.Count - 2)
            {
                PlaceObstacle(cell.Obstacle, worldPos);
            }

            // Place finish line on last tile
            if (i == cells.Count - 1)
            {
                Instantiate(config.finishLinePrefab,
                            worldPos + Vector3.up * 0.1f,
                            tile.transform.rotation);
            }

            bounds.Encapsulate(worldPos);

            if ((i + 1) % batchSize == 0)
                yield return null; // wait one frame every 5 tiles
        }

        // Set ball spawn at first tile
        ballSpawnPoint.position =
            new Vector3(cells[0].GridPos.x * config.tileSpacing,
                        1f,
                        cells[0].GridPos.y * config.tileSpacing);

        // Apply static batching for one draw call
        StaticBatchingUtility.Combine(levelRoot.gameObject);

        OnLevelBuilt?.Invoke(bounds);
    }
}
```

---

## ObstaclePlacer — Difficulty Curve

Obstacle spawn chance is driven by an `AnimationCurve` in `LevelGenConfig`, not hardcoded values. This means designers can reshape the difficulty curve in the Inspector without writing any code.

```
Obstacle spawn chance
  1.0 │                              ╭────
  0.8 │                         ╭───╯
  0.6 │                    ╭───╯
  0.4 │               ╭───╯
  0.2 │          ╭───╯
  0.0 │─────────╯
      └──────────────────────────────────
        Easy    Medium    Hard    Expert
        (0.0)   (0.3)    (0.6)   (1.0)
```

```csharp
void PlaceObstacleOnTile(LevelCell cell, LevelGenConfig cfg)
{
    float spawnChance = cfg.obstacleDensityCurve.Evaluate(cfg.difficulty);
    if (Random.value > spawnChance) return;

    // Randomly pick obstacle type weighted by difficulty
    ObstacleType type = cfg.difficulty < 0.5f
        ? ObstacleType.Hammer
        : (Random.value > 0.5f ? ObstacleType.Laser
                                : ObstacleType.MovingPlatform);

    cell.Obstacle = type;
}
```

### Obstacle Safe Zones

| Tile Index | Obstacle Allowed? | Reason |
|---|---|---|
| 0 | Never | Ball spawn tile |
| 1 | Never | Player needs one clear tile to react |
| 2 … N-3 | Yes | Obstacle zone |
| N-2 | Never | Approach to finish |
| N-1 (last) | Never | Finish line tile |

---

## Seed — Human-Readable Word Code

Instead of showing a raw integer (`Seed: 4829103`), the seed is converted to a **5-letter pronounceable code** — easy to read aloud and share with friends.

```csharp
public static string SeedToCode(int seed)
{
    const string vowels = "AEIOU";
    const string cons   = "BCDFGHJKLMNPRST";
    var rng = new System.Random(seed);
    // CVCVC pattern → e.g. "KELOR", "BIMAX", "FUTON"
    return $"{cons[rng.Next(cons.Length)]}" +
           $"{vowels[rng.Next(vowels.Length)]}" +
           $"{cons[rng.Next(cons.Length)]}" +
           $"{vowels[rng.Next(vowels.Length)]}" +
           $"{cons[rng.Next(cons.Length)]}";
}

public static int CodeToSeed(string code)
{
    return code.GetHashCode(); // deterministic, same string = same seed
}
```

The UI shows: **Today's Level — KELOR** instead of **Level — Seed: 4829103**.

---

## Four Game Modes

```
Main Menu
    │
    ├── 🎮 Campaign ────── Level 1–5 (hand-built, tutorial pacing)
    │                      └─ LevelProgress unlock system unchanged
    │
    ├── ⚙️  Procedural ─── LevelProcedural.unity
    │                      └─ Difficulty slider (Easy / Medium / Hard)
    │                      └─ Random seed each run
    │                      └─ Star rating on completion
    │
    ├── 📅 Daily ────────── LevelProcedural.unity
    │                      └─ seed = int.Parse(DateTime.UtcNow
    │                                 .ToString("yyyyMMdd"))
    │                      └─ Same level for all players worldwide
    │                      └─ "Today's Code: KELOR"
    │
    └── 🔁 Replay ─────── LevelProcedural.unity
                           └─ Player types 5-letter code
                           └─ Exact same level re-generated
                           └─ Use to challenge a friend
```

---

## Mobile Performance Rules

Mobile devices require strict discipline during procedural generation. These rules prevent frame drops on mid-range Android devices.

| Rule | Implementation |
|---|---|
| Object Pool | Pre-warm 30 tile instances on scene load — zero `Instantiate` cost during generation |
| Async Build | `IEnumerator` builder, 5 tiles per frame — no single-frame spike |
| Disable Physics | Disable all Rigidbodies until `OnLevelBuilt` fires |
| Static Batching | Call `StaticBatchingUtility.Combine(levelRoot)` after build for one draw call |
| Tile Count Cap | Hard cap at 30 tiles for mobile — beyond this, mid-range Android shows frame drops |
| No FindObjectOfType | Cache all references in Awake; never use `FindObjectOfType` during generation |

---

## What to Generate vs What to Keep Fixed

| Element | Generate? | Notes |
|---|---|---|
| Tile positions | Yes | From grid path algorithm |
| Tile Y rotation | Yes | Align to path direction; TileZone planar basis supports Y rotation |
| TileGlbVisual | Yes, post-spawn | Call `ApplyLayout` from reference asset (same as Level 1/2 batch) |
| Player paint zones | No (default None) | All tiles start unpainted — player still paints during play |
| Ball spawn position | Yes | First path tile + Y offset |
| Finish line | Yes | Last path tile |
| Hammer / Laser | Optional | Only if `difficulty >= threshold` and tile index is in safe zone |
| Death tiles | Optional | Mark random non-path cells |
| Clouds / sky | Yes | `LevelEnvironment` + `SkyCloudVisuals` sized to tile bounds |

---

## Scene Setup — LevelProcedural.unity

This single scene replaces all individual level scenes for non-campaign modes.

**Hierarchy:**

```
LevelProcedural
├── GameManager
├── ProceduralLevelGenerator   ← new component
├── ProceduralLevelBuilder     ← new component
├── LevelRoot                  ← tiles spawned here
├── Ball
│   └── BallController         ← unchanged
├── MainCamera
│   └── CameraFollow           ← unchanged
├── UI
│   ├── Canvas_HUD
│   └── SeedCodeLabel          ← shows "KELOR"
├── FinishLine                 ← instantiated by builder
├── LevelEnvironment           ← scaled by LevelEnvironmentScaler
└── SkyCloudVisuals            ← scaled to tile bounds
```

**On Start:**
1. `GameManager.Awake()` reads mode (Procedural / Daily / Replay)
2. `ProceduralLevelGenerator.Generate(seed)` → `List<LevelCell>`
3. `LevelValidator.ValidateOrRetry()` checks connectivity + solvability
4. `ProceduralLevelBuilder.BuildAsync()` spawns tiles frame-by-frame
5. `OnLevelBuilt` fires → camera bounds set, clouds resized, ball enabled

---

## LevelMenu Integration

```csharp
// Existing: loads named scene
SceneManager.LoadScene("Level " + levelIndex);

// Add: procedural mode
public void LoadProcedural(float difficulty)
{
    PlayerPrefs.SetFloat("ProceduralDifficulty", difficulty);
    PlayerPrefs.SetInt("ProceduralSeed", Random.Range(0, int.MaxValue));
    SceneManager.LoadScene("LevelProcedural");
}

public void LoadDaily()
{
    int seed = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
    PlayerPrefs.SetInt("ProceduralSeed", seed);
    SceneManager.LoadScene("LevelProcedural");
}

public void LoadReplay(string code)
{
    PlayerPrefs.SetInt("ProceduralSeed", SeedHelper.CodeToSeed(code));
    SceneManager.LoadScene("LevelProcedural");
}
```

---

## Rollout Phases

| Phase | Deliverable | Time Estimate | Demo-able? |
|---|---|---|---|
| **P0** | `ProceduralPathGenerator` + `LevelBuilder` + tile spawn + finish line | 1 week | ✅ Yes |
| **P1** | `LevelValidator` + seed UI (word code) + ball spawn wired | 1 week | ✅ Yes |
| **P2** | Obstacle placement + difficulty curve + star rating system | 1 week | ✅ Yes |
| **P3** | Object pool + async builder + Daily mode + Replay code entry | 1 week | ✅ Yes |
| **P4** | Chunk system (designer-authored prefab modules stitched at runtime) | 2 weeks | Bonus |

**P0 + P1 is a complete working demo** sufficient for an academic presentation. P2 and P3 add production depth.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Unsolvable layout generated | Validate path with BFS; auto-retry with `seed + 1` up to 5 times |
| Ugly or overlapping tiles | Enforce minimum `tileSpacing`; collision check on spawn |
| GLB visuals misaligned on rotated tiles | Always run `ApplyLayout` from reference asset after rotation is set |
| Level feels too random and frustrating | Difficulty curve + max obstacle cap per level |
| Hard to QA or reproduce bugs | Log seed code on every failure; Replay mode re-runs exact seed |
| Frame drop on mobile during generation | Async builder (5 tiles/frame) + object pool + 30-tile hard cap |
| Dead-end path (algorithm gets stuck) | Backtracking walk guarantees path length is always reached |

---

## Pitch for Academic Presentation

> Gravity Painter will implement procedural level generation using a **seeded backtracking random walk** on a bounded 2D tile grid. A `LevelGenConfig` ScriptableObject controls all parameters — path length, grid bounds, obstacle density, and prefab references — so difficulty can be tuned without touching code. A three-layer pipeline (Generator → Validator → Builder) ensures every generated level is solvable before play begins: the validator runs a BFS connectivity check and retries with `seed + 1` if the path fails. Campaign Levels 1–5 remain hand-authored for tutorial pacing; a single `LevelProcedural` scene handles Procedural, Daily Challenge, and Replay modes using the same existing `Tile` prefab, `TileGlbVisual` layout, `BallController`, and `FinishLine` systems already in the project. Mobile performance is maintained through an object pool, static batching, async frame-spread building, and a 30-tile hard cap. Players share levels using a 5-letter human-readable seed code.

---

## Minimal Implementation Checklist

- [ ] `LevelGenConfig.asset` — ScriptableObject with all parameters
- [ ] `ProceduralPathGenerator.cs` — backtracking random walk, returns `List<LevelCell>`
- [ ] `ObstaclePlacer.cs` — reads difficulty curve, respects safe zones
- [ ] `LevelValidator.cs` — BFS connectivity + solvability check + retry logic
- [ ] `ProceduralLevelBuilder.cs` — async spawner, applies GLB layout, fires `OnLevelBuilt`
- [ ] `TilePool.cs` — object pool pre-warmed on scene load
- [ ] `LevelEnvironmentScaler.cs` — resizes clouds + skybox to tile bounds
- [ ] `SeedHelper.cs` — `SeedToCode` and `CodeToSeed` utilities
- [ ] `LevelProcedural.unity` — scene with ball, camera, UI, no hand-placed tiles
- [ ] `LevelMenu.cs` additions — `LoadProcedural`, `LoadDaily`, `LoadReplay` methods
- [ ] Seed code label in HUD UI
- [ ] Replay code entry UI panel

