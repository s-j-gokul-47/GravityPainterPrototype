# Gravity Painter — Changelog

> **Active branch:** `kavin` (also synced to `master` as of June 2026)  
> **Unity:** `6000.3.11f1`  
> **Latest commit:** `77e483a` — *fixed coins*

---

## June 2026 — `kavin` / `master` (current)

### Coins & economy
- **`coins.glb`** 3D coin model replaces placeholder cylinder (`Assets/Art/Models/GLB/coins.glb`)
- **`CoinVisual`** + **`CoinAppearance`** + **`CoinAppearanceProfile`** — shared look for all coins
- **Master coin** on Level 1 Tile (46): `Coin_Master_Tile(46)` — edit `CoinVisualRoot`, then **Gravity Painter → Publish Master Coin To All Coins**
- **Baked coins** in Level 1 & 2 under `Coins` parent (visible in Hierarchy without Play)
- **Procedural coins** spawn from `Coin.prefab` using profile scale/height (`CampaignCoinPlacement`)
- **`CoinManager`** session coins reset on level start; committed at finish line
- **`CoinDisplayUI`** on main menu shows total coins
- Restored coin assets after accidental deletion in power-ups merge (`77e483a`)

### Power-ups & checkpoints *(friend update — Gokul)*
- **`PowerUpPickup`** + **`PowerUpManager`** — Speed Core, Magnet, Shield
- Prefabs: `Assets/Prefabs/PowerUps/`
- **Checkpoint** mid-level respawn (`Checkpoint.cs`, `BallController` checkpoint API)
- Procedural spawn for power-ups (`powerUpSpawnChance`, height tuning)
- Magnet strength tuned (`3166f20`)

### Procedural levels
- **Skybox + land** on procedural scene (matches campaign Levels 1–2 city background)
- **Hammers & laser gates** from **Level 10+** only (not on early procedural runs)
- **Tile hurdle** placement updated (`0a04d96`)
- Level 3+ routes to `Procedural(test).unity` from menu
- Deterministic seeds; difficulty progression via `DifficultyManager`

### Campaign & levels
- **Level 1** hand-authored (restored after merge conflicts)
- **Level 2** finish line GLB visual, Korrath Beam laser
- **Finish gate** strong body — ball rebounds until trigger crossed (`a3533dc`)
- **City skybox** on all campaign levels (`52d0dec`)
- **Level unlock fix** — levels unlock only on completion, not on replay

### Editor tools (Gravity Painter menu)
| Menu item | Purpose |
|-----------|---------|
| Repair Project Settings | Fix build scenes, URP, input after Unity restarts |
| Place Coins In Levels 1 And 2 | Bake coin prefabs into campaign scenes |
| Publish Master Coin To All Coins | Push master coin transforms to profile + all instances |
| Sync All Coins From Profile | Apply `CoinAppearanceProfile` everywhere |
| Apply Coin GLB To Prefab | Import `coins.glb` and wire `Coin.prefab` |
| Setup Procedural Level Scene | Wire builder, ball, camera in procedural scene |
| Test Procedural Path | Console tests for path generator |

### Stability
- **`GravityPainterProjectSettingsRepair`** — auto-repair on editor open
- Pinned **Unity 6000.3.11f1** (not 6000.4.x) in `ProjectVersion.txt`
- Known-good build settings: Main Menu, Level 1–2, `Procedural(test).unity`

---

## May–June 2026 — Earlier `kavin` milestones

| Commit | Summary |
|--------|---------|
| `67f80ef` | Procedural skybox and land environment |
| `b2652cf` | Project settings auto-repair + Unity version pin |
| `7ef0bb1` | Obstacles from level 10 in procedural |
| `9059867` | Level 1 restore + build settings after coin merge |
| `5acdce9` | Initial coin collection system |
| `799440e` | Finish line GLB, double-tap cross, GLB consolidation |
| `f51d3be` | Procedural starts at Level 3 |
| `cb24ec0` | Difficulty progression + Level 3 handoff |
| `0632cfe`–`0fe2b2b` | Procedural Steps 1–2: path gen, builder, corner pads |
| `ff79573` | Assets reorganization + positional tile painting |
| `07138cc` | Sci-fi tiles/ball, level complete UI, progression |

---

## Branches

| Branch | Role |
|--------|------|
| **`kavin`** | Main development — features above |
| **`master`** | Synced to match `kavin` (June 2026) |
| **`hari`** | Teammate branch |

---

## Known issues / notes

- Do **not** `git checkout origin/master -- ProjectSettings/` for fixes — use **Repair Project Settings** menu
- Close Unity before `git pull` on scene files
- Coin auto-sync on every inspector change was **removed** (caused editor hang) — use **Publish Master Coin** manually
- `coins.glb` is ~32 MB — keep in Git; avoid deleting in merges

---

*Update this file when merging significant features to `kavin` / `master`.*
