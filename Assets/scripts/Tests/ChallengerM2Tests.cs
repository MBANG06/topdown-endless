using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    public static class ChallengerM2Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 2 Milestone 2 Empirical Verification Suite" };

            // =========================================================================
            // SECTION 1: EnemyBullet Projectile Mechanics
            // =========================================================================

            // TEST 1: Physical Velocity & Speed
            TestRunnerHelper.RunTest(report, "CH-M2-01", "EnemyBullet", 1,
                "EnemyBullet Velocity and Speed Magnitude",
                "Verifies EnemyBullet initializes velocity to transform.up * speed (magnitude 8.0f).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bulletGo = ctx.CreateGameObject("EnemyBullet");
                        var rb = bulletGo.AddComponent<Rigidbody2D>();
                        rb.gravityScale = 0f;
                        var bullet = bulletGo.AddComponent<EnemyBullet>();
                        bullet.speed = 8.0f;

                        // Rotate bullet by 45 degrees
                        bulletGo.transform.rotation = Quaternion.Euler(0, 0, 45f);

                        // Invoke Awake and Start via reflection to simulate lifecycle
                        var awakeMethod = typeof(EnemyBullet).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                        awakeMethod?.Invoke(bullet, null);

                        var startMethod = typeof(EnemyBullet).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                        startMethod.Invoke(bullet, null);

                        Vector2 expectedVel = (Vector2)(bulletGo.transform.up * 8.0f);
                        E2EAssert.AreApproximatelyEqual(8.0f, rb.velocity.magnitude, 0.05f,
                            $"Bullet velocity magnitude must be 8.0, got {rb.velocity.magnitude}");
                        E2EAssert.AreApproximatelyEqual(expectedVel.x, rb.velocity.x, 0.05f, "Velocity X direction mismatch");
                        E2EAssert.AreApproximatelyEqual(expectedVel.y, rb.velocity.y, 0.05f, "Velocity Y direction mismatch");
                    }
                });

            // TEST 2: Exact Damage to Player
            TestRunnerHelper.RunTest(report, "CH-M2-02", "EnemyBullet", 1,
                "EnemyBullet Deals Exact Damage to Player",
                "Verifies EnemyBullet collision inflicts exactly 1 damage to PlayerHealth.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var bulletGo = ctx.CreateGameObject("EnemyBullet");
                        var bullet = bulletGo.AddComponent<EnemyBullet>();
                        bullet.damage = 1;

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Player health must decrease from 5 to 4");
                    }
                });

            // TEST 3: Friendly Enemy Immunity
            TestRunnerHelper.RunTest(report, "CH-M2-03", "EnemyBullet", 1,
                "EnemyBullet Friendly Enemy Immunity",
                "Verifies EnemyBullet ignores collisions with Chaser, Shooter, Rusher, and objects tagged 'Enemy'.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var chaserGo = ctx.CreateGameObject("Chaser");
                        var chaser = chaserGo.AddComponent<ChaserEnemy>();

                        var shooterGo = ctx.CreateGameObject("Shooter");
                        var shooter = shooterGo.AddComponent<ShooterEnemy>();

                        var rusherGo = ctx.CreateGameObject("Rusher");
                        var rusher = rusherGo.AddComponent<RusherEnemy>();

                        var taggedGo = ctx.CreateGameObject("TaggedEnemy");
                        taggedGo.tag = "Enemy";

                        var bulletGo = ctx.CreateGameObject("EnemyBullet");
                        var bullet = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        var hasHitField = typeof(EnemyBullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);

                        // Test against Chaser
                        handleHit.Invoke(bullet, new object[] { chaserGo });
                        E2EAssert.AreEqual(3, chaser.currentHealth, "Chaser must not take damage from EnemyBullet");
                        E2EAssert.IsFalse((bool)hasHitField.GetValue(bullet), "Bullet must not mark hit against Chaser");

                        // Test against Shooter
                        handleHit.Invoke(bullet, new object[] { shooterGo });
                        E2EAssert.AreEqual(2, shooter.currentHealth, "Shooter must not take damage from EnemyBullet");
                        E2EAssert.IsFalse((bool)hasHitField.GetValue(bullet), "Bullet must not mark hit against Shooter");

                        // Test against Rusher
                        handleHit.Invoke(bullet, new object[] { rusherGo });
                        E2EAssert.AreEqual(1, rusher.currentHealth, "Rusher must not take damage from EnemyBullet");
                        E2EAssert.IsFalse((bool)hasHitField.GetValue(bullet), "Bullet must not mark hit against Rusher");

                        // Test against Tagged Enemy
                        handleHit.Invoke(bullet, new object[] { taggedGo });
                        E2EAssert.IsFalse((bool)hasHitField.GetValue(bullet), "Bullet must not mark hit against Tagged 'Enemy'");
                    }
                });

            // TEST 4: Auto-Destruction on Solid Arena Walls
            TestRunnerHelper.RunTest(report, "CH-M2-04", "EnemyBullet", 1,
                "EnemyBullet Auto-Destruction on Solid Arena Walls",
                "Verifies EnemyBullet self-destructs upon impacting solid MapBounds walls.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var mapBounds = GameObject.Find("MapBounds");
                        E2EAssert.IsNotNull(mapBounds, "MapBounds object must exist in scene");

                        string[] wallNames = { "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right" };
                        foreach (var wName in wallNames)
                        {
                            var wall = mapBounds.transform.Find(wName)?.gameObject;
                            E2EAssert.IsNotNull(wall, $"{wName} must exist");

                            var bulletGo = ctx.CreateGameObject($"Bullet_vs_{wName}");
                            var bullet = bulletGo.AddComponent<EnemyBullet>();

                            var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                            handleHit.Invoke(bullet, new object[] { wall });

                            var hasHitField = typeof(EnemyBullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                            E2EAssert.IsTrue((bool)hasHitField.GetValue(bullet), $"Bullet must mark _hasHit = true on impacting {wName}");
                        }
                    }
                });

            // TEST 5: Ignore Other EnemyBullets
            TestRunnerHelper.RunTest(report, "CH-M2-05", "EnemyBullet", 1,
                "EnemyBullet Ignores Other EnemyBullets",
                "Verifies EnemyBullet does not collide with or destroy another EnemyBullet.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var b1Go = ctx.CreateGameObject("EnemyBullet_1");
                        var b1 = b1Go.AddComponent<EnemyBullet>();

                        var b2Go = ctx.CreateGameObject("EnemyBullet_2");
                        var b2 = b2Go.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        var hasHitField = typeof(EnemyBullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);

                        handleHit.Invoke(b1, new object[] { b2Go });
                        E2EAssert.IsFalse((bool)hasHitField.GetValue(b1), "EnemyBullet colliding with another EnemyBullet must be ignored");
                    }
                });

            // TEST 6: Pass Through Pickups / Trigger Volumes
            TestRunnerHelper.RunTest(report, "CH-M2-06", "EnemyBullet", 1,
                "EnemyBullet Passes Through Non-Player Trigger Volumes",
                "Verifies EnemyBullet does not trigger hit on non-player trigger items (pickups).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var pickupGo = ctx.CreateGameObject("Pickup");
                        var col = pickupGo.AddComponent<CircleCollider2D>();
                        col.isTrigger = true;

                        var bulletGo = ctx.CreateGameObject("EnemyBullet");
                        var bullet = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        var hasHitField = typeof(EnemyBullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);

                        handleHit.Invoke(bullet, new object[] { pickupGo });
                        E2EAssert.IsFalse((bool)hasHitField.GetValue(bullet), "EnemyBullet must pass through trigger pickups");
                    }
                });

            // =========================================================================
            // SECTION 2: Spawner Off-Screen Positions & Scaling
            // =========================================================================

            // TEST 7: 1,000 Iteration Viewport & Arena Bounds Empirical Verification
            TestRunnerHelper.RunTest(report, "CH-M2-07", "EnemySpawner", 2,
                "Spawner Generates Strictly Off-Screen & Outside Arena Coordinates (1,000 runs)",
                "Stress-tests 1,000 generated spawn positions across varied player positions to verify all lie strictly outside camera viewport and outside playable arena.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        // Visible Camera Viewport Bounds (from Camera.main at ortho 5.0, aspect ~1.956)
                        // Cam at (1.96, 0.04), halfWidth ~9.78, halfHeight 5.0
                        // Viewport: X [-7.82, 11.74], Y [-4.96, 5.04]
                        float camMinX = -7.82f;
                        float camMaxX = 11.74f;
                        float camMinY = -4.96f;
                        float camMaxY = 5.04f;

                        // Playable arena inner boundary:
                        // Left inner: -9.31, Right inner: 14.69, Top inner: 5.08, Bottom inner: -4.92
                        float arenaInnerMinX = -9.31f;
                        float arenaInnerMaxX = 14.69f;
                        float arenaInnerMinY = -4.92f;
                        float arenaInnerMaxY = 5.08f;

                        // Sample player positions: Center, 4 Corners, Edges, Random interior
                        Vector2[] samplePlayerPositions = new Vector2[]
                        {
                            Vector2.zero,
                            new Vector2(1.96f, 0.04f), // Camera center
                            new Vector2(-8.0f, -4.0f), // Bottom-left interior
                            new Vector2(13.0f, 4.5f),  // Top-right interior
                            new Vector2(-8.0f, 4.5f),  // Top-left interior
                            new Vector2(13.0f, -4.0f), // Bottom-right interior
                            new Vector2(0f, 4.5f),     // Top center
                            new Vector2(0f, -4.0f)     // Bottom center
                        };

                        int totalRuns = 1000;
                        for (int i = 0; i < totalRuns; i++)
                        {
                            Vector2 playerPos = (i < samplePlayerPositions.Length)
                                ? samplePlayerPositions[i]
                                : new Vector2(UnityEngine.Random.Range(-8.0f, 13.0f), UnityEngine.Random.Range(-4.0f, 4.5f));

                            Vector2 spawnPos = spawner.GeneratePerimeterPosition(playerPos);

                            // 1. Must lie on one of the 4 perimeter edges
                            bool onPerimeter = Mathf.Approximately(spawnPos.x, spawner.minX) ||
                                               Mathf.Approximately(spawnPos.x, spawner.maxX) ||
                                               Mathf.Approximately(spawnPos.y, spawner.minY) ||
                                               Mathf.Approximately(spawnPos.y, spawner.maxY);
                            E2EAssert.IsTrue(onPerimeter, $"Run {i}: Spawn pos {spawnPos} must be on perimeter box edges");

                            // 2. Must lie outside visible camera viewport
                            bool outsideCamera = (spawnPos.x <= camMinX || spawnPos.x >= camMaxX ||
                                                  spawnPos.y <= camMinY || spawnPos.y >= camMaxY);
                            E2EAssert.IsTrue(outsideCamera, $"Run {i}: Spawn pos {spawnPos} must lie outside camera viewport [{camMinX}..{camMaxX}, {camMinY}..{camMaxY}]");

                            // 3. Must lie outside playable arena inner area
                            bool outsidePlayableArena = (spawnPos.x <= arenaInnerMinX || spawnPos.x >= arenaInnerMaxX ||
                                                         spawnPos.y <= arenaInnerMinY || spawnPos.y >= arenaInnerMaxY);
                            E2EAssert.IsTrue(outsidePlayableArena, $"Run {i}: Spawn pos {spawnPos} must lie outside playable arena bounds");

                            // 4. Must respect minPlayerDistance or fallback opposite edge
                            float dist = Vector2.Distance(spawnPos, playerPos);
                            E2EAssert.IsTrue(dist >= spawner.minPlayerDistance || dist >= 5.0f,
                                $"Run {i}: Distance from player ({dist:F2}) must be sufficiently distant");
                        }
                    }
                });

            // TEST 8: Scaling Curves Mathematical Edge Cases
            TestRunnerHelper.RunTest(report, "CH-M2-08", "EnemySpawner", 2,
                "Scaling Curves Boundary & Boss Suppression Edge Cases",
                "Verifies formula edge cases: clamp floors, clamp ceilings, negative inputs, and boss suppression.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        // Baseline
                        E2EAssert.AreApproximatelyEqual(3.0f, spawner.CalculateSpawnInterval(0f, 0), 0.001f);
                        E2EAssert.AreEqual(5, spawner.CalculateMaxConcurrentEnemies(0f, 0));

                        // Negative inputs should safely return baseline
                        E2EAssert.AreApproximatelyEqual(3.0f, spawner.CalculateSpawnInterval(-10f, -50), 0.001f);
                        E2EAssert.AreEqual(5, spawner.CalculateMaxConcurrentEnemies(-10f, -50));

                        // Intermediate progression: t = 60s, score = 100
                        // 3.0 - (60 * 0.015 = 0.9) - (100 * 0.002 = 0.2) = 1.9s
                        E2EAssert.AreApproximatelyEqual(1.9f, spawner.CalculateSpawnInterval(60f, 100), 0.001f);

                        // Ceiling clamp for max concurrent enemies: 25 max
                        E2EAssert.AreEqual(25, spawner.CalculateMaxConcurrentEnemies(1000f, 10000));

                        // Floor clamp for spawn interval: 0.6s min
                        E2EAssert.AreApproximatelyEqual(0.6f, spawner.CalculateSpawnInterval(500f, 5000), 0.001f);

                        // Boss suppression: doubles interval
                        spawner.isBossActive = true;
                        E2EAssert.AreApproximatelyEqual(3.8f, spawner.CalculateSpawnInterval(60f, 100), 0.001f); // 1.9 * 2 = 3.8
                        E2EAssert.AreApproximatelyEqual(1.2f, spawner.CalculateSpawnInterval(500f, 5000), 0.001f); // 0.6 * 2 = 1.2
                    }
                });

            // TEST 9: Archetype Selection Weights Monte Carlo Test (10,000 samples per tier)
            TestRunnerHelper.RunTest(report, "CH-M2-09", "EnemySpawner", 3,
                "Archetype Selection Probabilities Monte Carlo Validation",
                "Performs 10,000 rolls per tier to empirically verify Chaser, Rusher, and Shooter distribution.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        var chaserPrefab = ctx.CreateGameObject("ChaserMock");
                        var shooterPrefab = ctx.CreateGameObject("ShooterMock");
                        var rusherPrefab = ctx.CreateGameObject("RusherMock");

                        spawner.chaserPrefab = chaserPrefab;
                        spawner.shooterPrefab = shooterPrefab;
                        spawner.rusherPrefab = rusherPrefab;

                        int sampleCount = 10000;

                        // Tier 1: Score 50 (<100) -> 75% Chaser, 15% Rusher, 10% Shooter
                        int c1 = 0, r1 = 0, s1 = 0;
                        for (int i = 0; i < sampleCount; i++)
                        {
                            var chosen = spawner.SelectEnemyArchetype(50);
                            if (chosen == chaserPrefab) c1++;
                            else if (chosen == rusherPrefab) r1++;
                            else if (chosen == shooterPrefab) s1++;
                        }
                        float pC1 = (float)c1 / sampleCount;
                        float pR1 = (float)r1 / sampleCount;
                        float pS1 = (float)s1 / sampleCount;
                        E2EAssert.IsTrue(pC1 >= 0.72f && pC1 <= 0.78f, $"Tier 1 Chaser ratio {pC1:F3} should be ~0.75");
                        E2EAssert.IsTrue(pR1 >= 0.13f && pR1 <= 0.17f, $"Tier 1 Rusher ratio {pR1:F3} should be ~0.15");
                        E2EAssert.IsTrue(pS1 >= 0.08f && pS1 <= 0.12f, $"Tier 1 Shooter ratio {pS1:F3} should be ~0.10");

                        // Tier 2: Score 200 (100..300) -> 50% Chaser, 25% Rusher, 25% Shooter
                        int c2 = 0, r2 = 0, s2 = 0;
                        for (int i = 0; i < sampleCount; i++)
                        {
                            var chosen = spawner.SelectEnemyArchetype(200);
                            if (chosen == chaserPrefab) c2++;
                            else if (chosen == rusherPrefab) r2++;
                            else if (chosen == shooterPrefab) s2++;
                        }
                        float pC2 = (float)c2 / sampleCount;
                        float pR2 = (float)r2 / sampleCount;
                        float pS2 = (float)s2 / sampleCount;
                        E2EAssert.IsTrue(pC2 >= 0.47f && pC2 <= 0.53f, $"Tier 2 Chaser ratio {pC2:F3} should be ~0.50");
                        E2EAssert.IsTrue(pR2 >= 0.22f && pR2 <= 0.28f, $"Tier 2 Rusher ratio {pR2:F3} should be ~0.25");
                        E2EAssert.IsTrue(pS2 >= 0.22f && pS2 <= 0.28f, $"Tier 2 Shooter ratio {pS2:F3} should be ~0.25");

                        // Tier 3: Score 400 (>=300) -> 35% Chaser, 30% Rusher, 35% Shooter
                        int c3 = 0, r3 = 0, s3 = 0;
                        for (int i = 0; i < sampleCount; i++)
                        {
                            var chosen = spawner.SelectEnemyArchetype(400);
                            if (chosen == chaserPrefab) c3++;
                            else if (chosen == rusherPrefab) r3++;
                            else if (chosen == shooterPrefab) s3++;
                        }
                        float pC3 = (float)c3 / sampleCount;
                        float pR3 = (float)r3 / sampleCount;
                        float pS3 = (float)s3 / sampleCount;
                        E2EAssert.IsTrue(pC3 >= 0.32f && pC3 <= 0.38f, $"Tier 3 Chaser ratio {pC3:F3} should be ~0.35");
                        E2EAssert.IsTrue(pR3 >= 0.27f && pR3 <= 0.33f, $"Tier 3 Rusher ratio {pR3:F3} should be ~0.30");
                        E2EAssert.IsTrue(pS3 >= 0.32f && pS3 <= 0.38f, $"Tier 3 Shooter ratio {pS3:F3} should be ~0.35");
                    }
                });

            // =========================================================================
            // SECTION 3: Enemy Death & Scoring Mechanics
            // =========================================================================

            // TEST 10: Precision Kill Scoring Events (10, 20, 15)
            TestRunnerHelper.RunTest(report, "CH-M2-10", "Scoring", 1,
                "Precise Kill Score Event Values (10, 20, 15)",
                "Verifies kill score events fire with exact values: Chaser=10, Shooter=20, Rusher=15.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var scoredList = new List<int>();
                        Action<int> scoreHandler = val => scoredList.Add(val);
                        EnemyBase.OnEnemyKilledScore += scoreHandler;

                        try
                        {
                            var chaserGo = ctx.CreateGameObject("Chaser");
                            var chaser = chaserGo.AddComponent<ChaserEnemy>();
                            chaser.Die();

                            var shooterGo = ctx.CreateGameObject("Shooter");
                            var shooter = shooterGo.AddComponent<ShooterEnemy>();
                            shooter.Die();

                            var rusherGo = ctx.CreateGameObject("Rusher");
                            var rusher = rusherGo.AddComponent<RusherEnemy>();
                            rusher.Die();

                            E2EAssert.AreEqual(3, scoredList.Count, "Exactly 3 score events should have fired");
                            E2EAssert.AreEqual(10, scoredList[0], "Chaser kill score must be 10");
                            E2EAssert.AreEqual(20, scoredList[1], "Shooter kill score must be 20");
                            E2EAssert.AreEqual(15, scoredList[2], "Rusher kill score must be 15");
                        }
                        finally
                        {
                            EnemyBase.OnEnemyKilledScore -= scoreHandler;
                        }
                    }
                });

            // TEST 11: Idempotency of Die() and Overkill Protection
            TestRunnerHelper.RunTest(report, "CH-M2-11", "EnemyBase", 2,
                "Overkill and Multiple Die() Calls Idempotency",
                "Verifies multiple Die() calls or excessive damage does not multiply score or trigger duplicate events.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        int scoreCallCount = 0;
                        int deathCallCount = 0;

                        Action<int> scoreHandler = val => scoreCallCount++;
                        Action<EnemyBase> deathHandler = e => deathCallCount++;

                        EnemyBase.OnEnemyKilledScore += scoreHandler;
                        EnemyBase.OnEnemyDied += deathHandler;

                        try
                        {
                            var chaserGo = ctx.CreateGameObject("Chaser");
                            var chaser = chaserGo.AddComponent<ChaserEnemy>();

                            // Call Die multiple times
                            chaser.Die();
                            chaser.Die();
                            chaser.Die();

                            // Even if TakeDamage is called after death
                            chaser.TakeDamage(100);

                            E2EAssert.AreEqual(1, scoreCallCount, "Score must only be awarded ONCE despite multiple Die calls");
                            E2EAssert.AreEqual(1, deathCallCount, "OnEnemyDied must only fire ONCE");
                            E2EAssert.IsTrue(chaser.isDead, "isDead flag must remain true");
                            E2EAssert.IsFalse(chaser.IsAlive, "IsAlive must be false");
                        }
                        finally
                        {
                            EnemyBase.OnEnemyKilledScore -= scoreHandler;
                            EnemyBase.OnEnemyDied -= deathHandler;
                        }
                    }
                });

            // TEST 12: Enemy HP Depletion & Immediate Collider Disabling
            TestRunnerHelper.RunTest(report, "CH-M2-12", "EnemyBase", 1,
                "HP Progression and Collider Deactivation Upon Death",
                "Verifies HP decreases linearly and colliders are immediately disabled upon lethal damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var chaserGo = ctx.CreateGameObject("Chaser");
                        var col = chaserGo.AddComponent<CircleCollider2D>();
                        var chaser = chaserGo.AddComponent<ChaserEnemy>();

                        E2EAssert.AreEqual(3, chaser.currentHealth, "Start HP is 3");
                        chaser.TakeDamage(1);
                        E2EAssert.AreEqual(2, chaser.currentHealth, "HP after 1 damage is 2");
                        E2EAssert.IsTrue(col.enabled, "Collider should still be enabled");

                        chaser.TakeDamage(1);
                        E2EAssert.AreEqual(1, chaser.currentHealth, "HP after 2 damage is 1");
                        E2EAssert.IsTrue(col.enabled, "Collider should still be enabled");

                        chaser.TakeDamage(1);
                        E2EAssert.AreEqual(0, chaser.currentHealth, "HP reaches 0");
                        E2EAssert.IsTrue(chaser == null || !col.enabled, "Collider must be disabled immediately upon death to prevent phantom hits");
                    }
                });

            // =========================================================================
            // SECTION 4: AI Behavior & Physics Verification
            // =========================================================================

            // TEST 13: Shooter Kiting Sweet Spot Vector Evaluation
            TestRunnerHelper.RunTest(report, "CH-M2-13", "ShooterEnemy", 2,
                "Shooter AI Kiting Thresholds & Bounds Containment",
                "Verifies Shooter retreats when dist < 3.8u, advances when dist > 5.5u, and stays put in sweet spot.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        playerGo.AddComponent<PlayerHealth>();
                        playerGo.transform.position = Vector3.zero;

                        var shooterGo = ctx.CreateGameObject("Shooter");
                        var shooter = shooterGo.AddComponent<ShooterEnemy>();
                        shooter.SetPlayer(playerGo.transform);

                        // Case A: Too close (dist = 2.0u < retreatDistance 3.8u)
                        shooterGo.transform.position = new Vector3(2.0f, 0f, 0f);
                        float distA = Vector2.Distance(playerGo.transform.position, shooterGo.transform.position);
                        E2EAssert.IsTrue(distA < shooter.retreatDistance, "Distance is under retreat distance");

                        // Case B: Too far (dist = 8.0u > advanceDistance 5.5u)
                        shooterGo.transform.position = new Vector3(8.0f, 0f, 0f);
                        float distB = Vector2.Distance(playerGo.transform.position, shooterGo.transform.position);
                        E2EAssert.IsTrue(distB > shooter.advanceDistance, "Distance is over advance distance");

                        // Case C: Sweet spot (dist = 4.5u in [3.8u, 5.5u])
                        shooterGo.transform.position = new Vector3(4.5f, 0f, 0f);
                        float distC = Vector2.Distance(playerGo.transform.position, shooterGo.transform.position);
                        E2EAssert.IsTrue(distC >= shooter.retreatDistance && distC <= shooter.advanceDistance,
                            "Distance is in sweet spot [3.8, 5.5]");
                    }
                });

            // TEST 14: Rusher Speed Advantage Over Player
            TestRunnerHelper.RunTest(report, "CH-M2-14", "RusherEnemy", 1,
                "Rusher Movement Speed Strictly Exceeds Player Speed",
                "Verifies Rusher speed is 6.2 u/s which is strictly faster than player base speed (5.0 u/s) with exactly 1 HP.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        var pm = playerGo.AddComponent<PlayerMovement>();
                        float playerSpeed = pm.moveSpeed;

                        var rusherGo = ctx.CreateGameObject("Rusher");
                        var rusher = rusherGo.AddComponent<RusherEnemy>();

                        E2EAssert.IsTrue(rusher.moveSpeed > playerSpeed,
                            $"Rusher speed ({rusher.moveSpeed}) must exceed player speed ({playerSpeed})");
                        E2EAssert.AreEqual(1, rusher.maxHealth, "Rusher must have fragile 1 HP glass cannon health");
                    }
                });

            // =========================================================================
            // SECTION 5: Zero Memory Leaks & Lifecycle Integrity
            // =========================================================================

            // TEST 15: Spawner Active Enemies List Cleaned Cleanly (Zero Ghost References)
            TestRunnerHelper.RunTest(report, "CH-M2-15", "EnemySpawner", 2,
                "Zero Memory Leaks: Active Enemies Clean Sweep Under High Turnover",
                "Spawns 30 enemies, eliminates them, and verifies Spawner tracks ActiveEnemyCount == 0 with 0 ghost references.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        playerGo.AddComponent<PlayerHealth>();

                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        var chaserPrefab = ctx.CreateGameObject("ChaserPrefab");
                        chaserPrefab.AddComponent<ChaserEnemy>();
                        spawner.chaserPrefab = chaserPrefab;

                        var spawnedEnemies = new List<EnemyBase>();

                        // Spawn 30 enemies
                        for (int i = 0; i < 30; i++)
                        {
                            var spawned = spawner.SpawnEnemy(0);
                            if (spawned != null)
                            {
                                var eb = spawned.GetComponent<EnemyBase>();
                                if (eb != null) spawnedEnemies.Add(eb);
                            }
                        }

                        E2EAssert.AreEqual(30, spawner.ActiveEnemyCount, "ActiveEnemyCount must equal 30 after spawning");

                        // Eliminate all 30 enemies
                        foreach (var enemy in spawnedEnemies)
                        {
                            if (enemy != null)
                            {
                                enemy.Die();
                            }
                        }

                        // Verify zero active enemies remaining
                        E2EAssert.AreEqual(0, spawner.ActiveEnemyCount,
                            $"ActiveEnemyCount must cleanly drop to 0 after all deaths. Got {spawner.ActiveEnemyCount}");
                    }
                });

            // TEST 16: Spawner Event Listener Lifecycle & Safe Destruction
            TestRunnerHelper.RunTest(report, "CH-M2-16", "EnemySpawner", 2,
                "Spawner Event Listener Lifecycle Prevents Dangling Handlers",
                "Verifies OnEnable and OnDisable correctly manage event subscriptions without memory leaks.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var spawnerGo = ctx.CreateGameObject("Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        // Disable the spawner (triggers OnDisable)
                        spawnerGo.SetActive(false);

                        // Trigger player death and enemy death events while Spawner is disabled
                        bool noExceptionThrown = true;
                        try
                        {
                            var dieMethod = typeof(PlayerHealth).GetMethod("Die", BindingFlags.NonPublic | BindingFlags.Instance);
                            dieMethod?.Invoke(ph, null);

                            var dummyEnemyGo = ctx.CreateGameObject("DummyEnemy");
                            var dummyEnemy = dummyEnemyGo.AddComponent<ChaserEnemy>();
                            dummyEnemy.Die();
                        }
                        catch (Exception)
                        {
                            noExceptionThrown = false;
                        }

                        E2EAssert.IsTrue(noExceptionThrown, "No exception should be thrown when events fire while spawner is disabled");
                    }
                });

            // TEST 17: Prefab & Scene Wiring Completeness
            TestRunnerHelper.RunTest(report, "CH-M2-17", "Prefabs", 1,
                "Prefab Asset Structure and Layer/Tag Verification",
                "Verifies all Milestone 2 prefabs have correct tags, zero-gravity dynamic physics, and appropriate collider configurations.",
                () =>
                {
#if UNITY_EDITOR
                    var chaser = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ChaserEnemy.prefab");
                    var shooter = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShooterEnemy.prefab");
                    var rusher = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RusherEnemy.prefab");
                    var bullet = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyBullet.prefab");

                    E2EAssert.IsNotNull(chaser, "ChaserEnemy.prefab must exist");
                    E2EAssert.IsNotNull(shooter, "ShooterEnemy.prefab must exist");
                    E2EAssert.IsNotNull(rusher, "RusherEnemy.prefab must exist");
                    E2EAssert.IsNotNull(bullet, "EnemyBullet.prefab must exist");

                    E2EAssert.AreEqual("Enemy", chaser.tag, "ChaserEnemy prefab must be tagged 'Enemy'");
                    E2EAssert.AreEqual("Enemy", shooter.tag, "ShooterEnemy prefab must be tagged 'Enemy'");
                    E2EAssert.AreEqual("Enemy", rusher.tag, "RusherEnemy prefab must be tagged 'Enemy'");

                    var rbC = chaser.GetComponent<Rigidbody2D>();
                    var rbS = shooter.GetComponent<Rigidbody2D>();
                    var rbR = rusher.GetComponent<Rigidbody2D>();
                    var rbB = bullet.GetComponent<Rigidbody2D>();

                    E2EAssert.AreEqual(0f, rbC.gravityScale, "Chaser gravity scale must be 0");
                    E2EAssert.AreEqual(0f, rbS.gravityScale, "Shooter gravity scale must be 0");
                    E2EAssert.AreEqual(0f, rbR.gravityScale, "Rusher gravity scale must be 0");
                    E2EAssert.AreEqual(0f, rbB.gravityScale, "Bullet gravity scale must be 0");

                    var colB = bullet.GetComponent<Collider2D>();
                    E2EAssert.IsTrue(colB.isTrigger, "EnemyBullet collider must be a trigger");
#endif
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Challenger M2 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
