# Forensic Audit Report: Milestone 3 (Grenade Mechanic: AoE Pickup & Throw)

**Work Product**: Milestone 3 Implementation (`Assets/scripts/GrenadePickup.cs`, `Assets/scripts/GrenadeThrower.cs`, `Assets/scripts/GrenadeProjectile.cs`, `Assets/scripts/ExplosionAoE.cs`, `Assets/Prefabs/GrenadePickup.prefab`, `Assets/Prefabs/GrenadeProjectile.prefab`, `Assets/Prefabs/ExplosionAoE.prefab`, `Assets/Scenes/shooting.unity`)  
**Profile**: General Project (Integrity Mode: Development)  
**Verdict**: **CLEAN**

---

## 1. Observation

Direct, empirical observations recorded from independent forensic checks, source code analysis, and Unity execution:

1. **Integrity Mode & Ground Truth Specifications**:
   - `ORIGINAL_REQUEST.md` (Line 8): Explicitly sets `Integrity mode: development`.
   - `ORIGINAL_REQUEST.md` (Lines 26-30): Mandates quái rơi lựu đạn (Grenade item), nhặt cộng vào dự trữ, ném bằng phím E / Chuột phải về hướng chuột, và nổ AoE gây sát thương diện rộng.

2. **Source Code Implementation Integrity**:
   - `Assets/scripts/GrenadePickup.cs`:
     - Line 33: `col.isTrigger = true;`
     - Line 39: `rb.gravityScale = 0f;`
     - Lines 46-49: Clamps dropped position within arena bounds `[-8.5, 13.8] x [-4.2, 5.2]`.
     - Lines 83-139: `TryCollect(GameObject collector)` verifies caller is Player (`collector.CompareTag("Player") || collector.GetComponent<PlayerHealth>() != null || collector.GetComponent<GrenadeThrower>() != null`), checks capacity guard (`thrower.grenadeCount >= thrower.maxGrenades`), increments inventory, and destroys pickup. Non-players (enemies/bullets) return `false` without consuming the item.
     - **No hardcoded test outputs or dummy return constants found.**
   - `Assets/scripts/GrenadeThrower.cs`:
     - Lines 13-16: `grenadeCount = 2; maxGrenades = 5;`
     - Lines 68-75: Blocks throwing if game is paused (`Time.timeScale <= 0f`) or player is dead (`!_playerHealth.IsAlive`).
     - Lines 82-84: Handles input via `Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2") || Input.GetMouseButtonDown(1)`.
     - Lines 114-126: Decrements `grenadeCount`, invokes `OnGrenadeCountChanged`, clamps direction to `maxThrowDistance = 7.0f`, and bounds target within arena rectangle.
     - Lines 130-137: Real instantiation of `grenadePrefab` and initialization of trajectory coordinates via `proj.Initialize(playerPos, finalTarget)`.
     - **No facade or dummy logic found.**
   - `Assets/scripts/GrenadeProjectile.cs`:
     - Lines 12-18: `flightDuration = 0.7f; fuseTime = 1.2f; maxArcHeight = 0.5f;`
     - Lines 68-80: Lerps position from start to target over 0.7s; scales sprite parabolically via `sin(t * PI)` reaching peak scale at $t=0.5$ and returning to base scale at $t=1.0$.
     - Lines 84-88: Forced detonation upon fuse expiration (`_elapsedTime >= fuseTime`).
     - Lines 106-130: Friendly player collision ignored (`target.CompareTag("Player")` returns immediately); hostile enemy or wall collision causes immediate detonation.
     - Lines 135-151: Instantiates `explosionPrefab` and cleanly destroys projectile.
     - **Genuine trajectory, collision, and fuse detonation physics verified.**
   - `Assets/scripts/ExplosionAoE.cs`:
     - Lines 14-20: `explosionRadius = 3.5f; damage = 50; lifetime = 0.6f; visualScale = 3.5f;`
     - Line 45: Calls `Physics2D.SyncTransforms()`.
     - Line 50: Genuinely queries 2D physics world: `Physics2D.OverlapCircleAll(blastCenter, explosionRadius)`.
     - Lines 60-66: Friendly fire immunity for Player (`col.CompareTag("Player") || col.GetComponent<PlayerHealth>() != null`).
     - Lines 68-77: Extracts `IDamageable`, verifies `!(target is PlayerHealth)`, de-duplicates multi-collider entities with `HashSet<IDamageable>`, and calls `target.TakeDamage(damage)`.
     - Lines 96-112: Instantiates and scales visual effect prefab (`visualScale = 3.5f`).
     - Lines 85-88: Schedules destruction after `lifetime`.
     - **Authentic AoE query and damage dispatch confirmed.**

3. **Prefab Assets Validation**:
   - `Assets/Prefabs/GrenadePickup.prefab`:
     - Valid Unity YAML prefab.
     - Contains `SpriteRenderer` (`gem-1` sprite), `CircleCollider2D` (`isTrigger: 1`, `radius: 0.35`), `Rigidbody2D` (`gravityScale: 0`, `bodyType: 1` Kinematic), and `GrenadePickup` MonoBehaviour (`guid: c54020a98307bba4d88c9f81430e6b78`).
   - `Assets/Prefabs/GrenadeProjectile.prefab`:
     - Valid Unity YAML prefab.
     - Contains `SpriteRenderer` (`gem-1` sprite), `CircleCollider2D` (`isTrigger: 1`, `radius: 0.25`), `Rigidbody2D` (`gravityScale: 0`, `bodyType: 1` Kinematic), and `GrenadeProjectile` MonoBehaviour (`guid: c7975d31ca038ed4391282aa9ec2d88e`).
     - References `ExplosionAoE.prefab` (`guid: 5524545c2bb47c242bd85c0cf5dad145`).
   - `Assets/Prefabs/ExplosionAoE.prefab`:
     - Valid Unity YAML prefab.
     - Contains `ExplosionAoE` MonoBehaviour (`guid: ff166f04bc8f0f247a4e82b00de41a25`).
     - References `Fire Effect.prefab` (`guid: 0e8f370e3fc89334c8824f00d14ac1fc`).
   - `Assets/Scenes/shooting.unity`:
     - Player GameObject (ID `1073545479`) is configured with `GrenadeThrower` MonoBehaviour (Component ID `1073545487`, `guid: 021996ff9093ba34fa298017f0f754fd`).
     - Correctly references `GrenadeProjectile.prefab` (`guid: 70eba4092b198d54eaa4ebe4b5fa5e55`).

4. **Empirical Forensic Execution Results (via Unity MCP `execute_code`)**:
   - *Test A (GrenadePickup collection & capacity)*:
     - Output: `NonPlayerCollect: False (expected False), PlayerCollect: True (expected True), CountAfter: 2 (expected 2), MaxCollect: False (expected False), CountAfterMax: 5 (expected 5)` -> **PASS**
   - *Test B (ExplosionAoE OverlapCircle & Damage Delivery)*:
     - Output: `T1(2u): Init=3->Final=0 (expected 0) | T2(3u): Init=3->Final=0 (expected 0) | T3(5u): Init=3->Final=3 (expected 3) | Player(1u): Init=5->Final=5 (expected 5) | T5(multi-col): Final=0 (expected 0)` -> **PASS**
   - *Test C (Trajectory Motion & Parabolic Arc)*:
     - Output: `RemainingGrenades: 1 (expected 1) | SpawnPos: 0.0 | MidPos: 6.83 | MidScale: 1.04 (>1.0 apex) | EndPos: 7.00 | EndScale: 1.00 | ExplosionSpawned: True` -> **PASS**
   - *Test D (Enemy Impact vs Player Collision)*:
     - Output: `DetonatedOnEnemy: True (expected True)` -> **PASS**
     - Output: `DetonatedOnPlayer: False (expected False)` -> **PASS**
   - *Test E (Boss Damage Reduction)*:
     - Output: `MockBoss Initial: 60 | Remaining: 10 (expected 10)` -> **PASS**

5. **Test Suite Execution Results**:
   - `Tests.Milestone3Tests.RunAllTests()`: **20/20 Passed (0 Failed)**
   - `Tests.ChallengerM3Tests.RunAllTests()`: **23/23 Passed (0 Failed)**
   - `E2ETests.E2ETestRunner.RunAll()` (Features F16-F20): **50/50 Passed (0 Failed)**
   - Regression Suites (`Milestone1Tests`: 12/12, `Milestone2Tests`: 16/16, `ChallengerM1Tests`: 14/14, `ChallengerM2Tests`: 17/17): **100% Passed (0 Failed)**
   - Console Error Verification (`read_console`): **0 compilation errors, 0 runtime exceptions**

6. **Pre-populated Artifact Check**:
   - No pre-populated test result files, logs, or attestation shortcuts exist in the repository.

---

## 2. Logic Chain

1. **Rule Compliance**: Under Development Mode (`ORIGINAL_REQUEST.md`), prohibited patterns include hardcoded test results, facade implementations, and fabricated artifacts.
2. **Empirical Inspection**:
   - Code inspection of all four core scripts demonstrated authentic physics calculation, event broadcasting, and state mutation.
   - Independent C# scripts directly executed inside the Unity Editor verified that `Physics2D.OverlapCircleAll` really queries the 2D physics scene, accurately excludes friendly players, accurately filters duplicate colliders, and accurately inflicts 50 damage on hostile `IDamageable` entities.
   - Independent C# scripts confirmed that `GrenadeThrower` instantiates real `GrenadeProjectile` prefabs that travel over time, follow parabolic scaling, detonate on impact or fuse timeout, and spawn real `ExplosionAoE` prefabs.
   - Scene inspection proved that `Assets/Scenes/shooting.unity` has the Player GameObject properly wired with `GrenadeThrower` referencing `GrenadeProjectile.prefab`.
   - All 93 milestone and regression tests pass natively in Unity without mocks or bypasses.
3. **Conclusion Derivation**: Since all Phase 1 source code checks and Phase 2 behavioral checks passed empirically with zero failures, zero facades, and zero hardcoded shortcuts, the work product is authentic and uncompromised.

---

## 3. Caveats

- Audio feedback in `GrenadePickup.cs` uses optional reflection hooks targeting `SoundManager` because audio assets and the SoundManager singleton are designated for Milestone 5. This is an intended architectural design and does not impair grenade gameplay mechanics.
- No other caveats. All Milestone 3 deliverables are fully implemented, functional, and verified.

---

## 4. Conclusion

**Verdict: CLEAN**

Milestone 3 (Grenade Mechanic: AoE Pickup & Throw) is fully authentic, rigorous, and complies completely with the ground-truth requirements of `ORIGINAL_REQUEST.md` and `PROJECT.md`. There are no integrity violations, no facades, no hardcoded results, and no fabricated artifacts.

---

## 5. Verification Method

To independently reproduce the forensic findings:

1. **Verify 0 Compiler Errors**:
   Call Unity MCP `read_console` with `{ action: "get", types: ["error"] }`. Expected: 0 entries.

2. **Execute Milestone 3 Suite**:
   Execute via Unity MCP `execute_code`:
   ```csharp
   var report = Tests.Milestone3Tests.RunAllTests();
   return $"Passed: {report.PassedCount}/{report.TotalCount}, Failed: {report.FailedCount}";
   // Expected: Passed: 20/20, Failed: 0
   ```

3. **Execute Challenger Empirical Suite**:
   Execute via Unity MCP `execute_code`:
   ```csharp
   var report = Tests.ChallengerM3Tests.RunAllTests();
   return $"Passed: {report.PassedCount}/{report.TotalCount}, Failed: {report.FailedCount}";
   // Expected: Passed: 23/23, Failed: 0
   ```

4. **Execute E2E F16-F20 Suite**:
   Execute via Unity MCP `execute_code`:
   ```csharp
   var report = E2ETests.E2ETestRunner.RunAll();
   var f = report.Results.Where(r => r.FeatureId == "F16" || r.FeatureId == "F17" || r.FeatureId == "F18" || r.FeatureId == "F19" || r.FeatureId == "F20");
   return $"Passed: {f.Count(r => r.Status == E2ETests.TestStatus.Passed)}/{f.Count()}, Failed: {f.Count(r => r.Status == E2ETests.TestStatus.Failed)}";
   // Expected: Passed: 50/50, Failed: 0
   ```

5. **Verify Scene Player Wiring**:
   Inspect `Assets/Scenes/shooting.unity` at GameObject `1073545479` (Player), confirming MonoBehaviour `1073545487` is present with `grenadePrefab` pointing to `GrenadeProjectile.prefab`.
