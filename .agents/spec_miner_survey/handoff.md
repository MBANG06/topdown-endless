# Handoff Report: Specification Survey for 2D Top-Down Endless Shooter

**Agent Archetype**: Specification Investigator (Spec Miner)  
**Working Directory**: `c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\spec_miner_survey`  
**Target Specification Document**: `survey_spec.md`  
**Date**: 2026-09-21  

---

## 1. Observation

1. **Original User Request & Requirements (`.agents/ORIGINAL_REQUEST.md`)**:
   - Lines 12-16 (R1): Player moves 8 directions with WASD in bounded arena, rotates to cursor, fires basic bullets; has 5 HP, loses 1 HP/hit with i-frames visual flash; Game Over at 0 HP.
   - Lines 17-25 (R2): Spawner creates enemies outside camera/map edge; spawn rate and concurrent count scale with time and score; 3 archetypes: Chaser (medium melee), Shooter (kiting ranged), Rusher (fast, low-HP melee).
   - Lines 26-31 (R3): Grenade drops from dead enemies with roll; collected by walking over; thrown with E or RMB; explodes with AoE blast destroying enemies.
   - Lines 32-37 (R4): Boss spawns at 500 score; single instance; large HP pool with dedicated HUD health bar; 360-degree radial projectile barrage; reward score on death; endless continues.
   - Lines 38-46 (R5): HUD (5 HP icons/hearts, Score, High Score via `PlayerPrefs`, Grenade count, Boss HP slider); Screens (Main Menu, Pause Menu ESC/P with `Time.timeScale=0`, Game Over, Victory/Continue); modular C# architecture.
   - Lines 47-50 (R6): `Tiny RPG Forest` sprite integration, visual damage flashes, particle/explosion VFX.
   - Lines 51-65 (Acceptance Criteria): 0 compiler errors, 0 runtime exceptions.

2. **Existing Codebase State**:
   - `Assets/scripts/PlayerMovement.cs`: `moveSpeed = 5f`, normalized WASD movement (`Line 37`), rotation to mouse (`Lines 39-41`), lacks arena clamping and health logic.
   - `Assets/scripts/Shooting.cs`: Spawns `bulletPrefab` at `firePoint.position`, applies impulse force `bulletForce = 20f` (`Lines 22-24`), unthrottled single-click fire.
   - `Assets/scripts/Bullet.cs`: Collides via `OnCollisionEnter2D`, instantiates `hitEffect`, destroys effect after 0.5s and bullet (`Lines 8-13`).
   - `Assets/Scenes/shooting.unity`: Active scene (build index 0); camera orthographic size 5.0 at $(1.96, 0.04, -10)$; floor tilemap bounds center $(2.69, 0.58, 0)$ size $(24, 11)$.
   - `Packages/manifest.json`: Includes `com.unity.ugui` (1.0.0) and `com.unity.textmeshpro` (3.0.7).
   - `Assets/Tiny RPG Forest`: Includes `treant`, `mole`, `hearts` (`hearts-1.png`, `hearts-2.png`), `enemy-death` (1-6), `gem`, `coin`, and `arrow.png`.
   - `unityMCP read_console`: Verified Unity Editor 2022.3.62f2 is active with 0 compilation errors.

---

## 2. Logic Chain

1. **Baseline Assessment**: The prototype provides basic player movement and projectile instantiation. However, it completely lacks damage handling, health, i-frames, spawner systems, enemy AI archetypes, grenade mechanics, boss encounters, UI screens, HUD, and data persistence.
2. **Boundary Formulation**: The floor tilemap spans $X \in [-9.31, 14.69]$ and $Y \in [-4.92, 6.08]$. By setting physical clamping boundaries at $X \in [-8.5, 13.8]$ and $Y \in [-4.2, 5.2]$, player movement is reliably constrained inside the arena.
3. **Mathematical Spawning Curves**: To achieve smooth endless progression without causing CPU lag:
   - Spawn interval is formulated as $I(t, S) = \max(0.6\text{s}, 3.0\text{s} - 0.015t - 0.002S)$.
   - Concurrency cap is formulated as $N(t, S) = \min(25, 5 + \lfloor t/20 \rfloor + \lfloor S/60 \rfloor)$.
4. **Boss Idempotency & Radial Pattern**:
   - Boss trigger must use a strict latch (`bossEncounterTriggered == true`) so scores jumping past 500 (e.g. from 490 to 540) fire the spawn event exactly once.
   - Radial barrage is mathematically defined as 16 projectiles spaced by $\Delta\theta = 22.5^\circ$ traveling at $5.0\text{ u/s}$.
5. **Grenade AoE & Clamping**:
   - Max throw distance clamped to $7.0\text{ units}$.
   - Detonation uses `Physics2D.OverlapCircleAll` with radius $3.5\text{ units}$ and $50\text{ damage}$, guaranteeing one-shot destruction of regular enemies.
6. **UI & State Machine**:
   - 5-heart display cleanly maps to `hearts-1.png` (full) and `hearts-2.png` (empty) from `Tiny RPG Forest`.
   - Single-scene canvas architecture with panels (`HUDPanel`, `MainMenuPanel`, `PausePanel`, `GameOverPanel`, `VictoryPanel`) managed by `UIManager` and `GameManager` ensures zero scene-loading stutter and straightforward state resets.

---

## 3. Caveats

1. **Audio Clips**: The local `Tiny RPG Forest` package provides spritesheets and artwork, but no built-in audio clips. Audio hooks are specified in the architecture, while visual feedback (damage flash, screen shake, particles) serves as the guaranteed feedback layer.
2. **Input System**: The project uses Unity's Legacy Input Manager (`Input.GetAxisRaw`, `Input.GetButtonDown`, `Input.GetKeyDown`). The specification adheres to this to avoid requiring Input System package migration.

---

## 4. Conclusion

The specification survey is complete, exhaustive, and formalized in `survey_spec.md`. It covers all requirements R1 through R6, 34 discovered features, 15 edge cases, exact mathematical balance curves, 2D physics layer collision matrices, class interface contracts, and acceptance verification criteria. The document serves as an authoritative implementation blueprint.

---

## 5. Verification Method

To independently verify this specification:
1. **Inspect Specification Document**:
   - View `c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\spec_miner_survey\survey_spec.md`.
   - Verify presence of: "Executive Summary", "Features Discovered" table, "Detailed Module Specifications" (Modules 1 to 7), "Architectural Blueprint & Class Contracts", "Edge Cases" table, and "Acceptance Criteria Verification Matrix".
2. **Cross-Check against Original Requirements**:
   - Compare with `c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\ORIGINAL_REQUEST.md` to ensure every requirement (R1-R6) and acceptance criterion is accounted for with concrete parameters.
3. **Verify Existing Project Consistency**:
   - Check `Assets/scripts/PlayerMovement.cs`, `Assets/scripts/Shooting.cs`, and `Assets/Tiny RPG Forest` paths to confirm exact alignment with existing asset and component names.
