# Gravity Painter — Enhanced Project Plan

**Document version:** 2.0  
**Date:** May 2026  
**Platform:** Android (primary), Unity Editor for development  
**Engine:** Unity 6.3 LTS (6000.3.x)  
**Genre:** 3D Physics Puzzle — Sci-Fi Adventure  

---

## 1. Executive Summary

**Gravity Painter** is a mobile physics puzzle game where the player never steers the ball directly. Instead, they **paint gravity fields** on floor tiles by tapping: each tile cycles through neutral, red (forward), blue (left), and yellow (right). The probe rolls according to the gravity path the player creates.

**Elevator pitch:** *"You don't control the probe — you control the laws of physics."*

This document is the **enhanced version** of the prototype plan. It adds:

- A **deep narrative story** with meaningful lore behind every object
- **Lore-accurate meaning** for every tile color, power-up, obstacle, and pickup
- New **game mechanics** to make the game richer
- A **Chapter system** replacing raw level numbers
- **Boss encounters** and **environmental storytelling**

---

## 2. The Story — "Signal from Velorath"

### 2.1 Background

In the year **2187**, a deep-space research agency called **AETHER Labs** launched an unmanned **Gravitational Research Probe (GRP-7)** — a self-contained metallic sphere carrying sensors to study alien gravity fields.

The probe was sent to **Velorath** — a mysterious exoplanet with **unstable gravitational laws**. Scientists had detected strange signals from the planet suggesting its surface was covered in **ancient gravity-manipulation platforms** built by a long-extinct civilization called the **Velori**.

GRP-7 successfully landed on one of the **Velori floating platforms** — but the moment it touched down, communication was lost. The probe is alive but **stuck** — it cannot move on its own because Velorath's gravity is **different in every region of the planet**.

**You are the remote operator** back on Earth. You can see the platform through a satellite camera feed. You cannot move the probe directly. But you can **reprogram the ancient Velori gravity tiles** under the probe — painting the gravity field to guide the probe to the **signal beacon** at the end of each platform.

> *"The Velori did not walk. They painted. And the world moved for them."*

---

### 2.2 Chapter Structure

The game is divided into **5 Chapters** (replacing raw Level 1–12 numbers). Each chapter is a different region of Velorath with its own visual theme, story beat, and new mechanic.

| Chapter | Name | Setting | Story beat | Levels |
|---------|------|---------|------------|--------|
| **1** | The Awakening | Outer platform ring, clear alien sky | GRP-7 lands. First contact with Velori tiles | 1–2 |
| **2** | The Fractured Bridge | Crumbling mid-platform, stormy sky | Ancient tiles begin malfunctioning — death zones appear | 3–4 |
| **3** | The Guardian's Path | Inner sanctum, glowing architecture | Velori defense systems (hammer, crushers) activate | 5–6 |
| **4** | The Deep Current | Underground platform, lava far below | Gravity currents (wind zones), teleporters discovered | 7–9 |
| **5** | The Core Signal | Central Velori machine, core energy visible | All mechanics combine; reach the source of the signal | 10–12 |

---

### 2.3 Per-level story card (displayed before each level)

Each level begins with a short **transmission log** shown on screen:

> **CHAPTER 1 — LEVEL 1**  
> *"GRP-7 has touched down on Platform Outer-7. Gravity here is calm. Our scientists have identified three Velori tile types: red acceleration fields, blue deflectors, and yellow deflectors. Reprogram them to guide the probe to the beacon. Good luck, operator."*

This makes every level feel purposeful, not just a numbered puzzle.

---

## 3. Lore-Accurate Meaning for Every Object

Every tile, color, power-up, and obstacle has a **real in-universe explanation** so nothing feels arbitrary.

### 3.1 Tile Colors — The Velori Gravity Field Language

The Velori civilization communicated gravity instructions using **chromatic energy frequencies**. Each color represents a specific gravity field directive:

| Color | Velori Name | In-universe meaning | Gameplay effect |
|-------|------------|---------------------|-----------------|
| **Grey** | *Null Field* | Tile is unprogrammed — no artificial gravity active | No force on ball |
| **Red** | *Akara* (meaning "forward path") | Velori's primary travel directive — pushes forward | Push along tile forward |
| **Blue** | *Seval* (meaning "deflect left") | Used at crossroads to redirect left | Push tile left |
| **Yellow** | *Thovar* (meaning "deflect right") | Used at crossroads to redirect right | Push tile right |
| **Gold (finish)** | *Velara* (meaning "arrival") | The beacon tile — destination of every mission | Level complete trigger |
| **Black-Red (death)** | *Korrath* (meaning "forbidden zone") | Corrupted tiles — Velori's warning markers | Instant restart |

> *"The Velori never used maps. They read the floor."*

---

### 3.2 The Probe (Ball) — GRP-7

GRP-7 is not just a ball — it is a **living research instrument**:

- Its **metallic shell** is designed to survive Velorath's harsh atmosphere.
- The **glowing red ring** around it is its **sensor array** — actively scanning gravity fields.
- When the probe **falls off** the platform, it does not "die" — it enters **free-fall mode** and AETHER Labs activates an **emergency recall** (the 5-second restart delay represents the recall signal travel time from Earth).

This gives the restart mechanic a narrative reason instead of feeling like a punishment.

---

### 3.3 The Cannon — Velori Launch Accelerator

The cannon on certain tiles is a **Velori Launch Accelerator** — an ancient device the Velori used to transfer probes between distant platforms at high speed when the gravity field alone was too slow.

- Its **barrel points** in the direction the ancient Velori intended probes to travel.
- When GRP-7 enters the accelerator pad, the machine **recognizes** the probe's signal and fires it automatically.
- The cannon is **not hostile** — it is infrastructure, like a train launcher.

> *"The Velori built roads of light. The cannons were their express lanes."*

---

## 4. Enhanced Power-Ups — With Lore Meaning

Every power-up is a **Velori artifact** discovered mid-platform. Picking one up means GRP-7's sensors have **absorbed** its energy pattern.

| Power-up | Velori Artifact Name | In-universe meaning | Gameplay effect | Duration |
|----------|---------------------|---------------------|-----------------|----------|
| **Speed Core** ⚡ | *Akara Shard* | Fragment of a hypercharged red tile — amplifies forward momentum | +50% force on painted tiles | 5 s |
| **Chrono Lens** 🕐 | *Time Anchor* | Velori device that locally slows gravity oscillations | Time scale 0.5× for precise painting | 8 s |
| **Gravity Anchor** 🧲 | *Velori Anchor Stone* | Ancient weight used to stabilize probes over painted fields | Ball slightly pulled toward nearest painted tile | 6 s |
| **Phase Shell** 🛡️ | *Resonance Barrier* | Velori energy dome that absorbs one impact | Ignore one death / hazard hit | One use |
| **Void Cloak** 👻 | *Null Veil* | Cloaking field that lets probe pass through corrupted tiles | Pass through one obstacle | One use |
| **Echo Brush** 🖌️ | *Dual Paint Rune* | Ancient Velori painter's tool — applies two gravity fields at once | Next 3 taps paint two adjacent tiles | 3 taps |
| **Memory Stone** ↩️ | *Velori Memory Crystal* | Records and reverts the last field change | Undo last tile color change | 1 use |
| **Seeker Glyph** 💡 | *Path Finder Rune* | Glows when pointing toward the next correct tile | Highlights suggested next tile | 1 use |
| **Gravity Surge** 🌀 | *Surge Conduit* | Overcharges all painted tiles simultaneously | All colored tiles double force | 4 s |
| **Platform Lock** 🔒 | *Anchor Grid* | Freezes current tile state so taps do nothing wrong | Prevents accidental tile changes | 5 s |

### 4.1 Power-up rules

- Max **1 active timed** power-up at a time.
- **Phase Shell / Void Cloak / Memory Stone** are consumables stored in inventory.
- Power-ups are **disabled on Chapter 1** (tutorial — pure gravity painting).
- Artifacts appear from **Chapter 2** onward as glowing pickups on tiles.
- Can also be purchased in the **AETHER Store** (see Section 7).

---

## 5. Obstacles & Hazards — With Lore Meaning

Every obstacle has an in-universe reason for existing on the Velori platforms.

### 5.1 Existing obstacles

| Obstacle | Velori Name | In-universe meaning | Gameplay effect |
|----------|------------|---------------------|-----------------|
| **Void (fall off)** | *The Abyss* | The platform simply ends — Velorath's lower atmosphere is lethal | Restart after 5 s recall |
| **Death tile (black-red)** | *Korrath Zone* | Corrupted gravity field — causes feedback loop that destroys probes | Instant restart |
| **Finish tile (gold)** | *Velara Beacon* | The mission target — an ancient Velori signal transmitter | Level complete |
| **Swinging Hammer** | *Guardian Arm* | Velori defensive mechanism that activates when it detects foreign probes | Knocks ball off platform |

### 5.2 New planned obstacles (with lore)

| Obstacle | Velori Name | In-universe meaning | Gameplay effect |
|----------|------------|---------------------|-----------------|
| **Moving platform** | *Drift Slab* | Ancient platforms that shift position on gravitational tides | Carries ball, must time crossing |
| **Laser gate** | *Korrath Beam* | Corrupted energy fence — activates in cycles | Ball destroyed if hit |
| **Ice tile** | *Cryoveil Pad* | Tiles cooled by Velorath's polar winds — low grip | Very low friction, hard to stop |
| **Sticky tile** | *Adhesion Pad* | Velori maintenance pads coated in magnetic polymer | High friction, ball slows abruptly |
| **One-way tile** | *Directional Lock* | Velori path enforcer — gravity flows only one way | Locked color, cannot repaint |
| **Breakable tile** | *Fragile Slab* | Tile deteriorated after millennia — crumbles after one pass | Disappears after ball crosses once |
| **Wind zone tile** | *Aether Current* | Natural gravity stream venting from the planet core | Constant sideways push regardless of paint |
| **Teleporter pad** | *Velori Rift Gate* | Instant matter transfer system the Velori used between platforms | Ball teleports to paired pad |
| **Paint limit zone** | *Ration Field* | Energy-depleted region — only N fields can be active at once | Max N colored tiles allowed |
| **Switch tile** | *Velori Trigger Plate* | Pressure plate that opens locked sections of the platform | Ball rolls over it to open a door/route |
| **Gravity Reverser** | *Inversion Node* | Velori experimental device that flips horizontal forces | Painted forces work in opposite direction |
| **Mirror tile** | *Reflection Pad* | Redirects ball at 90° regardless of paint | Bounces ball perpendicular to entry |

### 5.3 Obstacle introduction curve

| Chapter | New obstacles |
|---------|--------------|
| 1 (Levels 1–2) | None — only paint and reach beacon |
| 2 (Levels 3–4) | Korrath death tiles, Guardian Arm hammer |
| 3 (Levels 5–6) | Moving platform, laser gate |
| 4 (Levels 7–9) | Ice, sticky, wind zone, teleporter |
| 5 (Levels 10–12) | Breakable, gravity reverser, paint limit, mirror |

---

## 6. New Mechanics to Add

These are extra systems not in the original prototype that will make the game significantly better.

### 6.1 Star Rating Per Level

| Stars | Condition |
|-------|-----------|
| ⭐☆☆ | Complete level |
| ⭐⭐☆ | Complete under time limit |
| ⭐⭐⭐ | Complete under time + under tile paint count limit |

Stars drive **bonus coins** and gate later chapters (e.g. need 10 total stars to unlock Chapter 3).

---

### 6.2 Gravity Paint Counter (new HUD element)

In harder levels, display a **"Paints Remaining"** counter in the HUD.  
Each tap costs 1 paint charge. Adds a resource management layer on top of direction puzzles.  
Introduced from Chapter 3 onward.

---

### 6.3 Operator Rank System (progression meta)

Replace simple level unlock with a **rank system**:

| Rank | Title | Unlock condition |
|------|-------|-----------------|
| 0 | Cadet Operator | Start |
| 1 | Field Operator | Complete Chapter 1 |
| 2 | Senior Operator | Complete Chapter 2 |
| 3 | Gravity Engineer | Complete Chapter 3 |
| 4 | Velori Scholar | Complete Chapter 4 |
| 5 | Chief Architect | Complete Chapter 5 (all 12 levels) |

Rank is shown on the main menu and level select screen. Unlocks exclusive ball skins.

---

### 6.4 Replay Mode

After completing a level, a **"Watch Replay"** button lets the player watch their own run played back. This helps them understand what went right and makes the game feel more polished.

---

### 6.5 Challenge Mode (post-launch)

Each level gets an optional **AETHER Challenge**:  
- Beat with 0 mistakes  
- Beat in under X seconds  
- Beat without using any power-ups  

Reward: exclusive coins + special "Challenge Badge" on level select.

---

### 6.6 Transmission Log (Story Screen)

Before each level, a **transmission card** slides in from the top showing:
- Chapter number + Level name  
- A short briefing paragraph (2–3 lines of lore)  
- A **"Begin Mission"** button  

After level complete, a short **"Mission Report"** card shows stars earned + brief result log.  
This wraps every single level in the story so nothing feels like a blank puzzle.

---

### 6.7 Ambient Storytelling Objects

Each level scene should contain **non-interactive background objects** that tell the story visually:

| Object | Lore meaning | Where to place |
|--------|-------------|---------------|
| Broken Velori pillars | Signs of the ancient civilization | Background edges |
| Ancient Velori glyphs on tiles | Decorative writing system | Tile surface texture |
| Floating debris | Crumbling platforms of the old empire | Background, Chapter 2+ |
| Planet core glow | Energy from Velorath's gravity engine | Sky, visible from Chapter 4 |
| Satellite dish prop | AETHER Labs communication node on the platform | Near spawn point |
| Distant destroyed platforms | Evidence of failed earlier missions | Far background |

---

## 7. Coins & Economy — The AETHER Credit System

### 7.1 Currency: AETHER Credits

The in-game currency is now called **AETHER Credits (AC)** — the interstellar currency used to fund research missions.

| Source | Amount | Notes |
|--------|--------|-------|
| Complete level (first clear) | 10 AC | One-time per level |
| Complete level (replay) | 3 AC | Reduced farm |
| 3-star a level | +15 AC | Bonus for perfection |
| Daily mission login | 25–100 AC | Streak bonus |
| Achievement unlock | 50–500 AC | One-time |
| Rewarded ad (optional) | 20 AC | Cap 5/day |
| IAP packs (optional) | Variable | Cosmetic only |

### 7.2 AETHER Credits sinks

| Item | Cost |
|------|------|
| Ball skin (common) | 200–500 AC |
| Ball skin (rare/legendary) | 1000–2500 AC |
| Artifact pack (×3 Phase Shell) | 150 AC |
| Memory Stone (×5) | 100 AC |
| Seeker Glyph (×3) | 120 AC |
| Level skip (casual mode only) | 300 AC |
| Chapter unlock (optional early access) | 500 AC |

---

## 8. Ball Skins — With Lore Meaning

Every skin is a **probe variant** used by AETHER Labs on different missions. Not just cosmetic — each has a story.

| Skin | Probe Designation | Story | Unlock |
|------|------------------|-------|--------|
| **Default** | GRP-7 Standard | The base research probe sent to Velorath | Free |
| **Chrome Sentinel** | GRP-1 Legacy | The very first probe ever launched by AETHER — decommissioned, now a skin | 500 AC |
| **Neon Pathfinder** | GRP-3 Explorer | Modified probe with neon-lit sensors for dark environments | 800 AC |
| **Plasma Core** | GRP-9 Experimental | Prototype powered by unstable plasma — too dangerous for real missions | 1200 AC |
| **Hologram Shell** | GRP-X Phantom | Test probe made from energy fields — appears translucent | 2000 AC |
| **Velori Relic** | Unknown Origin | A sphere discovered on Velorath itself — origin unknown | Beat all 12 levels |
| **Gold Operator** | GRP-7 Gold Edition | Special edition probe for elite operators | Achieve Rank 5 |
| **Corrupted Core** | GRP-4 Lost | A probe that was lost on Velorath and returned mysteriously changed | Complete all Chapter 5 challenges |

---

## 9. Level Roadmap — With Story

### 9.1 Detailed level plan (12 levels, v1.0)

| # | Chapter | Level Name | Story beat | New mechanic | Difficulty |
|---|---------|-----------|------------|--------------|------------|
| 1 | 1 — The Awakening | *First Contact* | GRP-7 lands. Learn Akara (red). | Red only | ★☆☆☆☆ |
| 2 | 1 — The Awakening | *Three Frequencies* | Discover Seval and Thovar (blue, yellow). | Blue + yellow | ★★☆☆☆ |
| 3 | 2 — Fractured Bridge | *Korrath Warning* | First corrupted tiles detected. | Death tiles | ★★☆☆☆ |
| 4 | 2 — Fractured Bridge | *The Guardian Wakes* | Velori defense arm activates. | Swinging hammer | ★★★☆☆ |
| 5 | 3 — Guardian's Path | *Drift Platform* | Platform tectonics — moving slabs. | Moving platform | ★★★☆☆ |
| 6 | 3 — Guardian's Path | *Laser Grid Sanctum* | Energy fences guard inner platform. | Laser gate | ★★★☆☆ |
| 7 | 4 — Deep Current | *Cryoveil Fields* | Polar winds cool the tiles. | Ice + sticky | ★★★★☆ |
| 8 | 4 — Deep Current | *Aether Streams* | Planet core vents gravity currents. | Wind zones | ★★★★☆ |
| 9 | 4 — Deep Current | *Rift Maze* | Velori rift gates discovered. | Teleporters | ★★★★☆ |
| 10 | 5 — Core Signal | *Resource Rationing* | Energy depleted — limited paints. | Paint budget limit | ★★★★★ |
| 11 | 5 — Core Signal | *Velori Gauntlet* | All defenses active simultaneously. | All mechanics mixed | ★★★★★ |
| 12 | 5 — Core Signal | *Signal Source* | The final chamber — source of the signal. | Boss-style long stage | ★★★★★ |

### 9.2 Level 12 — "Signal Source" (Boss Level Design)

Level 12 is not a typical level. It is a **long, multi-section platform** with:

1. **Section A:** Moving platforms + laser gates (Guardian section)
2. **Section B:** Wind zones + ice tiles (Cryoveil section)
3. **Section C:** Teleporter maze + paint limit (Rift section)
4. **Final section:** A long straight path to the **Velori Signal Core** (giant glowing beacon model) where the level completes

After completing Level 12, a short **cutscene text** appears:

> *"GRP-7 has reached the Velori Signal Core. The source of the transmission is identified. AETHER Labs begins decoding the message. You have completed your mission, Operator. But the signal... is only the beginning."*

This sets up a potential **Season 2 / Chapter 6** without requiring it for v1.0.

---

## 10. New Objects to Add (Not in Original Plan)

### 10.1 New tile types

| Object | Purpose |
|--------|---------|
| **Cannon Launcher tile** | Shoots ball in barrel direction with impulse — already in development |
| **Gravity Reverser tile** | Inverts all current tile forces temporarily |
| **Mirror tile** | Deflects ball 90° regardless of painted zone |
| **Switch tile** | Opens a locked door/path when ball rolls over it |
| **Fragile tile** | Breaks after one ball crossing |
| **Charge tile** | Recharges one consumed power-up |

### 10.2 New environmental objects

| Object | In-universe meaning | Visual |
|--------|---------------------|--------|
| **Velori Signal Beacon** | End goal of every level — ancient transmitter | Glowing gold pillar |
| **Gravity Node** | Velori energy distributor — decorative but powers tile glow | Floating crystal above tiles |
| **Aether Conduit** | Pipeline connecting platforms — background prop | Metal tube between platforms |
| **AETHER Comm Dish** | Player's satellite uplink — start point prop | Small dish near ball spawn |
| **Warning Siren** | Activates near death tiles — visual alert | Red blinking ring on floor |
| **Artifact Orb** | Power-up pickup object | Floating glowing sphere |

### 10.3 Audio objects (new additions)

| Sound | Trigger |
|-------|---------|
| Velori tile hum (ambient) | Background loop on each level |
| Tile paint SFX (per color) | Each tap — different tone for red/blue/yellow |
| Cannon fire boom | Cannon launch |
| Ball rolling loop | Ball moving at speed |
| Artifact pickup chime | Collect power-up orb |
| Guardian Arm warning creak | Hammer about to swing |
| Success transmission sting | Level complete |
| Emergency recall buzz | Ball falls off |

---

## 11. UI / Screen Flow (Enhanced)

```
┌─────────────────────────────────┐
│         Main Menu               │
│  [Play] [How to Play] [Settings]│
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│     Chapter / Level Select      │
│  Chapter 1: The Awakening       │
│  [L1✅] [L2✅] [L3🔒] ...      │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│     Transmission Card           │
│  "Chapter 2 — Level 3"          │
│  Mission briefing text...       │
│  [Begin Mission]                │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│         Gameplay                │
│  HUD: Stars / Timer / Paints    │
│  Tile grid + Ball               │
│  [Pause button]                 │
└──────────────┬──────────────────┘
               │ Win / Fail
               ▼
┌─────────────────────────────────┐
│    Mission Report Card          │
│  ⭐⭐☆  "Well done, Operator"   │
│  +10 AC earned                  │
│  [Restart] [Next] [Home]        │
└─────────────────────────────────┘
```

### Screen checklist

| Screen | Status |
|--------|--------|
| Main menu | ✅ Done |
| Level select (locked/unlocked) | ✅ Done |
| Gameplay HUD (minimal) | ✅ Partial |
| Level complete overlay | ✅ Done |
| Transmission card (pre-level story) | 📋 Planned |
| Mission report card (post-level) | 📋 Planned |
| Pause menu | 📋 Planned |
| AETHER Store (shop) | 📋 Planned |
| Probe (skin) select | 📋 Planned |
| Operator rank screen | 📋 Planned |
| Settings (audio, controls) | 📋 Planned |
| Daily mission popup | 📋 Planned |

---

## 12. Technical Architecture

### 12.1 Existing scripts

| Script | Role |
|--------|------|
| `TileZone` | Tile state, visuals, force |
| `BallController` | Ball physics & zone logic |
| `InputManager` | Input → tile tap |
| `CameraFollow` | Camera smooth follow |
| `LevelProgress` | PlayerPrefs unlock |
| `LevelCompleteUI` | End-of-level UI |
| `FinishLine` | Win trigger |
| `GameOverHandler` | Hazard fail |
| `HammerHazard` | Swinging arm obstacle |
| `LevelEnvironment` | Planet/deck backdrop |
| `CannonShooter` | Cannon launcher — in development |

### 12.2 New planned scripts

| Script | Purpose |
|--------|---------|
| `PowerUpManager` | Activate / consume / display artifact power-ups |
| `ArtifactPickup` | Pickup orb on tile — collected by ball trigger |
| `CoinWallet` | Add / spend / get AETHER Credit balance |
| `SkinManager` | Equip probe skin |
| `LevelStarSave` | Per-level star score storage |
| `AudioManager` | SFX + ambient music manager |
| `TransmissionCard` | Pre-level story slide-in UI controller |
| `MissionReport` | Post-level result card |
| `OperatorRank` | Calculate and display rank from total stars |
| `MovingPlatform` | Drift slab movement + ball carry |
| `LaserGate` | On/off cycle laser fence |
| `TeleporterPad` | Paired rift gate teleport |
| `GravityReverserTile` | Inversion node — flip tile forces |
| `FragileTile` | Breaks after one ball contact |
| `WindZoneTile` | Constant directional push |
| `PaintCounter` | HUD: remaining paint charges |
| `ReplayRecorder` | Record and replay level run |

---

## 13. Development Phases & Timeline

### Phase 0 — Prototype ✅ (Complete)

- Core paint loop, 5 levels, menu, unlock, Android build
- Cannon tile — in development

### Phase 1 — Story layer + Polish (2–3 weeks)

| Task | Owner |
|------|-------|
| Transmission card UI + 12 briefing texts | Dev + Design |
| Mission report card (stars + AC reward) | Dev |
| Fix Level 3–4 layouts and death tiles | Dev |
| Finish cannon launcher tile | Dev |
| Ambient sound pass v1 | Audio |
| Chapter screen layout | Dev + Art |

### Phase 2 — Content expansion (3–4 weeks)

| Task | Owner |
|------|-------|
| Levels 5–8 blockout with new obstacles | Design + Dev |
| Moving platform, laser gate, ice/sticky prefabs | Dev |
| Wind zone tile | Dev |
| Teleporter pad pair | Dev |
| Star rating + timer system | Dev |
| 3 ambient storytelling props per level | Art |

### Phase 3 — Meta & economy (2–3 weeks)

| Task | Owner |
|------|-------|
| AETHER Credits wallet + earn on complete | Dev |
| AETHER Store screen | Dev + Art |
| 5–8 probe skins | Art + Dev |
| Artifact power-ups (Phase Shell, Speed Core, Memory Stone) | Dev |
| Operator rank system | Dev |
| Daily mission popup | Dev |

### Phase 4 — Final levels + Release prep (2 weeks)

| Task | Owner |
|------|-------|
| Levels 9–12 (including boss level 12) | All |
| Performance profiling on Android | Dev |
| Play Store assets (icon, screenshots, description) | Art |
| Soft launch / beta test | QA |

**Estimated v1.0:** ~10–12 weeks from Phase 1 start.

---

## 14. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Physics unpredictability | Tune `forceStrength`, drag, max speed; test on low-end Android |
| Story text too long | Keep each transmission card under 3 lines; skip button always available |
| Level 12 boss stage too complex | Build in 3 testable sections; can ship Level 12 as two levels if needed |
| Scope creep | Lock v1.0 to 12 levels + 8 skins + 5 power-ups |
| Branch merge conflicts | Single integration branch weekly |
| Large assets in git | `.gitignore` for APK / unitypackage / GLB |

---

## 15. Success Metrics (Post-Launch)

| Metric | Target |
|--------|--------|
| Level 1 completion rate | > 85% |
| Chapter 2 retention (reach Level 3) | > 55% |
| 3-star rate on any level | > 30% of players |
| Average session length | 8–15 min |
| AETHER Store visit rate | > 20% of DAU |
| Crash-free sessions | > 99% |

---

## 16. Appendix — Velori Language Reference

A mini-glossary of Velori terms used in the game world for consistent lore writing:

| Velori Word | Meaning |
|-------------|---------|
| *Akara* | Forward — red tile |
| *Seval* | Left deflect — blue tile |
| *Thovar* | Right deflect — yellow tile |
| *Velara* | Arrival / destination — gold finish tile |
| *Korrath* | Forbidden — black-red death tile |
| *Null Field* | Unprogrammed tile — grey |
| *Rift Gate* | Teleporter |
| *Guardian Arm* | Hammer obstacle |
| *Aether Current* | Wind zone |
| *Cryoveil* | Ice tile |
| *Surge Conduit* | Speed boost artifact |
| *Resonance Barrier* | Shield artifact |

---

## 17. Repository Branches

| Branch | Purpose |
|--------|---------|
| `master` | Early baseline |
| `kavin` | Integration / packages lock |
| `hari` | Level 3, death/finish materials, tile fixes |

**Recommendation:** Create `story-layer` branch for transmission cards and mission report UI without touching gameplay code.

---

*This is the enhanced v2.0 plan for Gravity Painter. Every object, color, power-up, and mechanic is now grounded in the story of GRP-7 and the Velori civilization. Update version numbers and dates as milestones are completed.*
