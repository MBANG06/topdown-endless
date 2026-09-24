# Review & Adversarial Critic Report: Milestone 3 — Grenade Mechanic (AoE Pickup & Throw)

## 1. Observation

### Test Execution & Compiler Log Checks
- **Console Check (`unityMCP.read_console`)**:
  - `read_console` returned 0 compiler errors.
  - Clean test execution runs with 0 error logs and 0 unhandled runtime exceptions.
- **Automated Milestone 3 Test Suite (`Tests.Milestone3Tests.RunAllTests()`)**:
  - Executed via `unityMCP.execute_code`:
    ```
    Passed: 20/20, Failed: 0
    ```
- **Automated E2E Test Suite (`E2ETests.E2ETestRunner.RunAll()`)**:
  - Executed via `unityMCP.execute_code`:
    ```
    Total: 385, Passed: 362, Failed: 0, Pending: 23
    F16-F20 Passed: 50/50
    ```
  - The remaining 23 pending tests are reserved for subsequent milestones (M4 Boss Controller: 11 tests; M5 UI & Audio: 12 tests).
- **Regression Test Suites**:
  - `Milestone1Tests`: 12/12 Passed (0 Failed)
  - `Milestone2Tests`: 16/16 Passed (0 Failed)
  - `ChallengerM1Tests`: 14/14 Passed (0 Failed)
  - `ChallengerM2Tests`: 17/17 Passed (0 Failed)

### Code & Asset Inspection
1. **`Assets/scripts/GrenadePickup.cs`**:
   - Lines 28-41: Ensures `CircleCollider2D.isTrigger = true` and `Rigidbody2D.gravityScale = 0f`.
   - Lines 43-52: In `Start()`, clamps world position within arena bounds `[-8.5, 13.8] x [-4.2, 5.2]`.
   - Lines 83-139: In `TryCollect(GameObject collector)`:
     - Verifies collector identity (`collector.CompareTag("Player")` or component checks for `PlayerHealth` / `GrenadeThrower`).
     - Checks capacity limit: `if (thrower.grenadeCount >= thrower.maxGrenades) return false;`. If at cap (5), the pickup is **not** consumed and remains in the arena.
     - Adds charges via `thrower.AddGrenades(grenadeAmount)`.
     - Self-destroys cleanly via `Destroy` (or `DestroyImmediate` in edit mode).
2. **`Assets/scripts/GrenadeThrower.cs`**:
   - Lines 13-16: `grenadeCount = 2`, `maxGrenades = 5`.
   - Lines 36-37: Dispatches `OnGrenadeCountChanged` and `OnCountChanged`.
   - Lines 67-95: In `Update()`:
     - Guards against paused state (`Time.timeScale <= 0f`) and dead player (`!_playerHealth.IsAlive`).
     - Checks `throwCooldown` (0.3s) and inputs: `Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2") || Input.GetMouseButtonDown(1)`. Multiple inputs in the same frame evaluate to a single invocation.
   - Lines 102-146: In `ThrowGrenade(Vector2 targetPos)`:
     - Checks inventory > 0, decrements `grenadeCount`, invokes count changed events.
     - Clamps vector from player to target using `Vector2.ClampMagnitude(throwDirection, maxThrowDistance)` (7.0u).
     - Clamps target position within arena boundary coordinates.
     - Instantiates `grenadePrefab` and calls `proj.Initialize(playerPos, finalTarget)`.
   - Lines 152-164: In `AddGrenades(int count)`:
     - Clamps maximum to `maxGrenades` (5).
3. **`Assets/scripts/GrenadeProjectile.cs`**:
   - Lines 10-18: `flightDuration = 0.7f`, `fuseTime = 1.2f`, `maxArcHeight = 0.5f`.
   - Lines 59-88: In `Update()`:
     - Interpolates position: `Vector2.Lerp(_startPosition, _targetPosition, t)`.
     - Visual height arc oscillation: `_baseScale * (1.0f + Mathf.Sin(t * Mathf.PI) * maxArcHeight)`.
     - Detonates when `_elapsedTime >= fuseTime`.
   - Lines 106-130: In `HandleImpact(GameObject target)`:
     - Friendly fire immunity check: ignores `Player`.
     - Detonates on hostile enemies (`Enemy` tag, `EnemyBase`, `IDamageable`) or walls (`Colliders` tag, `Collider`/`Wall` name).
   - Lines 135-164: In `Detonate()`:
     - Spawns `explosionPrefab` (`ExplosionAoE`), destroys projectile.
4. **`Assets/scripts/ExplosionAoE.cs`**:
   - Lines 14-27: `explosionRadius = 3.5f`, `damage = 50`, `lifetime = 0.6f`, `visualScale = 3.5f`.
   - Lines 39-89: In `Explode()`:
     - Calls `Physics2D.SyncTransforms()` to ensure physics collision cache is up to date.
     - Queries `Physics2D.OverlapCircleAll(blastCenter, explosionRadius)`.
     - Filters out Player colliders (`CompareTag("Player")`, `PlayerHealth`, `!(target is PlayerHealth)`).
     - Uses `HashSet<IDamageable> damagedEntities` to deduplicate entities with multiple colliders, guaranteeing each entity receives exactly 50 damage.
     - Spawns `visualEffectPrefab` (`Fire Effect.prefab`) scaled 3.5x.
     - Schedules auto-destruction after `lifetime`.
5. **Prefab & Scene Verification**:
   - `Assets/Prefabs/GrenadePickup.prefab`: CircleCollider2D (isTrigger=True, radius=0.35), Rigidbody2D (gravityScale=0, isKinematic=True), GrenadePickup.
   - `Assets/Prefabs/GrenadeProjectile.prefab`: GrenadeProjectile, references ExplosionAoE, flightDuration=0.7s, fuseTime=1.2s.
   - `Assets/Prefabs/ExplosionAoE.prefab`: ExplosionAoE, radius=3.5u, damage=50, lifetime=0.6s, references Fire Effect VFX, visualScale=3.5.
   - `Assets/Prefabs/ChaserEnemy.prefab`: dropChance=0.20, references GrenadePickup.prefab.
   - `Assets/Prefabs/ShooterEnemy.prefab`: dropChance=0.25, references GrenadePickup.prefab.
   - `Assets/Prefabs/RusherEnemy.prefab`: dropChance=0.15, references GrenadePickup.prefab.
   - `Assets/Scenes/shooting.unity`: Player GameObject possesses `GrenadeThrower` configured with `grenadeCount = 2`, `maxGrenades = 5`, referencing `GrenadeProjectile.prefab`.

---

## 2. Logic Chain

1. **Drop Logic Verification**:
   - `EnemyBase.Die()` rolls `UnityEngine.Random.value <= grenadeDropChance` upon death.
   - Archetype values strictly match requirements: Chaser 20%, Shooter 25%, Rusher 15%.
   - Off-screen or perimeter death drops are clamped within arena borders upon `GrenadePickup.Start()`.
2. **Collection & Inventory Bounds Verification**:
   - Walk-over collection is triggered by `CircleCollider2D` trigger.
   - `TryCollect()` ensures non-players cannot collect grenades.
   - If player inventory is at `maxGrenades` (5), `TryCollect()` returns `false` without destroying the pickup, preserving it for later collection.
   - Successfully collected pickups increment count and fire `OnGrenadeCountChanged`.
3. **Throw Mechanics & Bounds Verification**:
   - `GrenadeThrower.Update()` listens for `KeyCode.E`, `Fire2`, and RMB. Simultaneous inputs trigger a single throw due to cooldown timer `throwCooldown = 0.3f`.
   - Throw distance is clamped using `Vector2.ClampMagnitude` to 7.0u.
   - Target position is clamped to arena bounds.
   - Paused game (`Time.timeScale <= 0`) and dead player (`!_playerHealth.IsAlive`) strictly block grenade throwing.
4. **Detonation & AoE Blast Verification**:
   - Projectile reaches target destination over 0.7s with sinusoidal height scale illusion, detonating at 1.2s fuse or upon direct enemy/wall contact.
   - Friendly player collision does not trigger early detonation.
   - Radial blast of 3.5u delivers 50 damage, eliminating standard enemies in one shot (Chaser 3 HP, Shooter 2 HP, Rusher 1 HP).
   - Player friendly fire immunity is guarded at multiple layers (tag check, component check, interface type check).
5. **Integrity & Anti-Cheat Audit**:
   - **No hardcoded test outputs**: All test assertions execute against real GameObject components and physics simulations.
   - **No facade implementations**: Physics queries, input handling, event triggers, cooldown timers, and mathematical clamping are fully implemented.
   - **No bypassed tasks**: Prefabs and scene files are wired in Unity and serialized to disk.

---

## 3. Caveats
- No audio assets currently exist in the repo; `GrenadePickup` uses optional reflection hooks for `SoundManager.PlayPickupSFX()` which will be fulfilled when Milestone 5 implements audio.
- The 23 pending tests in `E2ETestRunner` belong to Milestone 4 (`BossController`) and Milestone 5 (`GameManager`, `UIManager`, `SoundManager`) and do not impact Milestone 3 functionality.
- No other caveats.

---

## 4. Adversarial Stress-Test Findings

Eight adversarial stress tests were constructed and executed against the implementation via Unity MCP:

| # | Stress Scenario | Expected Behavior | Actual Behavior | Result |
|---|----------------|-------------------|-----------------|--------|
| 1 | Player at epicenter with 3 nested colliders | Player HP remains 5/5 (friendly fire immunity) | Player HP remained 5/5 | **PASS** |
| 2 | Hostile target with 3 separate colliders (boss-like) | Exactly 1 damage instance (50 dmg, not 150) | Target HP reduced by exactly 50 (120 -> 70) | **PASS** |
| 3 | Inventory overflow (3 pickups at 5 grenades) + pickup retry | Pickups rejected, remain unconsumed; retry succeeds after throw | Rejected at 5, persisted, 1 collected after throw to 4 | **PASS** |
| 4 | Throwing to extreme coordinates `(100f, 100f)` | Target clamped to max arena bounds `(13.8, 5.2)` | Target clamped to `(13.80, 5.20)` | **PASS** |
| 5 | Zero inventory rapid throw spam | Inventory stays 0, no negative count, no rogue events | Inventory remained 0, 0 events emitted | **PASS** |
| 6 | Dead player throw attempt | Throw blocked, count unchanged | Throw blocked, count unchanged | **PASS** |
| 7 | Detonation in empty space | Safe execution, no NullReferenceException | Executed cleanly | **PASS** |
| 8 | Detonation on already dead enemy | Safe execution, no double kill or exception | Handled gracefully | **PASS** |

---

## 5. Conclusion & Verdict

Milestone 3 (Grenade Mechanic: AoE Pickup & Throw) satisfies all requirements defined in `ORIGINAL_REQUEST.md` (§R3) and `PROJECT.md` (F16-F20, F33). The code is robust, cleanly structured, resilient against adversarial edge cases, and completely free of compiler errors or integrity violations.

**Verdict**: **APPROVE**

---

## 6. Verification Method

To independently verify this evaluation, execute the following commands in the Unity Editor:

1. **Milestone 3 Test Suite**:
   ```csharp
   var report = Tests.Milestone3Tests.RunAllTests();
   Debug.Log($"M3 Tests: {report.PassedCount}/{report.TotalCount} Passed, {report.FailedCount} Failed");
   ```
2. **E2E Test Suite (F16-F20)**:
   ```csharp
   var report = E2ETests.E2ETestRunner.RunAll();
   var m3 = report.Results.FindAll(r => r.FeatureId == "F16" || r.FeatureId == "F17" || r.FeatureId == "F18" || r.FeatureId == "F19" || r.FeatureId == "F20");
   Debug.Log($"F16-F20 Tests: {m3.FindAll(r => r.Status == E2ETests.TestStatus.Passed).Count}/{m3.Count} Passed, {report.FailedCount} Failed");
   ```
3. **Console Error Verification**:
   Call `read_console` with `{ "action": "get", "types": ["error"] }`. Expected: 0 errors.
