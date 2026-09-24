# Reviewer 2 Handoff Report: Milestone 3 — Grenade Mechanic (AoE Pickup & Throw)

## Review Summary
- **Verdict**: **APPROVE**
- **Adversarial Risk Assessment**: **LOW**
- **Integrity Audit**: **PASSED** (0 violations, genuine logic, no facades, no hardcoded results)

---

## 1. Observation
- **Codebase and Assets Inspected**:
  - `Assets/scripts/GrenadePickup.cs` (166 lines): Configured with trigger `CircleCollider2D`, zero-gravity `Rigidbody2D`, arena perimeter clamping `[-8.5, 13.8] x [-4.2, 5.2]`, capacity guard rejecting collection when inventory >= 5, and edit-mode safe destruction (`DestroyImmediate` in editor, `Destroy` in play mode).
  - `Assets/scripts/GrenadeThrower.cs` (196 lines): Handles `KeyCode.E`, `Fire2`, and right mouse button (`GetMouseButtonDown(1)`). Deduplicates simultaneous input in single boolean evaluation, enforces 0.3s throw cooldown, clamps throw distance to `maxThrowDistance = 7.0f`, clamps destination within arena bounds, enforces pause (`Time.timeScale <= 0`) and dead player (`!_playerHealth.IsAlive`) guards, maintains inventory (default 2, max 5), and provides runtime fallback instantiation if prefab unassigned.
  - `Assets/scripts/GrenadeProjectile.cs` (166 lines): Parabolic flight trajectory via `Vector2.Lerp` with `Mathf.Sin(t * Mathf.PI)` height scaling, flight duration 0.7s, total fuse time 1.2s, impact trigger on enemies and wall colliders while ignoring friendly player.
  - `Assets/scripts/ExplosionAoE.cs` (121 lines): Calls `Physics2D.SyncTransforms()`, performs `Physics2D.OverlapCircleAll(blastCenter, 3.5f)`, deduplicates targets via `HashSet<IDamageable>`, inflicts 50 damage to hostile `IDamageable` entities, guarantees friendly fire immunity for player, and spawns scaled VFX.
  - `Assets/Prefabs/GrenadePickup.prefab`: Configured with `GrenadePickup`, `CircleCollider2D` (isTrigger=True, radius=0.35), `Rigidbody2D` (gravityScale=0, Kinematic).
  - `Assets/Prefabs/GrenadeProjectile.prefab`: Configured with `GrenadeProjectile`, referencing `ExplosionAoE.prefab`, `CircleCollider2D` (isTrigger=True, radius=0.25), `Rigidbody2D` (gravityScale=0, Kinematic), `SpriteRenderer` (sprite=gem-1).
  - `Assets/Prefabs/ExplosionAoE.prefab`: Configured with `ExplosionAoE` (radius=3.5, damage=50, lifetime=0.6, visualEffectPrefab: Fire Effect).
  - `Assets/Scenes/shooting.unity`: Active scene contains Player with `GrenadeThrower` referencing `GrenadeProjectile.prefab`. Enemy prefabs (`ChaserEnemy`, `ShooterEnemy`, `RusherEnemy`) reference `GrenadePickup.prefab` with 20%, 25%, 15% drop chances.
- **Verification Results**:
  - `Milestone3Tests.RunAllTests()`: 20/20 Passed (0 Failed).
  - `E2ETestRunner.RunAll()`: 362/385 Passed, 0 Failed, 23 Pending (strictly reserved for M4 Boss & M5 UI/SFX).
  - Features F16-F20: 50/50 Passed (0 Failed).
  - Regression Suites: `Milestone1Tests` (12/12), `Milestone2Tests` (16/16), `ChallengerM1Tests` (14/14), `ChallengerM2Tests` (17/17) all Passed.
  - Compiler Errors via `read_console`: 0 compiler errors.

---

## 2. Logic Chain
1. **Input Deduplication & Robustness**:
   - In `GrenadeThrower.Update()`, input is sampled as:
     `bool inputThrow = Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2") || Input.GetMouseButtonDown(1);`
   - If both E and RMB are pressed in the exact same frame, the expression evaluates to `true` once, invoking `ThrowGrenade()` exactly once, decrementing inventory by 1, and setting `_cooldownTimer = 0.3f`.
   - Rapid subsequent throws within 0.3 seconds are rejected by the cooldown timer.
2. **State & Boundary Protection**:
   - Throws are strictly blocked when paused (`Time.timeScale <= 0`), when player is dead (`!_playerHealth.IsAlive`), or when inventory is zero (`grenadeCount <= 0`).
   - Throw coordinates are clamped to maximum radius 7.0 units from player origin, and further clamped inside arena boundary colliders `[-8.5, 13.8] x [-4.2, 5.2]`.
   - Inventory addition is capped at `maxGrenades` (5). When full, `GrenadePickup.TryCollect()` returns false without consuming the item.
3. **Trajectory & Detonation**:
   - Projectile moves via normalized time `t = elapsedTime / 0.7f`. Scale scales by `1.0 + sin(t*pi) * 0.5f` simulating 2.5D elevation arc.
   - Detonation triggers on fuse expiration (1.2s) or upon collision with an enemy or wall collider, instantiating `ExplosionAoE`.
4. **AoE Physics Accuracy & Friendly Fire**:
   - `ExplosionAoE.Explode()` calls `Physics2D.SyncTransforms()` before querying `OverlapCircleAll(blastCenter, 3.5f)`.
   - Player entities (identified via tag "Player" or component `PlayerHealth`) are bypassed, ensuring 0 friendly fire damage.
   - Enemy entities are tracked in a `HashSet<IDamageable>` so multi-collider enemy hierarchies are only damaged once per blast.
   - 50 damage is lethal to all standard enemies (Chaser: 3 HP, Shooter: 2 HP, Rusher: 1 HP) and delivers 50/60 damage to Boss.

---

## 3. Findings

### [Minor] Finding 1: Dead Player Pickup Collection
- **What**: If an enemy drops a grenade directly on top of the player's corpse after the player has died, `GrenadePickup.TryCollect` increments the dead player's grenade count.
- **Where**: `Assets/scripts/GrenadePickup.cs`, lines 88-100.
- **Why**: `TryCollect` verifies `collector.GetComponent<PlayerHealth>() != null`, but does not verify `health.IsAlive`.
- **Severity**: Minor. When player dies, controls are disabled and `GrenadeThrower.ThrowGrenade` blocks throws via `!_playerHealth.IsAlive`, so this has no gameplay consequence.
- **Suggestion**: Add `if (collector.GetComponent<PlayerHealth>() is PlayerHealth ph && !ph.IsAlive) return false;`.

---

## 4. Adversarial Stress Test Results

| Test ID | Scenario | Expected Behavior | Actual Behavior | Result |
|---------|----------|-------------------|-----------------|--------|
| ADV-01 | Rapid-fire spam (50 calls/frame) | Clamped at 0, no negative inventory | Inventory = 0, no exceptions | PASS |
| ADV-02 | Extreme coordinates `(100000, 100000)` | Clamped within arena bounds | Clamped safely, no crash | PASS |
| ADV-03a | Null `grenadePrefab` on Thrower | Runtime fallback projectile | Fallback spawned cleanly | PASS |
| ADV-03b | Null `explosionPrefab` on Projectile | Runtime fallback AoE | Fallback spawned cleanly | PASS |
| ADV-03c | Null `visualEffectPrefab` on ExplosionAoE | Graceful execution without VFX | Blast damages enemies, no NRE | PASS |
| ADV-04 | Destroyed enemy in blast radius | Handle null/destroyed collider | Handled without MissingReferenceException | PASS |
| ADV-05 | Overfill collection (5 pickups at 4 count, max 5) | Collect 1, reject 4 | Collected 1, final count 5 | PASS |
| ADV-06 | Multi-collider enemy in blast radius | Exactly 1 damage hit (50 HP) | Hit once via HashSet deduplication | PASS |
| ADV-07 | Player at explosion epicenter `(0, 0)` | 0 damage, 5 HP remaining | Health remained 5 HP | PASS |
| ADV-08 | Simultaneous E + RMB in 1 frame | Single throw, 1 grenade consumed | Evaluates as single input, 1 consumed | PASS |

---

## 5. Caveats
- No caveats. The implementation contains genuine logic, passes all unit, integration, and E2E regression suites, and integrates cleanly with scene hierarchy and prefabs.

---

## 6. Conclusion
Milestone 3 meets all architectural contracts, functional requirements (F16, F17, F18, F19, F20), and acceptance criteria. Integrity checks confirm no dummy implementations or fabricated tests.

**Verdict**: **APPROVE**

---

## 7. Verification Method
Run the following verification script in Unity via Unity MCP `execute_code`:

```csharp
var m3Report = Tests.Milestone3Tests.RunAllTests();
var e2eReport = E2ETests.E2ETestRunner.RunAll();
var f16_20 = System.Linq.Enumerable.Where(e2eReport.Results, r => r.FeatureId == "F16" || r.FeatureId == "F17" || r.FeatureId == "F18" || r.FeatureId == "F19" || r.FeatureId == "F20");
int fPass = System.Linq.Enumerable.Count(f16_20, r => r.Status == E2ETests.TestStatus.Passed);
int fTotal = System.Linq.Enumerable.Count(f16_20);

return $"M3 Tests: {m3Report.PassedCount}/{m3Report.TotalCount}\n" +
       $"F16-F20 E2E: {fPass}/{fTotal}\n" +
       $"Total E2E: {e2eReport.PassedCount}/{e2eReport.TotalCount} (Pending: {e2eReport.PendingCount}, Failed: {e2eReport.FailedCount})";
```
Expected output:
`M3 Tests: 20/20`
`F16-F20 E2E: 50/50`
`Total E2E: 362/385 (Pending: 23, Failed: 0)`
