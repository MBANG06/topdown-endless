# Handoff Report: Milestone 3 — Grenade Mechanic (AoE Pickup & Throw)

**Agent**: Challenger 2 (Empirical Challenger M3)  
**Milestone**: Milestone 3 (Grenade Mechanic: AoE Pickup & Throw)  
**Date**: 2026-09-22T03:33:00+07:00  
**Verdict**: **APPROVE**  

---

## 1. Observation

1. **Empirical Adversarial Test Suite (`Tests.ChallengerM3Tests.RunAllTests()`)**:
   - Total Tests: **26 | Passed: 26 | Failed: 0 | Pending: 0**
   - Tier 1: Feature Coverage (Happy Path): 5/5 Passed
   - Tier 2: Boundary & Corner Cases: 17/17 Passed
   - Tier 3: Pairwise Combinations: 2/2 Passed
   - Tier 4: Real-World Scenarios & Integration: 2/2 Passed

2. **Regression and Baseline Suites via Unity MCP (`execute_code`)**:
   - `Tests.Milestone3Tests.RunAllTests()`: 20/20 Passed (0 Failed)
   - `Tests.Milestone1Tests.RunAllTests()`: 12/12 Passed (0 Failed)
   - `Tests.Milestone2Tests.RunAllTests()`: 16/16 Passed (0 Failed)
   - `Tests.ChallengerM1Tests.RunAllTests()`: 14/14 Passed (0 Failed)
   - `Tests.ChallengerM2Tests.RunAllTests()`: 17/17 Passed (0 Failed)
   - `E2ETests.E2ETestRunner.RunAll()`:
     - Total: 385 tests | **362 Passed | 0 Failed | 23 Pending**
     - Milestone 3 features (F16, F17, F18, F19, F20): **50/50 Passed (0 Failed)**
     - *(Note: All 23 pending tests strictly belong to M4 Boss and M5 UI/Audio).*

3. **Live Unity PlayMode Execution (`manage_editor` action: play)**:
   - Thrown grenade from player position: inventory decremented 2 -> 1, instantiated `GrenadeProjectile(Clone)`.
   - Trajectory flight and elevation scale completed in 0.7s; rested on ground until 1.2s fuse detonation.
   - Detonation instantiated `ExplosionAoE(Clone)` and `Fire Effect(Clone)`.
   - After 1.8s, all projectile, explosion, and VFX GameObjects were automatically destroyed by Unity runtime (`proj: False, exp: False, vfx: False`).
   - Player walked over pickup item: inventory count incremented 1 -> 2; pickup was cleanly destroyed.
   - Player at capacity 5 walked over pickup: pickup collection rejected (`false`); pickup GameObject remained intact in scene.
   - Explosion detonated directly at Player coordinates: Player HP remained strictly 5 (0 friendly fire damage).

4. **Unity Console & Compilation (`read_console`)**:
   - 0 compiler errors.
   - 0 runtime exceptions.
   - 0 warnings during test execution.

5. **Hierarchy Clutter & Memory Leak Verification**:
   - Base scene object count in `shooting.unity`: exactly 18 objects.
   - Object count after 20 consecutive projectile spawns, detonations, and cleanup in `CH-M3-26`: exactly 18 objects (net leak: 0).
   - Object count after exiting PlayMode: exactly 18 objects (net leak: 0).

---

## 2. Logic Chain

1. **Player Friendly Fire Immunity (`Assets/scripts/ExplosionAoE.cs:60-74`)**:
   - *Observation*: `ExplosionAoE.Explode()` queries `Physics2D.OverlapCircleAll(blastCenter, explosionRadius)`. When iterating colliders, it applies 4 independent safeguards:
     1. `if (col.CompareTag("Player")) continue;`
     2. `if (col.GetComponent<PlayerHealth>() != null) continue;`
     3. `if (col.GetComponentInParent<PlayerHealth>() != null) continue;`
     4. `if (target is PlayerHealth) continue;`
   - *Empirical Proof*:
     - Epicenter detonation ($d=0$, `CH-M3-01`): Player HP remained 5.
     - Varying radii ($d \in [0.5, 3.49]$, `CH-M3-02`): Player HP remained 5.
     - Compound hierarchies (untagged child hurtbox collider, `CH-M3-03`): caught by `GetComponentInParent<PlayerHealth>()`, HP remained 5.
     - 20 overlapping explosions (`CH-M3-04`): HP remained 5.
     - Lethal discrimination (`CH-M3-05`): Co-located Chaser, Shooter, and Rusher enemies all received 50 damage (HP dropped to 0) while Player took 0 damage.
     - Flying projectile impact (`CH-M3-06`): `GrenadeProjectile.HandleImpact` ignores objects with `PlayerHealth`, preventing mid-air detonation on player.

2. **Max Capacity Rejection (`Assets/scripts/GrenadePickup.cs:113-121`, `GrenadeThrower.cs:152-164`)**:
   - *Observation*: `GrenadePickup.TryCollect()` evaluates `if (thrower.grenadeCount >= thrower.maxGrenades) return false;` before setting `_isCollected = true` or destroying the GameObject.
   - *Empirical Proof*:
     - In `CH-M3-07`, player at 5 grenades walked over pickup: `TryCollect` returned `false`, `grenadeCount` remained 5, and the pickup GameObject was NOT destroyed.
     - In `CH-M3-08`, 3 pickups on the ground were walked over at 5 grenades: all 3 rejected, 0 consumed, all 3 remained alive.
     - In `CH-M3-09`, player threw 1 grenade (5 -> 4), then walked over the previously rejected pickup: `TryCollect` succeeded, count restored to 5, and pickup was cleanly destroyed.
     - In `CH-M3-10`, `AddGrenades(10)` when count was 3 clamped strictly to `maxGrenades` (5).
     - In `CH-M3-11`, enemies, bullets, and walls touching the pickup were rejected.

3. **Parabolic Trajectory & Fuse Detonation (`Assets/scripts/GrenadeProjectile.cs:59-88`, `104-130`)**:
   - *Observation*: In `GrenadeProjectile.Update()`, flight progress $t = \text{Clamp01}(\text{elapsed} / 0.7\text{s})$ drives `Vector2.Lerp(start, target, t)` and scale oscillation `1.0f + Mathf.Sin(t * Mathf.PI) * 0.5f`. Detonation occurs when `elapsed >= 1.2s` or upon impact with an enemy or wall.
   - *Empirical Proof*:
     - In `CH-M3-13`, scale was sampled: launch = 1.00, apex ($t=0.5$) = 1.50, arrival ($t=1.0$) = 1.00.
     - In `CH-M3-14`, target destination was reached in exactly 0.7s.
     - In `CH-M3-15`, projectile was confirmed not detonated at $t=1.15$s, and detonated at $t \ge 1.20$s.
     - In `CH-M3-16` and `CH-M3-17`, projectile colliding with an Enemy or Wall detonated immediately before fuse expiration.
     - In `CH-M3-18` and `CH-M3-19`, throws beyond 7.0u or outside arena boundaries were clamped to 7.0u and inside $[-8.5, 13.8] \times [-4.2, 5.2]$.
     - In `CH-M3-20`, throwing directly at player position $(0, 0)$ was handled safely without NaN or zero-division.

4. **Zero Memory Leaks & Clutter Elimination (`ExplosionAoE.cs:85-113`, `GrenadeProjectile.cs:152-164`)**:
   - *Observation*: `GrenadeProjectile.Detonate()` destroys itself (`DestroyImmediate` in edit-mode, `Destroy` in play-mode). `ExplosionAoE` and instantiated `Fire Effect` VFX call `Destroy(..., lifetime = 0.6f)` in play-mode.
   - *Empirical Proof*:
     - In live PlayMode, after 1.8s (1.2s fuse + 0.6s lifetime), all projectile, explosion, and VFX clones were confirmed destroyed (`proj: False, exp: False, vfx: False`).
     - In `CH-M3-26`, 20 consecutive projectile spawns and detonations resulted in 0 lingering projectile objects and an exact match between initial and final scene counts (net leak: 0).
     - The scene hierarchy retains exactly its baseline 18 level objects.

---

## 3. Caveats

- The 23 pending tests in `E2ETestRunner` strictly correspond to Milestone 4 (`BossController`, radial attack) and Milestone 5 (`GameManager` FSM, `UIManager` HUD hearts/menus, `SoundManager` audio) which are scheduled for future milestones.
- Audio feedback in `GrenadePickup.PlayPickupFeedback()` uses reflection to interface with `SoundManager.PlayPickupSFX()` once Milestone 5 is implemented; in the absence of audio assets, it fails silently without errors.

---

## 4. Conclusion

**VERDICT: APPROVE**

Milestone 3 (Grenade Mechanic: AoE Pickup & Throw) is fully verified, mathematically sound, memory-leak free, and compliant with all project interface contracts:
- Player friendly fire immunity is 100% verified across 4 independent safety checks.
- Max capacity rejection at 5 grenades is 100% verified (pickups survive and can be collected later).
- Parabolic arc (apex 1.5x scale) and 1.2s fuse / early impact detonation operate exactly to specification.
- Zero memory leaks confirmed across EditMode tests and live PlayMode gameplay.
- All 26 `ChallengerM3Tests`, 20 `Milestone3Tests`, and 50 M3 E2E tests pass with 0 failures and 0 console errors.

---

## 5. Verification Method

To independently verify these results in the Unity Editor:

1. **Run Challenger M3 Test Suite**:
   ```csharp
   var report = Tests.ChallengerM3Tests.RunAllTests();
   return $"Total: {report.TotalCount}, Passed: {report.PassedCount}, Failed: {report.FailedCount}";
   // Expected: Total: 26, Passed: 26, Failed: 0
   ```

2. **Run All Milestone Suites & E2E Runner**:
   ```csharp
   var m3 = Tests.Milestone3Tests.RunAllTests();
   var e2e = E2ETests.E2ETestRunner.RunAll();
   var f16_20 = e2e.Results.Where(r => r.FeatureId == "F16" || r.FeatureId == "F17" || r.FeatureId == "F18" || r.FeatureId == "F19" || r.FeatureId == "F20");
   return $"M3: {m3.PassedCount}/{m3.TotalCount}, F16-F20: {f16_20.Count(r => r.Status == E2ETests.TestStatus.Passed)}/{f16_20.Count()} Passed";
   // Expected: M3: 20/20, F16-F20: 50/50 Passed
   ```

3. **Check Console for Errors**:
   Call `read_console` with `{ action: "get", count: "10", types: ["error"] }`.
   // Expected: 0 error entries.

4. **Verify Clean Scene Count**:
   ```csharp
   return GameObject.FindObjectsOfType<GameObject>().Length;
   // Expected: 18 base level objects (0 orphaned clones or fallbacks).
   ```
