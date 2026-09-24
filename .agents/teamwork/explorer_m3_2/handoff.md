# Handoff Report: Milestone 3 (R3 Boss Attack & Rewards Investigation)

**Author**: `explorer_m3_2` (teamwork_preview_explorer)  
**Date**: 2026-09-22T20:42:00Z  
**Target Component**: `Assets/scripts/BossController.cs` (and interactions with `EnemyBullet.cs`, `GrenadePickup.cs`, `GameManager.cs`, `MapManager.cs`, `ScrollingCameraController.cs`)  
**Status**: Investigation Complete — Ready for Implementation

---

## 1. Observation

### 1.1 Direct Code Inspection of `Assets/scripts/BossController.cs`
- **Inheritance & Interfaces**: Line 14: `public class BossController : EnemyBase` (which implements `IDamageable`).
- **Combat Field Configuration**:
  - Line 17: `public float burstInterval = 3.5f;`
  - Line 18: `public float telegraphDuration = 0.5f;`
  - Line 19: `public int radialBulletCount = 16;`
  - Line 20: `public float projectileSpeed = 5.0f;`
  - Line 21: `public float projectileLifetime = 5.0f;`
  - Line 22: `public int bulletDamage = 1;`
  - Line 23: `public int contactDamage = 1;`
  - Line 24: `public int guaranteedGrenadeDrops = 2;`
  - Line 25: `public GameObject bossBulletPrefab;`
  - Line 26: `public Color telegraphColor = new Color(1f, 0.9f, 0.2f, 1f);`
  - Line 29: `public bool isAttacking = false;`
- **Events**:
  - Lines 32-35 (Static): `OnBossSpawned`, `OnBossHealthChanged`, `OnBossDefeatedEvent`, `OnBossKilled`.
  - Lines 38-39 (Instance): `OnHealthChanged`, `OnDefeated`.
- **Initialization & Baseline Stats**:
  - Lines 43-50 (Constructor) & Lines 52-67 (`Awake`): `maxHealth = 60`, `currentHealth = 60`, `moveSpeed = 1.8f`, `scoreValue = 500`, `grenadeDropChance = 1.0f`.
- **Start Lifecycle**:
  - Lines 69-82: Broadcasts initial spawn and full health (`60/60`). In PlayMode (`Application.isPlaying`), launches `_attackRoutine = StartCoroutine(RadialBarrageLoop());`. EditMode is protected from unplayable coroutine invocation.
- **Movement & Player Tracking**:
  - Lines 84-135 (`FixedUpdate`): If `isAttacking == true`, explicitly sets `rb.velocity = Vector2.zero` and halts tracking. Otherwise tracks player position smoothly at `moveSpeed * Time.fixedDeltaTime` via `rb.MovePosition()`.
- **Radial Barrage Attack Execution**:
  - Lines 140-150 (`RadialBarrageLoop`): Waits `burstInterval` (3.5s) then executes `ExecuteRadialBarrage()`.
  - Lines 155-179 (`ExecuteRadialBarrage`): Sets `isAttacking = true`, freezes velocity, changes sprite color to `telegraphColor`, waits `telegraphDuration` (0.5s), restores `originalColor`, fires `FireRadialBurst()`, and resets `isAttacking = false`.
  - Lines 184-197 (`FireRadialBurst`): Calculates `angleStep = 360f / radialBulletCount` (22.5°), loops 16 times emitting radial unit vectors `dir = (Cos(θ), Sin(θ))`, and calls `SpawnBossProjectile()`.
  - Lines 202-250 (`SpawnBossProjectile`): Rotates projectile by `Quaternion.Euler(0, 0, angleDeg - 90f)` aligning `transform.up` with `dir`. Configures `EnemyBullet` speed (5.0 u/s), damage (1 HP), lifetime (5.0s), and `Rigidbody2D.velocity = dir * 5.0f`. Has in-code procedural fallback if `bossBulletPrefab` is unassigned.
- **Defeat & Guaranteed Drops**:
  - Lines 262-294 (`Die`): Halts all coroutines, resets `isAttacking = false`, broadcasts 0 HP, calls `EnemySpawner.OnBossDefeated()`, dispatches victory via reflection to `GameManager.Instance.TriggerVictory()`, broadcasts defeat events, and invokes `base.Die()`.
  - Lines 299-305 (`RollGrenadeDrop`): Overrides standard RNG roll and unconditionally instantiates exactly 2 grenade pickups at offsets `(-0.6f, 0f)` and `(+0.6f, 0f)` relative to boss position.
  - `AwardScore()` (inherited from `EnemyBase.cs` lines 238-265): Dispatches `OnEnemyKilledScore(500)` and invokes `GameManager.Instance.AddScore(500)`.

### 1.2 Inspection of Related Components & Prefabs
- **`Assets/Prefabs/BossEnemy.prefab`**:
  - Verified via reflection in Unity Editor: `bossBulletPrefab` is assigned to `EnemyBullet.prefab`, `grenadePickupPrefab` is assigned to `GrenadePickup.prefab`, `hitEffect` is `Fire Effect`, `deathEffect` is `Fire Effect`. Local scale is `(2.8, 2.8, 1.0)`, color is crimson `(1.0, 0.35, 0.35, 1.0)`.
- **`Assets/scripts/EnemyBullet.cs`**:
  - Lines 55-62: Bullet ignores friendly enemies (`EnemyBase`, `EnemyBullet`, and `BossController`).
  - Lines 65-71: Bullet passes through trigger colliders that are not the Player (e.g. `GrenadePickup`).
  - Lines 75-82: Bullet damages Player for 1 HP, interacting correctly with Player i-frames (`PlayerHealth.isInvulnerable`).
- **`Assets/scripts/GrenadePickup.cs`**:
  - Lines 49-62: Checks `useDynamicBounds && scrollCam != null`. If scrolling camera is present, only clamps X within `[-8.5, 13.8]`, leaving Y unrestricted to support drops anywhere along the infinite upward map.
- **`Assets/scripts/GameManager.cs`**:
  - Lines 136-137: Subscribes to `EnemyBase.OnEnemyKilledScore` and `BossController.OnBossDefeatedEvent`.
  - Lines 204-247: Handles score addition (+500) with reflection deduplication (`_suppressNextReflection = true`).
  - Lines 373-394: `TriggerVictory()` freezes `Time.timeScale = 0f` and opens Victory panel.
  - Lines 400-415: `ResumeEndlessAfterBoss()` unpauses `Time.timeScale = 1.0f`.
- **`Assets/scripts/ScrollingCameraController.cs`**:
  - Lines 251-265: `LockAt(worldY)` locks camera scrolling at the Boss Arena center.
  - Lines 270-275: `UnlockAndResume()` unlocks camera scrolling to resume endless mode.
- **`Assets/scripts/MapManager.cs`**:
  - Lines 343-346: `QueueBossArena()` queues boss arena segment.
  - Lines 348-377: `SpawnBossArenaInternal()` instantiates `bossArenaPrefab` and locks camera at `_bossArenaCenterY`.
  - Lines 382-391: `ResumeStandardSpawning()` clears boss arena flag and calls `cameraController.UnlockAndResume()`.

### 1.3 Test Suite Baseline Status
Executed live inside Unity Editor via `unityMCP`:
- `Tests.Milestone4Tests.RunAllTests()`: **20/20 Passed (0 Failed)**
- `Tests.Challenger1M4Tests.RunAllTests()`: **25/25 Passed (0 Failed)**
- `Tests.Challenger2M4Tests.RunAllTests()`: **35/35 Passed (0 Failed)**
- `E2ETests.E2ETestRunner.RunAll()`: **505/505 Total Tests Passed (0 Failed)**

---

## 2. Logic Chain

1. **Boss Behavior & Movement**:
   - The boss possesses 60 HP, moves at 1.8 u/s towards the player in `FixedUpdate()`, and deals 1 contact damage upon touching the player.
   - When preparing or firing an attack (`isAttacking == true`), `rb.velocity` must be set to `Vector2.zero` to prevent sliding or drifting. Tests `M4-10` and `CH2-M4-30` explicitly assert that setting `boss.isAttacking = true` forces `rb.velocity == Vector2.zero` in `FixedUpdate()`.

2. **Enhanced Radial Barrage Attack Loop & Telegraph**:
   - **Barrage Parameters**:
     * Bullet count: `radialBulletCount = 16`.
     * Angular step: `360f / 16 = 22.5f` degrees.
     * Speed: `projectileSpeed = 5.0f` units/sec.
     * Damage: `bulletDamage = 1` HP.
     * Lifetime: `projectileLifetime = 5.0f` seconds.
     * Burst interval: `burstInterval = 3.5f` seconds.
     * Telegraph duration: `telegraphDuration = 0.5f` seconds.
   - **Telegraph Enhancement Mechanism**:
     * Current telegraph in `BossController.cs` (lines 161-171) is a static single color swap:
       `spriteRenderer.color = telegraphColor; yield return new WaitForSeconds(telegraphDuration);`
     * Requirement: 0.5s visual telegraph (e.g. flashing warning / charge-up color change) before firing.
     * Logic: A rapid pulsating / flashing warning (e.g., 8 Hz ping-pong color interpolation between `originalColor` and `telegraphColor = Color(1f, 0.9f, 0.2f, 1f)`) gives immediate visual feedback.
     * Concurrency & Damage Flash conflict resolution: If the player shoots the boss during the 0.5s telegraph, `EnemyBase.TakeDamage()` runs `FlashRoutine()` which resets color to `originalColor` after 0.1s. In `BossController`, overriding `FlashRoutine()` to restore `isAttacking ? telegraphColor : originalColor` prevents damage flashes from extinguishing the telegraph warning.
     * Safe EditMode stepping: If `ExecuteRadialBarrage()` is stepped in EditMode where `Time.deltaTime` is 0, using `Mathf.Max(Time.deltaTime, 0.05f)` prevents infinite while-loops.
   - **Projectile Spawning**:
     * `SpawnBossProjectile()` uses `bossBulletPrefab` (`EnemyBullet.prefab`).
     * Formula: `rot = Quaternion.Euler(0f, 0f, angleDeg - 90f);`
     * Vector dot product: Test `CH1-M4-14` requires `Vector2.Dot(bullet.transform.up, dir) > 0.999f`. The `- 90f` rotation accurately transforms Unity's default upward `(0, 1)` vector to match `(Cos θ, Sin θ)`.
     * Kinematics: `rb2d.velocity = dir * projectileSpeed;` and `enemyBullet.speed = projectileSpeed;` (verified by `CH1-M4-13` and `CH2-M4-21`).

3. **Guaranteed Rewards (2x Grenade Drop)**:
   - When boss dies, `base.Die()` calls `RollGrenadeDrop()`.
   - `BossController` overrides `RollGrenadeDrop()`:
     ```csharp
     protected override void RollGrenadeDrop()
     {
         if (grenadePickupPrefab == null) return;
         Instantiate(grenadePickupPrefab, (Vector2)transform.position + new Vector2(-0.6f, 0f), Quaternion.identity);
         Instantiate(grenadePickupPrefab, (Vector2)transform.position + new Vector2(0.6f, 0f), Quaternion.identity);
     }
     ```
   - Invariant: `Challenger2M4Tests` Test 7 explicitly checks positions `(-0.6, 0)` and `(+0.6, 0)`. These exact offsets MUST be maintained.
   - Invariant: `Challenger2M4Tests` Test 8 requires null safety if `grenadePickupPrefab == null`.
   - Invariant: `Challenger2M4Tests` Test 9 requires idempotency (no duplicate drops if `Die()` is called multiple times).
   - Invariant: `GrenadePickup.cs` uses `useDynamicBounds = true`, automatically supporting high +Y arena positions without clamping to legacy bounds.

4. **Score Award (+500 Points)**:
   - In `BossController.cs`: `scoreValue = 500`.
   - In `base.Die()`: `AwardScore()` fires static event `EnemyBase.OnEnemyKilledScore?.Invoke(scoreValue)` and reflection call to `GameManager.Instance.AddScore(scoreValue)`.
   - In `GameManager.cs`: `HandleEnemyKilledScore` receives the 500 points and updates `CurrentScore` and `HighScore`. Reflection deduplication prevents duplicate points.

5. **Continuation & Camera Unlock**:
   - `BossController.Die()` invokes static event `BossController.OnBossDefeatedEvent`.
   - `GameManager` handles this event and triggers `TriggerVictory()`.
   - When the victory modal is dismissed or continue is clicked, `GameManager.ResumeEndlessAfterBoss()` is called.
   - In the scrolling map system, `GameManager.ResumeEndlessAfterBoss()` or a direct event handler must invoke `MapManager.Instance.ResumeStandardSpawning()`, which in turn calls `cameraController.UnlockAndResume()` to resume continuous upward scrolling.

---

## 3. Caveats

1. **Dual Milestone Taxonomy**: The legacy test files use the label "Milestone 4" (`Milestone4Tests.cs`, `Challenger1M4Tests.cs`, `Challenger2M4Tests.cs`) for the Boss Encounter features (F21-F24), whereas the endless scrolling project re-indexes Boss Arena Encounter as "Milestone 3 (R3)". All tests in both suites must pass without discrepancy.
2. **Boss Arena Segment Separation**: Creation of `MapSegment_BossArena.prefab` and MapManager queueing is assigned to peer subagent M3-1 (`explorer_m3_1` / `builder_m3_1`). `BossController` is designed to be fully agnostic of its world Y coordinate so it functions identically in a scrolled arena (e.g. Y = 150) or a local unit test scene (Y = 0).
3. **EditMode Coroutine Constraint**: Coroutines (`StartCoroutine`) cannot execute in EditMode tests. `BossController` guards coroutine execution with `if (Application.isPlaying)` in `Start()`. Any manual call to `ExecuteRadialBarrage()` in EditMode must remain step-safe.

---

## 4. Conclusion & Recommended Implementation Details

The current `BossController.cs` has the fundamental architecture in place and currently passes all 80 boss-related tests across `Milestone4Tests`, `Challenger1M4Tests`, and `Challenger2M4Tests`. To meet all Milestone 3 (R3 Boss Attack & Rewards) requirements, the following specific C# improvements are recommended:

### Recommended C# Implementation for `BossController.cs`

```csharp
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Boss encounter controller inheriting from EnemyBase and IDamageable.
/// Stats: 60 HP, 1.8 move speed, 500 score value, guaranteed 2 grenade pickups.
/// Features a periodic 360-degree radial projectile barrage (16 bullets at 5.0 u/s),
/// a 0.5s visual telegraph with pulsating warning flash,
/// static and instance events for spawning, health updates, and defeat,
/// and notifies EnemySpawner and GameManager upon defeat to resume endless scaling.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : EnemyBase
{
    [Header("Boss Specific Combat")]
    public float burstInterval = 3.5f;
    public float telegraphDuration = 0.5f;
    public int radialBulletCount = 16;
    public float projectileSpeed = 5.0f;
    public float projectileLifetime = 5.0f;
    public int bulletDamage = 1;
    public int contactDamage = 1;
    public int guaranteedGrenadeDrops = 2;
    public GameObject bossBulletPrefab;
    public Color telegraphColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Visual Telegraph Settings")]
    public float telegraphFlashFrequency = 8.0f; // Rapid pulsing warning during telegraph

    [Header("Boss State")]
    public bool isAttacking = false;

    // Static Events
    public static event Action<BossController> OnBossSpawned;
    public static event Action<int, int> OnBossHealthChanged;
    public static event Action OnBossDefeatedEvent;
    public static event Action OnBossKilled;

    // Instance Events
    public event Action<int, int> OnHealthChanged;
    public event Action OnDefeated;

    private Coroutine _attackRoutine;

    public BossController()
    {
        maxHealth = 60;
        currentHealth = 60;
        moveSpeed = 1.8f;
        scoreValue = 500;
        grenadeDropChance = 1.0f;
    }

    protected override void Awake()
    {
        maxHealth = 60;
        if (currentHealth <= 0 || currentHealth == 3)
        {
            currentHealth = maxHealth;
        }
        if (moveSpeed == 2.8f || moveSpeed <= 0f)
        {
            moveSpeed = 1.8f;
        }
        scoreValue = 500;
        grenadeDropChance = 1.0f;

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Broadcast spawn and initial health events
        OnBossSpawned?.Invoke(this);
        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (Application.isPlaying)
        {
            _attackRoutine = StartCoroutine(RadialBarrageLoop());
        }
    }

    private void FixedUpdate()
    {
        if (isDead || !IsAlive) return;

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Pause movement during attack telegraph and burst
        if (isAttacking)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        if (playerTransform == null)
        {
            LocatePlayer();
            if (playerTransform == null) return;
        }

        if (!IsPlayerAlive())
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 targetPos = playerTransform.position;
        Vector2 diff = targetPos - currentPos;

        if (diff.sqrMagnitude <= 0.0001f)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = diff.normalized;
        Vector2 targetMove = currentPos + moveDir * moveSpeed * Time.fixedDeltaTime;

        if (rb != null)
        {
            rb.MovePosition(targetMove);
        }
        else
        {
            transform.position = targetMove;
        }

        if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.05f)
        {
            spriteRenderer.flipX = moveDir.x < 0;
        }
    }

    /// <summary>
    /// Continuous periodic attack loop during combat.
    /// </summary>
    private IEnumerator RadialBarrageLoop()
    {
        while (!isDead && IsAlive)
        {
            yield return new WaitForSeconds(burstInterval);

            if (isDead || !IsAlive) yield break;

            yield return StartCoroutine(ExecuteRadialBarrage());
        }
    }

    /// <summary>
    /// Executes telegraph warning and then radial projectile emission.
    /// Enhanced with a pulsating warning flash over telegraphDuration (0.5s).
    /// </summary>
    public IEnumerator ExecuteRadialBarrage()
    {
        isAttacking = true;
        if (rb != null) rb.velocity = Vector2.zero;

        // Enhanced telegraph: flashing warning / charge-up color pulse
        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            if (isDead || !IsAlive) yield break;

            if (spriteRenderer != null)
            {
                float pulse = Mathf.PingPong(elapsed * telegraphFlashFrequency, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, telegraphColor, pulse);
            }

            float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.05f;
            elapsed += dt;
            yield return null;
        }

        // Restore base color before projectile emission
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (!isDead && IsAlive)
        {
            FireRadialBurst();
        }

        isAttacking = false;
    }

    /// <summary>
    /// Emits 16 projectiles spaced evenly by 22.5 degrees in a 360-degree circle.
    /// </summary>
    public void FireRadialBurst()
    {
        float angleStep = 360f / radialBulletCount; // 22.5 degrees
        Vector2 origin = transform.position;

        for (int i = 0; i < radialBulletCount; i++)
        {
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            SpawnBossProjectile(origin, dir, angleDeg);
        }
    }

    /// <summary>
    /// Instantiates and initializes a single radial projectile.
    /// </summary>
    public GameObject SpawnBossProjectile(Vector2 origin, Vector2 dir, float angleDeg)
    {
        GameObject bulletObj;
        Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg - 90f);

        if (bossBulletPrefab != null)
        {
            bulletObj = Instantiate(bossBulletPrefab, origin, rot);
        }
        else
        {
            bulletObj = new GameObject("BossBullet");
            bulletObj.transform.position = origin;
            bulletObj.transform.rotation = rot;

            var sr = bulletObj.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.4f, 0.1f, 1f);
            sr.sortingOrder = 2;

            var col = bulletObj.AddComponent<CircleCollider2D>();
            col.radius = 0.25f;
            col.isTrigger = true;

            var bulletRb = bulletObj.AddComponent<Rigidbody2D>();
            bulletRb.gravityScale = 0f;
            bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var eb = bulletObj.AddComponent<EnemyBullet>();
            eb.speed = projectileSpeed;
            eb.damage = bulletDamage;
            eb.lifetime = projectileLifetime;
        }

        var enemyBullet = bulletObj.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.speed = projectileSpeed;
            enemyBullet.damage = bulletDamage;
            enemyBullet.lifetime = projectileLifetime;
        }

        var rb2d = bulletObj.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.velocity = dir * projectileSpeed;
        }

        return bulletObj;
    }

    public override void TakeDamage(int damage)
    {
        if (!IsAlive || isDead || damage <= 0) return;

        base.TakeDamage(damage);

        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected override IEnumerator FlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = isAttacking ? telegraphColor : originalColor;
        }

        flashCoroutine = null;
    }

    public override void Die()
    {
        if (isDead) return;

        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
        StopAllCoroutines();
        isAttacking = false;

        // Report 0 HP to listeners
        OnBossHealthChanged?.Invoke(0, maxHealth);
        OnHealthChanged?.Invoke(0, maxHealth);

        // Notify EnemySpawner to clear boss status and resume endless mode
        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.OnBossDefeated();
        }

        // Trigger GameManager victory panel if GameManager exists
        TriggerGameManagerVictory();

        // Broadcast death events
        OnBossDefeatedEvent?.Invoke();
        OnBossKilled?.Invoke();
        OnDefeated?.Invoke();

        base.Die();
    }

    /// <summary>
    /// Guaranteed drop of 2 grenade pickups upon Boss defeat.
    /// Positioned at symmetric offsets (-0.6, 0) and (+0.6, 0) relative to boss.
    /// </summary>
    protected override void RollGrenadeDrop()
    {
        if (grenadePickupPrefab == null) return;

        Instantiate(grenadePickupPrefab, (Vector2)transform.position + new Vector2(-0.6f, 0f), Quaternion.identity);
        Instantiate(grenadePickupPrefab, (Vector2)transform.position + new Vector2(0.6f, 0f), Quaternion.identity);
    }

    private void TriggerGameManagerVictory()
    {
        try
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var gmType = assembly.GetType("GameManager");
                if (gmType != null)
                {
                    var instanceProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var victoryMethod = gmType.GetMethod("TriggerVictory", BindingFlags.Public | BindingFlags.Instance);
                        victoryMethod?.Invoke(instance, null);
                    }
                    break;
                }
            }
        }
        catch
        {
            // GameManager may not be present in standalone tests
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryInflictContactDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryInflictContactDamage(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryInflictContactDamage(collider.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        TryInflictContactDamage(collider.gameObject);
    }

    private void TryInflictContactDamage(GameObject target)
    {
        if (isDead || !IsAlive || target == null) return;

        if (target.CompareTag("Player") || target.GetComponent<PlayerHealth>() != null)
        {
            var ph = target.GetComponent<PlayerHealth>() ?? target.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.IsAlive)
            {
                ph.TakeDamage(contactDamage);
            }
        }
    }
}
```

---

## 5. Verification Method

To independently verify all findings and validate that the implementation satisfies 100% of requirements without regressions:

1. **Unity MCP Test Runner Execution**:
   Execute the following code blocks in Unity Editor via `execute_code`:
   ```csharp
   // Milestone 4 Test Suite (20 tests)
   var repM4 = Tests.Milestone4Tests.RunAllTests();
   Debug.Log($"M4: {repM4.PassedCount}/{repM4.TotalCount} passed, {repM4.FailedCount} failed");

   // Challenger 1 M4 Suite (25 tests)
   var repCH1 = Tests.Challenger1M4Tests.RunAllTests();
   Debug.Log($"CH1: {repCH1.PassedCount}/{repCH1.TotalCount} passed, {repCH1.FailedCount} failed");

   // Challenger 2 M4 Suite (35 tests)
   var repCH2 = Tests.Challenger2M4Tests.RunAllTests();
   Debug.Log($"CH2: {repCH2.PassedCount}/{repCH2.TotalCount} passed, {repCH2.FailedCount} failed");

   // Comprehensive E2E Runner (505 tests)
   var repAll = E2ETests.E2ETestRunner.RunAll();
   Debug.Log($"Total: {repAll.PassedCount}/{repAll.TotalCount} passed, {repAll.FailedCount} failed");
   ```

2. **Unity Editor Menu Items**:
   - `E2E Tests -> Run Milestone 4 Tests`
   - `E2E Tests -> Run Challenger 1 Milestone 4 Tests`
   - `E2E Tests -> Run Challenger 2 Milestone 4 Tests`
   - `E2E Tests -> Run All Tests`

3. **Invalidation Conditions**:
   - Any test failure in `Milestone4Tests`, `Challenger1M4Tests`, or `Challenger2M4Tests`.
   - Modifying grenade drop offsets away from `(-0.6, 0)` and `(+0.6, 0)`.
   - Modifying bullet count (16), angular spacing (22.5°), bullet speed (5.0 u/s), or score value (500).
   - Changing static event signatures or omitting `EnemySpawner.OnBossDefeated()` notification.
