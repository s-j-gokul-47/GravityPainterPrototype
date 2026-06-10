# Gravity Painter — Procedural Level Generation Plan

## Overview

This document outlines the procedural level generation system for **Gravity Painter**, a 3D mobile puzzle game built in Unity. The system generates connected tile paths at runtime using seeded random walk algorithms and integrates with existing systems: `TileGlbVisual`, `BallController`, `FinishLine`, and `LevelEnvironment`.

Campaign Levels 1–2 remain hand-authored for tutorial pacing. A dedicated `LevelProcedural` scene will eventually handle **Procedural**, **Daily Challenge**, and **Replay** modes.

---

## Current Implementation Status (June 2026 — `kavin` branch)

### ✅ Completed (Steps 1–2)

| Component | Status | Location |
|---|---|---|
| `LevelCell` data struct | ✅ Done | `Assets/Scripts/Procedural/LevelCell.cs` |
| `LevelGenConfig` ScriptableObject | ✅ Done | `Assets/Scripts/Procedural/LevelGenConfig.cs` |
| Default config asset | ✅ Done | `Assets/Settings/LevelGenConfig_Default.asset` |
| `ProceduralPathGenerator` (biased backtracking walk) | ✅ Done | `Assets/Scripts/Procedural/ProceduralPathGenerator.cs` |
| `GenerateWithRetry()` + snake fallback | ✅ Done | Up to 50 seed retries |
| `ProceduralPathVisualizer` (Edit-mode preview) | ✅ Done | `Assets/Scripts/Procedural/ProceduralPathVisualizer.cs` |
| `ProceduralLevelBuilder` (runtime playable build) | ✅ Done | `Assets/Scripts/Procedural/ProceduralLevelBuilder.cs` |
| `ProceduralTilePlacement` (edge-aligned + corner pads) | ✅ Done | `Assets/Scripts/Procedural/ProceduralTilePlacement.cs` |
| GLB layout on spawned tiles | ✅ Done | Via `TileGlbVisual.ApplyLayout` + reference asset |
| Ball spawn via `BallController.PlaceAt()` | ✅ Done | `Assets/Scripts/Core/BallController.cs` |
| Finish line wiring via `FinishLine.Configure()` | ✅ Done | `Assets/Scripts/Gameplay/FinishLine.cs` |
| Editor menus + tests | ✅ Done | See **Editor Menus** below |
| Test scene | ✅ Done | `Assets/Procedural(test).unity` |

### Editor Menus (Gravity Painter)

| Menu item | Purpose |
|---|---|
| **Test Procedural Path** | Runs seed, adjacency, path-length, and finish-distance tests in Console |
| **Create Level Gen Config** | Creates `Assets/Settings/LevelGenConfig_Default.asset` |
| **Wire Level Gen Config Prefabs** | Wires `Tile.prefab` onto the config |
| **Setup Procedural Level Scene (Step 2)** | Wires builder, ball, camera, config in the open scene |
| **Generate Visual Path** | Inspector button on `ProceduralPathVisualizer` |

### How to Playtest Today

1. Open `Assets/Procedural(test).unity`
2. Run **Gravity Painter → Setup Procedural Level Scene (Step 2)** (first time only)
3. Press **Play** — level builds from seed `12345` on `ProceduralLevel`
4. Paint tiles, roll the ball to the last tile to win
5. Change seed on `ProceduralLevel → Procedural Level Builder` and Play again

### Path Generator Algorithm (as implemented)

- **Biased backtracking random walk** with forward preference, turn limiter, and Manhattan journey validation
- **Incoming tile rotation**: each tile faces the direction it was *arrived on* (`path[i] - path[i-1]`), not the outgoing segment — prevents pre-rotation overlap at corners
- **Edge-aligned placement**: tile centers are stepped by oriented half-extents (not uniform grid × spacing)
- **Turn offset**: when direction changes, an extra perpendicular offset prevents rectangular tiles clipping
- **Corner pads**: at every 90° turn, **2 extra tiles** are spawned with the **same rotation as the forward tile**, placed edge-to-edge along the straight run so the ball does not fall through corner gaps
- **Tile footprint**: Level 2 GLB scale `(9.76, 0.21, 4.59)` applied via `tileLocalScale` on config

### Git history (`kavin` branch)

| Commit | Summary |
|---|---|
| `0632cfe` | Step 1 procedural path generator + split Korrath Beam laser fix |
| `1eb4eb0` | Biased walk + reliable snake fallback |
| `e3781a9` | Step 2 runtime level builder + tile placement fixes |
| `0fe2b2b` | Edge-aligned turns + forward corner pads (2 per turn) |

### 📋 Not Yet Implemented (Steps 3+)

- [ ] `LevelProcedural.unity` dedicated scene
- [ ] `LevelValidator` (BFS connectivity + solvability)
- [ ] `ObstaclePlacer` + difficulty curve
- [ ] `TilePool` object pooling
- [ ] Async `BuildAsync` (5 tiles/frame)
- [ ] `LevelEnvironmentScaler` (clouds/sky to bounds)
- [ ] `SeedHelper` word codes (KELOR-style)
- [ ] Main menu hooks: Procedural / Daily / Replay modes
- [ ] Star rating on procedural completion

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
                 menuName = "Gravity Painter/Level Gen Config")]
public class LevelGenConfig : ScriptableObject
{
    [Header("Path Settings")]
    public int minPathLength = 8;
    public int maxPathLength = 16;
    public int gridWidth = 7;
    public int gridDepth = 7;

    [Header("Tile Footprint")]
    public Vector3 tileLocalScale = new Vector3(9.764219f, 0.20627001f, 4.5902023f);
    public float tileSpacingX = 9.764219f;   // legacy reference; placement uses edge-aligned math
    public float tileSpacingZ = 4.85f;
    public float tileGap = 0.05f;
    public bool addCornerPads = true;
    public int cornerPadTileCount = 2;

    [Header("Prefab References")]
    public GameObject tilePrefab;            // Assets/Prefabs/Gameplay/Tile.prefab
    public GameObject finishLinePrefab;      // optional; FinishLine added to last tile if empty
    public TileGlbReferenceLayoutAsset glbLayout;
}
```

Create via **Assets → Create → Gravity Painter → Level Gen Config**, or use the existing `Assets/Settings/LevelGenConfig_Default.asset`.

**Planned (not in code yet):** `difficulty`, `obstacleDensityCurve`, obstacle prefabs, dead-end branches, elevation.

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

The generator uses a **biased backtracking random walk** (upgraded from the original plain backtracking design). When every direction is blocked, it steps backward and tries again. `GenerateWithRetry()` attempts up to 50 seeds; if all fail, a guaranteed **snake fallback** path is returned.

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

### Path Rotation Rules (implemented)

Each tile's `YRotation` uses the **incoming** direction (segment the ball traveled to reach this tile):

| Direction | YRotation |
|---|---|
| `Vector2Int.right` (+X) | 90° |
| `Vector2Int.up` (+Z) | 0° |
| `Vector2Int.left` (−X) | 270° |
| `Vector2Int.down` (−Z) | 180° |

Start tile faces toward `path[1]`. This prevents the last straight tile before a turn from rotating early and overlapping its neighbours.

### Tile Placement Rules (implemented — `ProceduralTilePlacement`)

Rectangular Level 2 GLB tiles (~9.76 × 4.59) cannot use uniform grid-center spacing. Placement uses:

1. **Edge-aligned stepping** — each step advances by previous tile half-extent + next tile half-extent along the travel axis (+ `tileGap`)
2. **Turn perpendicular offset** — when `previousStep != step`, an extra offset along the previous leg clears the corner
3. **Forward corner pads** — if `addCornerPads` is true, spawn `cornerPadTileCount` (default **2**) extra tiles at each turn, same rotation as the forward tile, extending the straight run into the corner gap

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

## ProceduralLevelBuilder (implemented)

`ProceduralLevelBuilder` on `ProceduralLevel` in `Procedural(test).unity`:

```csharp
public class ProceduralLevelBuilder : MonoBehaviour
{
    // On Start (if buildOnStart): BuildFromSeed(seed)
    public bool BuildFromSeed(int buildSeed)
    {
        // 1. Generate path via ProceduralPathGenerator.GenerateWithRetry()
        // 2. Spawn main-path tiles with ProceduralTilePlacement.ApplyPathTransform()
        // 3. Spawn corner pads (2 forward-aligned tiles per turn)
        // 4. Apply TileGlbVisual layout from config.glbLayout
        // 5. Place ball at first tile via BallController.PlaceAt()
        // 6. Add FinishLine.Configure() on last tile
    }
}
```

**Planned upgrade:** `IEnumerator BuildAsync()` with object pool, obstacle placement, and `OnLevelBuilt(Bounds)` event.

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
    ├── 🎮 Campaign ────── Level 1–2 (hand-built, tutorial pacing)
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

| Phase | Deliverable | Status |
|---|---|---|
| **P0** | `ProceduralPathGenerator` + path tests + visualizer | ✅ Done |
| **P0.5** | `ProceduralLevelBuilder` + edge-aligned placement + corner pads + GLB + ball + finish | ✅ Done |
| **P1** | `LevelValidator` + seed UI (word code) + `LevelProcedural` scene | 📋 Next |
| **P2** | Obstacle placement + difficulty curve + star rating | 📋 Planned |
| **P3** | Object pool + async builder + Daily mode + Replay code entry | 📋 Planned |
| **P4** | Chunk system (designer-authored prefab modules stitched at runtime) | 📋 Bonus |

**P0 + P0.5 is a playable procedural demo** in `Procedural(test).unity`. P1 adds validation and menu integration.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Unsolvable layout generated | Validate path with BFS; auto-retry with `seed + 1` up to 5 times |
| Ugly or overlapping tiles | Edge-aligned placement + incoming rotation + forward corner pads (implemented) |
| Ball falls at turns | 2 forward-aligned corner pad tiles per turn (`cornerPadTileCount`) |
| GLB visuals misaligned on rotated tiles | Always run `ApplyLayout` from reference asset after rotation is set |
| Level feels too random and frustrating | Difficulty curve + max obstacle cap per level |
| Hard to QA or reproduce bugs | Log seed code on every failure; Replay mode re-runs exact seed |
| Frame drop on mobile during generation | Async builder (5 tiles/frame) + object pool + 30-tile hard cap |
| Dead-end path (algorithm gets stuck) | Backtracking walk guarantees path length is always reached |

---

## Pitch for Academic Presentation

> Gravity Painter will implement procedural level generation using a **seeded backtracking random walk** on a bounded 2D tile grid. A `LevelGenConfig` ScriptableObject controls all parameters — path length, grid bounds, obstacle density, and prefab references — so difficulty can be tuned without touching code. A three-layer pipeline (Generator → Validator → Builder) ensures every generated level is solvable before play begins: the validator runs a BFS connectivity check and retries with `seed + 1` if the path fails. Campaign Levels 1–2 remain hand-authored for tutorial pacing; a single `LevelProcedural` scene handles Procedural, Daily Challenge, and Replay modes using the same existing `Tile` prefab, `TileGlbVisual` layout, `BallController`, and `FinishLine` systems already in the project. Mobile performance is maintained through an object pool, static batching, async frame-spread building, and a 30-tile hard cap. Players share levels using a 5-letter human-readable seed code.

---

## Minimal Implementation Checklist

- [x] `LevelGenConfig.asset` — `Assets/Settings/LevelGenConfig_Default.asset`
- [x] `ProceduralPathGenerator.cs` — biased backtracking walk + retry + snake fallback
- [x] `ProceduralPathVisualizer.cs` — Edit-mode path preview
- [x] `ProceduralTilePlacement.cs` — edge-aligned placement + corner pads
- [x] `ProceduralLevelBuilder.cs` — runtime spawner, GLB layout, ball, finish line
- [x] Editor tests — **Gravity Painter → Test Procedural Path**
- [x] Test scene — `Assets/Procedural(test).unity`
- [ ] `ObstaclePlacer.cs` — reads difficulty curve, respects safe zones
- [ ] `LevelValidator.cs` — BFS connectivity + solvability check + retry logic
- [ ] `TilePool.cs` — object pool pre-warmed on scene load
- [ ] `LevelEnvironmentScaler.cs` — resizes clouds + skybox to tile bounds
- [ ] `SeedHelper.cs` — `SeedToCode` and `CodeToSeed` utilities
- [ ] `LevelProcedural.unity` — production scene with ball, camera, UI
- [ ] `LevelMenu.cs` additions — `LoadProcedural`, `LoadDaily`, `LoadReplay` methods
- [ ] Seed code label in HUD UI
- [ ] Replay code entry UI panel

