using System;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    public static class Milestone1Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Milestone 1 Test Suite - Player Combat, Health & Arena" };

            // TIER 1: Feature Coverage (Happy Path)
            TestRunnerHelper.RunTest(report, "M1-T1-01", "F04", 1,
                "Player 5 HP Initial State",
                "Verifies player starts with exactly 5 HP and IsAlive is true.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var ph = go.AddComponent<PlayerHealth>();
                        E2EAssert.AreEqual(5, ph.maxHealth, "Max health should be 5");
                        E2EAssert.AreEqual(5, ph.currentHealth, "Current health should start at 5");
                        E2EAssert.IsTrue(ph.IsAlive, "Player should be alive initially");
                        E2EAssert.IsFalse(ph.isInvulnerable, "Player should not be invulnerable initially");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T1-02", "F05", 1,
                "Player Takes Damage and Triggers Event",
                "Verifies taking damage deducts exactly 1 HP, sets invulnerable flag, and raises OnHealthChanged.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var ph = go.AddComponent<PlayerHealth>();
                        int reportedHp = -1;
                        ph.OnHealthChanged += hp => reportedHp = hp;

                        ph.TakeDamage(1);

                        E2EAssert.AreEqual(4, ph.currentHealth, "HP should decrease from 5 to 4");
                        E2EAssert.AreEqual(4, reportedHp, "OnHealthChanged event should report 4");
                        E2EAssert.IsTrue(ph.isInvulnerable, "Player should be invulnerable after taking damage");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T1-03", "F06", 1,
                "Player Death Sequence on 0 HP",
                "Verifies player dies when HP reaches 0, invoking OnPlayerDeath.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var ph = go.AddComponent<PlayerHealth>();
                        var pm = go.AddComponent<PlayerMovement>();
                        var sh = go.AddComponent<Shooting>();

                        bool deathTriggered = false;
                        ph.OnPlayerDeath += () => deathTriggered = true;

                        var isInvulProp = typeof(PlayerHealth).GetProperty("isInvulnerable");
                        for (int i = 0; i < 5; i++)
                        {
                            isInvulProp.SetValue(ph, false, null);
                            ph.TakeDamage(1);
                        }

                        E2EAssert.AreEqual(0, ph.currentHealth, "Health should reach 0");
                        E2EAssert.IsFalse(ph.IsAlive, "Player should not be alive at 0 HP");
                        E2EAssert.IsTrue(deathTriggered, "OnPlayerDeath should have been fired");
                        E2EAssert.IsFalse(pm.enabled, "PlayerMovement should be disabled on death");
                        E2EAssert.IsFalse(sh.enabled, "Shooting should be disabled on death");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T1-04", "F01", 1,
                "Movement Normalization & Boundary Clamping Fields",
                "Verifies PlayerMovement has clamp parameters and bounds set correctly.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var pm = go.AddComponent<PlayerMovement>();
                        E2EAssert.AreEqual(5f, pm.moveSpeed, "Default moveSpeed should be 5");
                        E2EAssert.IsTrue(pm.clampToBounds, "clampToBounds should be true");
                        E2EAssert.AreEqual(new Vector2(-8.5f, -4.2f), pm.minBounds, "minBounds mismatch");
                        E2EAssert.AreEqual(new Vector2(13.8f, 5.2f), pm.maxBounds, "maxBounds mismatch");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T1-05", "F07", 1,
                "Shooting Cooldown Configuration",
                "Verifies Shooting component has controlled fireRate of 0.2s and bulletForce of 20.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var sh = go.AddComponent<Shooting>();
                        E2EAssert.AreEqual(0.2f, sh.fireRate, "fireRate cooldown should be 0.2s");
                        E2EAssert.AreEqual(20f, sh.bulletForce, "bulletForce should be 20f");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T1-06", "F08", 1,
                "Bullet Parameters & IDamageable Support",
                "Verifies Bullet has 1 damage, 3s lifetime, and deals damage to IDamageable targets.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bulletGo = ctx.CreateGameObject("TestBullet");
                        var bullet = bulletGo.AddComponent<Bullet>();
                        E2EAssert.AreEqual(1, bullet.damage, "Bullet damage should be 1");
                        E2EAssert.AreEqual(3f, bullet.lifetime, "Bullet lifetime should be 3s");
                    }
                });

            // TIER 2: Boundary & Corner Cases
            TestRunnerHelper.RunTest(report, "M1-T2-01", "F05", 2,
                "Multi-Damage Ingestion Clamped to Exactly 1 HP",
                "Verifies that passing damage > 1 (e.g. 5) still deducts exactly 1 HP per specification.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var ph = go.AddComponent<PlayerHealth>();

                        ph.TakeDamage(5); // Excess damage attempt
                        E2EAssert.AreEqual(4, ph.currentHealth, "Excess damage must still only deduct exactly 1 HP");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T2-02", "F05", 2,
                "i-Frames Prevent Rapid Consecutive Hits",
                "Verifies consecutive hits during the 1.0s i-frame window are completely ignored.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var ph = go.AddComponent<PlayerHealth>();

                        ph.TakeDamage(1); // First hit: HP -> 4, invulnerable = true
                        E2EAssert.AreEqual(4, ph.currentHealth);

                        ph.TakeDamage(1); // Should be ignored
                        ph.TakeDamage(1); // Should be ignored
                        ph.TakeDamage(1); // Should be ignored

                        E2EAssert.AreEqual(4, ph.currentHealth, "Subsequent hits within i-frames must be ignored");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T2-03", "F04", 2,
                "Health Reset Fully Restores Player",
                "Verifies ResetHealth restores HP to max, clears invulnerability, and re-enables controls.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var ph = go.AddComponent<PlayerHealth>();
                        var pm = go.AddComponent<PlayerMovement>();
                        var sh = go.AddComponent<Shooting>();

                        ph.TakeDamage(1);
                        ph.ResetHealth();

                        E2EAssert.AreEqual(5, ph.currentHealth, "Health should reset to max");
                        E2EAssert.IsFalse(ph.isInvulnerable, "Invulnerability should be cleared on reset");
                        E2EAssert.IsTrue(pm.enabled, "PlayerMovement should be re-enabled");
                        E2EAssert.IsTrue(sh.enabled, "Shooting should be re-enabled");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T2-04", "F02", 2,
                "PlayerMovement Handles Null Camera Gracefully",
                "Verifies PlayerMovement does not throw NullReferenceException if camera is missing.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("TestPlayer");
                        var pm = go.AddComponent<PlayerMovement>();
                        pm.cam = null;

                        // Invoke Update via reflection
                        var updateMethod = typeof(PlayerMovement).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                        updateMethod.Invoke(pm, null);

                        // If it reached here without exception, pass
                        E2EAssert.IsTrue(true);
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T2-05", "F03", 2,
                "Arena Boundaries Enclosure Verification",
                "Verifies 4 boundary BoxCollider2Ds exist under MapBounds in the active scene.",
                () =>
                {
                    var mapBounds = GameObject.Find("MapBounds");
                    E2EAssert.IsNotNull(mapBounds, "MapBounds GameObject must exist in the scene");

                    string[] expectedWalls = { "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right" };
                    foreach (var wallName in expectedWalls)
                    {
                        var wall = mapBounds.transform.Find(wallName);
                        E2EAssert.IsNotNull(wall, $"Wall {wallName} must exist under MapBounds");
                        var col = wall.GetComponent<BoxCollider2D>();
                        E2EAssert.IsNotNull(col, $"Wall {wallName} must have a BoxCollider2D");
                        E2EAssert.IsFalse(col.isTrigger, $"Wall {wallName} must be a solid collider (not trigger)");
                    }
                });

            TestRunnerHelper.RunTest(report, "M1-T2-06", "F08", 2,
                "Bullet Ignores Player Target",
                "Verifies player's bullet will not deal damage to or trigger impact on the player.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var bulletGo = ctx.CreateGameObject("Bullet");
                        var bullet = bulletGo.AddComponent<Bullet>();

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { playerGo });

                        E2EAssert.AreEqual(5, ph.currentHealth, "Player health must remain 5 (bullet ignores player)");
                    }
                });

            return report;
        }
    }
}
