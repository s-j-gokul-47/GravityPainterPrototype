# Gravity Painter — Level Difficulty System

## Overview

Gravity Painter uses a **progressive difficulty system** that automatically increases challenge each time a player completes a procedural level. The system has five layers: path complexity, grid size, turn frequency, obstacle density, and time pressure. All five layers are driven by a single `difficulty` float value between 0.0 (Easy) and 1.0 (Expert), which is stored in `PlayerPrefs` and increases by a fixed step after every level completion.

The campaign levels (Level 1–5) are hand-authored and use fixed difficulty. The procedural mode, daily challenge, and replay mode all use this dynamic difficulty system.

---

## Difficulty Value

### Definition

```
difficulty = Clamp01(levelsCompleted * DIFFICULTY_STEP)
```

- `levelsCompleted` — how many procedural levels the player has finished (stored in PlayerPrefs)
- `DIFFICULTY_STEP` — controls how fast difficulty grows (default: `0.05f`)
- Result is always clamped between `0.0` and `1.0`

### Growth Rate

| DIFFICULTY_STEP | Levels to reach Expert (1.0) | Feel |
|---|---|---|
| 0.03 | 34 levels | Slow, gradual burn |
| 0.05 | 20 levels | Default — balanced |
| 0.08 | 13 levels | Fast ramp |
| 0.10 | 10 levels | Aggressive |

The default value of `0.05` means a player who completes one level per session will reach Expert difficulty after roughly 20 sessions. This is tunable in `DifficultyManager.cs` without touching any other code.

---

## Difficulty Tiers

For readability, the 0–1 difficulty range maps to four named tiers:

| Tier | Difficulty Range | Typical Level Count |
|---|---|---|
| Easy | 0.00 – 0.24 | Levels 1–5 |
| Medium | 0.25 – 0.49 | Levels 6–10 |
| Hard | 0.50 – 0.74 | Levels 11–15 |
| Expert | 0.75 – 1.00 | Levels 16+ |

```csharp
public static string GetTierName(float difficulty)
{
    if (difficulty < 0.25f) return "Easy";
    if (difficulty < 0.50f) return "Medium";
    if (difficulty < 0.75f) return "Hard";
    return "Expert";
}
```

---

## Layer 1 — Path Length

Path length is the most visible difficulty indicator. Short paths feel manageable; long winding paths feel like a real challenge.

### Formula

```
minPathLength = Lerp(absoluteMin, absoluteMax * 0.6, difficulty)
maxPathLength = Lerp(absoluteMin + 3, absoluteMax, difficulty)
```

Default values: `absoluteMin = 8`, `absoluteMax = 30`

### Values Across Difficulty

| Difficulty | Min Path | Max Path | Avg Tiles |
|---|---|---|---|
| 0.00 (Easy) | 8 | 11 | ~9 |
| 0.25 (Medium) | 10 | 15 | ~12 |
| 0.50 (Hard) | 13 | 20 | ~16 |
| 0.75 (Hard+) | 16 | 25 | ~20 |
| 1.00 (Expert) | 18 | 30 | ~24 |

A longer path means more taps, more thinking, and more chances for the ball to go wrong.

---

## Layer 2 — Grid Size

The grid bounds control how much space the path can spread across. A larger grid allows more complex winding paths; a smaller grid forces the path to loop tightly in a small area.

### Formula

```
gridWidth = RoundToInt(Lerp(5, 9, difficulty))
gridDepth = RoundToInt(Lerp(5, 9, difficulty))
```

### Values Across Difficulty

| Difficulty | Grid Width | Grid Depth | Total Cells |
|---|---|---|---|
| 0.00 (Easy) | 5 | 5 | 25 |
| 0.25 (Medium) | 6 | 6 | 36 |
| 0.50 (Hard) | 7 | 7 | 49 |
| 0.75 (Hard+) | 8 | 8 | 64 |
| 1.00 (Expert) | 9 | 9 | 81 |

A 9×9 grid with a 30-tile path means the path visits roughly one-third of the available cells — creating a genuinely complex maze-like layout.

---

## Layer 3 — Turn Frequency

Turn frequency controls how often the path changes direction. A low value produces mostly straight runs with occasional turns; a high value produces rapid zigzag turns that require the player to pay close attention to tile orientation.

### Formula

```
turnFrequency = Lerp(0.1, 0.7, difficulty)
```

The generator uses this value as the probability of choosing a non-preferred direction at each step. A value of `0.1` means 90% of steps go in the preferred direction; a value of `0.7` means 70% of steps turn.

### Values Across Difficulty

| Difficulty | Turn Frequency | Path Character |
|---|---|---|
| 0.00 (Easy) | 0.10 | Mostly straight with rare turns |
| 0.25 (Medium) | 0.28 | Gentle curves and occasional turns |
| 0.50 (Hard) | 0.40 | Frequent direction changes |
| 0.75 (Hard+) | 0.55 | Rapid zigzag turns |
| 1.00 (Expert) | 0.70 | Near-maximum turn density |

Turn frequency is intentionally capped at 0.70 (not 1.0) because a fully random turn rate can create visually unreadable paths. The cap ensures the path always has a visible sense of direction.

---

## Layer 4 — Obstacle Density

Obstacles are not spawned on Easy levels. They are introduced gradually from Medium onward using an `AnimationCurve` in `LevelGenConfig`, which designers can adjust in the Unity Inspector without code changes.

### Safe Zones (Never Spawn Obstacles)

Certain tiles are always obstacle-free regardless of difficulty:

| Tile Index | Reason |
|---|---|
| 0 | Ball spawn tile — must be clear |
| 1 | First reaction tile — player needs one clear step |
| N-2 | Approach to finish — give player a clear run-in |
| N-1 (last) | Finish line tile — always clear |

### Obstacle Count by Difficulty

| Difficulty | Max Obstacles | Obstacle Types Available |
|---|---|---|
| 0.00 (Easy) | 0 | None |
| 0.25 (Medium) | 1 | Swinging Hammer |
| 0.40 (Medium+) | 1 | Hammer, Laser Gate |
| 0.50 (Hard) | 2 | Hammer, Laser Gate |
| 0.65 (Hard+) | 3 | Hammer, Laser, Moving Platform |
| 0.80 (Expert) | 3 | All obstacles |
| 1.00 (Expert) | 4 | All obstacles, higher spawn chance |

### Obstacle Spawn Chance Formula

```
spawnChance = obstacleDensityCurve.Evaluate(difficulty)
```

The `AnimationCurve` is set in the `LevelGenConfig` Inspector. The recommended curve shape is:

```
Chance
1.0 |                                   ╭──────
0.8 |                              ╭────╯
0.6 |                         ╭───╯
0.4 |                    ╭───╯
0.2 |          ╭─────────╯
0.0 |──────────╯
    └─────────────────────────────────────────
      0.0      0.25     0.50     0.75     1.0
     Easy    Medium    Hard   Hard+   Expert
```

No obstacle appears before difficulty 0.20, ensuring the first several levels are always clean.

---

## Layer 5 — Minimum Finish Distance

To prevent the generator from placing the finish tile near the start (which would make levels feel trivially short), a minimum distance check is enforced after path generation.

### Formula

```
minFinishDistance = Max(gridWidth, gridDepth) * 0.4
```

If the Euclidean distance between the start tile and the finish tile is less than `minFinishDistance`, the generator retries with `seed + 1`. Up to 10 retries are allowed before falling back to a safe straight path.

### Effect by Difficulty

| Difficulty | Grid | Min Finish Distance |
|---|---|---|
| 0.00 (Easy) | 5×5 | 2.0 units |
| 0.50 (Hard) | 7×7 | 2.8 units |
| 1.00 (Expert) | 9×9 | 3.6 units |

This guarantees that even on small Easy grids, the ball always feels like it has traveled a meaningful distance.

---

## Layer 6 — Time Limit (Future Feature)

A time limit is planned for Expert difficulty. It is not active in the current build but the field is reserved in `LevelGenConfig`.

```csharp
[Header("Time Limit (Expert Only)")]
public bool enableTimeLimit = false;
public float timeLimitSeconds = 60f; // applied only when difficulty >= 0.75
```

When enabled, the timer appears in the HUD only at Hard+ and Expert tiers. Easy and Medium levels are always untimed.

---

## DifficultyManager — Code Reference

`Assets/Scripts/Procedural/DifficultyManager.cs`

```csharp
public static class DifficultyManager
{
    const string KEY_LEVELS_COMPLETED = "ProceduralLevelsCompleted";
    const float  DIFFICULTY_STEP      = 0.05f;

    public static int   LevelsCompleted  => PlayerPrefs.GetInt(KEY_LEVELS_COMPLETED, 0);
    public static float CurrentDifficulty => Mathf.Clamp01(LevelsCompleted * DIFFICULTY_STEP);

    /// Call this exactly once when ball touches FinishLine trigger
    public static void OnLevelCompleted()
    {
        PlayerPrefs.SetInt(KEY_LEVELS_COMPLETED, LevelsCompleted + 1);
        PlayerPrefs.Save();
    }

    /// Editor / new game reset
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KEY_LEVELS_COMPLETED);
        PlayerPrefs.Save();
    }
}
```

---

## DifficultyScaler — Code Reference

`Assets/Scripts/Procedural/DifficultyScaler.cs`

```csharp
public static class DifficultyScaler
{
    public static void Apply(LevelGenConfig config, float difficulty)
    {
        // Layer 1: Path length
        config.minPathLength = Mathf.RoundToInt(
            Mathf.Lerp(config.absoluteMinPath, config.absoluteMaxPath * 0.6f, difficulty));
        config.maxPathLength = Mathf.RoundToInt(
            Mathf.Lerp(config.absoluteMinPath + 3, config.absoluteMaxPath, difficulty));

        // Layer 2: Grid size
        config.gridWidth = Mathf.RoundToInt(Mathf.Lerp(5f, 9f, difficulty));
        config.gridDepth = Mathf.RoundToInt(Mathf.Lerp(5f, 9f, difficulty));

        // Layer 3: Turn frequency
        config.turnFrequency = Mathf.Lerp(0.1f, 0.7f, difficulty);

        // Layer 4: Obstacle count
        config.maxObstaclesPerLevel = Mathf.RoundToInt(Mathf.Lerp(0f, 4f, difficulty));
    }
}
```

---

## LevelGenConfig — Difficulty Fields

```csharp
[Header("Difficulty Progression")]
[Range(0f, 1f)] public float difficulty         = 0f;
public int   absoluteMinPath                    = 8;
public int   absoluteMaxPath                    = 30;
public AnimationCurve obstacleDensityCurve;
public int   maxObstaclesPerLevel               = 4;
[Range(0f, 1f)] public float turnFrequency      = 0.2f;

[Header("Time Limit (Expert Only — Future)")]
public bool  enableTimeLimit                    = false;
public float timeLimitSeconds                   = 60f;
```

---

## Full Progression Table

This table shows how all five layers change together as the player progresses.

| Levels Done | Difficulty | Tier | Path Tiles | Grid | Turn Freq | Obstacles | Time Limit |
|---|---|---|---|---|---|---|---|
| 0 | 0.00 | Easy | 8 – 11 | 5×5 | 0.10 | 0 | No |
| 1 | 0.05 | Easy | 8 – 12 | 5×5 | 0.12 | 0 | No |
| 3 | 0.15 | Easy | 9 – 13 | 5×5 | 0.17 | 0 | No |
| 5 | 0.25 | Medium | 10 – 15 | 6×6 | 0.28 | 1 | No |
| 7 | 0.35 | Medium | 11 – 17 | 6×6 | 0.34 | 1 | No |
| 10 | 0.50 | Hard | 13 – 20 | 7×7 | 0.40 | 2 | No |
| 13 | 0.65 | Hard | 15 – 23 | 8×8 | 0.52 | 3 | No |
| 15 | 0.75 | Expert | 16 – 25 | 8×8 | 0.58 | 3 | Optional |
| 17 | 0.85 | Expert | 17 – 27 | 9×9 | 0.64 | 4 | Optional |
| 20 | 1.00 | Expert | 18 – 30 | 9×9 | 0.70 | 4 | Optional |

---

## What the Player Feels at Each Tier

### Easy (Levels 1–5)
Short straight path, no obstacles, small grid. The player learns the core mechanic: tap a tile to give the ball force in that direction. No punishment for mistakes beyond falling off the edge.

### Medium (Levels 6–10)
The path starts turning. One obstacle appears occasionally — usually a Swinging Hammer on a mid-path tile. The player starts needing to plan two or three taps ahead instead of reacting one tile at a time.

### Hard (Levels 11–15)
Long winding paths across a 7×7 grid with two obstacles. The path may double back on itself. The player cannot see the finish line from the start and must trust the tile rotations. Laser Gate obstacles are introduced here — instant kill on contact.

### Expert (Levels 16+)
Maximum path length, 9×9 grid, frequent turns, three to four obstacles including Moving Platforms. The ball must cross moving platforms at the right moment. An optional time limit adds final pressure. This tier is designed for players who have mastered all mechanics.

---

## Obstacle Introduction Order

Obstacles are introduced one type at a time so the player has time to learn each one:

| Obstacle | Introduced At Difficulty | Tier |
|---|---|---|
| Swinging Hammer | 0.25 | Medium |
| Laser Gate | 0.40 | Medium+ |
| Moving Platform | 0.65 | Hard+ |
| Death Pit | 0.70 | Hard+ |
| Spike Wall | 0.80 | Expert |
| Electric Floor | 0.85 | Expert |
| Crusher Block | 0.90 | Expert |

No new obstacle type appears in the same level as another newly introduced type. The `ObstaclePlacer` enforces this by checking which types have been seen before and restricting new introductions to one per level.

---

## Testing and Tuning

### Reset Progress (Editor)
Add this to an Editor menu for quick testing:

```csharp
[MenuItem("Gravity Painter/Reset Difficulty Progress")]
static void ResetDifficulty() => DifficultyManager.ResetProgress();
```

### Force a Specific Difficulty
For QA testing a specific tier without completing levels:

```csharp
[MenuItem("Gravity Painter/Force Hard Difficulty")]
static void ForceHard()
{
    // 0.50 difficulty = 10 levels completed at step 0.05
    PlayerPrefs.SetInt("ProceduralLevelsCompleted", 10);
    PlayerPrefs.Save();
}
```

### Key Tuning Variables

| Variable | Location | Effect |
|---|---|---|
| `DIFFICULTY_STEP` | `DifficultyManager.cs` | Speed of difficulty growth |
| `absoluteMinPath` | `LevelGenConfig.asset` | Shortest possible path |
| `absoluteMaxPath` | `LevelGenConfig.asset` | Longest possible path |
| `obstacleDensityCurve` | `LevelGenConfig` Inspector | Obstacle spawn chance shape |
| `maxObstaclesPerLevel` | `LevelGenConfig.asset` | Hard cap on obstacles per level |
| `turnFrequency` max (0.7) | `DifficultyScaler.cs` line 12 | Max turn density at Expert |

---

## Integration Checklist

- [ ] `DifficultyManager.OnLevelCompleted()` called in `FinishLine` trigger
- [ ] `DifficultyScaler.Apply(config, difficulty)` called before every level generation
- [ ] `LevelGenConfig_Default.asset` has `obstacleDensityCurve` set in Inspector
- [ ] `absoluteMinPath = 8` and `absoluteMaxPath = 30` set in config asset
- [ ] Editor reset menu added for testing
- [ ] Difficulty tier name displayed in HUD after level complete
- [ ] `PlayerPrefs` key `ProceduralLevelsCompleted` persists across sessions

