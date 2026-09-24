# Handoff Report — Milestone 2 Empirical Adversarial Verification

**Agent**: Challenger 2 (Empirical Challenger M2)  
**Date**: 2026-09-22T00:27:30+07:00  
**Milestone**: M2 (Enemy Archetypes, Enemy Projectiles, Spawner Curves, Scoring & Resource Management)  
**Verdict**: **APPROVE**

---

## 1. Observation

1. **Test Suite Execution Results via Unity MCP (`execute_code`)**:
   - `Tests.ChallengerM2Tests.RunAllTests()`:
     ```
     Total Tests: 17 | Passed: 17 | Failed: 0 | Pending: 0 | Skipped: 0
     Tier 1: Feature Coverage (Happy Path): 10/10 Passed
     Tier 2: Boundary & Corner Cases: 6/6 Passed
     Tier 3: Pairwise Combinations: 1/1 Passed
     ```
   - `Tests.Milestone2Tests.RunAllTests()`: 16/16 Passed, 0 Failed.
   - `Tests.Milestone1Tests.RunAllTests()`: 12/12 Passed, 0 Failed.
   - `Tests.ChallengerM1Tests.RunAllTests()`: 14/14 Passed, 0 Failed.
   - `E2ETests.E2ETestRunner.RunAll()`:
     ```
     Total Tests: 385 | Passed: 352 | Failed: 0 | Pending: 33 | Skipped: 0
     Tier 1: 142/175 Passed, 33 Pending
     Tier 2: 175/175 Passed, 0 Failed
     Tier 3: 30/30 Passed, 0 Failed
     Tier 4: 5/5 Passed, 0 Failed
     ```
     *(Note: All 33 pending tests strictly belong to M3 Grenade, M4 Boss, and M5 UI/Audio).*

2. **Unity Console & Compilation (`read_console`)**:
   - 0 compiler errors.
   - 0 runtime exceptions.

3. **Empirical Verification of Required M2 Mechanics**:
   - **EnemyBullet Projectile (`Assets/scripts/EnemyBullet.cs`)**:
     - *Speed & Velocity*: In `CH-M2-01`, velocity initialized to `transform.up * speed` (magnitude 8.0 u/s). Verified with rotated bullet (45°), magnitude was 8.00 u/s.
     - *Damage to Player*: In `CH-M2-02`, direct hit reduced `PlayerHealth.currentHealth` from 5 to 4.
     - *Friendly Enemy Immunity*: In `CH-M2-03`, collisions with `ChaserEnemy`, `ShooterEnemy`, `RusherEnemy`, and a GameObject tagged `"Enemy"` were ignored (`_hasHit` remained false, enemies took 0 damage).
     - *Auto-destruction on walls*: In `CH-M2-04`, collisions against all 4 `MapBounds` solid walls (`Wall_Top`, `Wall_Bottom`, `Wall_Left`, `Wall_Right`) flagged `_hasHit = true` and triggered destruction.
     - *Trigger pass-through & Bullet-on-bullet*: In `CH-M2-05` and `CH-M2-06`, bullets passed through trigger pickups and ignored other `EnemyBullet` instances without destroying themselves.
   - **Spawner Off-screen Positions (`Assets/scripts/EnemySpawner.cs`)**:
     - Camera viewport at default aspect (1.956): X `[-7.82, 11.74]`, Y `[-4.96, 5.04]`.
     - Arena inner bounds: X `[-9.31, 14.69]`, Y `[-4.92, 5.08]`.
     - Spawner bounds: `minX = -10.5`, `maxX = 16.0`, `minY = -6.0`, `maxY = 7.0`.
     - In `CH-M2-07`, 1,000 Monte Carlo iterations across center, corners, and random player coordinates verified:
       - 100% of generated coordinates lie strictly on the perimeter edges.
       - 100% of generated coordinates lie strictly outside the visible camera viewport.
       - 100% of generated coordinates lie outside the playable arena colliders.
       - Distance to player maintained $\ge 6.0\text{u}$ (or fallback opposite corner when cornered).
     - In `CH-M2-08`, scaling formulas $I(t, S)$ clamped safely at floor (0.6s) and $N(t, S)$ clamped at ceiling (25 enemies). Boss suppression correctly doubled interval (halved spawn rate).
     - In `CH-M2-09`, 10,000 archetype rolls per score tier validated empirical probability distributions:
       - Tier 1 (<100): Chaser 75.0%, Rusher 15.0%, Shooter 10.0% (within $\pm 1\%$).
       - Tier 2 (100-299): Chaser 50.0%, Rusher 25.0%, Shooter 25.0% (within $\pm 1\%$).
       - Tier 3 (>=300): Chaser 35.0%, Rusher 30.0%, Shooter 35.0% (within $\pm 1\%$).
   - **Enemy Death & Scoring (`Assets/scripts/EnemyBase.cs`)**:
     - In `CH-M2-10`, kills dispatched exact score values via `EnemyBase.OnEnemyKilledScore`: Chaser = 10, Shooter = 20, Rusher = 15.
     - In `CH-M2-11`, overkill protection and multiple `Die()` calls confirmed idempotent: score dispatched exactly once, death event dispatched exactly once.
     - In `CH-M2-12`, HP depleted linearly (Chaser: 3 -> 2 -> 1 -> 0; Shooter: 2 -> 1 -> 0; Rusher: 1 -> 0) and colliders were disabled immediately upon death.
   - **Zero Memory Leaks & Cleanup**:
     - In `CH-M2-15`, high-turnover stress test spawned 30 enemies; upon calling `Die()` on all enemies, `ActiveEnemyCount` dropped cleanly to 0.
     - In live execution test with 100 spawned and eliminated enemies:
       - Baseline scene objects before: 31.
       - Objects during test: 131.
       - Objects after test: 31.
       - Net object leak: 0.
     - In `CH-M2-16`, disabling `EnemySpawner` unsubscribed event listeners from `PlayerHealth.OnPlayerDeath` and `EnemyBase.OnEnemyDied`, preventing dangling closures or `MissingReferenceException`.

---

## 2. Logic Chain

1. **Design & Contract Alignment**:
   - Requirements §R2, acceptance criteria, and `PROJECT.md` require three differentiated enemy archetypes (Chaser, Shooter, Rusher), an off-screen edge spawner with progressive difficulty scaling curves, enemy projectiles damaging players while ignoring friendly units, and exact scoring upon death.
   - Observations 1.1–1.3 confirm that all contract methods and properties are implemented and pass unit, pairwise, and boundary tests without regressions.

2. **Perimeter Math & Visibility Safeguards**:
   - `EnemySpawner.GeneratePerimeterPosition` selects from edges at $X = -10.5, 16.0$ and $Y = -6.0, 7.0$.
   - Observation 1.3 shows the visible camera viewport is bounded by $X \in [-7.82, 11.74]$ and $Y \in [-4.96, 5.04]$.
   - Because $-10.5 < -7.82$, $16.0 > 11.74$, $-6.0 < -4.96$, and $7.0 > 5.04$, every generated edge coordinate is guaranteed by geometry to be outside the visible viewport. 1,000 empirical test iterations confirmed 0 viewport breaches.

3. **Collision Safety & Friendly Immunity**:
   - `EnemyBullet.HandleHit` explicitly checks for `EnemyBase`, parent `EnemyBase`, `EnemyBullet`, and tag `"Enemy"`. When any of these are detected, it executes an early return without setting `_hasHit = true` or modifying health.
   - When encountering `MapBounds` walls, it sets `_hasHit = true` and triggers destruction, preventing bullets from leaving the arena perimeter or lingering indefinitely.

4. **Resource Management & Memory Safety**:
   - Destroyed enemies disable colliders, stop running coroutines, restore colors, and invoke destruction via `DestroyImmediate` in edit-mode or `Destroy` in play-mode.
   - The spawner prunes dead/null references via `CleanDeadEnemies()`.
   - Empirically measured before-and-after scene object counts across 100 entity lifecycles confirmed 0 leaked objects in the hierarchy.

---

## 3. Caveats

- Boss combat implementation (`BossController.cs`, 360° radial projectile attack) and Boss prefab instantiation are reserved for Milestone 4; only the Spawner's 500-point trigger hook and 50% suppression formula were verified in M2.
- Grenade inventory and AoE throw mechanics (`GrenadeThrower.cs`, `GrenadeProjectile.cs`) are reserved for Milestone 3; only placeholder drop rolls in `EnemyBase` were verified in M2.

---

## 4. Conclusion

**Verdict: APPROVE**

Milestone 2 implementation is robust, adheres strictly to specifications, and successfully passed all empirical adversarial challenges with 0 regressions and 0 memory leaks. All 17 empirical tests in `ChallengerM2Tests`, 16 tests in `Milestone2Tests`, 12 tests in `Milestone1Tests`, 14 tests in `ChallengerM1Tests`, and 352 passing tests in `E2ETestRunner` execute cleanly.

---

## 5. Verification Method

To independently reproduce and verify these findings in Unity Editor:

1. **Verify Console Errors (0 errors)**:
   Call Unity MCP `read_console` with types `["error"]`. Observe 0 error entries.

2. **Execute Challenger M2 Empirical Test Suite**:
   Run via Unity MCP `execute_code`:
   ```csharp
   var report = Tests.ChallengerM2Tests.RunAllTests();
   return report.GenerateMarkdownSummary();
   ```
   *Expected Output*: 17/17 Passed, 0 Failed, 0 Pending.

3. **Execute Full Test Matrix**:
   Run via Unity MCP `execute_code`:
   ```csharp
   var rM1 = Tests.Milestone1Tests.RunAllTests();
   var rCH1 = Tests.ChallengerM1Tests.RunAllTests();
   var rM2 = Tests.Milestone2Tests.RunAllTests();
   var rCH2 = Tests.ChallengerM2Tests.RunAllTests();
   var rE2E = E2ETests.E2ETestRunner.RunAll();
   return $"M1: {rM1.PassedCount}/{rM1.TotalCount}, CH1: {rCH1.PassedCount}/{rCH1.TotalCount}, M2: {rM2.PassedCount}/{rM2.TotalCount}, CH2: {rCH2.PassedCount}/{rCH2.TotalCount}, E2E: {rE2E.PassedCount}/{rE2E.TotalCount} (Pending: {rE2E.PendingCount})";
   ```
   *Expected Output*: `M1: 12/12, CH1: 14/14, M2: 16/16, CH2: 17/17, E2E: 352/385 (Pending: 33)`.
