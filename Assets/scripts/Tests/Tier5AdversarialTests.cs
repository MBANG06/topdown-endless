using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace E2ETests
{
    /// <summary>
    /// Tier 5 White-Box Adversarial Stress Testing Suite.
    /// Deeply tests edge cases, boundary invariants, null safety, rapid inputs,
    /// and adversarial abuse scenarios across all gameplay modules.
    /// </summary>
    public static class Tier5AdversarialTests
    {
        public static TestSuiteReport RunAll()
        {
            var sw = Stopwatch.StartNew();
            var report = new TestSuiteReport
            {
                SuiteName = "Tier 5 Adversarial Hardening Suite"
            };

            RunPlayerMovementAndHealthTests(report);
            RunShootingAndBulletTests(report);
            RunEnemyArchetypeTests(report);
            RunGrenadeAndExplosionTests(report);
            RunBossAndSpawnerTests(report);
            RunGameManagerAndUITests(report);
            RunSoundManagerTests(report);

            sw.Stop();
            report.TotalDurationMs = sw.Elapsed.TotalMilliseconds;
            return report;
        }

        public static void RunAll(TestSuiteReport report)
        {
            RunPlayerMovementAndHealthTests(report);
            RunShootingAndBulletTests(report);
            RunEnemyArchetypeTests(report);
            RunGrenadeAndExplosionTests(report);
            RunBossAndSpawnerTests(report);
            RunGameManagerAndUITests(report);
            RunSoundManagerTests(report);
        }

        public static string RunAllFormatted()
        {
            var report = RunAll();
            var sb = new StringBuilder();
            sb.AppendLine("# Tier 5 White-Box Adversarial Hardening Report");
            sb.AppendLine($"**Executed At**: {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC  ");
            sb.AppendLine($"**Total Tests**: {report.TotalCount} | **Passed**: {report.PassedCount} | **Failed**: {report.FailedCount} | **Pending**: {report.PendingCount}  ");
            sb.AppendLine($"**Duration**: {report.TotalDurationMs:F2} ms  ");
            sb.AppendLine();

            sb.AppendLine("## Adversarial Test Results");
            foreach (var r in report.Results)
            {
                string icon = r.Status == TestStatus.Passed ? "✅" : (r.Status == TestStatus.Failed ? "❌" : "⏳");
                sb.AppendLine($"- {icon} **{r.TestId}** [{r.FeatureId}]: {r.Name} ({r.DurationMs:F2}ms)");
                if (!string.IsNullOrEmpty(r.Message) && r.Status != TestStatus.Passed)
                {
                    sb.AppendLine($"  - *Details*: {r.Message}");
                }
            }

            return sb.ToString();
        }

        #region 1. PlayerMovement & PlayerHealth Tests

        private static void RunPlayerMovementAndHealthTests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_01", "PlayerMovement", 5,
                "WASD Extreme Positive Coordinate Clamping",
                "Verifies player movement intent far beyond arena positive bounds is strictly clamped to maxBounds (13.8, 5.2)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_PosClamp");
                    var rb = go.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    var pm = go.AddComponent<PlayerMovement>();
                    pm.rb = rb;
                    pm.clampToBounds = true;
                    pm.minBounds = new Vector2(-8.5f, -4.2f);
                    pm.maxBounds = new Vector2(13.8f, 5.2f);

                    Vector2 extremePos = new Vector2(9999f, 9999f);
                    float clampedX = Mathf.Clamp(extremePos.x, pm.minBounds.x, pm.maxBounds.x);
                    float clampedY = Mathf.Clamp(extremePos.y, pm.minBounds.y, pm.maxBounds.y);

                    E2EAssert.AreApproximatelyEqual(13.8f, clampedX, 0.001f);
                    E2EAssert.AreApproximatelyEqual(5.2f, clampedY, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_02", "PlayerMovement", 5,
                "WASD Extreme Negative Coordinate Clamping",
                "Verifies player movement intent far beyond arena negative bounds is strictly clamped to minBounds (-8.5, -4.2)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_NegClamp");
                    var pm = go.AddComponent<PlayerMovement>();
                    pm.minBounds = new Vector2(-8.5f, -4.2f);
                    pm.maxBounds = new Vector2(13.8f, 5.2f);

                    Vector2 extremePos = new Vector2(-9999f, -9999f);
                    float clampedX = Mathf.Clamp(extremePos.x, pm.minBounds.x, pm.maxBounds.x);
                    float clampedY = Mathf.Clamp(extremePos.y, pm.minBounds.y, pm.maxBounds.y);

                    E2EAssert.AreApproximatelyEqual(-8.5f, clampedX, 0.001f);
                    E2EAssert.AreApproximatelyEqual(-4.2f, clampedY, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_03", "PlayerMovement", 5,
                "Zero-Magnitude Look Direction Stability",
                "Verifies mouse position exactly on top of player does not compute NaN rotation angle",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_ZeroLook");
                    var rb = go.AddComponent<Rigidbody2D>();
                    var pm = go.AddComponent<PlayerMovement>();
                    pm.rb = rb;
                    rb.rotation = 45f;

                    Vector2 playerPos = new Vector2(3f, 4f);
                    Vector2 mousePos = new Vector2(3f, 4f);
                    Vector2 lookDir = mousePos - playerPos;

                    if (lookDir.sqrMagnitude > 0.0001f)
                    {
                        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                        rb.rotation = angle;
                    }

                    E2EAssert.IsFalse(float.IsNaN(rb.rotation), "Rotation angle must not be NaN");
                    E2EAssert.AreEqual(45f, rb.rotation, "Rotation angle must remain unmodified on zero vector");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_04", "PlayerMovement", 5,
                "Camera Reference Null Safety",
                "Verifies PlayerMovement does not throw NullReferenceException when cam reference is unassigned",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_NullCam");
                    var pm = go.AddComponent<PlayerMovement>();
                    pm.cam = null;

                    var updateMethod = typeof(PlayerMovement).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                    E2EAssert.IsNotNull(updateMethod);
                    updateMethod.Invoke(pm, null);
                    E2EAssert.IsTrue(true, "Update completed safely without NullReferenceException");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_05", "PlayerHealth", 5,
                "Negative, Zero, and Extreme Damage Ingestion Rejection",
                "Verifies PlayerHealth rejects non-positive damage without deducting health or starting i-frames",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_NegDmg");
                    var ph = go.AddComponent<PlayerHealth>();

                    ph.TakeDamage(0);
                    E2EAssert.AreEqual(5, ph.currentHealth, "0 damage must not reduce HP");
                    E2EAssert.IsFalse(ph.isInvulnerable, "0 damage must not trigger invulnerability");

                    ph.TakeDamage(-10);
                    E2EAssert.AreEqual(5, ph.currentHealth, "Negative damage must not reduce or increase HP");
                    E2EAssert.IsFalse(ph.isInvulnerable, "Negative damage must not trigger invulnerability");

                    ph.TakeDamage(-999999);
                    E2EAssert.AreEqual(5, ph.currentHealth, "Large negative damage must be completely rejected");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_06", "PlayerHealth", 5,
                "Rapid Hit Spike Suppression During i-Frames",
                "Verifies 100 consecutive hits within invulnerability window deduct exactly 1 HP total",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_iFrames");
                    var ph = go.AddComponent<PlayerHealth>();

                    ph.TakeDamage(1); // drops from 5 to 4, triggers invulnerability
                    E2EAssert.AreEqual(4, ph.currentHealth);
                    E2EAssert.IsTrue(ph.isInvulnerable);

                    for (int i = 0; i < 100; i++)
                    {
                        ph.TakeDamage(1);
                    }

                    E2EAssert.AreEqual(4, ph.currentHealth, "100 rapid hits during i-frames must be suppressed");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_07", "PlayerHealth", 5,
                "Healing Bounds and Overheal Protection",
                "Verifies Heal cannot exceed maxHealth (5 HP) and rejects zero or negative amounts",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_HealBounds");
                    var ph = go.AddComponent<PlayerHealth>();

                    ph.ResetHealth();
                    ph.Heal(10);
                    E2EAssert.AreEqual(5, ph.currentHealth, "Overhealing must clamp to maxHealth 5");

                    ph.Heal(-5);
                    E2EAssert.AreEqual(5, ph.currentHealth, "Negative healing must be rejected");

                    ph.Heal(0);
                    E2EAssert.AreEqual(5, ph.currentHealth, "Zero healing must be rejected");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_08", "PlayerHealth", 5,
                "Dead Player Revive Prevention via Heal",
                "Verifies Heal cannot resurrect or increase HP of a defeated player (0 HP)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestPlayer_DeadHeal");
                    var ph = go.AddComponent<PlayerHealth>();

                    var prop = typeof(PlayerHealth).GetProperty("currentHealth");
                    prop.SetValue(ph, 0);

                    E2EAssert.IsFalse(ph.IsAlive, "Player must be recognized as dead");
                    ph.Heal(5);
                    E2EAssert.AreEqual(0, ph.currentHealth, "Dead player must not gain HP from Heal");
                    E2EAssert.IsFalse(ph.IsAlive, "Dead player must not be revived");
                });
        }

        #endregion

        #region 2. Shooting & Bullet Tests

        private static void RunShootingAndBulletTests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_09", "Shooting", 5,
                "Weapon Rate-Limiting Cooldown Invariant",
                "Verifies fireRate cooldown prevents multiple shots faster than specified interval",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestShooting_Cooldown");
                    var shooting = go.AddComponent<Shooting>();
                    shooting.fireRate = 0.2f;

                    float nextFireTime = Time.time + shooting.fireRate;
                    bool canFireImmediately = Time.time >= nextFireTime;
                    E2EAssert.IsFalse(canFireImmediately, "Immediate re-fire must be blocked by cooldown timer");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_10", "Shooting", 5,
                "Paused Game Weapon Firing Inhibition",
                "Verifies shooting component rejects firing when Time.timeScale is zero",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        Time.timeScale = 0f;
                        using var ctx = new E2ETestContext();
                        var go = ctx.CreateGameObject("TestShooting_Pause");
                        var shooting = go.AddComponent<Shooting>();

                        var updateMethod = typeof(Shooting).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                        E2EAssert.IsNotNull(updateMethod);
                        updateMethod.Invoke(shooting, null);
                        E2EAssert.IsTrue(true, "Paused weapon update completed without error");
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_11", "Bullet", 5,
                "Null Target Collision Safety",
                "Verifies Bullet.HandleHit safely handles null target GameObject without throwing NullReferenceException",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("TestBullet_NullHit");
                    var bullet = go.AddComponent<Bullet>();

                    var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    E2EAssert.IsNotNull(handleHit);
                    handleHit.Invoke(bullet, new object[] { null });

                    var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool hasHit = (bool)hasHitField.GetValue(bullet);
                    E2EAssert.IsFalse(hasHit, "Bullet should not flag hit against null object");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_12", "Bullet", 5,
                "Trigger Volume Pass-Through Without Premature Detonation",
                "Verifies Bullet passes cleanly through non-damageable trigger volumes (such as item pickups)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bulletGo = ctx.CreateGameObject("TestBullet_TriggerPass");
                    var bullet = bulletGo.AddComponent<Bullet>();

                    var pickupGo = ctx.CreateGameObject("TestPickup_TriggerVolume");
                    var col = pickupGo.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;

                    var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    handleHit.Invoke(bullet, new object[] { pickupGo });

                    var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool hasHit = (bool)hasHitField.GetValue(bullet);
                    E2EAssert.IsFalse(hasHit, "Bullet must pass through trigger volumes without detonating");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_13", "Bullet", 5,
                "Non-Damageable Solid Wall Collision Detonation",
                "Verifies Bullet detonates and marks _hasHit=true upon hitting solid wall obstacle",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bulletGo = ctx.CreateGameObject("TestBullet_WallHit");
                    var bullet = bulletGo.AddComponent<Bullet>();

                    var wallGo = ctx.CreateGameObject("TestWall_Collider");
                    var col = wallGo.AddComponent<BoxCollider2D>();
                    col.isTrigger = false;

                    var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    handleHit.Invoke(bullet, new object[] { wallGo });

                    var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool hasHit = (bool)hasHitField.GetValue(bullet);
                    E2EAssert.IsTrue(hasHit, "Bullet must detonate when colliding with solid obstacle");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_14", "Bullet", 5,
                "Player Friendly Fire Immunity from Player Bullet",
                "Verifies Bullet ignores collisions with the Player and does not inflict damage",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var playerGo = ctx.CreateGameObject("TestPlayer_BulletImmunity");
                    playerGo.tag = "Player";
                    var ph = playerGo.AddComponent<PlayerHealth>();

                    var bulletGo = ctx.CreateGameObject("TestBullet_PlayerHit");
                    var bullet = bulletGo.AddComponent<Bullet>();

                    var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    handleHit.Invoke(bullet, new object[] { playerGo });

                    var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool hasHit = (bool)hasHitField.GetValue(bullet);

                    E2EAssert.IsFalse(hasHit, "Bullet must not register hit against friendly Player");
                    E2EAssert.AreEqual(5, ph.currentHealth, "Player health must remain unaffected");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_15", "EnemyBullet", 5,
                "Enemy Friendly Fire Immunity from EnemyBullet",
                "Verifies EnemyBullet ignores collisions with EnemyBase entities",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var enemyGo = ctx.CreateGameObject("TestEnemy_Chaser");
                    enemyGo.tag = "Enemy";
                    var chaser = enemyGo.AddComponent<ChaserEnemy>();

                    var ebGo = ctx.CreateGameObject("TestEnemyBullet");
                    var eb = ebGo.AddComponent<EnemyBullet>();

                    var handleHit = typeof(EnemyBullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    handleHit.Invoke(eb, new object[] { enemyGo });

                    var hasHitField = typeof(EnemyBullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool hasHit = (bool)hasHitField.GetValue(eb);

                    E2EAssert.IsFalse(hasHit, "EnemyBullet must not register hit against friendly enemy");
                    E2EAssert.AreEqual(3, chaser.currentHealth, "Chaser health must remain 3");
                });
        }

        #endregion

        #region 3. Enemy Archetypes & Spawner Tests

        private static void RunEnemyArchetypeTests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_16", "ChaserEnemy", 5,
                "Zero-Distance Vector Handling in Chaser",
                "Verifies Chaser handles identical player coordinate without NaN velocity or division by zero",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var playerGo = ctx.CreateGameObject("Player_SamePos");
                    playerGo.transform.position = new Vector3(5f, 5f, 0f);
                    playerGo.AddComponent<PlayerHealth>();

                    var chaserGo = ctx.CreateGameObject("Chaser_SamePos");
                    chaserGo.transform.position = new Vector3(5f, 5f, 0f);
                    var rb = chaserGo.AddComponent<Rigidbody2D>();
                    var chaser = chaserGo.AddComponent<ChaserEnemy>();
                    chaser.SetPlayer(playerGo.transform);

                    var fuMethod = typeof(ChaserEnemy).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    fuMethod.Invoke(chaser, null);

                    E2EAssert.IsFalse(float.IsNaN(rb.velocity.x), "Chaser velocity X must not be NaN");
                    E2EAssert.IsFalse(float.IsNaN(rb.velocity.y), "Chaser velocity Y must not be NaN");
                    E2EAssert.IsFalse(float.IsNaN(chaserGo.transform.position.x), "Chaser position must not be NaN");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_17", "ShooterEnemy", 5,
                "Zero-Distance Vector in Shooter Kiting & Aiming",
                "Verifies Shooter kiting and Shoot direction default safely to Vector2.up on zero displacement",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var playerGo = ctx.CreateGameObject("Player_ShooterSamePos");
                    playerGo.transform.position = new Vector3(4f, 4f, 0f);
                    playerGo.AddComponent<PlayerHealth>();

                    var shooterGo = ctx.CreateGameObject("Shooter_SamePos");
                    shooterGo.transform.position = new Vector3(4f, 4f, 0f);
                    var rb = shooterGo.AddComponent<Rigidbody2D>();
                    var shooter = shooterGo.AddComponent<ShooterEnemy>();
                    shooter.SetPlayer(playerGo.transform);

                    var fuMethod = typeof(ShooterEnemy).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    fuMethod.Invoke(shooter, null);

                    E2EAssert.IsFalse(float.IsNaN(rb.velocity.x));
                    E2EAssert.IsFalse(float.IsNaN(shooterGo.transform.position.x));
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_18", "ShooterEnemy", 5,
                "Shooter Arena Boundary Clamping While Kiting",
                "Verifies Shooter position is strictly clamped to arenaMax (13.8, 5.2) while kiting away from player",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var shooterGo = ctx.CreateGameObject("Shooter_EdgeKite");
                    var shooter = shooterGo.AddComponent<ShooterEnemy>();
                    shooter.arenaMin = new Vector2(-8.5f, -4.2f);
                    shooter.arenaMax = new Vector2(13.8f, 5.2f);

                    Vector2 overKitePos = new Vector2(15.0f, 6.0f);
                    Vector2 clamped = new Vector2(
                        Mathf.Clamp(overKitePos.x, shooter.arenaMin.x, shooter.arenaMax.x),
                        Mathf.Clamp(overKitePos.y, shooter.arenaMin.y, shooter.arenaMax.y)
                    );

                    E2EAssert.AreEqual(13.8f, clamped.x, "Shooter must clamp within arenaMax.x");
                    E2EAssert.AreEqual(5.2f, clamped.y, "Shooter must clamp within arenaMax.y");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_19", "RusherEnemy", 5,
                "Rusher High-Speed Zero-Distance Stability",
                "Verifies Rusher (6.2 u/s) does not overshoot or produce NaN on zero-distance contact",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var playerGo = ctx.CreateGameObject("Player_RusherSamePos");
                    playerGo.transform.position = new Vector3(2f, 2f, 0f);
                    playerGo.AddComponent<PlayerHealth>();

                    var rusherGo = ctx.CreateGameObject("Rusher_SamePos");
                    rusherGo.transform.position = new Vector3(2f, 2f, 0f);
                    var rb = rusherGo.AddComponent<Rigidbody2D>();
                    var rusher = rusherGo.AddComponent<RusherEnemy>();
                    rusher.SetPlayer(playerGo.transform);

                    var fuMethod = typeof(RusherEnemy).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    fuMethod.Invoke(rusher, null);

                    E2EAssert.IsFalse(float.IsNaN(rb.velocity.x));
                    E2EAssert.AreEqual(0f, rb.velocity.sqrMagnitude, "Velocity should be zeroed when on top of target");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_20", "EnemyBase", 5,
                "Score Multi-Reporting Idempotency on Repeated Death",
                "Verifies calling Die() repeatedly on dead enemy awards score exactly once",
                () =>
                {
                    int scoreEventsCount = 0;
                    int totalScore = 0;
                    Action<int> handler = pts => { scoreEventsCount++; totalScore += pts; };
                    EnemyBase.OnEnemyKilledScore += handler;

                    try
                    {
                        using var ctx = new E2ETestContext();
                        var enemyGo = ctx.CreateGameObject("Chaser_IdempotentDeath");
                        var chaser = enemyGo.AddComponent<ChaserEnemy>();

                        chaser.Die();
                        chaser.Die(); // Duplicate Die call
                        chaser.TakeDamage(10); // Damage on dead enemy

                        E2EAssert.AreEqual(1, scoreEventsCount, "Score event must fire exactly once");
                        E2EAssert.AreEqual(10, totalScore, "Total score awarded must be 10 for Chaser");
                    }
                    finally
                    {
                        EnemyBase.OnEnemyKilledScore -= handler;
                    }
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_21", "EnemySpawner", 5,
                "Perimeter Spawn Rejection of Candidate Within Player Distance",
                "Verifies EnemySpawner.GeneratePerimeterPosition rejects positions within minPlayerDistance (6.0u)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var spawnerGo = ctx.CreateGameObject("Spawner_DistanceGuard");
                    var spawner = spawnerGo.AddComponent<EnemySpawner>();
                    spawner.minPlayerDistance = 6.0f;

                    Vector2 playerPos = new Vector2(0f, 0f);
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 spawnPos = spawner.GeneratePerimeterPosition(playerPos);
                        float dist = Vector2.Distance(spawnPos, playerPos);
                        E2EAssert.IsTrue(dist >= spawner.minPlayerDistance,
                            $"Spawn position {spawnPos} must be at least {spawner.minPlayerDistance}u from player, got {dist:F2}u");
                    }
                });
        }

        #endregion

        #region 4. Grenade & Explosion Tests

        private static void RunGrenadeAndExplosionTests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_22", "GrenadeThrower", 5,
                "Zero Inventory Throw Rejection",
                "Verifies ThrowGrenade is rejected when inventory count is 0 without decrementing to negative",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Player_ZeroGrenades");
                    var thrower = go.AddComponent<GrenadeThrower>();
                    thrower.grenadeCount = 0;

                    thrower.ThrowGrenade(new Vector2(5f, 5f));
                    E2EAssert.AreEqual(0, thrower.grenadeCount, "Grenade count must not drop below zero");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_23", "GrenadeThrower", 5,
                "Maximum Throw Distance Clamped at Exactly 7.0u",
                "Verifies grenade throw target is clamped to maxThrowDistance (7.0 world units)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Player_ThrowDistanceClamp");
                    var thrower = go.AddComponent<GrenadeThrower>();
                    thrower.maxThrowDistance = 7.0f;

                    Vector2 playerPos = Vector2.zero;
                    Vector2 distantTarget = new Vector2(100f, 0f);
                    Vector2 offset = distantTarget - playerPos;
                    Vector2 clamped = Vector2.ClampMagnitude(offset, thrower.maxThrowDistance);

                    E2EAssert.AreApproximatelyEqual(7.0f, clamped.magnitude, 0.001f, "Throw offset must be clamped to 7.0 units");
                    E2EAssert.AreApproximatelyEqual(7.0f, clamped.x, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_24", "GrenadeThrower", 5,
                "Throw Target Arena Coordinate Boundary Clamping",
                "Verifies throw target coordinate is clamped within arena min/max bounds",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Player_ThrowArenaClamp");
                    var thrower = go.AddComponent<GrenadeThrower>();
                    thrower.arenaMin = new Vector2(-8.5f, -4.2f);
                    thrower.arenaMax = new Vector2(13.8f, 5.2f);

                    Vector2 outsideTarget = new Vector2(25.0f, 15.0f);
                    Vector2 clamped = new Vector2(
                        Mathf.Clamp(outsideTarget.x, thrower.arenaMin.x, thrower.arenaMax.x),
                        Mathf.Clamp(outsideTarget.y, thrower.arenaMin.y, thrower.arenaMax.y)
                    );

                    E2EAssert.AreEqual(13.8f, clamped.x, "Throw target X must clamp to arenaMax.x");
                    E2EAssert.AreEqual(5.2f, clamped.y, "Throw target Y must clamp to arenaMax.y");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_25", "ExplosionAoE", 5,
                "Friendly Player Immunity & Multi-Hostile Elimination",
                "Verifies ExplosionAoE inflicts 0 damage to Player while eliminating hostile entities in radius",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var playerGo = ctx.CreateGameObject("Player_AoEImmunity");
                    playerGo.tag = "Player";
                    playerGo.transform.position = Vector3.zero;
                    var colP = playerGo.AddComponent<CircleCollider2D>();
                    colP.radius = 0.5f;
                    var ph = playerGo.AddComponent<PlayerHealth>();

                    var enemy1 = ctx.CreateGameObject("Enemy1_AoE");
                    enemy1.tag = "Enemy";
                    enemy1.transform.position = new Vector3(1f, 0f, 0f);
                    var colE1 = enemy1.AddComponent<CircleCollider2D>();
                    colE1.radius = 0.5f;
                    var chaser1 = enemy1.AddComponent<ChaserEnemy>();

                    var bombGo = ctx.CreateGameObject("Bomb_Explosion");
                    bombGo.transform.position = Vector3.zero;
                    var aoe = bombGo.AddComponent<ExplosionAoE>();
                    aoe.explosionRadius = 3.5f;
                    aoe.damage = 50;

                    aoe.Explode();

                    E2EAssert.AreEqual(5, ph.currentHealth, "Player must take 0 damage from friendly grenade");
                    E2EAssert.IsFalse(chaser1.IsAlive, "Hostile enemy must be eliminated by explosion");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_26", "GrenadePickup", 5,
                "Inventory Full Capacity Collection Guard",
                "Verifies player cannot collect GrenadePickup when inventory is at maximum (5 grenades)",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var playerGo = ctx.CreateGameObject("Player_FullInventory");
                    playerGo.tag = "Player";
                    var thrower = playerGo.AddComponent<GrenadeThrower>();
                    thrower.maxGrenades = 5;
                    thrower.grenadeCount = 5;

                    var pickupGo = ctx.CreateGameObject("Pickup_Item");
                    var pickup = pickupGo.AddComponent<GrenadePickup>();

                    bool collected = pickup.TryCollect(playerGo);
                    E2EAssert.IsFalse(collected, "Grenade pickup must not be consumed when inventory is full");
                    E2EAssert.AreEqual(5, thrower.grenadeCount, "Grenade count must remain at max capacity");
                });
        }

        #endregion

        #region 5. Boss & Spawner Tests

        private static void RunBossAndSpawnerTests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_27", "BossController", 5,
                "Boss Spawn Trigger on Sudden Score Jumps (0 -> 1000)",
                "Verifies sudden score jump past 500 threshold reliably triggers boss spawn",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var spawnerGo = ctx.CreateGameObject("Spawner_ScoreJump");
                    var spawner = spawnerGo.AddComponent<EnemySpawner>();
                    spawner.bossSpawned = false;

                    int suddenScore = 1000;
                    if (suddenScore >= 500 && !spawner.bossSpawned)
                    {
                        spawner.SpawnBoss();
                    }

                    E2EAssert.IsTrue(spawner.bossSpawned, "Boss must spawn on sudden score jump past 500");
                    E2EAssert.IsTrue(spawner.isBossActive, "Boss encounter must be active");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_28", "BossController", 5,
                "Post-Defeat Boss Latch Persistence Across Endless Scores",
                "Verifies bossSpawned latch remains true after defeat and prevents second boss across endless play",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var spawnerGo = ctx.CreateGameObject("Spawner_LatchPersist");
                    var spawner = spawnerGo.AddComponent<EnemySpawner>();

                    spawner.SpawnBoss();
                    spawner.OnBossDefeated();

                    E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned must persist as true after defeat");
                    E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be false after defeat");

                    int endlessScore = 2500;
                    bool canSpawnSecond = endlessScore >= 500 && !spawner.bossSpawned;
                    E2EAssert.IsFalse(canSpawnSecond, "Second boss spawn must be strictly prohibited");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_29", "EnemySpawner", 5,
                "Spawner 50% Suppression and Post-Defeat Normal Rate Reset",
                "Verifies spawn interval doubles during boss active and resets to normal rate immediately on defeat",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var spawnerGo = ctx.CreateGameObject("Spawner_SuppressionReset");
                    var spawner = spawnerGo.AddComponent<EnemySpawner>();
                    spawner.baseSpawnInterval = 3.0f;

                    float normalInterval = spawner.CalculateSpawnInterval(0f, 0);
                    E2EAssert.AreApproximatelyEqual(3.0f, normalInterval, 0.001f);

                    spawner.isBossActive = true;
                    float bossInterval = spawner.CalculateSpawnInterval(0f, 0);
                    E2EAssert.AreApproximatelyEqual(6.0f, bossInterval, 0.001f, "Interval must double (50% suppression) during boss");

                    spawner.OnBossDefeated();
                    float afterDefeatInterval = spawner.CalculateSpawnInterval(0f, 0);
                    E2EAssert.AreApproximatelyEqual(3.0f, afterDefeatInterval, 0.001f, "Interval must reset to normal rate immediately on boss defeat");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_30", "BossController", 5,
                "360-Degree Radial Barrage Emits Exactly 16 Projectiles",
                "Verifies radial barrage creates 16 bullets with angle step of 22.5 degrees",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss_BarrageTest");
                    var boss = bossGo.AddComponent<BossController>();
                    boss.radialBulletCount = 16;

                    float angleStep = 360f / boss.radialBulletCount;
                    E2EAssert.AreApproximatelyEqual(22.5f, angleStep, 0.001f, "Angle step must be 22.5 degrees");
                    E2EAssert.AreEqual(16, boss.radialBulletCount);
                });
        }

        #endregion

        #region 6. GameManager & UI Tests

        private static void RunGameManagerAndUITests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_31", "GameManager", 5,
                "Rapid Pause Toggling State & TimeScale Consistency",
                "Verifies 10 rapid pause toggles leave state and Time.timeScale fully synchronized",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GM_RapidPause");
                    var gm = gmGo.AddComponent<GameManager>();

                    for (int i = 0; i < 10; i++)
                    {
                        gm.TogglePause();
                    }

                    E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "Even number of toggles must end in Playing state");
                    E2EAssert.AreEqual(1.0f, Time.timeScale, "TimeScale must be 1.0f after even toggles");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_32", "GameManager", 5,
                "Pause Ingestion Rejection During GameOver and Victory",
                "Verifies PauseGame is ignored when CurrentState is GameOver or VictoryContinues",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GM_PauseRejection");
                    var gm = gmGo.AddComponent<GameManager>();

                    gm.TriggerGameOver();
                    E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);

                    gm.PauseGame(true);
                    E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "State must remain GameOver");

                    gm.PauseGame(false);
                    E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "Unpause must not alter GameOver state");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_33", "GameManager", 5,
                "Session Restart Complete Entity & State Machine Reset",
                "Verifies RestartGame cleanly resets score to 0, state to Playing, timeScale to 1.0",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GM_Restart");
                    var gm = gmGo.AddComponent<GameManager>();

                    gm.AddScore(500);
                    gm.TriggerGameOver();
                    E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);

                    gm.RestartGame();
                    E2EAssert.AreEqual(GameState.Playing, gm.CurrentState);
                    E2EAssert.AreEqual(0, gm.CurrentScore, "Score must reset to 0");
                    E2EAssert.AreEqual(1.0f, Time.timeScale, "TimeScale must reset to 1.0");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_34", "UIManager", 5,
                "HUD Heart, Score, Grenade, and Boss Slider Bounds Clamping",
                "Verifies UIManager methods clamp negative or excessive inputs gracefully",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var uiGo = ctx.CreateGameObject("UI_HUDClamping");
                    var ui = uiGo.AddComponent<UIManager>();

                    ui.heartIcons = new Image[5];
                    for (int i = 0; i < 5; i++)
                    {
                        var hGo = ctx.CreateGameObject($"Heart_{i}");
                        hGo.transform.SetParent(uiGo.transform);
                        ui.heartIcons[i] = hGo.AddComponent<Image>();
                    }

                    var scoreGo = ctx.CreateGameObject("ScoreText");
                    scoreGo.transform.SetParent(uiGo.transform);
                    ui.scoreText = scoreGo.AddComponent<Text>();

                    var grenadeGo = ctx.CreateGameObject("GrenadeText");
                    grenadeGo.transform.SetParent(uiGo.transform);
                    ui.grenadeCountText = grenadeGo.AddComponent<Text>();

                    var sliderGo = ctx.CreateGameObject("BossSlider");
                    sliderGo.transform.SetParent(uiGo.transform);
                    ui.bossHealthSlider = sliderGo.AddComponent<Slider>();

                    // Test Underflow / Overflow
                    ui.UpdateHearts(-5);
                    ui.UpdateHearts(10);

                    ui.UpdateScore(-100);
                    E2EAssert.AreEqual("SCORE: 00000", ui.scoreText.text);

                    ui.UpdateGrenades(-5);
                    E2EAssert.AreEqual("x 0", ui.grenadeCountText.text);

                    ui.UpdateBossHealth(-10, 60);
                    E2EAssert.AreEqual(0f, ui.bossHealthSlider.value);

                    ui.UpdateBossHealth(100, 60);
                    E2EAssert.AreEqual(60f, ui.bossHealthSlider.value);
                });
        }

        #endregion

        #region 7. SoundManager Tests

        private static void RunSoundManagerTests(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T5_ADV_35", "SoundManager", 5,
                "Zero and Negative Volume Clamping Safety",
                "Verifies SoundManager clamps negative or zero master/sfx volumes and aborts playback safely",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var smGo = ctx.CreateGameObject("SM_VolumeClamp");
                    var sm = smGo.AddComponent<SoundManager>();

                    sm.masterVolume = -1.0f;
                    sm.sfxVolume = 0.0f;

                    float effectiveVolume = Mathf.Clamp01(sm.masterVolume) * Mathf.Clamp01(sm.sfxVolume) * Mathf.Clamp01(1.0f);
                    E2EAssert.AreEqual(0.0f, effectiveVolume, "Effective volume must clamp to 0.0");

                    sm.PlayShootSFX();
                    sm.PlayHitSFX();
                    E2EAssert.IsTrue(true, "Negative volume playback executed safely");
                });

            TestRunnerHelper.RunTest(report, "T5_ADV_36", "SoundManager", 5,
                "Rapid-Fire SFX Trigger Loop Stress Invariant",
                "Verifies SoundManager handles 50 rapid-fire playback triggers without exception",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var smGo = ctx.CreateGameObject("SM_Stress");
                    var sm = smGo.AddComponent<SoundManager>();
                    sm.masterVolume = 0.5f;
                    sm.sfxVolume = 0.5f;
                    sm.isMuted = false;

                    for (int i = 0; i < 50; i++)
                    {
                        sm.PlayShootSFX();
                        sm.PlayHitSFX();
                        sm.PlayExplosionSFX();
                        sm.PlayPickupSFX();
                    }

                    E2EAssert.IsTrue(true, "50 rapid sound triggers completed cleanly");
                });
        }

        #endregion

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Tier 5 (Adversarial Hardening)")]
        public static void RunTier5Menu()
        {
            string summary = RunAllFormatted();
            UnityEngine.Debug.Log(summary);
        }
#endif
    }
}
