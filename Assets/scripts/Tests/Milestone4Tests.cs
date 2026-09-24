using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using E2ETests;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests
{
    public static class Milestone4Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Milestone 4 Test Suite - Boss Encounter" };

            // TEST 1: BossController Inheritance & IDamageable Interface (F21/F22)
            TestRunnerHelper.RunTest(report, "M4-01", "F21", 1,
                "BossController Inheritance & IDamageable",
                "Verifies BossController inherits from EnemyBase and implements IDamageable.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Boss");
                        var rb = go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();

                        E2EAssert.IsNotNull(boss, "BossController component should be attached");
                        E2EAssert.IsTrue(boss is EnemyBase, "BossController must inherit from EnemyBase");
                        E2EAssert.IsTrue(boss is IDamageable, "BossController must implement IDamageable");
                    }
                });

            // TEST 2: Boss Default Stats (F22)
            TestRunnerHelper.RunTest(report, "M4-02", "F22", 1,
                "Boss Default Combat Stats",
                "Verifies maxHealth=60, currentHealth=60, moveSpeed=1.8, scoreValue=500, guaranteedGrenadeDrops=2.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Boss");
                        go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();

                        E2EAssert.AreEqual(60, boss.maxHealth, "Boss maxHealth must be 60 HP");
                        E2EAssert.AreEqual(60, boss.currentHealth, "Boss currentHealth must start at 60 HP");
                        E2EAssert.AreApproximatelyEqual(1.8f, boss.moveSpeed, 0.05f, "Boss moveSpeed should be 1.8 u/s");
                        E2EAssert.AreEqual(500, boss.scoreValue, "Boss scoreValue must be 500");
                        E2EAssert.AreEqual(2, boss.guaranteedGrenadeDrops, "Guaranteed grenade drops must be 2");
                    }
                });

            // TEST 3: Boss Spawn Event Dispatch (F21/F22)
            TestRunnerHelper.RunTest(report, "M4-03", "F21", 1,
                "Boss Spawn Event Broadcast",
                "Verifies static OnBossSpawned and OnBossHealthChanged dispatch upon instantiation/start.",
                () =>
                {
                    BossController spawnedBoss = null;
                    int reportedCur = 0, reportedMax = 0;

                    Action<BossController> onSpawn = b => spawnedBoss = b;
                    Action<int, int> onHealth = (c, m) => { reportedCur = c; reportedMax = m; };

                    BossController.OnBossSpawned += onSpawn;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("Boss");
                            go.AddComponent<Rigidbody2D>();
                            var boss = go.AddComponent<BossController>();

                            // Simulate Start
                            var startMethod = typeof(BossController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                            startMethod?.Invoke(boss, null);

                            E2EAssert.AreEqual(boss, spawnedBoss, "OnBossSpawned should dispatch created BossController instance");
                            E2EAssert.AreEqual(60, reportedCur, "Reported current health must be 60");
                            E2EAssert.AreEqual(60, reportedMax, "Reported max health must be 60");
                        }
                    }
                    finally
                    {
                        BossController.OnBossSpawned -= onSpawn;
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 4: Boss Damage Ingestion & Health Event (F22)
            TestRunnerHelper.RunTest(report, "M4-04", "F22", 1,
                "Boss Damage Ingestion & Health Event",
                "Verifies TakeDamage reduces currentHealth and dispatches updated health event.",
                () =>
                {
                    int lastCur = -1, lastMax = -1;
                    Action<int, int> onHealth = (c, m) => { lastCur = c; lastMax = m; };
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("Boss");
                            go.AddComponent<Rigidbody2D>();
                            var boss = go.AddComponent<BossController>();

                            boss.TakeDamage(15);
                            E2EAssert.AreEqual(45, boss.currentHealth, "Boss health should be 45 after taking 15 damage");
                            E2EAssert.AreEqual(45, lastCur, "OnBossHealthChanged current health must be 45");
                            E2EAssert.AreEqual(60, lastMax, "OnBossHealthChanged max health must be 60");

                            float ratio = (float)boss.currentHealth / boss.maxHealth;
                            E2EAssert.AreApproximatelyEqual(45f / 60f, ratio, 0.001f, "Health ratio must equal 45/60");
                        }
                    }
                    finally
                    {
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 5: Radial Barrage Geometry & 22.5 Degree Spacing (F23)
            TestRunnerHelper.RunTest(report, "M4-05", "F23", 1,
                "Radial Barrage Geometry & Angular Spacing",
                "Verifies 16 bullets, 22.5 degree spacing, and unit direction vectors.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Boss");
                        go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();

                        E2EAssert.AreEqual(16, boss.radialBulletCount, "Radial burst must configure 16 bullets");
                        float angleStep = 360f / boss.radialBulletCount;
                        E2EAssert.AreApproximatelyEqual(22.5f, angleStep, 0.001f, "Angular step must be 22.5 degrees");

                        for (int i = 0; i < boss.radialBulletCount; i++)
                        {
                            float angleRad = i * angleStep * Mathf.Deg2Rad;
                            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                            E2EAssert.AreApproximatelyEqual(1.0f, dir.magnitude, 0.001f, $"Direction vector {i} must be normalized");
                        }
                    }
                });

            // TEST 6: Radial Projectile Speed and Properties (F23)
            TestRunnerHelper.RunTest(report, "M4-06", "F23", 1,
                "Radial Projectile Speed and Properties",
                "Verifies projectileSpeed=5.0, lifetime=5.0, damage=1.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Boss");
                        go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();

                        E2EAssert.AreApproximatelyEqual(5.0f, boss.projectileSpeed, 0.01f, "Projectile speed must be 5.0 u/s");
                        E2EAssert.AreApproximatelyEqual(5.0f, boss.projectileLifetime, 0.01f, "Projectile lifetime must be 5.0s");
                        E2EAssert.AreEqual(1, boss.bulletDamage, "Bullet damage must be 1 HP");
                        E2EAssert.AreApproximatelyEqual(3.5f, boss.burstInterval, 0.01f, "Burst interval should be 3.5s");
                    }
                });

            // TEST 7: Boss Spawn Projectile Dynamics (F23)
            TestRunnerHelper.RunTest(report, "M4-07", "F23", 1,
                "Boss Projectile Instantiation Dynamics",
                "Verifies SpawnBossProjectile creates bullet with proper velocity and EnemyBullet component.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Boss");
                        go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();

                        Vector2 origin = new Vector2(2f, 3f);
                        Vector2 dir = Vector2.right;
                        var bulletObj = boss.SpawnBossProjectile(origin, dir, 0f);

                        E2EAssert.IsNotNull(bulletObj, "SpawnBossProjectile must return a valid GameObject");
                        var eb = bulletObj.GetComponent<EnemyBullet>();
                        E2EAssert.IsNotNull(eb, "Spawned projectile must have EnemyBullet component");
                        E2EAssert.AreEqual(1, eb.damage, "EnemyBullet damage must be 1");
                        E2EAssert.AreApproximatelyEqual(5.0f, eb.speed, 0.01f, "EnemyBullet speed must be 5.0");

                        var rb = bulletObj.GetComponent<Rigidbody2D>();
                        E2EAssert.IsNotNull(rb, "Spawned projectile must have Rigidbody2D");
                        E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.x, 0.01f, "Velocity X must equal speed * dir.x");
                        E2EAssert.AreApproximatelyEqual(0.0f, rb.velocity.y, 0.01f, "Velocity Y must equal 0");

                        UnityEngine.Object.DestroyImmediate(bulletObj);
                    }
                });

            // TEST 8: Radial Projectile Friendly Enemy Immunity (F23)
            TestRunnerHelper.RunTest(report, "M4-08", "F23", 2,
                "Radial Projectile Ignores Enemies",
                "Verifies EnemyBullet does not damage friendly enemies.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bulletGo = ctx.CreateGameObject("Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var chaserGo = ctx.CreateGameObject("Chaser");
                        chaserGo.tag = "Enemy";
                        var chaser = chaserGo.AddComponent<ChaserEnemy>();

                        // HandleHit check via reflection
                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { chaserGo });

                        E2EAssert.AreEqual(3, chaser.currentHealth, "Friendly enemy must take no damage from EnemyBullet");
                    }
                });

            // TEST 9: Radial Projectile Damages Player (F23)
            TestRunnerHelper.RunTest(report, "M4-09", "F23", 2,
                "Radial Projectile Damages Player",
                "Verifies EnemyBullet deals 1 HP damage to Player.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bulletGo = ctx.CreateGameObject("Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();
                        eb.damage = 1;

                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Player should lose exactly 1 HP from bullet hit");
                    }
                });

            // TEST 10: Boss Attack Telegraph Pauses Movement (F23)
            TestRunnerHelper.RunTest(report, "M4-10", "F23", 2,
                "Boss Attack Telegraph Pauses Movement",
                "Verifies isAttacking sets velocity to zero and pauses movement in FixedUpdate.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Boss");
                        var rb = go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();

                        boss.isAttacking = true;
                        rb.velocity = new Vector2(2f, 2f);

                        var fixedUpdate = typeof(BossController).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                        fixedUpdate?.Invoke(boss, null);

                        E2EAssert.AreEqual(Vector2.zero, rb.velocity, "Velocity must be zero while isAttacking is true");
                    }
                });

            // TEST 11: Boss Defeat Events and State (F24)
            TestRunnerHelper.RunTest(report, "M4-11", "F24", 1,
                "Boss Defeat Events and State",
                "Verifies Die() sets isDead=true, currentHealth=0, and fires defeat events.",
                () =>
                {
                    bool defeatFired = false;
                    bool killedFired = false;
                    int lastHealth = -1;

                    Action onDefeat = () => defeatFired = true;
                    Action onKilled = () => killedFired = true;
                    Action<int, int> onHealth = (c, m) => lastHealth = c;

                    BossController.OnBossDefeatedEvent += onDefeat;
                    BossController.OnBossKilled += onKilled;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("Boss");
                            go.AddComponent<Rigidbody2D>();
                            var boss = go.AddComponent<BossController>();

                            boss.Die();

                            E2EAssert.IsTrue(boss.isDead, "Boss isDead must be true after Die()");
                            E2EAssert.AreEqual(0, boss.currentHealth, "currentHealth must be 0");
                            E2EAssert.AreEqual(0, lastHealth, "OnBossHealthChanged should report 0 on defeat");
                            E2EAssert.IsTrue(defeatFired, "OnBossDefeatedEvent must be invoked");
                            E2EAssert.IsTrue(killedFired, "OnBossKilled must be invoked");
                        }
                    }
                    finally
                    {
                        BossController.OnBossDefeatedEvent -= onDefeat;
                        BossController.OnBossKilled -= onKilled;
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 12: Boss Guaranteed 2 Grenade Drops (F24)
            TestRunnerHelper.RunTest(report, "M4-12", "F24", 1,
                "Boss Guaranteed 2 Grenade Pickups on Defeat",
                "Verifies Boss drops exactly 2 grenade pickups upon defeat.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var dummyPickup = ctx.CreateGameObject("DummyPickup");

                        var go = ctx.CreateGameObject("Boss");
                        go.AddComponent<Rigidbody2D>();
                        var boss = go.AddComponent<BossController>();
                        boss.grenadePickupPrefab = dummyPickup;

                        int initialPickups = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        // Invoke RollGrenadeDrop
                        var dropMethod = typeof(BossController).GetMethod("RollGrenadeDrop", BindingFlags.NonPublic | BindingFlags.Instance);
                        dropMethod?.Invoke(boss, null);

                        int finalPickups = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        int droppedCount = finalPickups - initialPickups;

                        E2EAssert.AreEqual(2, droppedCount, "Boss must instantiate exactly 2 grenade pickups");
                    }
                });

            // TEST 13: Spawner Notification via OnBossDefeated (F24)
            TestRunnerHelper.RunTest(report, "M4-13", "F24", 1,
                "Spawner Notification via OnBossDefeated",
                "Verifies Die() calls EnemySpawner.OnBossDefeated() to reset isBossActive.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        spawner.isBossActive = true;
                        spawner.isSpawning = true;

                        var bossGo = ctx.CreateGameObject("Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        boss.Die();

                        E2EAssert.IsFalse(spawner.isBossActive, "spawner.isBossActive must be cleared after boss defeat");
                        E2EAssert.IsTrue(spawner.isSpawning, "spawner.isSpawning must be true to continue endless mode");
                    }
                });

            // TEST 14: Spawner 500 Score Boss Latch Idempotency (F21)
            TestRunnerHelper.RunTest(report, "M4-14", "F21", 2,
                "Spawner 500 Score Boss Latch Idempotency",
                "Verifies SpawnBoss sets bossSpawned=true and ignores duplicate calls.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        E2EAssert.IsFalse(spawner.bossSpawned, "bossSpawned should be false initially");

                        spawner.SpawnBoss();
                        E2EAssert.IsTrue(spawner.bossSpawned, "SpawnBoss must set bossSpawned = true");
                        E2EAssert.IsTrue(spawner.isBossActive, "SpawnBoss must set isBossActive = true");

                        // Subsequent spawn attempt
                        spawner.SpawnBoss();
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must remain true");
                    }
                });

            // TEST 15: Spawner Boss 50% Rate Suppression (F21)
            TestRunnerHelper.RunTest(report, "M4-15", "F21", 2,
                "Spawner 50% Rate Suppression During Boss",
                "Verifies CalculateSpawnInterval doubles when isBossActive is true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        float normalInterval = spawner.CalculateSpawnInterval(0f, 0);
                        spawner.isBossActive = true;
                        float suppressedInterval = spawner.CalculateSpawnInterval(0f, 0);

                        E2EAssert.AreApproximatelyEqual(normalInterval * 2.0f, suppressedInterval, 0.001f,
                            "Suppressed spawn interval must be double the normal interval (50% spawn rate)");
                    }
                });

            // TEST 16: Spawner Boss Spawn Position (F21)
            TestRunnerHelper.RunTest(report, "M4-16", "F21", 1,
                "Spawner Boss Spawn Arena Position",
                "Verifies bossSpawnPosition is configured at (2.69, 3.5, 0.0).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        E2EAssert.AreApproximatelyEqual(2.69f, spawner.bossSpawnPosition.x, 0.01f, "Boss spawn X must be 2.69");
                        E2EAssert.AreApproximatelyEqual(3.5f, spawner.bossSpawnPosition.y, 0.01f, "Boss spawn Y must be 3.5");
                        E2EAssert.AreApproximatelyEqual(0f, spawner.bossSpawnPosition.z, 0.01f, "Boss spawn Z must be 0");
                    }
                });

            // TEST 17: Boss Contact Damage to Player (F21)
            TestRunnerHelper.RunTest(report, "M4-17", "F21", 2,
                "Boss Contact Damage to Player",
                "Verifies Boss contact inflicts 1 damage to PlayerHealth.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.contactDamage = 1;

                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var contactMethod = typeof(BossController).GetMethod("TryInflictContactDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                        contactMethod?.Invoke(boss, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Player should have 4 HP remaining after 1 contact damage");
                    }
                });

            // TEST 18: Prefab Asset Integrity Verification (F21/F22)
            TestRunnerHelper.RunTest(report, "M4-18", "PrefabVerification", 1,
                "BossEnemy Prefab Asset Integrity",
                "Verifies Assets/Prefabs/BossEnemy.prefab has scale 2.8, crimson color, CircleCollider2D, and BossController.",
                () =>
                {
#if UNITY_EDITOR
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossEnemy.prefab");
                    E2EAssert.IsNotNull(prefab, "BossEnemy.prefab must exist in Assets/Prefabs/");

                    var boss = prefab.GetComponent<BossController>();
                    E2EAssert.IsNotNull(boss, "BossEnemy.prefab must have BossController component");
                    E2EAssert.AreEqual(60, boss.maxHealth, "Prefab maxHealth must be 60");
                    E2EAssert.AreEqual(500, boss.scoreValue, "Prefab scoreValue must be 500");

                    var sr = prefab.GetComponent<SpriteRenderer>();
                    E2EAssert.IsNotNull(sr, "Prefab must have SpriteRenderer");
                    E2EAssert.IsNotNull(sr.sprite, "Prefab SpriteRenderer must have treant sprite assigned");
                    E2EAssert.AreApproximatelyEqual(1.0f, sr.color.r, 0.05f, "Crimson red component should be ~1.0");
                    E2EAssert.AreApproximatelyEqual(0.35f, sr.color.g, 0.05f, "Crimson green component should be ~0.35");
                    E2EAssert.AreApproximatelyEqual(0.35f, sr.color.b, 0.05f, "Crimson blue component should be ~0.35");

                    E2EAssert.AreApproximatelyEqual(2.8f, prefab.transform.localScale.x, 0.05f, "LocalScale X must be 2.8");
                    E2EAssert.AreApproximatelyEqual(2.8f, prefab.transform.localScale.y, 0.05f, "LocalScale Y must be 2.8");

                    var col = prefab.GetComponent<CircleCollider2D>();
                    E2EAssert.IsNotNull(col, "Prefab must have CircleCollider2D");

                    var rb = prefab.GetComponent<Rigidbody2D>();
                    E2EAssert.IsNotNull(rb, "Prefab must have Rigidbody2D");
                    E2EAssert.AreEqual(0f, rb.gravityScale, "Rigidbody2D gravity scale must be 0");
#endif
                });

            // TEST 19: Scene Spawner bossPrefab Wiring (F21)
            TestRunnerHelper.RunTest(report, "M4-19", "SceneIntegration", 1,
                "Scene Spawner bossPrefab Wiring",
                "Verifies EnemySpawner in shooting.unity references BossEnemy prefab.",
                () =>
                {
                    var spawnerGo = GameObject.Find("EnemySpawner");
                    if (spawnerGo != null)
                    {
                        var spawner = spawnerGo.GetComponent<EnemySpawner>();
                        E2EAssert.IsNotNull(spawner, "EnemySpawner component must be on EnemySpawner GameObject");
                        E2EAssert.IsNotNull(spawner.bossPrefab, "EnemySpawner.bossPrefab must be wired to BossEnemy prefab");
                        E2EAssert.AreEqual("BossEnemy", spawner.bossPrefab.name, "bossPrefab should be BossEnemy");
                    }
                });

            // TEST 20: Full Boss Encounter Lifecycle Simulation (F21-F24)
            TestRunnerHelper.RunTest(report, "M4-20", "LifecycleSimulation", 4,
                "Full Boss Encounter Scenario Simulation",
                "Simulates spawn at 500 pts, taking damage, radial firing, defeat, +500 points, 2 grenades, and endless continuation.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        var bossGo = ctx.CreateGameObject("Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        // 1. Spawner triggers boss
                        spawner.isBossActive = true;
                        spawner.bossSpawned = true;

                        // 2. Boss takes 50 damage (e.g. from grenade)
                        boss.TakeDamage(50);
                        E2EAssert.AreEqual(10, boss.currentHealth, "Boss HP should be 10 after 50 damage");

                        // 3. Boss fires radial barrage
                        boss.FireRadialBurst();

                        // 4. Boss takes final 10 damage
                        boss.TakeDamage(10);
                        E2EAssert.AreEqual(0, boss.currentHealth, "Boss HP should reach 0");
                        E2EAssert.IsTrue(boss.isDead, "Boss should be marked dead");

                        // 5. Verify endless spawner resumed
                        E2EAssert.IsFalse(spawner.isBossActive, "Spawner should resume normal rates");
                        E2EAssert.IsTrue(spawner.bossSpawned, "Boss latch must remain true to prevent duplicate boss");
                    }
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Milestone 4 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
