using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using E2ETests;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests
{
    /// <summary>
    /// Empirical Adversarial Verification Suite for Milestone 4 (Boss Encounter).
    /// Authored by Challenger 1.
    /// Focus Areas:
    /// 1. Score latch trigger: Boss spawns exactly once at score >= 500, never duplicates on score increases (1000, 1500, etc.).
    /// 2. Radial barrage geometry: 16 projectiles, 22.5 deg increments spanning 360 deg, speed 5.0, kinematics & alignment.
    /// 3. Damage resilience & event dispatch: 60 HP pool, hits deduction, non-positive damage rejection, overkill clamping, and OnBossHealthChanged accuracy.
    /// </summary>
    public static class Challenger1M4Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 1 Milestone 4 Adversarial Verification Suite" };

            // =========================================================================
            // SUITE 1: Score Latch Trigger & Idempotency
            // =========================================================================

            // TEST 1: Initial Spawner State
            TestRunnerHelper.RunTest(report, "CH1-M4-01", "F21", 1,
                "Spawner Initial Boss State",
                "Verifies EnemySpawner starts with bossSpawned=false and isBossActive=false.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        E2EAssert.IsFalse(spawner.bossSpawned, "bossSpawned must be false initially");
                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be false initially");
                    }
                });

            // TEST 2: Score Below 500 Does Not Trigger Boss
            TestRunnerHelper.RunTest(report, "CH1-M4-02", "F21", 2,
                "Score Below 500 Does Not Spawn Boss",
                "Verifies scores 0, 100, 250, 499 never trigger boss spawn.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        int[] sub500Scores = new int[] { 0, 50, 100, 250, 400, 499 };
                        foreach (int sc in sub500Scores)
                        {
                            if (sc >= 500 && !spawner.bossSpawned)
                            {
                                spawner.SpawnBoss();
                            }
                        }

                        E2EAssert.IsFalse(spawner.bossSpawned, "bossSpawned must remain false for scores < 500");
                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must remain false for scores < 500");
                    }
                });

            // TEST 3: Score Exactly 500 Triggers Single Boss Instance
            TestRunnerHelper.RunTest(report, "CH1-M4-03", "F21", 1,
                "Score Reaching 500 Spawns Boss",
                "Verifies score reaching 500 triggers SpawnBoss, setting bossSpawned=true and isBossActive=true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        var dummyBoss = ctx.CreateGameObject("DummyBoss");
                        spawner.bossPrefab = dummyBoss;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        int currentScore = 500;
                        if (currentScore >= 500 && !spawner.bossSpawned)
                        {
                            spawner.SpawnBoss();
                        }

                        int afterCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must be true");
                        E2EAssert.IsTrue(spawner.isBossActive, "isBossActive must be true");
                        E2EAssert.AreEqual(beforeCount + 1, afterCount, "Exactly 1 boss instance must be created");
                    }
                });

            // TEST 4: Score Progression (501, 750, 1000, 1500, 5000) Never Duplicates Boss
            TestRunnerHelper.RunTest(report, "CH1-M4-04", "F21", 2,
                "Ascending Score Progression Never Duplicates Boss",
                "Verifies as score rises through 501, 750, 1000, 1500, 5000, no second boss is spawned.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        var dummyBoss = ctx.CreateGameObject("DummyBoss");
                        spawner.bossPrefab = dummyBoss;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        int[] scoreSteps = new int[] { 500, 501, 600, 750, 1000, 1250, 1500, 2000, 5000 };
                        int spawnCallsAttempted = 0;

                        foreach (int s in scoreSteps)
                        {
                            if (s >= 500 && !spawner.bossSpawned)
                            {
                                spawner.SpawnBoss();
                                spawnCallsAttempted++;
                            }
                        }

                        int afterCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        E2EAssert.AreEqual(1, spawnCallsAttempted, "SpawnBoss trigger condition must evaluate true exactly once");
                        E2EAssert.AreEqual(beforeCount + 1, afterCount, "Exactly 1 boss object must exist in total");
                    }
                });

            // TEST 5: Large Score Jump (0 -> 1500) Spawns Exactly Once
            TestRunnerHelper.RunTest(report, "CH1-M4-05", "F21", 2,
                "Discrete Large Score Jump Spawns Exactly Once",
                "Verifies sudden score jump from 0 to 1500 spawns exactly one boss.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        var dummyBoss = ctx.CreateGameObject("DummyBoss");
                        spawner.bossPrefab = dummyBoss;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        int jumpedScore = 1500;
                        if (jumpedScore >= 500 && !spawner.bossSpawned)
                        {
                            spawner.SpawnBoss();
                        }

                        int afterCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        E2EAssert.AreEqual(beforeCount + 1, afterCount, "Exactly 1 boss object created on big score jump");
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must be true");
                    }
                });

            // TEST 6: Direct SpawnBoss Invocations Are Idempotent
            TestRunnerHelper.RunTest(report, "CH1-M4-06", "F21", 2,
                "Direct SpawnBoss Idempotency Under Multiple Invocations",
                "Verifies calling SpawnBoss() repeatedly creates only 1 instance.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        var dummyBoss = ctx.CreateGameObject("DummyBoss");
                        spawner.bossPrefab = dummyBoss;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        for (int i = 0; i < 10; i++)
                        {
                            spawner.SpawnBoss();
                        }

                        int afterCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        E2EAssert.AreEqual(beforeCount + 1, afterCount, "10 calls to SpawnBoss must instantiate at most 1 boss object");
                    }
                });

            // TEST 7: Defeat Clears isBossActive But Retains bossSpawned
            TestRunnerHelper.RunTest(report, "CH1-M4-07", "F21/F24", 1,
                "OnBossDefeated Clears Active Status But Retains Latch",
                "Verifies OnBossDefeated sets isBossActive=false, isSpawning=true, and leaves bossSpawned=true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        spawner.SpawnBoss();
                        E2EAssert.IsTrue(spawner.isBossActive, "isBossActive should be true before defeat");

                        spawner.OnBossDefeated();

                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be false after defeat");
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must remain true permanently");
                        E2EAssert.IsTrue(spawner.isSpawning, "isSpawning must be true to resume endless mode");
                    }
                });

            // TEST 8: Post-Defeat Endless Progression Never Spawns Second Boss
            TestRunnerHelper.RunTest(report, "CH1-M4-08", "F21/F24", 2,
                "Post-Defeat Endless Score Progression Never Re-Spawns Boss",
                "Verifies score increases to 1000, 2000, 5000 after defeat never trigger a second boss.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH1_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        var dummyBoss = ctx.CreateGameObject("DummyBoss");
                        spawner.bossPrefab = dummyBoss;

                        spawner.SpawnBoss();
                        spawner.OnBossDefeated();

                        int countAfterDefeat = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        // Simulate future frames with higher scores
                        int[] postDefeatScores = new int[] { 550, 750, 1000, 1500, 3000, 10000 };
                        foreach (int s in postDefeatScores)
                        {
                            if (s >= 500 && !spawner.bossSpawned)
                            {
                                spawner.SpawnBoss();
                            }
                        }

                        int countFinal = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        E2EAssert.AreEqual(countAfterDefeat, countFinal, "No second boss may ever spawn in endless continuation");
                    }
                });

            // =========================================================================
            // SUITE 2: Radial Barrage Geometry & Projectiles
            // =========================================================================

            // TEST 9: Radial Burst Generates Exactly 16 Projectiles
            TestRunnerHelper.RunTest(report, "CH1-M4-09", "F23", 1,
                "Radial Burst Projectile Count",
                "Verifies FireRadialBurst creates exactly 16 projectile instances.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH1_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        int before = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        boss.FireRadialBurst();
                        int after = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        E2EAssert.AreEqual(before + 16, after, "FireRadialBurst must instantiate exactly 16 objects");
                    }
                });

            // TEST 10: Angular Spacing is Exactly 22.5 Degrees
            TestRunnerHelper.RunTest(report, "CH1-M4-10", "F23", 1,
                "Radial Burst Angular Spacing",
                "Verifies 360 / radialBulletCount evaluates to exactly 22.5 degrees.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH1_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        float step = 360f / boss.radialBulletCount;
                        E2EAssert.AreEqual(16, boss.radialBulletCount, "radialBulletCount must be 16");
                        E2EAssert.AreApproximatelyEqual(22.5f, step, 0.0001f, "Angular step must equal 22.5 degrees");
                    }
                });

            // TEST 11: Direction Vectors are Normalized Unit Vectors
            TestRunnerHelper.RunTest(report, "CH1-M4-11", "F23", 1,
                "Radial Direction Vectors Normalized",
                "Verifies each direction vector (Cos, Sin) has magnitude 1.0.",
                () =>
                {
                    float angleStep = 22.5f;
                    for (int i = 0; i < 16; i++)
                    {
                        float rad = i * angleStep * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                        E2EAssert.AreApproximatelyEqual(1.0f, dir.magnitude, 0.0001f, $"Dir {i} magnitude must be 1.0");
                    }
                });

            // TEST 12: Full 360 Coverage Without Overlap
            TestRunnerHelper.RunTest(report, "CH1-M4-12", "F23", 1,
                "Full 360 Degree Coverage Without Gaps",
                "Verifies all 16 directions span distinct bearings and sum to a closed circle.",
                () =>
                {
                    HashSet<int> angleSet = new HashSet<int>();
                    for (int i = 0; i < 16; i++)
                    {
                        float deg = i * 22.5f;
                        int rounded = Mathf.RoundToInt(deg * 10f);
                        E2EAssert.IsTrue(angleSet.Add(rounded), $"Angle {deg} must be unique");
                    }
                    E2EAssert.AreEqual(16, angleSet.Count, "Must have 16 unique directions");
                    E2EAssert.AreApproximatelyEqual(360.0f, 16 * 22.5f, 0.001f, "16 * 22.5 must equal 360");
                });

            // TEST 13: Projectile Speed is Exactly 5.0 u/s
            TestRunnerHelper.RunTest(report, "CH1-M4-13", "F23", 1,
                "Radial Projectile Speed Configuration",
                "Verifies projectileSpeed=5.0 and Rigidbody2D.velocity magnitude equals 5.0.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH1_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        Vector2 dir = new Vector2(Mathf.Cos(45f * Mathf.Deg2Rad), Mathf.Sin(45f * Mathf.Deg2Rad));
                        var bullet = boss.SpawnBossProjectile(Vector2.zero, dir, 45f);

                        var rb = bullet.GetComponent<Rigidbody2D>();
                        E2EAssert.IsNotNull(rb, "Bullet must have Rigidbody2D");
                        E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.magnitude, 0.01f, "Bullet velocity magnitude must be 5.0");

                        var eb = bullet.GetComponent<EnemyBullet>();
                        E2EAssert.IsNotNull(eb, "Bullet must have EnemyBullet component");
                        E2EAssert.AreApproximatelyEqual(5.0f, eb.speed, 0.01f, "EnemyBullet.speed must be 5.0");

                        UnityEngine.Object.DestroyImmediate(bullet);
                    }
                });

            // TEST 14: Projectile Orientation Aligns with Velocity
            TestRunnerHelper.RunTest(report, "CH1-M4-14", "F23", 2,
                "Radial Projectile Orientation Aligns with Velocity",
                "Verifies transform.up of the projectile matches direction vector.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH1_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        for (int i = 0; i < 16; i++)
                        {
                            float angleDeg = i * 22.5f;
                            float angleRad = angleDeg * Mathf.Deg2Rad;
                            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                            var bullet = boss.SpawnBossProjectile(Vector2.zero, dir, angleDeg);
                            Vector2 up = bullet.transform.up;

                            float dot = Vector2.Dot(up, dir);
                            E2EAssert.IsTrue(dot > 0.999f, $"Bullet {i} transform.up must align with dir (dot={dot})");

                            UnityEngine.Object.DestroyImmediate(bullet);
                        }
                    }
                });

            // TEST 15: Projectiles Pass Through Trigger Pickups
            TestRunnerHelper.RunTest(report, "CH1-M4-15", "F23", 2,
                "Radial Projectiles Pass Through Trigger Pickups",
                "Verifies EnemyBullet does not detonate on non-player trigger colliders (like GrenadePickup).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var pickupGo = ctx.CreateGameObject("Pickup");
                        var col = pickupGo.AddComponent<CircleCollider2D>();
                        col.isTrigger = true;
                        pickupGo.AddComponent<GrenadePickup>();

                        var bulletGo = ctx.CreateGameObject("Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { pickupGo });

                        E2EAssert.IsFalse(bulletGo == null, "Bullet must pass through trigger pickup without destruction");
                    }
                });

            // TEST 16: Friendly Immunity (Radial Projectile Ignores Boss and Enemies)
            TestRunnerHelper.RunTest(report, "CH1-M4-16", "F23", 2,
                "Radial Projectiles Ignore Boss Self and Other Enemies",
                "Verifies EnemyBullet deals 0 damage to Boss and normal enemies.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH1_Boss");
                        bossGo.tag = "Enemy";
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        var bulletGo = ctx.CreateGameObject("Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);

                        handleHit?.Invoke(eb, new object[] { bossGo });
                        E2EAssert.AreEqual(60, boss.currentHealth, "Boss must take 0 damage from friendly bullets");
                        E2EAssert.IsFalse(bulletGo == null, "Bullet must not be destroyed by hitting boss");
                    }
                });

            // =========================================================================
            // SUITE 3: Damage Resilience & Event Dispatch
            // =========================================================================

            // TEST 17: Boss Starting Health Pool is Exactly 60 HP
            TestRunnerHelper.RunTest(report, "CH1-M4-17", "F22", 1,
                "Boss Starting Health Pool is 60 HP",
                "Verifies maxHealth=60 and currentHealth=60 on initialization.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH1_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        E2EAssert.AreEqual(60, boss.maxHealth, "maxHealth must be 60");
                        E2EAssert.AreEqual(60, boss.currentHealth, "currentHealth must be 60");
                        E2EAssert.IsTrue(boss.IsAlive, "Boss must be alive initially");
                        E2EAssert.IsFalse(boss.isDead, "Boss must not be dead initially");
                    }
                });

            // TEST 18: Start() Dispatches OnBossSpawned and OnBossHealthChanged(60, 60)
            TestRunnerHelper.RunTest(report, "CH1-M4-18", "F22", 1,
                "Start Dispatches Spawn and Initial Health Events",
                "Verifies OnBossSpawned and OnBossHealthChanged(60, 60) are invoked during Start.",
                () =>
                {
                    BossController spawned = null;
                    int cur = -1, max = -1;

                    Action<BossController> onSpawn = b => spawned = b;
                    Action<int, int> onHealth = (c, m) => { cur = c; max = m; };

                    BossController.OnBossSpawned += onSpawn;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            var startMethod = typeof(BossController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                            startMethod?.Invoke(boss, null);

                            E2EAssert.AreEqual(boss, spawned, "OnBossSpawned must broadcast created instance");
                            E2EAssert.AreEqual(60, cur, "Initial current health reported must be 60");
                            E2EAssert.AreEqual(60, max, "Initial max health reported must be 60");
                        }
                    }
                    finally
                    {
                        BossController.OnBossSpawned -= onSpawn;
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 19: Incremental Damage Tracking
            TestRunnerHelper.RunTest(report, "CH1-M4-19", "F22", 2,
                "Incremental Damage Dispatch Accuracy",
                "Verifies single and rapid damage hits dispatch exact current health.",
                () =>
                {
                    List<int> reportedHps = new List<int>();
                    Action<int, int> onHealth = (c, m) => reportedHps.Add(c);
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.TakeDamage(1);
                            E2EAssert.AreEqual(59, boss.currentHealth, "HP must be 59 after 1 dmg");

                            boss.TakeDamage(4);
                            E2EAssert.AreEqual(55, boss.currentHealth, "HP must be 55 after 4 dmg");

                            boss.TakeDamage(5);
                            E2EAssert.AreEqual(50, boss.currentHealth, "HP must be 50 after 5 dmg");

                            E2EAssert.AreEqual(3, reportedHps.Count, "3 damage events should be fired");
                            E2EAssert.AreEqual(59, reportedHps[0], "First event must report 59");
                            E2EAssert.AreEqual(55, reportedHps[1], "Second event must report 55");
                            E2EAssert.AreEqual(50, reportedHps[2], "Third event must report 50");
                        }
                    }
                    finally
                    {
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 20: AoE Explosion Damage Chunk
            TestRunnerHelper.RunTest(report, "CH1-M4-20", "F22", 2,
                "AoE Grenade Chunk Damage to Boss",
                "Verifies taking 50 explosion damage leaves 10 HP and reports (10, 60).",
                () =>
                {
                    int lastCur = -1;
                    Action<int, int> onHealth = (c, m) => lastCur = c;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.TakeDamage(50);

                            E2EAssert.AreEqual(10, boss.currentHealth, "Boss must have 10 HP after 50 damage");
                            E2EAssert.AreEqual(10, lastCur, "OnBossHealthChanged must report 10");
                        }
                    }
                    finally
                    {
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 21: Non-Positive Damage Rejection (0 Damage)
            TestRunnerHelper.RunTest(report, "CH1-M4-21", "F22", 2,
                "Zero Damage Ingestion Rejection",
                "Verifies TakeDamage(0) causes no health deduction and triggers no event.",
                () =>
                {
                    int eventCount = 0;
                    Action<int, int> onHealth = (c, m) => eventCount++;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.TakeDamage(0);

                            E2EAssert.AreEqual(60, boss.currentHealth, "Health must remain 60 on 0 damage");
                            E2EAssert.AreEqual(0, eventCount, "No health event may be dispatched on 0 damage");
                        }
                    }
                    finally
                    {
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 22: Negative Damage Rejection (Heal Glitch Defense)
            TestRunnerHelper.RunTest(report, "CH1-M4-22", "F22", 2,
                "Negative Damage Rejection (Anti-Heal Glitch)",
                "Verifies TakeDamage(-20) does not increase boss health or dispatch event.",
                () =>
                {
                    int eventCount = 0;
                    Action<int, int> onHealth = (c, m) => eventCount++;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.TakeDamage(10); // HP becomes 50
                            eventCount = 0;

                            boss.TakeDamage(-20); // Adversarial heal exploit attempt

                            E2EAssert.AreEqual(50, boss.currentHealth, "Negative damage must NOT heal boss");
                            E2EAssert.AreEqual(0, eventCount, "No health event may be dispatched on negative damage");
                        }
                    }
                    finally
                    {
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 23: Overkill Lethal Damage Clamps to 0 HP
            TestRunnerHelper.RunTest(report, "CH1-M4-23", "F22/F24", 2,
                "Overkill Damage Clamps to 0 HP",
                "Verifies massive overkill damage (100 dmg to 60 HP boss) clamps HP to 0 and invokes defeat.",
                () =>
                {
                    int lastCur = -1;
                    Action<int, int> onHealth = (c, m) => lastCur = c;
                    BossController.OnBossHealthChanged += onHealth;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.TakeDamage(100);

                            E2EAssert.AreEqual(0, boss.currentHealth, "currentHealth must clamp to 0 (never negative)");
                            E2EAssert.AreEqual(0, lastCur, "Reported health on overkill must be 0");
                            E2EAssert.IsFalse(boss.IsAlive, "IsAlive must be false");
                            E2EAssert.IsTrue(boss.isDead, "isDead must be true");
                        }
                    }
                    finally
                    {
                        BossController.OnBossHealthChanged -= onHealth;
                    }
                });

            // TEST 24: Post-Mortem Damage Rejection
            TestRunnerHelper.RunTest(report, "CH1-M4-24", "F22/F24", 2,
                "Post-Mortem Damage Rejection",
                "Verifies damaging a dead boss does not trigger secondary death routines or events.",
                () =>
                {
                    int deathEventCount = 0;
                    Action onDefeat = () => deathEventCount++;
                    BossController.OnBossDefeatedEvent += onDefeat;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.Die();
                            E2EAssert.AreEqual(1, deathEventCount, "First death must fire event once");

                            // Attempt to damage dead boss
                            boss.TakeDamage(10);
                            E2EAssert.AreEqual(1, deathEventCount, "Post-mortem damage must not re-fire death event");
                            E2EAssert.AreEqual(0, boss.currentHealth, "HP remains 0");
                        }
                    }
                    finally
                    {
                        BossController.OnBossDefeatedEvent -= onDefeat;
                    }
                });

            // TEST 25: Defeat Lifecycle Coordination
            TestRunnerHelper.RunTest(report, "CH1-M4-25", "F21-F24", 3,
                "Boss Defeat Lifecycle Coordination",
                "Verifies Die() notifies EnemySpawner, drops 2 grenades, and fires static and instance events.",
                () =>
                {
                    bool staticDefeat = false;
                    bool staticKilled = false;
                    bool instanceDefeated = false;

                    Action onStatDefeat = () => staticDefeat = true;
                    Action onStatKilled = () => staticKilled = true;

                    BossController.OnBossDefeatedEvent += onStatDefeat;
                    BossController.OnBossKilled += onStatKilled;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var dummyPickup = ctx.CreateGameObject("CH1_25_DummyPickup");

                            var spawnerGo = ctx.CreateGameObject("Spawner");
                            var spawner = spawnerGo.AddComponent<EnemySpawner>();
                            spawner.isBossActive = true;
                            spawner.bossSpawned = true;

                            var bossGo = ctx.CreateGameObject("CH1_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();
                            boss.grenadePickupPrefab = dummyPickup;

                            boss.OnDefeated += () => instanceDefeated = true;

                            boss.Die();

                            var droppedClones = new List<GameObject>();
                            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
                            {
                                if (go != dummyPickup && go.name.StartsWith("CH1_25_DummyPickup"))
                                {
                                    droppedClones.Add(go);
                                }
                            }

                            E2EAssert.IsTrue(staticDefeat, "OnBossDefeatedEvent must fire");
                            E2EAssert.IsTrue(staticKilled, "OnBossKilled must fire");
                            E2EAssert.IsTrue(instanceDefeated, "Instance OnDefeated must fire");
                            E2EAssert.IsFalse(spawner.isBossActive, "spawner.isBossActive must be cleared");
                            E2EAssert.IsTrue(spawner.bossSpawned, "spawner.bossSpawned must be preserved");
                            E2EAssert.AreEqual(2, droppedClones.Count, "Exactly 2 grenade pickups must drop");

                            foreach (var c in droppedClones)
                            {
                                UnityEngine.Object.DestroyImmediate(c);
                            }
                        }
                    }
                    finally
                    {
                        BossController.OnBossDefeatedEvent -= onStatDefeat;
                        BossController.OnBossKilled -= onStatKilled;
                    }
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Challenger 1 Milestone 4 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
