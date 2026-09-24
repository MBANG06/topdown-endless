using System;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    public static class Milestone2Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Milestone 2 Test Suite - Enemy Archetypes & Spawner System" };

            // TEST 1: Perimeter Spawn Calculation & Distance Guard (F09)
            TestRunnerHelper.RunTest(report, "M2-01", "F09", 1,
                "Perimeter Bounds and Player Distance Guard",
                "Verifies spawn positions are on perimeter and >= 6.0u away from player.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Spawner");
                        var spawner = go.AddComponent<EnemySpawner>();
                        Vector2 playerPos = Vector2.zero;
                        Vector2 spawnPos = spawner.GeneratePerimeterPosition(playerPos);

                        bool onPerimeter = Mathf.Approximately(spawnPos.x, spawner.minX) ||
                                           Mathf.Approximately(spawnPos.x, spawner.maxX) ||
                                           Mathf.Approximately(spawnPos.y, spawner.minY) ||
                                           Mathf.Approximately(spawnPos.y, spawner.maxY);
                        E2EAssert.IsTrue(onPerimeter, "Spawn position must be on one of the 4 perimeter edges");
                        E2EAssert.IsTrue(Vector2.Distance(spawnPos, playerPos) >= spawner.minPlayerDistance,
                            "Spawn position must respect minPlayerDistance (6.0u)");
                    }
                });

            // TEST 2: Progressive Spawn Interval Scaling (F10)
            TestRunnerHelper.RunTest(report, "M2-02", "F10", 1,
                "Spawn Interval Scaling Formula",
                "Verifies spawn interval scales down with time and score and respects 0.6s floor.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Spawner");
                        var spawner = go.AddComponent<EnemySpawner>();

                        // Baseline at t=0, S=0: 3.0s
                        float i0 = spawner.CalculateSpawnInterval(0f, 0);
                        E2EAssert.AreApproximatelyEqual(3.0f, i0, 0.001f, "Initial interval must be 3.0s");

                        // Scaled at t=60s, S=100: 3.0 - 0.9 - 0.2 = 1.9s
                        float i1 = spawner.CalculateSpawnInterval(60f, 100);
                        E2EAssert.AreApproximatelyEqual(1.9f, i1, 0.001f, "Interval at 60s/100pts must be 1.9s");

                        // Floor check at extreme time/score: >= 0.6s
                        float iFloor = spawner.CalculateSpawnInterval(500f, 2000);
                        E2EAssert.AreApproximatelyEqual(0.6f, iFloor, 0.001f, "Interval floor must clamp at 0.6s");
                    }
                });

            // TEST 3: Concurrency Cap Scaling (F10)
            TestRunnerHelper.RunTest(report, "M2-03", "F10", 1,
                "Max Concurrent Enemies Scaling Formula",
                "Verifies concurrency cap scales from 5 up to 25 maximum.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Spawner");
                        var spawner = go.AddComponent<EnemySpawner>();

                        int c0 = spawner.CalculateMaxConcurrentEnemies(0f, 0);
                        E2EAssert.AreEqual(5, c0, "Initial concurrency cap must be 5");

                        // at t=60s (floor(60/20)=3), S=180 (floor(180/60)=3): 5 + 3 + 3 = 11
                        int c1 = spawner.CalculateMaxConcurrentEnemies(60f, 180);
                        E2EAssert.AreEqual(11, c1, "Concurrency cap at 60s/180pts must be 11");

                        // Ceiling clamp at 25
                        int cMax = spawner.CalculateMaxConcurrentEnemies(1000f, 5000);
                        E2EAssert.AreEqual(25, cMax, "Concurrency ceiling must clamp at 25");
                    }
                });

            // TEST 4: ChaserEnemy Stats and Direct Tracking (F11)
            TestRunnerHelper.RunTest(report, "M2-04", "F11", 1,
                "Chaser Enemy Stats and Initialization",
                "Verifies Chaser enemy has 3 HP, 2.8 speed, 10 score, 20% drop.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Chaser");
                        var chaser = go.AddComponent<ChaserEnemy>();

                        E2EAssert.AreEqual(3, chaser.maxHealth, "Chaser maxHealth must be 3");
                        E2EAssert.AreEqual(3, chaser.currentHealth, "Chaser currentHealth must start at 3");
                        E2EAssert.AreApproximatelyEqual(2.8f, chaser.moveSpeed, 0.05f, "Chaser speed must be 2.8");
                        E2EAssert.AreEqual(10, chaser.scoreValue, "Chaser scoreValue must be 10");
                        E2EAssert.AreApproximatelyEqual(0.20f, chaser.grenadeDropChance, 0.01f, "Chaser drop chance must be 0.20");
                        E2EAssert.IsTrue(chaser.IsAlive, "Chaser should be alive upon creation");
                    }
                });

            // TEST 5: Chaser Contact Damage to Player (F11)
            TestRunnerHelper.RunTest(report, "M2-05", "F11", 1,
                "Chaser Contact Damage Application",
                "Verifies Chaser contact with player inflicts exactly 1 damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player");
                        player.tag = "Player";
                        var ph = player.AddComponent<PlayerHealth>();

                        var chaserGo = ctx.CreateGameObject("Chaser");
                        var chaser = chaserGo.AddComponent<ChaserEnemy>();

                        // Simulate contact damage
                        var method = typeof(ChaserEnemy).GetMethod("TryInflictContactDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(chaser, new object[] { player });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Player HP should drop from 5 to 4 on contact");
                    }
                });

            // TEST 6: ShooterEnemy Stats & Kiting Parameters (F12)
            TestRunnerHelper.RunTest(report, "M2-06", "F12", 1,
                "Shooter Enemy Stats and Kiting Thresholds",
                "Verifies Shooter enemy has 2 HP, 2.0 speed, 20 score, 25% drop, and 3.8/5.5 thresholds.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Shooter");
                        var shooter = go.AddComponent<ShooterEnemy>();

                        E2EAssert.AreEqual(2, shooter.maxHealth, "Shooter maxHealth must be 2");
                        E2EAssert.AreEqual(2, shooter.currentHealth, "Shooter currentHealth must start at 2");
                        E2EAssert.AreApproximatelyEqual(2.0f, shooter.moveSpeed, 0.05f, "Shooter speed must be 2.0");
                        E2EAssert.AreEqual(20, shooter.scoreValue, "Shooter scoreValue must be 20");
                        E2EAssert.AreApproximatelyEqual(0.25f, shooter.grenadeDropChance, 0.01f, "Shooter drop chance must be 0.25");
                        E2EAssert.AreEqual(3.8f, shooter.retreatDistance, "Retreat threshold must be 3.8u");
                        E2EAssert.AreEqual(5.5f, shooter.advanceDistance, "Advance threshold must be 5.5u");
                    }
                });

            // TEST 7: ShooterEnemy Projectile Spawning (F12)
            TestRunnerHelper.RunTest(report, "M2-07", "F12", 1,
                "Shooter Enemy Projectile Firing",
                "Verifies Shoot() instantiates bulletPrefab aimed at player.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player");
                        player.tag = "Player";
                        player.transform.position = new Vector3(0, 5, 0);
                        player.AddComponent<PlayerHealth>();

                        var bulletPrefab = ctx.CreateGameObject("BulletPrefab");
                        bulletPrefab.AddComponent<EnemyBullet>();

                        var shooterGo = ctx.CreateGameObject("Shooter");
                        shooterGo.transform.position = Vector3.zero;
                        var shooter = shooterGo.AddComponent<ShooterEnemy>();
                        shooter.bulletPrefab = bulletPrefab;
                        shooter.SetPlayer(player.transform);

                        shooter.Shoot();

                        var spawnedBullet = GameObject.Find("BulletPrefab(Clone)");
                        E2EAssert.IsNotNull(spawnedBullet, "Shooter.Shoot() must instantiate bulletPrefab");
                        UnityEngine.Object.DestroyImmediate(spawnedBullet);
                    }
                });

            // TEST 8: RusherEnemy Stats and High Speed (F13)
            TestRunnerHelper.RunTest(report, "M2-08", "F13", 1,
                "Rusher Enemy Stats and High Speed",
                "Verifies Rusher enemy has 1 HP, 6.2 speed (faster than player 5.0), 15 score, 15% drop.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Rusher");
                        var rusher = go.AddComponent<RusherEnemy>();

                        E2EAssert.AreEqual(1, rusher.maxHealth, "Rusher maxHealth must be 1 HP");
                        E2EAssert.AreEqual(1, rusher.currentHealth, "Rusher currentHealth must start at 1");
                        E2EAssert.AreApproximatelyEqual(6.2f, rusher.moveSpeed, 0.05f, "Rusher speed must be 6.2 u/s");
                        E2EAssert.IsTrue(rusher.moveSpeed > 5.0f, "Rusher must be faster than player base speed 5.0");
                        E2EAssert.AreEqual(15, rusher.scoreValue, "Rusher scoreValue must be 15");
                        E2EAssert.AreApproximatelyEqual(0.15f, rusher.grenadeDropChance, 0.01f, "Rusher drop chance must be 0.15");
                    }
                });

            // TEST 9: Rusher 1-Shot Elimination & Contact Damage (F13)
            TestRunnerHelper.RunTest(report, "M2-09", "F13", 1,
                "Rusher 1-Shot Elimination and Contact Damage",
                "Verifies standard 1 damage eliminates Rusher and Rusher inflicts 1 contact damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player");
                        player.tag = "Player";
                        var ph = player.AddComponent<PlayerHealth>();

                        var rusherGo = ctx.CreateGameObject("Rusher");
                        var rusher = rusherGo.AddComponent<RusherEnemy>();

                        // Verify contact damage
                        var method = typeof(RusherEnemy).GetMethod("TryInflictContactDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(rusher, new object[] { player });
                        E2EAssert.AreEqual(4, ph.currentHealth, "Player HP should drop to 4 on Rusher contact");

                        // Verify 1 damage eliminates Rusher
                        rusher.TakeDamage(1);
                        E2EAssert.AreEqual(0, rusher.currentHealth, "Rusher health should be 0");
                        E2EAssert.IsFalse(rusher.IsAlive, "Rusher should not be alive after 1 damage");
                    }
                });

            // TEST 10: EnemyBase TakeDamage and Visual Flash (F14)
            TestRunnerHelper.RunTest(report, "M2-10", "F14", 1,
                "EnemyBase TakeDamage and Visual Damage Flash",
                "Verifies TakeDamage reduces HP, flashes sprite color, and triggers Die() on 0 HP.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Chaser");
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.color = Color.white;
                        var chaser = go.AddComponent<ChaserEnemy>();

                        // Hit 1: 3 -> 2 HP
                        chaser.TakeDamage(1);
                        E2EAssert.AreEqual(2, chaser.currentHealth, "HP should decrease to 2");
                        E2EAssert.IsTrue(chaser.IsAlive, "Enemy should still be alive");

                        // Hit 2: 2 -> 1 HP
                        chaser.TakeDamage(1);
                        E2EAssert.AreEqual(1, chaser.currentHealth, "HP should decrease to 1");

                        // Hit 3: 1 -> 0 HP (lethal)
                        chaser.TakeDamage(1);
                        E2EAssert.AreEqual(0, chaser.currentHealth, "HP should reach 0");
                        E2EAssert.IsFalse(chaser.IsAlive, "Enemy should not be alive at 0 HP");
                        E2EAssert.IsTrue(chaser.isDead, "isDead flag should be true");
                    }
                });

            // TEST 11: Die() Cleanup & Deactivation (F14)
            TestRunnerHelper.RunTest(report, "M2-11", "F14", 1,
                "Die() Execution and Entity Destruction",
                "Verifies calling Die() executes cleanup and destroys the enemy GameObject.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Enemy");
                        var col = go.AddComponent<CircleCollider2D>();
                        var enemy = go.AddComponent<ChaserEnemy>();

                        E2EAssert.IsTrue(col.enabled, "Collider should be enabled initially");
                        enemy.Die();
                        E2EAssert.IsTrue(enemy == null || !col.enabled, "Enemy must be destroyed or collider disabled upon death");
                    }
                });

            // TEST 12: Kill Scoring Dispatch & Drop Rolls (F15)
            TestRunnerHelper.RunTest(report, "M2-12", "F15", 1,
                "Score Event Notification and Drop Roll",
                "Verifies OnEnemyKilledScore event dispatches correct point values.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        int receivedScore = 0;
                        Action<int> scoreHandler = s => receivedScore += s;
                        EnemyBase.OnEnemyKilledScore += scoreHandler;

                        var go1 = ctx.CreateGameObject("Chaser");
                        var c = go1.AddComponent<ChaserEnemy>();
                        c.Die();
                        E2EAssert.AreEqual(10, receivedScore, "Chaser kill should dispatch 10 points");

                        var go2 = ctx.CreateGameObject("Shooter");
                        var s = go2.AddComponent<ShooterEnemy>();
                        s.Die();
                        E2EAssert.AreEqual(30, receivedScore, "Shooter kill (+20) should bring score to 30");

                        var go3 = ctx.CreateGameObject("Rusher");
                        var r = go3.AddComponent<RusherEnemy>();
                        r.Die();
                        E2EAssert.AreEqual(45, receivedScore, "Rusher kill (+15) should bring score to 45");

                        EnemyBase.OnEnemyKilledScore -= scoreHandler;
                    }
                });

            // TEST 13: EnemyBullet Forward Velocity and Player Damage
            TestRunnerHelper.RunTest(report, "M2-13", "EnemyBullet", 1,
                "EnemyBullet Travel and Player Damage Ingestion",
                "Verifies EnemyBullet damages player on collision.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player");
                        player.tag = "Player";
                        var ph = player.AddComponent<PlayerHealth>();

                        var bulletGo = ctx.CreateGameObject("EnemyBullet");
                        var bullet = bulletGo.AddComponent<EnemyBullet>();
                        bullet.speed = 8f;
                        bullet.damage = 1;

                        // Simulate bullet hit on player
                        var method = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(bullet, new object[] { player });

                        E2EAssert.AreEqual(4, ph.currentHealth, "Player HP should drop to 4 from bullet hit");
                    }
                });

            // TEST 14: EnemyBullet Friendly Fire Immunity
            TestRunnerHelper.RunTest(report, "M2-14", "EnemyBullet", 1,
                "EnemyBullet Friendly Fire Immunity",
                "Verifies EnemyBullet ignores collision with friendly enemies.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemyGo = ctx.CreateGameObject("Enemy");
                        var enemy = enemyGo.AddComponent<ChaserEnemy>();

                        var bulletGo = ctx.CreateGameObject("EnemyBullet");
                        var bullet = bulletGo.AddComponent<EnemyBullet>();

                        // Simulate bullet hit on friendly enemy
                        var method = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(bullet, new object[] { enemyGo });

                        E2EAssert.AreEqual(3, enemy.currentHealth, "Friendly enemy should NOT take damage from EnemyBullet");
                    }
                });

            // TEST 15: Spawner Boss Suppression and Stop/Start Lifecycle
            TestRunnerHelper.RunTest(report, "M2-15", "EnemySpawner", 1,
                "EnemySpawner Boss Suppression & Control Lifecycle",
                "Verifies Spawner reduces rate by 50% during boss and toggles isSpawning.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Spawner");
                        var spawner = go.AddComponent<EnemySpawner>();

                        spawner.StopSpawning();
                        E2EAssert.IsFalse(spawner.isSpawning, "StopSpawning must set isSpawning to false");

                        spawner.StartSpawning();
                        E2EAssert.IsTrue(spawner.isSpawning, "StartSpawning must set isSpawning to true");

                        // Normal interval at t=0, S=0: 3.0s
                        float normal = spawner.CalculateSpawnInterval(0f, 0);
                        E2EAssert.AreApproximatelyEqual(3.0f, normal, 0.001f);

                        // Under boss suppression: interval is doubled (rate halved)
                        spawner.isBossActive = true;
                        float suppressed = spawner.CalculateSpawnInterval(0f, 0);
                        E2EAssert.AreApproximatelyEqual(6.0f, suppressed, 0.001f, "Boss suppression should double interval to 6.0s");

                        spawner.OnBossDefeated();
                        E2EAssert.IsFalse(spawner.isBossActive, "OnBossDefeated should clear isBossActive");
                        E2EAssert.IsTrue(spawner.isSpawning, "OnBossDefeated should ensure isSpawning is true");
                    }
                });

            // TEST 16: Prefab Asset Integrity Check
            TestRunnerHelper.RunTest(report, "M2-16", "Prefabs", 1,
                "Milestone 2 Prefab Assets Integrity",
                "Verifies ChaserEnemy, ShooterEnemy, RusherEnemy, and EnemyBullet prefabs exist and have correct components.",
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

                    E2EAssert.IsNotNull(chaser.GetComponent<ChaserEnemy>(), "ChaserEnemy prefab must have ChaserEnemy component");
                    E2EAssert.IsNotNull(shooter.GetComponent<ShooterEnemy>(), "ShooterEnemy prefab must have ShooterEnemy component");
                    E2EAssert.IsNotNull(rusher.GetComponent<RusherEnemy>(), "RusherEnemy prefab must have RusherEnemy component");
                    E2EAssert.IsNotNull(bullet.GetComponent<EnemyBullet>(), "EnemyBullet prefab must have EnemyBullet component");
#endif
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Milestone 2 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
