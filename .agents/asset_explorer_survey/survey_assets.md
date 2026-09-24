# Visual & Audio Asset Survey Report

**Project**: Unity 2D Top-Down Endless Shooter  
**Working Directory**: `c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity`  
**Date**: 2026-09-21  
**Investigator**: Visual Asset & Scene Explorer  

---

## 1. Executive Summary

This survey provides a comprehensive analysis of visual, audio, environment, and UI assets available in the repository. The project contains substantial high-quality 2D pixel art in `Assets/Tiny RPG Forest` and `Assets/bullets/Fire Effect and Bullet 16x16.png`, an existing player soldier sprite in `Assets/soldier-img/soldier-no-bg.png`, and a functioning tilemap scene in `Assets/Scenes/shooting.unity`.

Key takeaways:
1. **Player Character**: `Assets/soldier-img/soldier-no-bg.png` (156x186 px) is configured for 360-degree top-down mouse aiming (`rb.rotation = angle - 90f`) with a designated `Fire Point` at local offset `(0.35, 1.18)`. It directly satisfies R1.
2. **Enemy Triad (R2)**:
   - **Chaser (Melee)**: `treant` sprites (`Assets/Tiny RPG Forest/Artwork/sprites/treant/`, 31x35 px, walk & idle animations).
   - **Rusher (Fast Melee)**: `mole` sprites (`Assets/Tiny RPG Forest/Artwork/sprites/mole/`, 24x24 px, walk & idle animations).
   - **Shooter (Ranged)**: `treant` variant with green/poison color tint OR `mole` variant shooting ranged projectiles (`arrow.png` or bullet pellets).
3. **Boss (R4)**: `treant` scaled **2.5x to 3.0x** with a menacing crimson/corrupted tint (`Color(1.0f, 0.4f, 0.4f)`). Fired radial burst uses large fireballs from `Fire Effect and Bullet 16x16.png`.
4. **Grenade System (R3)**:
   - Item pickup: `Assets/Tiny RPG Forest/Artwork/sprites/misc/gem/gem-1.png` (4-frame sparkling animation, 7x7 px).
   - Thrown projectile: Compact fiery orb `Fire Effect and Bullet 16x16_36` or `101`.
   - AoE Explosion: `Assets/Fire Effect.prefab` scaled to 3.0x playing `FireAnimation.anim` (frames `114`–`119`).
5. **HUD Hearts (R5)**: `hearts-1.png` (full red heart) and `hearts-2.png` (empty heart container) from `Assets/Tiny RPG Forest/Artwork/sprites/misc/hearts/` provide a 1:1 solution for the 5 HP UI.
6. **Audio Gap**: Zero audio files exist in `Assets/`. Recommended solution: Lightweight procedural runtime audio synthesis (`AudioClip.Create`) generating crisp 8-bit retro sound effects (gunshot, hit, explosion, pickup, boss alert, hurt) with 0 external dependencies.

---

## 2. Directory Structure & Inventory

### 2.1 File Tree Overview
```
Assets/
├── Bullet.prefab                           # Working player bullet prefab
├── Fire Effect.prefab                      # Working bullet impact / fire animation prefab
├── Plugins/
│   └── Roslyn/                             # C# 12 compilation assemblies
├── Scenes/
│   └── shooting.unity                      # Main active gameplay scene
├── Tiny RPG Forest/
│   ├── Artwork/
│   │   ├── Environment/
│   │   │   ├── sliced-objects/             # Rocks, bushes, trees, signs, waterfall
│   │   │   ├── tileset-sliced.png          # 544x512 tileset source
│   │   │   └── tileset.png
│   │   └── sprites/
│   │       ├── hero/                       # 4-dir RPG hero (walk, idle, attack)
│   │       ├── misc/                       # arrow, coin, enemy-death, gem, hearts
│   │       ├── mole/                       # Mole monster (idle, walk)
│   │       └── treant/                     # Treant monster (idle, walk)
│   └── Scenes/
│       └── Demo.unity                      # Asset pack visual showcase scene
├── bullets/
│   ├── Fire Effect and Bullet 16x16.png    # Master 576x208 sheet (370 sliced sprites)
│   ├── Fire Effect.controller              # Animator controller for impact effect
│   ├── Fire Effect and Bullet 16x16_114.controller
│   └── FireAnimation.anim                  # 6-frame fiery impact animation
├── scripts/
│   ├── Bullet.cs                           # Destroys on collision, spawns hitEffect
│   ├── PlayerMovement.cs                   # WASD movement + mouse-aim rotation
│   └── Shooting.cs                         # Instantiates bulletPrefab from firePoint
├── soldier-img/
│   └── soldier-no-bg.png                   # 156x186 top-down soldier sprite
└── tilemap/
    ├── environment.prefab                  # Tilemap grid prefab
    ├── floor.prefab                        # Tilemap floor prefab
    └── tileset-sliced_*.asset              # 160 sliced Scriptable Tile assets
```

### 2.2 Root Folder Anomalies
- `Animation/` is located in the project root directory (outside `Assets/`). It contains:
  - `Player.controller`, `Player_Idle.anim` (empty), `Player_walk_down.anim`, `Player_walk_up.anim`, `Player_walk_left.anim`, `Player_walk_right.anim`.
  - These animations were created for the Tiny RPG `hero` sprite, but the active scene `shooting.unity` uses `soldier-no-bg.png` with mouse-aim rotation instead.

---

## 3. Detailed Sprite & Visual Asset Catalog

### 3.1 Tiny RPG Forest Characters & Monsters

| Entity | Subfolder / Sprite Path | Resolution | PPU | Frames | Description / Visual Style |
|---|---|---|---|---|---|
| **Treant** | `sprites/treant/idle/` | 31 x 35 | 16 | 3 dirs | Tree monster, wood bark, leafy details, walking arms |
| **Treant** | `sprites/treant/walk/treant-walk-front/` | 31 x 35 | 16 | 4 frames | Walking downward toward camera |
| **Treant** | `sprites/treant/walk/treant-walk-back/` | 31 x 35 | 16 | 4 frames | Walking upward away from camera |
| **Treant** | `sprites/treant/walk/treant-walk-side/` | 31 x 35 | 16 | 4 frames | Walking horizontally |
| **Mole** | `sprites/mole/idle/` | 24 x 24 | 16 | 3 dirs | Small, brown burrowing beast with claws |
| **Mole** | `sprites/mole/walk/mole-walk-front/` | 24 x 24 | 16 | 4 frames | Quick scuttling steps downward |
| **Mole** | `sprites/mole/walk/mole-walk-back/` | 24 x 24 | 16 | 4 frames | Quick scuttling steps upward |
| **Mole** | `sprites/mole/walk/mole-walk-side/` | 24 x 24 | 16 | 4 frames | Quick scuttling steps sideways |
| **Hero** | `sprites/hero/idle/` | 32 x 32 | 16 | 3 dirs | Green tunic adventurer, sword scabbard |
| **Hero** | `sprites/hero/walk/` | 32 x 32 | 16 | 6 frames/dir | 4-directional walk cycle |
| **Hero** | `sprites/hero/attack/` | 32 x 32 | 16 | 3 frames/dir | Sword slashing motion |

### 3.2 Miscellaneous Effects & Pickups (`sprites/misc/`)

| Asset | File Name | Size (px) | PPU | Frame Count | Visual Role |
|---|---|---|---|---|---|
| **Hearts (Full)** | `hearts/hearts-1.png` | 7 x 6 | 16 | 1 | Full red heart for player HP HUD |
| **Hearts (Empty)** | `hearts/hearts-2.png` | 7 x 6 | 16 | 1 | Depleted heart container for player HP HUD |
| **Enemy Death** | `enemy-death/enemy-death-1..6.png` | 30 x 32 | 16 | 6 | Poof / smoke burst when regular enemies die |
| **Gem (Pickup)** | `gem/gem-1..4.png` | 7 x 7 | 16 | 4 | Sparkling violet gemstone for Grenade item pickup |
| **Coin** | `coin/coin-1..4.png` | 5 x 7 | 16 | 4 | Rotating golden coin (optional score pickup) |
| **Arrow** | `arrow.png` | 5 x 19 | 16 | 1 | Vertical pixel arrow (candidate Shooter bullet) |

### 3.3 Bullets & Explosions Spritesheet (`Fire Effect and Bullet 16x16.png`)
- **Dimensions**: 576 x 208 px (36 columns x 13 rows of 16x16 tiles).
- **Import Settings**: Sprite Mode = Multiple, PPU = 16, Filter Mode = Point, 370 total sliced sprites.
- **Functional Clusters**:
  1. **Cols 0–5 (Cluster A)**: 6-frame continuous animated projectiles / fire orbs.
  2. **Cols 7–8 (Cluster B)**: 2-frame compact bullets / pellets (Sprites 6-7, 36-37, 66-67, 96-97, etc.).
  3. **Cols 10–12 (Cluster C)**: 3-frame elongated bolts / arrows.
     - *Sprite 38* (X=160, Y=176) is the active player bullet in `Bullet.prefab`.
  4. **Cols 14–17 (Cluster D)**: 4-frame impact / spark bursts.
  5. **Cols 19–22 (Cluster E)**: 4-frame flares / blast dissipations.
  6. **Cols 24–28 (Cluster F)**: 5-frame explosive detonations.
  7. **Cols 30–35 (Cluster G)**: 6-frame full circular fireball explosions.
     - *Sprites 114–119* (Y=144) form the active animation `FireAnimation.anim` in `Fire Effect.prefab`.

---

## 4. Role-to-Asset Mapping Matrix

| Game Element | Chosen Asset Path / Sprite ID | Scaling / Tint Settings | Animation / Behavior | Rationale |
|---|---|---|---|---|
| **Player** | `Assets/soldier-img/soldier-no-bg.png` | Scale: (1.0, 1.0, 1.0), PPU: 100 | Rotates 360° to face mouse (`rb.rotation = angle - 90f`) | Already integrated in `shooting.unity` with accurate `Fire Point` at (0.35, 1.18); fits 360° mouse aiming far better than 4-directional RPG hero |
| **Chaser Enemy** | `Assets/Tiny RPG Forest/Artwork/sprites/treant/` | Scale: (1.1, 1.1, 1.0), Default tint | `walk-front` (4 frames) or flipX for direction | Sturdy melee wood golem that relentlessly marches toward the player |
| **Rusher Enemy** | `Assets/Tiny RPG Forest/Artwork/sprites/mole/` | Scale: (0.9, 0.9, 1.0), Amber tint `Color(1f, 0.6f, 0.4f)` | `walk-front` (4 frames, 1.5x speed) | Small, fast burrowing mole that rapidly closes distance |
| **Shooter Enemy** | `Assets/Tiny RPG Forest/Artwork/sprites/treant/` (or `mole`) | Scale: (1.0, 1.0, 1.0), Purple/Violet tint `Color(0.75f, 0.4f, 1f)` | Walk + stop at range + shoot interval | Distinct corrupted tint signals ranged caster behavior; maintains distance |
| **Boss** | `Assets/Tiny RPG Forest/Artwork/sprites/treant/` | Scale: **(2.8, 2.8, 1.0)**, Dark Crimson tint `Color(1.0f, 0.35f, 0.35f)` | Heavy slow step + radial burst windup | Huge ancient corrupted tree titan; creates high tension at 500 points |
| **Player Bullet** | `Fire Effect and Bullet 16x16_38` (`Assets/Bullet.prefab`) | Scale: (0.4, 0.4, 0.4), Fiery orange | Travels along `firePoint.up` at speed 20 | Fast, high-visibility orange fiery bolt with trailing effect |
| **Enemy Bullet** | `Fire Effect and Bullet 16x16_6` or `arrow.png` | Scale: (0.8, 0.8, 1.0), Acid green or violet tint | Straight trajectory toward player position | Visually contrasting color ensures player can read incoming fire |
| **Boss Bullet** | `Fire Effect and Bullet 16x16_0` or `16x16_90` | Scale: (1.2, 1.2, 1.0), Deep crimson/purple | 360-degree radial ring of 16-20 projectiles | Large glowing fireball orb, highly dangerous pattern |
| **Grenade Item Pickup** | `Assets/Tiny RPG Forest/Artwork/sprites/misc/gem/gem-1..4.png` | Scale: (1.5, 1.5, 1.0), Sparkling violet | Floating hover bob (`sin(time)`), collected on contact | Shimmering 4-frame animation catches player attention when enemies drop it |
| **Grenade Projectile** | `Fire Effect and Bullet 16x16_36` or `101` | Scale: (1.0, 1.0, 1.0) | Arcs / flies toward cursor, explodes on arrival/timer | Compact round bomb/orb silhouette with continuous spin |
| **Grenade Explosion** | `Assets/Fire Effect.prefab` | Scale: **(3.5, 3.5, 1.0)** | Plays `FireAnimation.anim` (frames 114–119) + triggers AoE overlap circle | Massive screen-filling fiery explosion that obliterates swarms |
| **Enemy Death Effect** | `Assets/Tiny RPG Forest/Artwork/sprites/misc/enemy-death/` | Scale: (1.2, 1.2, 1.0) | 6-frame white poof / smoke animation | Provides crisp feedback when non-grenade kills occur |
| **HUD 5 HP Hearts** | `hearts/hearts-1.png` (full) & `hearts-2.png` (empty) | UI Image size 28x24 px (native 7x6 scaled 4x) | Toggles active sprite based on current HP (0..5) | 100% pixel-perfect match for the 5 HP design requirement |

---

## 5. Scene & Environment Survey

### 5.1 Active Scene (`Assets/Scenes/shooting.unity`)
- **Camera**:
  - Orthographic size = 5.0 (visible height = 10 units).
  - Position: `(1.96, 0.04, -10.0)`.
  - Aspect: ~1.96 (16:9 / 19:10 screen displays width ~19.5 units).
- **Floor Tilemap**:
  - GameObject: `floor` (Grid) -> `floor` (Tilemap, TilemapRenderer).
  - Cell bounds: `(-12, -5, 0)` with size `(24, 10, 1)` -> total width 24 units, height 10 units.
  - World extent: X from `-9.31` to `+14.69`, Y from `-4.92` to `+5.08`.
- **Environmental Colliders**:
  - `Colliders/Arvore` (CapsuleCollider2D, size ~3.68 x 2.23)
  - `Colliders/Arbusto` (CapsuleCollider2D, size ~1.06 x 0.58)
  - `Colliders/Cerca` (BoxCollider2D, size ~3.53 x 0.45)
- **CRITICAL GAP — Boundary Colliders**:
  - **Issue**: The current scene has NO boundary walls or colliders enclosing the perimeter of the floor map.
  - **Requirement Impact**: R1 demands: *"Nhân vật di chuyển 8 hướng bằng phím WASD trong phạm vi bản đồ kín (có biên giới ngăn rơi khỏi map)..."*
  - **Solution**: Add 4 BoxCollider2D boundary walls (Top, Bottom, Left, Right) positioned just outside the floor edge (e.g. Y=+5.2, Y=-5.0, X=-9.5, X=+15.0) or add edge colliders so neither the player nor spawned enemies can escape the arena.

---

## 6. UI, HUD & Game Flow Survey

### 6.1 Packages & Capabilities
- `com.unity.ugui` (1.0.0) and `com.unity.textmeshpro` (3.0.7) are installed in `Packages/manifest.json`.
- Standard Unity UI Canvas (`CanvasScaler`, `GraphicRaycaster`, `EventSystem`) is fully supported.
- Currently, `shooting.unity` has **no Canvas and no EventSystem**.

### 6.2 Recommended HUD & Screen Layout
All UI screens can reside under a single `Canvas` (Screen Space - Overlay) with dedicated panel GameObjects toggled by a centralized `UIManager.cs`:
1. **HUD Panel (Always visible during gameplay)**:
   - Top-Left: **Health Container** (HorizontalLayoutGroup with 5 UI Images using `hearts-1.png` / `hearts-2.png`).
   - Top-Center: **Boss Health Bar** (Slider with red fill image and "ANCIENT CORRUPTED TITAN" text; deactivated until Boss spawns).
   - Top-Right: **Score Text** (`SCORE: 000`) and **High Score Text** (`BEST: 000`).
   - Bottom-Right: **Grenade Inventory** (Gem/Grenade icon + `x 0` text).
2. **Main Menu Panel**:
   - Title banner: "FOREST SURVIVOR: 2D TOP-DOWN SHOOTER".
   - Buttons: "PLAY GAME", "CONTROLS" (shows WASD: Move, Mouse: Aim, Left Click: Shoot, E/Right Click: Grenade), "QUIT".
3. **Pause Menu Panel**:
   - Semi-transparent dark background (Color: `(0, 0, 0, 0.75)`).
   - Text: "GAME PAUSED".
   - Buttons: "RESUME", "RESTART", "MAIN MENU".
4. **Game Over Panel**:
   - Text: "GAME OVER".
   - Stats: "YOUR SCORE: {0}", "ALL-TIME RECORD: {1}".
   - Buttons: "PLAY AGAIN", "MAIN MENU".
5. **Victory / Boss Defeated Banner / Panel**:
   - Text: "TITAN SLAIN! +1000 BONUS POINTS".
   - Buttons: "CONTINUE ENDLESS", "MAIN MENU".

---

## 7. Audio Survey & Procedural Synthesis Blueprint

### 7.1 Current Status
- `Assets/` contains **0 audio files** (`.wav`, `.mp3`, `.ogg`).
- `Main Camera` has an `AudioListener`.
- No `AudioSource` components are currently placed.

### 7.2 Procedural Audio Solution (`SoundManager.cs`)
To prevent missing asset errors, external file licensing issues, or audio import discrepancies, we can implement a self-contained C# `AudioClip` procedural sound generator using standard waveforms:
- **`SoundManager.cs`** creates an `AudioSource` on launch and synthesizes retro 8-bit sound clips in memory via `AudioClip.Create`:
  1. **Shoot Sound**: Short 80ms white noise + exponential frequency pitch sweep down (800 Hz -> 200 Hz).
  2. **Hit Sound**: Short 40ms triangle wave blip (600 Hz -> 150 Hz).
  3. **Enemy Death**: 120ms crunchy low-pass noise pop (220 Hz -> 60 Hz).
  4. **Explosion (Grenade)**: 450ms heavy white noise rumble with low-pass resonance and exponential decay.
  5. **Pickup (Grenade)**: 150ms dual-tone ascending chime (523 Hz [C5] -> 659 Hz [E5]).
  6. **Player Hurt**: 180ms sawtooth dissonance buzzer (150 Hz).
  7. **Boss Roar / Warning**: 600ms descending deep rumble (120 Hz -> 40 Hz).
  8. **Boss Radial Burst**: 250ms metallic resonance pulse.
- **Advantage**: 100% portable, zero disk footprint, zero external dependencies, perfectly matches pixel-art retro style.

---

## 8. Asset Gaps & Resolution Matrix

| Required Asset | Status in Project | Proposed Resolution |
|---|---|---|
| **Player Sprite** | Available (`soldier-no-bg.png`) | Keep existing sprite; already calibrated with `Shooting.cs` FirePoint |
| **Chaser Enemy** | Available (`sprites/treant/`) | Create `ChaserEnemy.prefab` with Treant walk sprites |
| **Rusher Enemy** | Available (`sprites/mole/`) | Create `RusherEnemy.prefab` with Mole walk sprites, amber tint |
| **Shooter Enemy** | Available (`sprites/treant/` or `mole`) | Create `ShooterEnemy.prefab` with purple/violet tint; shoots `arrow.png` or bullet pellets |
| **Boss Sprite** | Available (`sprites/treant/`) | Create `BossEnemy.prefab` scaling Treant to 2.8x with crimson tint |
| **Player Bullet** | Available (`Bullet.prefab`) | Uses `Fire Effect and Bullet 16x16_38` |
| **Enemy Bullet** | Available (`Fire Effect..._6` / `arrow.png`) | Create `EnemyBullet.prefab` with trigger collider |
| **Boss Radial Bullet** | Available (`Fire Effect..._0` / `90`) | Create `BossBullet.prefab` with large glowing fireball sprite |
| **Grenade Item Pickup** | Available (`gem/gem-1..4.png`) | Create `GrenadePickup.prefab` with 4-frame sparkling animation |
| **Grenade Projectile** | Available (`Fire Effect..._36` / `101`) | Create `GrenadeProjectile.prefab` with throw trajectory |
| **Grenade AoE Explosion**| Available (`Fire Effect.prefab`) | Scale `Fire Effect.prefab` to 3.5x with `OverlapCircle` damage trigger |
| **Heart HP Icons** | Available (`hearts-1.png`, `hearts-2.png`) | Use directly in HUD 5-heart display |
| **Arena Boundaries** | **MISSING** | Add 4 boundary BoxCollider2D game objects around floor tilemap perimeter |
| **UI Canvas & Screens** | **MISSING** | Create Canvas hierarchy with HUD, Main Menu, Pause, Game Over, Victory panels |
| **Audio Clips** | **MISSING** | Implement procedural 8-bit sound synthesizer in `SoundManager.cs` |
| **Damage Flash Effect** | **MISSING (Logic)** | Implement SpriteRenderer color flash (red/white) + alpha flicker coroutine in Health component |

---

## 9. Conclusion & Recommendations for Implementation

The project possesses all necessary 2D sprite artwork to fulfill requirements R1 through R6 without requiring external graphical downloads. The existing `soldier-no-bg.png` player and `Tiny RPG Forest` monster assets fit together smoothly. The few missing elements (arena boundary colliders, UI Canvas panels, and audio clips) can be fully synthesized and created using Unity's built-in 2D physics, uGUI system, and C# procedural audio generation.
