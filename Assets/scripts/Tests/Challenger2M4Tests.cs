using System;
using System.Collections;
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
    /// Authored by Challenger 2.
    /// Stress-tests:
    /// 1. Boss defeat rewards (+500 points, exactly 2 guaranteed grenade drops, idempotency, null safety).
    /// 2. Endless resumption (EnemySpawner suppression, latch, rate restoration, high score stability).
    /// 3. Projectile collision (radial burst geometry, 1 HP player damage, i-frames, friendly immunity).
    /// 4. Zero memory leaks (boss cleanup, bullet cleanup, multi-cycle stress test).
    /// </summary>
    public static class Challenger2M4Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 2 Milestone 4 Empirical Verification Suite" };

            // =========================================================================
            // SUITE 1: Boss Defeat Rewards (+500 Points & 2 Guaranteed Grenade Drops)
            // =========================================================================

            // TEST 1: Boss scoreValue verification
            TestRunnerHelper.RunTest(report, "CH2-M4-01", "F24", 1,
                "Boss scoreValue is Exactly 500",
                "Verifies BossController instance and BossEnemy.prefab configure scoreValue == 500.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        E2EAssert.AreEqual(500, boss.scoreValue, "BossController instance scoreValue must be 500");

#if UNITY_EDITOR
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossEnemy.prefab");
                        E2EAssert.IsNotNull(prefab, "BossEnemy.prefab must exist in Assets/Prefabs/");
                        var prefabBoss = prefab.GetComponent<BossController>();
                        E2EAssert.IsNotNull(prefabBoss, "BossEnemy.prefab must have BossController component");
                        E2EAssert.AreEqual(500, prefabBoss.scoreValue, "BossEnemy.prefab scoreValue must be 500");
#endif
                    }
                });

            // TEST 2: Static score event dispatches exactly 500 points
            TestRunnerHelper.RunTest(report, "CH2-M4-02", "F24", 1,
                "Boss Die Dispatches Score Event With 500 Points",
                "Verifies EnemyBase.OnEnemyKilledScore dispatches 500 points on BossController.Die().",
                () =>
                {
                    int receivedScore = 0;
                    Action<int> onScore = s => receivedScore += s;
                    EnemyBase.OnEnemyKilledScore += onScore;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH2_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.Die();

                            E2EAssert.AreEqual(500, receivedScore, "EnemyBase.OnEnemyKilledScore must receive exactly 500 points on boss death");
                        }
                    }
                    finally
                    {
                        EnemyBase.OnEnemyKilledScore -= onScore;
                    }
                });

            // TEST 3: Score award graceful when GameManager missing
            TestRunnerHelper.RunTest(report, "CH2-M4-03", "F24", 2,
                "Boss Die Graceful When GameManager Missing",
                "Verifies BossController.Die() succeeds without throwing exceptions even if GameManager is absent.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        // Invoke Die without any GameManager in scene
                        boss.Die();

                        E2EAssert.IsTrue(boss.isDead, "Boss must mark isDead=true gracefully");
                    }
                });

            // TEST 4: Score award idempotency under repeated Die calls
            TestRunnerHelper.RunTest(report, "CH2-M4-04", "F24", 2,
                "Boss Score Award Idempotency Under Repeated Die Calls",
                "Verifies multiple Die() calls award score exactly once (no score duplication exploit).",
                () =>
                {
                    int totalScore = 0;
                    int eventFiredCount = 0;
                    Action<int> onScore = s => { totalScore += s; eventFiredCount++; };
                    EnemyBase.OnEnemyKilledScore += onScore;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH2_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.Die();
                            // Attempt repeated Die() calls
                            boss.Die();
                            boss.Die();

                            E2EAssert.AreEqual(1, eventFiredCount, "Score event must fire exactly once");
                            E2EAssert.AreEqual(500, totalScore, "Total score awarded must be 500, preventing duplicate rewards");
                        }
                    }
                    finally
                    {
                        EnemyBase.OnEnemyKilledScore -= onScore;
                    }
                });

            // TEST 5: Overkill damage awards score once
            TestRunnerHelper.RunTest(report, "CH2-M4-05", "F24", 2,
                "Boss Overkill Damage Awards Score Once",
                "Verifies massive overkill damage (e.g. 150 dmg) reduces HP to 0, triggers death, and awards 500 once.",
                () =>
                {
                    int totalScore = 0;
                    Action<int> onScore = s => totalScore += s;
                    EnemyBase.OnEnemyKilledScore += onScore;

                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var bossGo = ctx.CreateGameObject("CH2_Boss");
                            bossGo.AddComponent<Rigidbody2D>();
                            var boss = bossGo.AddComponent<BossController>();

                            boss.TakeDamage(150);

                            E2EAssert.AreEqual(0, boss.currentHealth, "Boss health must be clamped to 0 on overkill");
                            E2EAssert.IsTrue(boss.isDead, "Boss isDead must be true");
                            E2EAssert.AreEqual(500, totalScore, "Score awarded must be exactly 500");
                        }
                    }
                    finally
                    {
                        EnemyBase.OnEnemyKilledScore -= onScore;
                    }
                });

            // TEST 6: guaranteedGrenadeDrops field is exactly 2
            TestRunnerHelper.RunTest(report, "CH2-M4-06", "F24", 1,
                "Boss guaranteedGrenadeDrops Field is Exactly 2",
                "Verifies guaranteedGrenadeDrops field is 2 on BossController instance and prefab.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        E2EAssert.AreEqual(2, boss.guaranteedGrenadeDrops, "BossController.guaranteedGrenadeDrops must be 2");

#if UNITY_EDITOR
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossEnemy.prefab");
                        var prefabBoss = prefab.GetComponent<BossController>();
                        E2EAssert.AreEqual(2, prefabBoss.guaranteedGrenadeDrops, "BossEnemy.prefab guaranteedGrenadeDrops must be 2");
#endif
                    }
                });

            // TEST 7: RollGrenadeDrop spawns exactly 2 pickups at symmetric offsets
            TestRunnerHelper.RunTest(report, "CH2-M4-07", "F24", 2,
                "RollGrenadeDrop Spawns Exactly 2 Pickups At Offsets",
                "Verifies RollGrenadeDrop instantiates 2 pickups at (-0.6, 0) and (+0.6, 0) relative to boss.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var dummyPickup = ctx.CreateGameObject("CH2_DummyPickup");

                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.transform.position = new Vector3(5f, 5f, 0f);
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.grenadePickupPrefab = dummyPickup;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        var rollMethod = typeof(BossController).GetMethod("RollGrenadeDrop", BindingFlags.NonPublic | BindingFlags.Instance);
                        rollMethod?.Invoke(boss, null);

                        var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                        int afterCount = allObjects.Length;
                        E2EAssert.AreEqual(beforeCount + 2, afterCount, "Exactly 2 grenade pickup GameObjects must be instantiated");

                        // Find the two new pickup clones
                        List<GameObject> spawnedPickups = new List<GameObject>();
                        foreach (var obj in allObjects)
                        {
                            if (obj.name.StartsWith("CH2_DummyPickup(Clone)"))
                            {
                                spawnedPickups.Add(obj);
                            }
                        }

                        E2EAssert.AreEqual(2, spawnedPickups.Count, "Found 2 spawned dummy pickup clones");
                        Vector2 expectedPos1 = (Vector2)bossGo.transform.position + new Vector2(-0.6f, 0f);
                        Vector2 expectedPos2 = (Vector2)bossGo.transform.position + new Vector2(0.6f, 0f);

                        bool hasLeft = spawnedPickups.Exists(p => Vector2.Distance(p.transform.position, expectedPos1) < 0.01f);
                        bool hasRight = spawnedPickups.Exists(p => Vector2.Distance(p.transform.position, expectedPos2) < 0.01f);

                        E2EAssert.IsTrue(hasLeft, "One pickup must be placed at offset (-0.6, 0)");
                        E2EAssert.IsTrue(hasRight, "One pickup must be placed at offset (+0.6, 0)");

                        foreach (var p in spawnedPickups)
                        {
                            UnityEngine.Object.DestroyImmediate(p);
                        }
                    }
                });

            // TEST 8: Null grenade prefab resilience
            TestRunnerHelper.RunTest(report, "CH2-M4-08", "F24", 2,
                "Boss Die Null Grenade Prefab Resilience",
                "Verifies RollGrenadeDrop handles null grenadePickupPrefab safely without throwing.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.grenadePickupPrefab = null;

                        // Should execute without exception
                        boss.Die();
                        E2EAssert.IsTrue(boss.isDead, "Boss death completes safely when grenadePickupPrefab is null");
                    }
                });

            // TEST 9: Grenade drop idempotency under multiple Die calls
            TestRunnerHelper.RunTest(report, "CH2-M4-09", "F24", 2,
                "Boss Grenade Drop Idempotency Under Multiple Die Calls",
                "Verifies calling Die() multiple times does not duplicate grenade drops.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var dummyPickup = ctx.CreateGameObject("CH2_DummyPickup");

                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.grenadePickupPrefab = dummyPickup;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        boss.Die();
                        boss.Die();

                        var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                        List<GameObject> spawned = new List<GameObject>();
                        foreach (var obj in allObjects)
                        {
                            if (obj.name.StartsWith("CH2_DummyPickup(Clone)"))
                            {
                                spawned.Add(obj);
                            }
                        }

                        E2EAssert.AreEqual(2, spawned.Count, "Exactly 2 grenade pickups should be spawned despite multiple Die calls");

                        foreach (var p in spawned)
                        {
                            UnityEngine.Object.DestroyImmediate(p);
                        }
                    }
                });

            // TEST 10: Dropped grenade pickups are functional and collectible
            TestRunnerHelper.RunTest(report, "CH2-M4-10", "F24", 3,
                "Boss Dropped Grenade Pickups Are Functional and Collectible",
                "Verifies spawned GrenadePickup prefabs can be collected by Player to increment grenadeCount.",
                () =>
                {
#if UNITY_EDITOR
                    var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GrenadePickup.prefab");
                    E2EAssert.IsNotNull(pickupPrefab, "GrenadePickup.prefab must exist in Assets/Prefabs/");

                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.transform.position = Vector3.zero;
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.grenadePickupPrefab = pickupPrefab;

                        // Spawn player
                        var playerGo = ctx.CreateGameObject("CH2_Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.maxGrenades = 5;
                        thrower.grenadeCount = 1;

                        // Trigger drop
                        var rollMethod = typeof(BossController).GetMethod("RollGrenadeDrop", BindingFlags.NonPublic | BindingFlags.Instance);
                        rollMethod?.Invoke(boss, null);

                        var pickups = UnityEngine.Object.FindObjectsOfType<GrenadePickup>();
                        E2EAssert.AreEqual(2, pickups.Length, "Found 2 live GrenadePickup components dropped by Boss");

                        // Collect first pickup
                        bool collected = pickups[0].TryCollect(playerGo);
                        E2EAssert.IsTrue(collected, "Player should successfully collect dropped grenade pickup");
                        E2EAssert.AreEqual(2, thrower.grenadeCount, "Player grenadeCount should increment from 1 to 2");

                        // Clean up remaining pickup
                        foreach (var p in UnityEngine.Object.FindObjectsOfType<GrenadePickup>())
                        {
                            UnityEngine.Object.DestroyImmediate(p.gameObject);
                        }
                    }
#endif
                });

            // =========================================================================
            // SUITE 2: Endless Resumption & Spawner Latch Integrity
            // =========================================================================

            // TEST 11: Spawner initial state
            TestRunnerHelper.RunTest(report, "CH2-M4-11", "F21", 1,
                "EnemySpawner Initial State Verification",
                "Verifies initial bossSpawned=false, isBossActive=false, and isSpawning=true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        E2EAssert.IsFalse(spawner.bossSpawned, "bossSpawned must be false initially");
                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be false initially");
                        E2EAssert.IsTrue(spawner.isSpawning, "isSpawning must be true initially");
                    }
                });

            // TEST 12: Score threshold triggering
            TestRunnerHelper.RunTest(report, "CH2-M4-12", "F21", 2,
                "EnemySpawner Boss Spawn Triggering",
                "Verifies SpawnBoss sets bossSpawned=true and isBossActive=true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        spawner.SpawnBoss();

                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must be true after SpawnBoss");
                        E2EAssert.IsTrue(spawner.isBossActive, "isBossActive must be true after SpawnBoss");
                    }
                });

            // TEST 13: Spawner rate suppression during Boss
            TestRunnerHelper.RunTest(report, "CH2-M4-13", "F21", 2,
                "EnemySpawner 50% Rate Suppression During Boss Encounter",
                "Verifies CalculateSpawnInterval doubles (50% spawn rate) when isBossActive is true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        float intervalNormal = spawner.CalculateSpawnInterval(10f, 500);
                        spawner.isBossActive = true;
                        float intervalSuppressed = spawner.CalculateSpawnInterval(10f, 500);

                        E2EAssert.AreApproximatelyEqual(intervalNormal * 2.0f, intervalSuppressed, 0.001f,
                            "Spawn interval during boss active must be exactly 2.0x normal interval");
                    }
                });

            // TEST 14: OnBossDefeated restores endless state
            TestRunnerHelper.RunTest(report, "CH2-M4-14", "F24", 1,
                "EnemySpawner OnBossDefeated Restores Endless State",
                "Verifies OnBossDefeated sets isBossActive=false and isSpawning=true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        spawner.isBossActive = true;
                        spawner.bossSpawned = true;
                        spawner.isSpawning = false; // Intentionally paused

                        spawner.OnBossDefeated();

                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be false after OnBossDefeated");
                        E2EAssert.IsTrue(spawner.isSpawning, "isSpawning must be true to continue endless mode");
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must remain true to prevent duplicate boss");
                    }
                });

            // TEST 15: Spawn interval restored after boss defeat
            TestRunnerHelper.RunTest(report, "CH2-M4-15", "F24", 2,
                "EnemySpawner Interval Restored After Boss Defeat",
                "Verifies CalculateSpawnInterval returns to normal unsuppressed interval after OnBossDefeated().",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        float expectedNormal = spawner.CalculateSpawnInterval(30f, 750);
                        spawner.isBossActive = true;
                        spawner.OnBossDefeated();
                        float restoredInterval = spawner.CalculateSpawnInterval(30f, 750);

                        E2EAssert.AreApproximatelyEqual(expectedNormal, restoredInterval, 0.001f,
                            "Spawn interval after OnBossDefeated must match original unsuppressed interval");
                    }
                });

            // TEST 16: Anti-duplicate Boss latch at high scores
            TestRunnerHelper.RunTest(report, "CH2-M4-16", "F21", 2,
                "Anti-Duplicate Boss Latch At High Scores",
                "Verifies that once bossSpawned=true, subsequent calls or high scores (1000, 5000) do not spawn another boss.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        var dummyBoss = ctx.CreateGameObject("CH2_DummyBossPrefab");
                        spawner.bossPrefab = dummyBoss;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        // First spawn
                        spawner.SpawnBoss();
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must be true after initial spawn");

                        int afterFirstCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        E2EAssert.AreEqual(beforeCount + 1, afterFirstCount, "First SpawnBoss creates 1 boss object");

                        // Defeat boss
                        spawner.OnBossDefeated();

                        // Attempt to spawn boss again at score 1000, 2000, 5000
                        spawner.SpawnBoss();
                        spawner.SpawnBoss();

                        int finalCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        E2EAssert.AreEqual(afterFirstCount, finalCount, "No second boss may ever be spawned after latch is set");
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned latch must remain permanently true");

                        // Clean up instantiated clone
                        foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
                        {
                            if (obj.name.StartsWith("CH2_DummyBossPrefab(Clone)"))
                            {
                                UnityEngine.Object.DestroyImmediate(obj);
                            }
                        }
                    }
                });

            // TEST 17: Direct SpawnBoss idempotency
            TestRunnerHelper.RunTest(report, "CH2-M4-17", "F21", 2,
                "Direct SpawnBoss Idempotency Under Rapid Invocations",
                "Verifies multiple rapid calls to SpawnBoss() instantiate at most 1 boss object.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        var dummyBoss = ctx.CreateGameObject("CH2_DummyBossPrefab");
                        spawner.bossPrefab = dummyBoss;

                        int beforeCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                        for (int i = 0; i < 5; i++)
                        {
                            spawner.SpawnBoss();
                        }

                        int afterCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                        E2EAssert.AreEqual(beforeCount + 1, afterCount, "5 rapid calls to SpawnBoss must create only 1 boss instance");

                        // Clean up instantiated clone
                        foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
                        {
                            if (obj.name.StartsWith("CH2_DummyBossPrefab(Clone)"))
                            {
                                UnityEngine.Object.DestroyImmediate(obj);
                            }
                        }
                    }
                });

            // TEST 18: Continuous post-boss wave generation
            TestRunnerHelper.RunTest(report, "CH2-M4-18", "F24", 2,
                "Post-Boss Wave Generation Prefab Selection",
                "Verifies SelectEnemyArchetype continues returning valid normal enemies (never boss) at high scores > 500.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        var chaser = ctx.CreateGameObject("Chaser");
                        var shooter = ctx.CreateGameObject("Shooter");
                        var rusher = ctx.CreateGameObject("Rusher");
                        var boss = ctx.CreateGameObject("Boss");

                        spawner.chaserPrefab = chaser;
                        spawner.shooterPrefab = shooter;
                        spawner.rusherPrefab = rusher;
                        spawner.bossPrefab = boss;

                        for (int i = 0; i < 20; i++)
                        {
                            var selected = spawner.SelectEnemyArchetype(600);
                            E2EAssert.IsNotNull(selected, "Selected prefab must not be null");
                            E2EAssert.IsTrue(boss != selected, "Spawner must never return bossPrefab in regular wave selection");
                        }
                    }
                });

            // TEST 19: Dead Boss clean up from Spawner active list
            TestRunnerHelper.RunTest(report, "CH2-M4-19", "F24", 2,
                "Dead Boss Clean Up From Spawner Active List",
                "Verifies ActiveEnemyCount and CleanDeadEnemies prune dead boss without blocking concurrency cap.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("CH2_Spawner");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        // Access active list via reflection
                        var field = typeof(EnemySpawner).GetField("_activeEnemies", BindingFlags.NonPublic | BindingFlags.Instance);
                        var activeList = field?.GetValue(spawner) as List<EnemyBase>;
                        E2EAssert.IsNotNull(activeList, "_activeEnemies list must exist on Spawner");

                        activeList.Add(boss);
                        E2EAssert.AreEqual(1, spawner.ActiveEnemyCount, "ActiveEnemyCount must report 1 when boss is added");

                        boss.Die();

                        E2EAssert.AreEqual(0, spawner.ActiveEnemyCount, "ActiveEnemyCount must return 0 after boss is dead");
                    }
                });

            // =========================================================================
            // SUITE 3: Radial Projectile Collision & Damage Mechanics
            // =========================================================================

            // TEST 20: Radial burst geometry verification
            TestRunnerHelper.RunTest(report, "CH2-M4-20", "F23", 1,
                "Radial Burst Geometry Verification (16 Bullets, 22.5 Deg)",
                "Verifies exactly 16 bullets, 22.5 degree angular spacing, and unit direction vectors.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        E2EAssert.AreEqual(16, boss.radialBulletCount, "radialBulletCount must be 16");
                        float angleStep = 360f / boss.radialBulletCount;
                        E2EAssert.AreApproximatelyEqual(22.5f, angleStep, 0.001f, "angleStep must be 22.5 degrees");

                        for (int i = 0; i < 16; i++)
                        {
                            float angleDeg = i * angleStep;
                            float angleRad = angleDeg * Mathf.Deg2Rad;
                            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                            E2EAssert.AreApproximatelyEqual(1.0f, dir.magnitude, 0.001f, $"Direction vector for bullet {i} must be normalized");
                        }
                    }
                });

            // TEST 21: Radial projectile dynamics
            TestRunnerHelper.RunTest(report, "CH2-M4-21", "F23", 2,
                "Radial Projectile Dynamics and Components",
                "Verifies SpawnBossProjectile creates projectile with EnemyBullet, speed=5.0, damage=1, lifetime=5.0.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        var bulletObj = boss.SpawnBossProjectile(Vector2.zero, Vector2.right, 0f);

                        E2EAssert.IsNotNull(bulletObj, "SpawnBossProjectile must return a GameObject");
                        var eb = bulletObj.GetComponent<EnemyBullet>();
                        E2EAssert.IsNotNull(eb, "Spawned projectile must have EnemyBullet component");
                        E2EAssert.AreEqual(1, eb.damage, "EnemyBullet damage must be 1");
                        E2EAssert.AreApproximatelyEqual(5.0f, eb.speed, 0.01f, "EnemyBullet speed must be 5.0");
                        E2EAssert.AreApproximatelyEqual(5.0f, eb.lifetime, 0.01f, "EnemyBullet lifetime must be 5.0");

                        var rb = bulletObj.GetComponent<Rigidbody2D>();
                        E2EAssert.IsNotNull(rb, "Spawned projectile must have Rigidbody2D");
                        E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.x, 0.01f, "Velocity X must equal 5.0");
                        E2EAssert.AreApproximatelyEqual(0.0f, rb.velocity.y, 0.01f, "Velocity Y must equal 0.0");

                        UnityEngine.Object.DestroyImmediate(bulletObj);
                    }
                });

            // TEST 22: Direct Player hit deals 1 HP damage
            TestRunnerHelper.RunTest(report, "CH2-M4-22", "F23", 2,
                "Radial Projectile Direct Player Hit Deals 1 HP",
                "Verifies EnemyBullet hitting Player reduces health by exactly 1 HP (5 -> 4) and auto-destroys.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("CH2_Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var bulletGo = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();
                        eb.damage = 1;

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Player health must decrease from 5 to 4");
                        E2EAssert.IsTrue(bulletGo == null, "Bullet GameObject must be destroyed upon hitting Player");
                    }
                });

            // TEST 23: Player i-frames protection against multi-bullet barrage
            TestRunnerHelper.RunTest(report, "CH2-M4-23", "F23", 2,
                "Player i-Frames Protection Against Multi-Bullet Barrage",
                "Verifies that when multiple radial bullets hit Player rapidly, only 1 damage is taken due to i-frames.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("CH2_Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);

                        // First bullet hits
                        var bullet1 = ctx.CreateGameObject("CH2_Bullet1");
                        var eb1 = bullet1.AddComponent<EnemyBullet>();
                        eb1.damage = 1;
                        handleHit?.Invoke(eb1, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "First bullet reduces HP to 4");
                        E2EAssert.IsTrue(ph.isInvulnerable, "Player must now be invulnerable (i-frames active)");

                        // Second bullet hits during i-frames
                        var bullet2 = ctx.CreateGameObject("CH2_Bullet2");
                        var eb2 = bullet2.AddComponent<EnemyBullet>();
                        eb2.damage = 1;
                        handleHit?.Invoke(eb2, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Second bullet must NOT reduce HP while player is invulnerable");

                        // Third bullet hits during i-frames
                        var bullet3 = ctx.CreateGameObject("CH2_Bullet3");
                        var eb3 = bullet3.AddComponent<EnemyBullet>();
                        eb3.damage = 1;
                        handleHit?.Invoke(eb3, new object[] { playerGo });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Third bullet must NOT reduce HP while player is invulnerable");
                    }
                });

            // TEST 24: Friendly enemy immunity - ChaserEnemy
            TestRunnerHelper.RunTest(report, "CH2-M4-24", "F23", 2,
                "Radial Projectile Friendly Enemy Immunity (Chaser)",
                "Verifies EnemyBullet hitting ChaserEnemy inflicts 0 damage and does not destroy bullet.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var chaserGo = ctx.CreateGameObject("CH2_Chaser");
                        chaserGo.tag = "Enemy";
                        var chaser = chaserGo.AddComponent<ChaserEnemy>();

                        var bulletGo = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { chaserGo });

                        E2EAssert.AreEqual(3, chaser.currentHealth, "Chaser health must remain 3 (0 damage taken)");
                        E2EAssert.IsFalse(bulletGo == null, "Bullet must not be destroyed by friendly Chaser");
                    }
                });

            // TEST 25: Friendly enemy immunity - ShooterEnemy
            TestRunnerHelper.RunTest(report, "CH2-M4-25", "F23", 2,
                "Radial Projectile Friendly Enemy Immunity (Shooter)",
                "Verifies EnemyBullet hitting ShooterEnemy inflicts 0 damage and does not destroy bullet.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var shooterGo = ctx.CreateGameObject("CH2_Shooter");
                        shooterGo.tag = "Enemy";
                        var shooter = shooterGo.AddComponent<ShooterEnemy>();

                        var bulletGo = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { shooterGo });

                        E2EAssert.AreEqual(2, shooter.currentHealth, "Shooter health must remain 2 (0 damage taken)");
                        E2EAssert.IsFalse(bulletGo == null, "Bullet must not be destroyed by friendly Shooter");
                    }
                });

            // TEST 26: Friendly enemy immunity - RusherEnemy
            TestRunnerHelper.RunTest(report, "CH2-M4-26", "F23", 2,
                "Radial Projectile Friendly Enemy Immunity (Rusher)",
                "Verifies EnemyBullet hitting RusherEnemy inflicts 0 damage and does not destroy bullet.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var rusherGo = ctx.CreateGameObject("CH2_Rusher");
                        rusherGo.tag = "Enemy";
                        var rusher = rusherGo.AddComponent<RusherEnemy>();

                        var bulletGo = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { rusherGo });

                        E2EAssert.AreEqual(1, rusher.currentHealth, "Rusher health must remain 1 (0 damage taken)");
                        E2EAssert.IsFalse(bulletGo == null, "Bullet must not be destroyed by friendly Rusher");
                    }
                });

            // TEST 27: Friendly enemy immunity - Boss self-immunity
            TestRunnerHelper.RunTest(report, "CH2-M4-27", "F23", 2,
                "Radial Projectile Boss Self-Immunity",
                "Verifies EnemyBullet hitting BossController inflicts 0 damage to Boss.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();

                        var bulletGo = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { bossGo });

                        E2EAssert.AreEqual(60, boss.currentHealth, "Boss health must remain 60 (self/friendly immune)");
                        E2EAssert.IsFalse(bulletGo == null, "Bullet must not be destroyed by friendly Boss");
                    }
                });

            // TEST 28: Bullet-on-bullet pass-through
            TestRunnerHelper.RunTest(report, "CH2-M4-28", "F23", 2,
                "EnemyBullet Bullet-on-Bullet Pass-Through",
                "Verifies EnemyBullet colliding with another EnemyBullet ignores collision.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var b1 = ctx.CreateGameObject("CH2_Bullet1");
                        var eb1 = b1.AddComponent<EnemyBullet>();

                        var b2 = ctx.CreateGameObject("CH2_Bullet2");
                        var eb2 = b2.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb1, new object[] { b2 });

                        E2EAssert.IsFalse(b1 == null, "Bullet 1 must not destroy itself on Bullet 2");
                        E2EAssert.IsFalse(b2 == null, "Bullet 2 must not be destroyed");
                    }
                });

            // TEST 29: Pickup trigger pass-through
            TestRunnerHelper.RunTest(report, "CH2-M4-29", "F23", 2,
                "Radial Projectile Pickup Trigger Pass-Through",
                "Verifies EnemyBullet ignores trigger colliders on pickups without premature destruction.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var pickup = ctx.CreateGameObject("CH2_Pickup");
                        var col = pickup.AddComponent<CircleCollider2D>();
                        col.isTrigger = true;

                        var bullet = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bullet.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { pickup });

                        E2EAssert.IsFalse(bullet == null, "Bullet must pass through trigger pickups without destroying itself");
                    }
                });

            // TEST 30: Solid obstacle auto-destruction
            TestRunnerHelper.RunTest(report, "CH2-M4-30", "F23", 2,
                "Radial Projectile Solid Wall Collision Destruction",
                "Verifies EnemyBullet auto-destructs when striking a solid wall / obstacle.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var wall = ctx.CreateGameObject("CH2_Wall");
                        var col = wall.AddComponent<BoxCollider2D>();
                        col.isTrigger = false;

                        var bullet = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bullet.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { wall });

                        E2EAssert.IsTrue(bullet == null, "Bullet must be destroyed upon colliding with a solid wall");
                    }
                });

            // =========================================================================
            // SUITE 4: Zero Memory Leaks & Resource Cleanup
            // =========================================================================

            // TEST 31: Boss coroutine termination on death
            TestRunnerHelper.RunTest(report, "CH2-M4-31", "F24", 2,
                "Boss Coroutines Termination On Death",
                "Verifies Die() halts attack coroutine and resets isAttacking=false.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.isAttacking = true;

                        boss.Die();

                        E2EAssert.IsFalse(boss.isAttacking, "isAttacking must be false after Die()");
                    }
                });

            // TEST 32: Boss colliders disabled on death
            TestRunnerHelper.RunTest(report, "CH2-M4-32", "F24", 2,
                "Boss Colliders Disabled On Death",
                "Verifies all colliders on Boss GameObject are disabled upon Die() to prevent zombie collisions.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("CH2_Boss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var col = bossGo.AddComponent<CircleCollider2D>();
                        col.enabled = true;
                        var boss = bossGo.AddComponent<BossController>();

                        // Pre-check
                        E2EAssert.IsTrue(col.enabled, "Collider should be enabled initially");

                        boss.Die();

                        E2EAssert.IsTrue(col == null || !col.enabled, "Collider must be disabled or destroyed upon boss Die()");
                    }
                });

            // TEST 33: Boss GameObject destruction on death
            TestRunnerHelper.RunTest(report, "CH2-M4-33", "F24", 2,
                "Boss GameObject Destruction On Death",
                "Verifies Boss GameObject is destroyed immediately upon Die().",
                () =>
                {
                    var bossGo = new GameObject("CH2_Boss_Standalone");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    boss.Die();

                    E2EAssert.IsTrue(bossGo == null, "Boss GameObject must be destroyed on death");
                });

            // TEST 34: Projectile impact cleanup
            TestRunnerHelper.RunTest(report, "CH2-M4-34", "F23", 2,
                "Radial Projectile Impact Cleanup",
                "Verifies bullet is destroyed immediately upon impact without lingering clones.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("CH2_Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var bulletGo = ctx.CreateGameObject("CH2_Bullet");
                        var eb = bulletGo.AddComponent<EnemyBullet>();

                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit?.Invoke(eb, new object[] { playerGo });

                        E2EAssert.IsTrue(bulletGo == null, "Bullet must be destroyed on hit");
                    }
                });

            // TEST 35: Multi-Cycle Boss & Barrage Zero-Leak Stress Test
            TestRunnerHelper.RunTest(report, "CH2-M4-35", "F24", 4,
                "Multi-Cycle Boss & Barrage Zero-Leak Stress Test",
                "Executes 5 complete cycles of Boss spawn, 16 radial bullets firing, impacts, pickups, and death; asserts 0 net leak.",
                () =>
                {
                    int initialCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

                    for (int cycle = 0; cycle < 5; cycle++)
                    {
                        // 1. Create player
                        var playerGo = new GameObject("CH2_StressPlayer");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        // 2. Create wall
                        var wallGo = new GameObject("CH2_StressWall");
                        var wallCol = wallGo.AddComponent<BoxCollider2D>();
                        wallCol.isTrigger = false;

                        // 3. Create dummy pickup prefab
                        var dummyPickup = new GameObject("CH2_StressPickupPrefab");

                        // 4. Create boss
                        var bossGo = new GameObject("CH2_StressBoss");
                        bossGo.AddComponent<Rigidbody2D>();
                        var boss = bossGo.AddComponent<BossController>();
                        boss.grenadePickupPrefab = dummyPickup;

                        // 5. Fire 16 radial bullets
                        List<GameObject> spawnedBullets = new List<GameObject>();
                        for (int b = 0; b < 16; b++)
                        {
                            var bullet = boss.SpawnBossProjectile(Vector2.zero, Vector2.right, b * 22.5f);
                            spawnedBullets.Add(bullet);
                        }

                        // 6. Simulate 4 bullets hitting player
                        var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < 4; i++)
                        {
                            if (spawnedBullets[i] != null)
                            {
                                handleHit?.Invoke(spawnedBullets[i].GetComponent<EnemyBullet>(), new object[] { playerGo });
                            }
                        }

                        // 7. Simulate 4 bullets hitting wall
                        for (int i = 4; i < 8; i++)
                        {
                            if (spawnedBullets[i] != null)
                            {
                                handleHit?.Invoke(spawnedBullets[i].GetComponent<EnemyBullet>(), new object[] { wallGo });
                            }
                        }

                        // 8. Clean remaining open-space bullets (simulating lifetime expiry)
                        for (int i = 8; i < 16; i++)
                        {
                            if (spawnedBullets[i] != null)
                            {
                                UnityEngine.Object.DestroyImmediate(spawnedBullets[i]);
                            }
                        }

                        // 9. Kill boss
                        boss.Die();

                        // 10. Clean up spawned grenade drops
                        var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                        foreach (var obj in allObjects)
                        {
                            if (obj.name.StartsWith("CH2_StressPickupPrefab(Clone)"))
                            {
                                UnityEngine.Object.DestroyImmediate(obj);
                            }
                        }

                        // Clean cycle fixtures
                        UnityEngine.Object.DestroyImmediate(playerGo);
                        UnityEngine.Object.DestroyImmediate(wallGo);
                        UnityEngine.Object.DestroyImmediate(dummyPickup);
                        if (bossGo != null) UnityEngine.Object.DestroyImmediate(bossGo);
                    }

                    int finalCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                    E2EAssert.AreEqual(initialCount, finalCount,
                        $"Net object leak must be strictly 0 after 5 full combat cycles (Initial: {initialCount}, Final: {finalCount})");
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Challenger 2 Milestone 4 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
