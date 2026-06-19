# Gravity Painter — Project Plan (Index)

> **Use this document for the meeting:**  
> **[Gravity Painter Enhanced Plan.md](./Gravity%20Painter%20Enhanced%20Plan.md)** — **v2.0 (authoritative)**

The enhanced plan replaces the earlier generic `PROJECT_PLAN.md` outline. It is better suited for stakeholders because every system is tied to **story, lore, and chapters** — not just feature lists.

---

## Why the Enhanced Plan is the primary document

| Topic | Earlier outline | Enhanced Plan v2.0 |
|--------|----------------|---------------------|
| **Story** | Gameplay only | Full narrative: GRP-7 probe, Velorath, Velori civilization |
| **Levels** | Level 1–12 numbers | **5 chapters** with names, themes, and transmission briefings |
| **Tiles** | Red / blue / yellow | Velori names: *Akara*, *Seval*, *Thovar*, *Velara*, *Korrath* |
| **Power-ups** | Generic names | Lore artifacts: *Akara Shard*, *Resonance Barrier*, etc. |
| **Economy** | “Coins” | **AETHER Credits (AC)** with in-universe funding story |
| **Skins** | Cosmetic list | Probe designations (GRP-7, GRP-3, lost GRP-4…) |
| **Obstacles** | Mechanics only | *Guardian Arm*, *Korrath Zone*, *Drift Slab*, etc. |
| **New systems** | Basic stars | Operator ranks, transmission cards, paint counter, replay, boss Level 12 |
| **Cannon** | Not included | **Velori Launch Accelerator** — narrative + gameplay |
| **Meeting pitch** | Feature checklist | Elevator pitch + chapter table + talking points built in |

---

## Quick reference (from Enhanced Plan)

**Pitch:** *"You don't control the probe — you control the laws of physics."*

**Chapters**

1. **The Awakening** (L1–2) — learn gravity painting  
2. **The Fractured Bridge** (L3–4) — death tiles, hammer  
3. **The Guardian's Path** (L5–6) — mover, laser  
4. **The Deep Current** (L7–9) — ice, wind, teleporter  
5. **The Core Signal** (L10–12) — full mix + finale boss stage  

**Prototype today (✅)**  
Paint gravity, sci-fi tiles, GLB ball, level unlock, level-complete UI, Levels 1–2 in build, hammer on Level 4, split Korrath Beam laser on Level 2, **coins.glb collectibles**, **power-ups + checkpoints** (procedural), finish gate rebound.

**Procedural generation (✅ Steps 1–2 on `kavin` / `master`)**  
Seeded path generator, runtime level builder, edge-aligned tile placement, 2 forward corner pads per turn, GLB visuals, ball spawn, finish line, coins, power-ups, checkpoint, campaign skybox. Playtest in `Assets/Procedural(test).unity`. Level 3+ from menu. See [Gravity_Painter_Procedural_Level_Generation.md](./Gravity_Painter_Procedural_Level_Generation.md).

**Stability (✅)**  
Unity `6000.3.11f1`, Project Settings auto-repair menu, level unlock on completion only.

**Planned for v1.0 (📋)**  
AETHER Credits lore UI, probe skins, transmission/mission report UI, star ratings, 12 polished levels, procedural Daily / Replay modes.

**Changelog:** [CHANGELOG.md](./CHANGELOG.md)

**Timeline:** ~10–12 weeks from Phase 1 (see Enhanced Plan §13).

---

## Other docs

| File | Purpose |
|------|---------|
| [Gravity Painter Enhanced Plan.md](./Gravity%20Painter%20Enhanced%20Plan.md) | **Full project plan for meetings** |
| [Gravity_Painter_Procedural_Level_Generation.md](./Gravity_Painter_Procedural_Level_Generation.md) | **Procedural level gen — status, architecture, checklist** |
| [CHANGELOG.md](./CHANGELOG.md) | **What shipped on `kavin` / `master` (updated June 2026)** |
| [README.md](./README.md) | Technical development log (prototype) |
| [Gravity Painter (Description).md](./Gravity%20Painter%20Description).md) | Original design notes |
| [Gravity Painter Level Difficulty System.md](./Gravity%20Painter%20Level%20Difficulty%20System.md) | Procedural difficulty tiers |

---

*For any project-plan update, edit **Gravity Painter Enhanced Plan.md** (v2.0). Keep this file as a short index only.*
