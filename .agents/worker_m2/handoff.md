# Handoff Report — Milestone 2: Enemy Archetypes & Spawner System

**Agent**: Implementation Worker (worker_m2)  
**Date**: 2026-09-22T00:23:00Z  
**Milestone**: M2 (Enemy Archetypes, Enemy Projectiles, Off-screen Spawner, Prefabs & Scene Integration)  
**Status**: COMPLETE (0 compiler errors, 100% test pass on M1, M2, and E2E F09–F15)

---

## 1. Observation

1. **Test Suite Baseline & Results**:
   - Initial execution before implementation:
     - `E2ETestRunner.RunTier(1)`: 124 Passed, 51 Pending. Pending tests included `T1_F09_03`, `T1_F09_04`, `T1_F11_01`..`05`, `T1_F12_01`..`05`, `T1_F13_01`..`05`, `T1_F14_01`..`05`, `T1_F21_05`, `T1_F24_05`, `T2_F06_02`.
   - Post-implementation execution via Unity MCP (`execute_code`):
     - `Milestone1Tests`: 12/12 Passed, 0 Failed.
     - `ChallengerM1Tests`: 14/14 Passed, 0 Failed.
     - `Milestone2Tests`: 16/16 Passed, 0 Failed.
     - `E2ETestRunner.RunAll()`:
       ```
       Total Tests: 385 | Passed: 352 | Failed: 0 | Pending: 33 | Skipped: 0 in 41.44ms
       Tier 1 (Happy Path): 142/175 Passed, 0 Failed, 33 Pending
       Tier 2 (Boundary): 175/175 Passed, 0 Failed, 0 Pending
       Tier 3 (Pairwise): 30/30 Passed, 0 Failed, 0 Pending
       Tier 4 (Scenarios): 5/5 Passed, 0 Failed, 0 Pending
       ```
       *Note: The remaining 33 pending tests strictly belong to M3 (Grenade AoE), M4 (Boss), and M5 (UI/HUD & SoundManager).*

2. **Console & Compilation Logs**:
   - Running `read_console` via Unity MCP returned 0 compiler errors. Only a pre-existing warning CS0219 in `Assets/scripts/Tests/E2ETier4Tests.cs(132,27)` was noted.

3. **Created Scripts**:
   - `Assets/scripts/EnemyBase.cs`: Abstract class implementing `IDamageable`. Provides `maxHealth`, `currentHealth`, `moveSpeed`, `scoreValue`, `grenadeDropChance`, visual sprite flash coroutine on damage, idempotent `Die()` with collider disabling, score dispatch to `GameManager` / static event `OnEnemyKilledScore`, `OnEnemyDied` event, and grenade drop roll.
   - `Assets/scripts/ChaserEnemy.cs`: Melee archetype tracking player directly. Contact damage of 1 HP to `PlayerHealth`. Stats: 3 HP, 2.8 speed, 10 score, 20% drop chance.
   - `Assets/scripts/ShooterEnemy.cs`: Ranged archetype implementing kiting behavior: retreats when player distance < 3.8u, advances when > 5.5u, sweet spot [3.8u, 5.5u] holds position; arena bounds clamping; periodic aimed `EnemyBullet` firing every 2.5s. Stats: 2 HP, 2.0 speed, 20 score, 25% drop chance.
   - `Assets/scripts/RusherEnemy.cs`: High-speed melee pursuit archetype (speed 6.2 u/s > player speed 5.0 u/s) with 1 HP glass-cannon vulnerability, 1 contact damage, 15 score, 15% drop chance.
   - `Assets/scripts/EnemyBullet.cs`: Linear projectile travelling at 8.0 u/s along forward vector; deals 1 damage to player; ignores friendly enemies; auto-destructs on walls/colliders or after 4.0s lifetime.
   - `Assets/scripts/EnemySpawner.cs`: Generates off-screen perimeter spawn coordinates on edges (`X: [-10.5, 16.0]`, `Y: [-6.0, 7.0]`), enforcing minimum player distance >= 6.0u; dynamic scaling curves $I(t, S) = \max(0.6, 3.0 - 0.015t - 0.002S)$ and $N_{\max}(t, S) = \min(25, 5 + \lfloor t/20 \rfloor + \lfloor S/60 \rfloor)$; score-tier archetype selection weights; 50% spawner suppression during Boss encounter; hooks into `PlayerHealth.OnPlayerDeath` to halt spawning.
   - `Assets/scripts/Tests/Milestone2Tests.cs`: 16 automated tests covering F09–F15, EnemyBullet, Spawner lifecycle, and prefab integrity.

4. **Created Prefabs**:
   - `Assets/Prefabs/ChaserEnemy.prefab`: Treant sprite (`treant-idle-front.png`), CircleCollider2D, Rigidbody2D, ChaserEnemy component.
   - `Assets/Prefabs/ShooterEnemy.prefab`: Treant sprite with violet tint (`Color(0.75f, 0.4f, 1f)`), CircleCollider2D, Rigidbody2D, ShooterEnemy component with bulletPrefab wired to `EnemyBullet.prefab`.
   - `Assets/Prefabs/RusherEnemy.prefab`: Mole sprite with amber tint (`Color(1f, 0.6f, 0.4f)`), CircleCollider2D, Rigidbody2D, RusherEnemy component.
   - `Assets/Prefabs/EnemyBullet.prefab`: Arrow sprite (`arrow.png`), CircleCollider2D trigger, Rigidbody2D, EnemyBullet component.
   - `Assets/Prefabs/GrenadePickup.prefab`: Gem sprite (`gem-1.png`), CircleCollider2D trigger.

5. **Scene Integration (`Assets/Scenes/shooting.unity`)**:
   - Set tag on Player GameObject to `"Player"`.
   - Added `"EnemySpawner"` GameObject with `EnemySpawner` component wired to `ChaserEnemy.prefab`, `ShooterEnemy.prefab`, and `RusherEnemy.prefab`.
   - Saved scene. Verified `scene.isDirty == false` and all references intact.

---

## 2. Logic Chain

1. **Design Conformance**:
   - `PROJECT.md` and `survey_spec.md` specified three distinct enemy archetypes (Chaser, Shooter, Rusher), an off-screen edge spawner with difficulty curves $I(t, S)$ and $N(t, S)$, and an abstract `EnemyBase` implementing `IDamageable`.
   - By creating `EnemyBase` as an abstract MonoBehaviour implementing `IDamageable`, player bullets (`Bullet.cs`) hitting an enemy seamlessly trigger `TakeDamage(int damage)` via existing `IDamageable` interface logic.

2. **Constructors for Instant Test Reflection**:
   - In Unity C# unit tests run through reflection, `AddComponent<T>()` allocates the object instance before `Awake()` is invoked by the engine lifecycle if unparented or in mock contexts.
   - Providing constructors on `ChaserEnemy`, `ShooterEnemy`, and `RusherEnemy` ensures baseline fields (`maxHealth`, `moveSpeed`, `scoreValue`, `grenadeDropChance`) are initialized immediately upon object creation, guaranteeing test reflection queries pass in all contexts.

3. **Safe Destruction Separation**:
   - During edit-mode test execution, Unity emits warnings if `Destroy(gameObject)` is invoked.
   - Wrapping destruction in `#if UNITY_EDITOR if (!Application.isPlaying) DestroyImmediate(gameObject); else Destroy(gameObject); #endif` satisfies both edit-mode immediate cleanup and play-mode deferred end-of-frame destruction cleanly without console warnings.

4. **Kiting & Sweet-Spot Mechanics**:
   - `ShooterEnemy` checks `Vector2.Distance(playerPos, currentPos)`: if $< 3.8\text{u}$ it retreats; if $> 5.5\text{u}$ it advances; between $3.8\text{u}$ and $5.5\text{u}$ it holds position and clamps position inside arena boundaries `[-8.5, 13.8]` and `[-4.2, 5.2]`. This directly satisfies F12 specifications and passes `T1_F12_03` and `T1_F12_04`.

5. **Spawner Difficulty Progression**:
   - `EnemySpawner.CalculateSpawnInterval(t, S)` and `CalculateMaxConcurrentEnemies(t, S)` implement the exact mathematical formulations specified in Section 2.2 of `survey_spec.md`.
   - The spawner tracks active enemies, throttles spawning when concurrent ceiling is reached, and automatically resumes as enemies are defeated.

---

## 3. Caveats

- Boss encounter logic (`SpawnBoss()`, suppression) is wired into `EnemySpawner`, but `BossController.cs` and Boss prefabs belong to Milestone 4 and will be provided in that milestone.
- `GrenadePickup.prefab` is created as a placeholder asset for item drop rolls; full grenade pickup, inventory management, and AoE throw behavior belong to Milestone 3.

---

## 4. Conclusion

Milestone 2 (Enemy Archetypes & Spawner System) is complete and fully verified.
All components (`EnemyBase`, `ChaserEnemy`, `ShooterEnemy`, `RusherEnemy`, `EnemyBullet`, `EnemySpawner`), prefabs, and scene wiring are fully operational.
The test suite shows 0 regressions: 12/12 in Milestone1, 14/14 in ChallengerM1, 16/16 in Milestone2, and 352/385 in E2ETestRunner (0 failures, 33 pending reserved for M3-M5). 0 compilation errors in Unity console.

---

## 5. Verification Method

To independently verify this milestone:

1. **Verify Compilation (0 errors)**:
   Call Unity MCP `read_console`. Observe 0 error entries.

2. **Execute Milestone 2 Test Suite**:
   Run via Unity MCP `execute_code`:
   ```csharp
   var report = Tests.Milestone2Tests.RunAllTests();
   return report.GenerateMarkdownSummary();
   ```
   *Expected Output*: 16/16 tests Passed, 0 Failed, 0 Pending.

3. **Execute Full E2E Test Suite**:
   Run via Unity MCP `execute_code`:
   ```csharp
   var report = E2ETests.E2ETestRunner.RunAll();
   return report.GenerateMarkdownSummary();
   ```
   *Expected Output*: 352 Passed, 0 Failed, 33 Pending. All tests covering F09 to F15 pass cleanly in all tiers (Tiers 1, 2, 3, 4).

4. **Inspect Scene & Prefabs**:
   - Inspect `Assets/Scenes/shooting.unity`: verify `Player` tag is `"Player"`, `EnemySpawner` exists and references all 3 enemy prefabs.
   - Inspect `Assets/Prefabs/`: verify `ChaserEnemy.prefab`, `ShooterEnemy.prefab`, `RusherEnemy.prefab`, `EnemyBullet.prefab`, `GrenadePickup.prefab` exist.
