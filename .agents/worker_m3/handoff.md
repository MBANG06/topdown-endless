# Handoff Report: Milestone 3 — Grenade Mechanic (AoE Pickup & Throw)

## 1. Observation
- **Pre-implementation Baseline**:
  - `E2ETestRunner.RunAll()` initially reported 385 tests: 352 Passed, 0 Failed, 33 Pending.
  - The pending tests included 10 Milestone 3 tests: `T1_F16_04`, `T1_F16_05`, `T1_F17_02`, `T1_F17_03`, `T1_F17_04`, `T1_F18_04`, `T1_F18_05`, `T1_F20_01`, `T1_F20_02`, `T1_F33_03`.
  - Enemy archetypes (`ChaserEnemy.cs`, `ShooterEnemy.cs`, `RusherEnemy.cs`) and prefabs already possessed drop chances (20%, 25%, 15%) and references to `GrenadePickup.prefab`, but the script `GrenadePickup.cs` and related throw/projectile/aoe scripts were not yet implemented.
  - The `Player` GameObject in `Assets/Scenes/shooting.unity` lacked the `GrenadeThrower` component.

- **Post-implementation Verification Results**:
  - `Milestone3Tests.RunAllTests()`: 20/20 Passed, 0 Failed.
  - `E2ETestRunner.RunAll()`: Total 385 tests, 362 Passed, 0 Failed, 23 Pending (23 pending tests are strictly reserved for Milestone 4 Boss and Milestone 5 UI/Audio).
  - Features F16-F20 (Grenade drop, collection, throw input, projectile trajectory & fuse, AoE explosion radius & damage): 50/50 tests Passed (0 Failed).
  - Regression Suites:
    - `Milestone1Tests`: 12/12 Passed (0 Failed)
    - `Milestone2Tests`: 16/16 Passed (0 Failed)
    - `ChallengerM1Tests`: 14/14 Passed (0 Failed)
    - `ChallengerM2Tests`: 17/17 Passed (0 Failed)
  - Compiler Check via `read_console`: 0 compiler errors, 0 runtime exceptions.

- **Created and Modified Artifacts**:
  1. `Assets/scripts/GrenadePickup.cs`: Item pickup component with trigger collider, zero gravity, boundary clamping `[-8.5, 13.8] x [-4.2, 5.2]`, player collection, and max capacity rejection.
  2. `Assets/scripts/GrenadeThrower.cs`: Player component handling KeyCode.E and RMB (Fire2), clamping throw distance to max 7.0 units, managing inventory (starting 2, max 5), raising `OnGrenadeCountChanged`, and enforcing pause and dead player guards.
  3. `Assets/scripts/GrenadeProjectile.cs`: Parabolic trajectory motion with height curve `sin(t*pi)`, flight duration 0.7s, fuse 1.2s, impact detonation on enemies/colliders, friendly player collision ignored, spawning `ExplosionAoE`.
  4. `Assets/scripts/ExplosionAoE.cs`: Radial blast query in 3.5u radius using `Physics2D.OverlapCircleAll`, delivering 50 damage to hostile `IDamageable` entities, friendly fire immunity for Player, spawning scaled `Fire Effect` VFX (3.5x), auto-destroying after lifetime.
  5. `Assets/Prefabs/GrenadePickup.prefab`: Configured with `GrenadePickup.cs`, `CircleCollider2D` (isTrigger=true, radius=0.35), `Rigidbody2D` (gravityScale=0, isKinematic=true).
  6. `Assets/Prefabs/GrenadeProjectile.prefab`: Configured with `GrenadeProjectile.cs`, `gem-1` sprite, `CircleCollider2D` (isTrigger=true, radius=0.25), `Rigidbody2D` (gravityScale=0, isKinematic=true), referencing `ExplosionAoE.prefab`.
  7. `Assets/Prefabs/ExplosionAoE.prefab`: Configured with `ExplosionAoE.cs`, `Fire Effect.prefab` VFX reference, 3.5 radius, 50 damage.
  8. `Assets/Scenes/shooting.unity`: Player GameObject wired with `GrenadeThrower` referencing `GrenadeProjectile.prefab`.
  9. `Assets/scripts/Tests/Milestone3Tests.cs`: 20 comprehensive unit and integration tests covering M3 mechanics.

## 2. Logic Chain
1. **Drop & Collection (F16, F17)**:
   - `EnemyBase.RollGrenadeDrop()` rolls against archetype drop chances (Chaser: 0.20, Shooter: 0.25, Rusher: 0.15) on death and instantiates `GrenadePickup.prefab`.
   - `GrenadePickup` verifies that only objects tagged "Player" or containing `PlayerHealth`/`GrenadeThrower` can collect it.
   - If player inventory is already at `maxGrenades` (5), `TryCollect()` returns false without consuming the item, satisfying boundary requirement T2_F17_02.
   - Upon successful collection, `thrower.AddGrenades(1)` increments count, dispatches `OnGrenadeCountChanged`, and the pickup cleanly destroys itself.
2. **Throw Input & Clamping (F18, F19)**:
   - In `GrenadeThrower.Update()`, throw input is detected via `Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2") || Input.GetMouseButtonDown(1)` which triggers only once per frame even if both inputs occur simultaneously (T2_F18_04).
   - If paused (`Time.timeScale <= 0`) or player is dead (`_playerHealth.IsAlive == false`) or inventory is 0, throw is blocked without error.
   - Cursor world position is calculated from `Camera.main.ScreenToWorldPoint(Input.mousePosition)`.
   - Vector `targetPos - playerPos` is clamped using `Vector2.ClampMagnitude(..., 7.0f)` and bounded within arena bounds `[-8.5, 13.8]` and `[-4.2, 5.2]`.
   - Decrements `grenadeCount`, fires event, and instantiates `GrenadeProjectile.prefab`.
3. **Flight Trajectory & Detonation (F19, F20, F33)**:
   - `GrenadeProjectile` lerps from launch position to target position over `flightDuration = 0.7s`.
   - Scale multiplier oscillates via `1.0 + sin(t*pi) * 0.5f`, reaching max elevation at apex ($t=0.5$) and returning to base scale on landing.
   - Fuse timeout is 1.2s.
   - If the projectile impacts an enemy or wall collider before fuse expiration, it detonates immediately.
   - Detonation instantiates `ExplosionAoE`.
   - `ExplosionAoE.Explode()` calls `Physics2D.SyncTransforms()` to ensure fresh transform positions in the 2D physics world, then queries `Physics2D.OverlapCircleAll(center, 3.5f)`.
   - Each unique hostile entity implementing `IDamageable` receives 50 damage, eliminating Chaser (3 HP), Shooter (2 HP), Rusher (1 HP), and significantly damaging Boss (60 HP -> 10 HP).
   - Player is explicitly filtered out, guaranteeing friendly fire immunity (T2_F20_04).
   - Spawns `Fire Effect` scaled up to 3.5x and auto-destroys cleanly.

## 3. Caveats
- No external sound assets are present in the repository; pickup feedback includes optional reflection hooks for `SoundManager.PlayPickupSFX()` once Milestone 5 implements audio.
- The 23 pending tests in `E2ETestRunner` belong to Milestone 4 (`BossController`) and Milestone 5 (`GameManager`, `UIManager`, `SoundManager`) which are scheduled for subsequent milestones.
- No other caveats; all Milestone 3 requirements are fully implemented with real state and genuine logic.

## 4. Conclusion
Milestone 3 (Grenade Mechanic: AoE Pickup & Throw) is complete, robust, and verified:
- All 20 `Milestone3Tests` pass.
- All 50 E2E tests for features F16-F20 pass cleanly with 0 failures.
- All pre-existing regression suites (Milestone 1, Milestone 2, Challenger M1, Challenger M2) pass 100%.
- Zero compiler errors, zero runtime exceptions.
- Code changes strictly comply with the Integrity Mandate and File Ownership boundaries.

## 5. Verification Method
Independent verification can be executed via Unity MCP `execute_code`:

1. **Run Milestone 3 Test Suite**:
   ```csharp
   var report = Tests.Milestone3Tests.RunAllTests();
   return $"Passed: {report.PassedCount}/{report.TotalCount}, Failed: {report.FailedCount}";
   // Expected: Passed: 20/20, Failed: 0
   ```

2. **Run Comprehensive E2E Suite**:
   ```csharp
   var report = E2ETests.E2ETestRunner.RunAll();
   var f16_20 = report.Results.Where(r => r.FeatureId == "F16" || r.FeatureId == "F17" || r.FeatureId == "F18" || r.FeatureId == "F19" || r.FeatureId == "F20");
   return $"F16-F20: {f16_20.Count(r => r.Status == E2ETests.TestStatus.Passed)}/{f16_20.Count()} Passed, Failed: {report.FailedCount}";
   // Expected: F16-F20: 50/50 Passed, Failed: 0
   ```

3. **Check Console for Errors**:
   Call `read_console` with `{ action: "get", count: "10" }`.
   // Expected: 0 errors.

4. **Verify Scene Player Wiring**:
   Open scene `Assets/Scenes/shooting.unity`.
   Inspect Player GameObject: has `GrenadeThrower` component with `grenadePrefab` referencing `GrenadeProjectile.prefab`.
