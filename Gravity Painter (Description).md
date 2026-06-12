# Gravity Painter – Full Development Log
### Unity 3D Puzzle Game for Android | From Scratch to Prototype

---

## 1. Game Concept Summary

**Game Title:** Gravity Painter
**Platform:** Android (Unity 6.3 LTS)
**Genre:** 3D Physics Puzzle

The player guides a metallic ball through puzzle chambers without directly controlling the ball. Instead, the player taps colored zone tiles on the floor to change their gravity type. Each color applies a different directional force to the ball. The ball moves automatically based on whatever zone tile it is currently standing on.

**Core idea:** "You don't control the ball — you control gravity."

**Three zone types:**
- 🔴 Red → pushes ball forward (along tile's local forward)
- 🔵 Blue → pushes ball left (negative tile's local right)
- 🟡 Yellow → pushes ball right (tile's local right)
- ⬜ None (Grey) → no force, ball stays still

---

## 2. Unity Project Setup

**Unity Version:** Unity 6.3 LTS (6000.3.11f1)
**Template:** 3D (default)
**Project Name:** GravityPainterPrototype
**Platform Target:** Android

**Important setting confirmed:**
- Active Input Handling: `Input System Package (New)` or `Both`
- This was set in: Edit → Project Settings → Player → Other Settings → Active Input Handling

---

## 3. Scene Objects Created

| Object | Type | Purpose |
|--------|------|---------|
| Main Camera | Camera | Views the game scene |
| Directional Light | Light | Lights the scene |
| GameManager | Empty GameObject | Runs level generation script |
| Tile | Cube (Prefab) | One zone tile on the floor |
| Ball | Sphere | The player's ball, driven by physics |
| Coin | Cylinder/Prefab | Collectible item that grants session coins on touch |
| Finish Gate | Trigger | Marks level end; has strong body to cause ball rebound |

---

## 4. Scripts Created

Four C# scripts were created in `Assets/`:

| Script Name | Attached To | Purpose |
|-------------|-------------|---------|
| `TileZone.cs` | Tile prefab | Stores zone color type, applies visual material, provides force direction |
| `BallController.cs` | Ball | Reads current zone from trigger, applies AddForce every FixedUpdate |
| `InputManager.cs` | Main Camera | Detects mouse click / Android touch via raycast and calls CycleZone on hit tile |
| `GameManager.cs` | GameManager | Auto-spawns a grid of tile prefabs at runtime |
| `Coin.cs` | Coin prefab | Handles coin's visual rotation and trigger collection logic |
| `CoinManager.cs` | (Static) | Manages total permanent coins via PlayerPrefs and temporary session coins |
| `CoinDisplayUI.cs` | Canvas UI | Updates text to show current coin count |
| `ApplyThemeSkybox.cs` | Editor Script | Automatically applies `SkyCitySkybox` background to all level scenes |

---

## 5. TileZone.cs – Full Details

**File:** `Assets/TileZone.cs`
**Attached to:** Tile prefab

**Enum defined:**
```csharp
public enum ZoneType { None, Red, Blue, Yellow }
```

**Public fields (set in Inspector):**
- `ZoneType zoneType` → default: `None`
- `Material redMat` → drag RedZone material
- `Material blueMat` → drag BlueZone material
- `Material yellowMat` → drag YellowZone material
- `Material noneMat` → drag DefaultZone material

**Key methods:**
- `CycleZone()` → cycles None → Red → Blue → Yellow → None; called by InputManager on tap
- `UpdateVisual()` → swaps Renderer material to match current zoneType
- `GetForceDirection()` → returns a Vector3 force direction based on zoneType and tile's local transform:

```csharp
public Vector3 GetForceDirection()
{
    switch (zoneType)
    {
        case ZoneType.Red:    return transform.forward;   // along tile's local forward
        case ZoneType.Blue:   return -transform.right;    // tile's local left
        case ZoneType.Yellow: return transform.right;     // tile's local right
        default:              return Vector3.zero;
    }
}
```

**Why local transform.forward and not Vector3.forward:**
Using `transform.forward` means the tile pushes the ball relative to the tile's own rotation, not the world axis. This allows curved paths: rotate the tile 90° and Red will now push the ball along the new direction of the path.

---

## 6. BallController.cs – Full Details

**File:** `Assets/BallController.cs`
**Attached to:** Ball (Sphere)

**Public fields:**
- `float forceStrength = 10f` → how hard the ball is pushed each physics frame

**Private fields:**
- `Rigidbody rb` → cached Rigidbody reference
- `TileZone currentZone` → the tile the ball is currently standing on

**Key logic:**
- `Start()` → caches `rb = GetComponent<Rigidbody>()`
- `FixedUpdate()` → if `currentZone != null`, calls `rb.AddForce(currentZone.GetForceDirection() * forceStrength, ForceMode.Force)`
- `OnTriggerEnter(Collider other)` → when ball enters a tile's trigger collider, sets `currentZone` to that tile's TileZone
- `OnTriggerExit(Collider other)` → when ball leaves, clears `currentZone = null`

**Why FixedUpdate and not Update:**
Physics forces must always be applied in `FixedUpdate()` for consistent frame-rate independent behaviour.

---

## 7. InputManager.cs – Full Details

**File:** `Assets/InputManager.cs`
**Attached to:** Main Camera

**Why not OnMouseDown:**
`OnMouseDown()` does not work reliably with the New Input System or on Android touch. A Camera-based raycast approach works for both PC and Android.

**Key logic:**
```csharp
using UnityEngine.InputSystem;

void Update()
{
    bool inputDetected = false;
    Vector2 inputPosition = Vector2.zero;

    // PC mouse
    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
    {
        inputDetected = true;
        inputPosition = Mouse.current.position.ReadValue();
    }

    // Android touch
    if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
    {
        inputDetected = true;
        inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
    }

    if (inputDetected)
    {
        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            TileZone tile = hit.collider.GetComponent<TileZone>();
            if (tile != null) tile.CycleZone();
        }
    }
}
```

**Important:** Uses `Mouse.current` and `Touchscreen.current` from `UnityEngine.InputSystem` namespace, NOT the old `UnityEngine.Input`. This is because the project uses the New Input System package.

---

## 8. GameManager.cs – Full Details

**File:** `Assets/GameManager.cs`
**Attached to:** GameManager (empty GameObject)

**Purpose:** Auto-spawns a rectangular grid of Tile prefabs at runtime so the level is generated by code, not placed by hand.

**Public fields:**
- `GameObject tilePrefab` → drag Tile prefab from Assets/Prefabs/
- `int width = 3` → how many columns
- `int length = 10` → how many rows
- `float tileSpacing = 1.2f` → gap between tiles (1.2 avoids collider overlap)

**Key logic:**
```csharp
void Start()
{
    GenerateLevel();
}

void GenerateLevel()
{
    for (int x = 0; x < width; x++)
    {
        for (int z = 0; z < length; z++)
        {
            Vector3 pos = new Vector3(x * tileSpacing, 0f, z * tileSpacing);
            Instantiate(tilePrefab, pos, Quaternion.identity);
        }
    }
}
```

**Note for Level 1 design:**
`GenerateLevel()` was temporarily commented out (`// GenerateLevel();`) so tiles could be manually placed by hand to design Level 1 path layout.

---

## 9. Tile Prefab Setup (Inspector Settings)

**Location:** `Assets/Prefabs/Tile`
**Base object:** Unity Cube

**Components on the Tile prefab:**

### Transform
- Scale: `1, 0.1, 1` (flat, wide, thin floor tile)

### Box Collider #1 (Physical floor)
- Is Trigger: **OFF**
- Size: `1, 1, 1` (default)
- Purpose: Ball rests on this physically

### Box Collider #2 (Trigger zone)
- Is Trigger: **ON**
- Center: `0, 0.5, 0`
- Size: `1, 1, 1`
- Purpose: Detects when ball is above the tile; fires OnTriggerEnter/Exit

### TileZone Script
- Zone Type: `None` (default on start)
- Materials: RedZone, BlueZone, YellowZone, DefaultZone all assigned

---

## 10. Ball (Sphere) Setup (Inspector Settings)

**Base object:** Unity Sphere

### Transform
- Position: `0, 1, 0` (slightly above first tile)
- Scale: `1, 1, 1`

### Sphere Collider
- Is Trigger: **OFF** (must be unchecked; trigger is only on the tile)
- Radius: `0.5`

### Rigidbody
- Mass: `1`
- Linear Damping: `0.5`
- Angular Damping: `0.5`
- Use Gravity: **ON**
- Is Kinematic: **OFF**
- Interpolate: `Interpolate`
- Collision Detection: `Continuous`
- Constraints: all unchecked (no frozen axes)

### BallController Script
- Force Strength: `10`

---

## 11. Materials Created

**Location:** `Assets/` (or a Materials folder)

Four materials, created via Project panel → Create → Material:

| Material Name | Albedo Color | Notes |
|---------------|-------------|-------|
| RedZone | Bright Red | Applied when ZoneType = Red |
| BlueZone | Bright Blue | Applied when ZoneType = Blue |
| YellowZone | Bright Yellow | Applied when ZoneType = Yellow |
| DefaultZone | Grey | Applied when ZoneType = None |
| CoinMaterial | Golden | Applied to the newly added Coin prefab |
| SkyCitySkybox | Panoramic | 360-degree Sky City background used across all levels |

---

## 12. Physics Material

**GroundPhysics** was added to the Tile Mesh Collider to allow friction:
- Dynamic Friction: `0.4`
- Static Friction: `0.4`
- Bounciness: `0`

---

## 13. Input System Notes

**Package used:** Unity Input System (new)
All input code uses:
- `UnityEngine.InputSystem.Mouse.current`
- `UnityEngine.InputSystem.Touchscreen.current`

**Never use** `UnityEngine.Input.GetAxis()`, `UnityEngine.Input.GetMouseButtonDown()`, or `UnityEngine.Input.touchCount` in this project — they are from the legacy Input Manager and will throw `InvalidOperationException`.

---

## 14. Key Problems Solved

| Problem | Cause | Fix Applied |
|---------|-------|-------------|
| InvalidOperationException on Input | Project uses New Input System | Switched all input code to `UnityEngine.InputSystem` namespace |
| Tile color changed but ball didn't move | Trigger collider not large enough | Increased trigger Box Collider height to cover ball position |
| Ball static, no force applied | `OnTriggerEnter` not firing | Added second Box Collider with `Is Trigger = ON` and correct center/size |
| Clicking tile selects it in editor | Was in Scene view, not Game view | Must tap in Game view tab during Play mode |
| OnMouseDown not working | New Input System conflict | Replaced with Camera Raycast using Mouse.current |
| Ball falls off when path turns | `Vector3.forward` is world-space | Changed to `transform.forward` (tile's local forward) |
| Tiles only appear in Play mode | GameManager.Start() spawns them | Commented out GenerateLevel() for manual Level 1 design |
| Procedural tiles overlap at turns | Uniform grid spacing + outgoing rotation | Edge-aligned placement, incoming rotation, 2 forward corner pads |
| Procedural tiles too small | 1×1 prefab scale with Level 2 GLB layout | `tileLocalScale` matching Level 2 footprint on config |
| Ball falls at procedural turns | Corner gap after edge-aligned offset | `addCornerPads` + `cornerPadTileCount = 2` forward-aligned tiles |
| Ball completes level without satisfying feedback | Finish gate was a simple trigger | Made the finish gate a strong body so the ball rebounds upon hitting it |

---

## 15. Level 1 Design (Current State)

- Tiles placed manually (GenerateLevel disabled)
- Path: straight line of tiles, some with preset zone colours
- Ball starts at `(0, 1, 0)` above first tile
- Player must tap tiles to change colour and guide ball forward
- Turning tiles are rotated in the Scene so their local forward (blue axis) points along the path

### Zone Behaviour Summary

| Zone | Force Direction | Ball Behaviour |
|------|----------------|----------------|
| Red | `transform.forward` | Moves along tile's path direction |
| Blue | `-transform.right` | Moves left of current path direction |
| Yellow | `transform.right` | Moves right of current path direction |
| None | `Vector3.zero` | No force, ball stays still |

---

## 16. Procedural Level Generation (June 2026 — `kavin` branch)

Runtime procedural levels are **playable** in `Assets/Procedural(test).unity`. Full architecture and checklist: [Gravity_Painter_Procedural_Level_Generation.md](./Gravity_Painter_Procedural_Level_Generation.md).

### What works today

| Feature | Status |
|---------|--------|
| Seeded path generation (biased backtracking + snake fallback) | ✅ |
| Edit-mode path preview (`ProceduralPathVisualizer`) | ✅ |
| Runtime level build (`ProceduralLevelBuilder`) | ✅ |
| Level 2 GLB tile scale + edge-aligned placement | ✅ |
| 2 forward-aligned corner pad tiles per 90° turn | ✅ |
| Ball spawn + finish line on last tile | ✅ |
| Console tests (**Gravity Painter → Test Procedural Path**) | ✅ |

### Key scripts (`Assets/Scripts/Procedural/`)

| Script | Purpose |
|--------|---------|
| `ProceduralPathGenerator.cs` | Pure C# path logic from seed |
| `ProceduralLevelBuilder.cs` | Spawns tiles, ball, finish at runtime |
| `ProceduralTilePlacement.cs` | Edge-aligned positions + corner pads |
| `ProceduralPathVisualizer.cs` | Edit-mode tile preview |
| `LevelGenConfig.cs` | ScriptableObject parameters |
| `LevelCell.cs` | Per-tile data struct |

### Quick playtest

1. Open `Procedural(test).unity`
2. **Gravity Painter → Setup Procedural Level Scene (Step 2)**
3. Press **Play** (default seed `12345`)

### Not yet built

- `LevelProcedural.unity` production scene
- Obstacle placement, validator, object pool, Daily/Replay menu modes
- Seed word codes (KELOR-style)

---

## 17. Still To Build (Next Steps)

**Campaign**
- Polish Levels 3–5 layouts, transmission cards, star ratings
- Cannon launcher tile completion

**Procedural (Step 3+)**
- Dedicated `LevelProcedural` scene + main menu hooks
- `LevelValidator`, `ObstaclePlacer`, `TilePool`, async builder
- Daily challenge + replay seed codes

**General**
- Android build polish, performance profiling on mid-range devices

---

## 18. Folder Structure

```
Assets/
├── Art/
│   ├── Icons/              App icon, marketing images
│   ├── Materials/
│   │   ├── Tiles/          RedZone, BlueZone, YellowZone, DefaultZone
│   │   └── Environment/    PlanetLand, PlatformDeck, BoardColor
│   ├── Models/             Sci-Fi Ball 3D Model.glb, tiles.glb, RedLaserBeam.glb
│   └── Sprites/UI/         Menu & HUD images
├── Editor/                 Unity menu tools (Gravity Painter)
├── Prefabs/
│   ├── Gameplay/           Tile.prefab
│   └── Obstacles/          KorrathBeam.prefab, hammer, etc.
├── Resources/
│   └── Visuals/Tiles/      TilesGlbMesh.prefab (runtime load)
├── Scenes/
│   ├── Menus/              MainMenu.unity
│   └── Levels/             Level 1–2.unity
├── Procedural(test).unity  Procedural playtest scene (Step 2)
├── Scripts/
│   ├── Core/               TileZone, BallController, TileGlbVisual, InputManager
│   ├── Gameplay/           GameManager, FinishLine, LaserGate, HammerHazard
│   ├── Procedural/         Path generator, level builder, tile placement
│   ├── Systems/            CameraFollow, LevelEnvironment, LevelProgress
│   └── UI/                 MainMenu, LevelMenu, LevelComplete*
├── Settings/
│   ├── LevelGenConfig_Default.asset
│   ├── TileGlbReferenceLayout.asset
│   └── URP assets, Input System actions
└── ThirdParty/             Asset Store packs (Sci-Fi, Skybox, TMP, etc.)
```

