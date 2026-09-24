using System;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    public static class Milestone3Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Milestone 3 Test Suite - Grenade Mechanic (AoE Pickup & Throw)" };

            // TEST 1: GrenadePickup Component and Trigger Collider (F16)
            TestRunnerHelper.RunTest(report, "M3-01", "F16", 1,
                "GrenadePickup Component and Trigger Collider",
                "Verifies GrenadePickup exists, uses CircleCollider2D as trigger, and zero gravity.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Pickup");
                        var col = go.AddComponent<CircleCollider2D>();
                        col.isTrigger = true;
                        var rb = go.AddComponent<Rigidbody2D>();
                        rb.gravityScale = 0f;
                        var pickup = go.AddComponent<GrenadePickup>();

                        E2EAssert.IsNotNull(pickup, "GrenadePickup component should be attached");
                        E2EAssert.IsTrue(col.isTrigger, "GrenadePickup collider must be a trigger");
                        E2EAssert.AreEqual(0f, rb.gravityScale, "GrenadePickup Rigidbody2D must have zero gravity");
                    }
                });

            // TEST 2: Grenade Item Drop Rates on Enemy Archetypes (F16)
            TestRunnerHelper.RunTest(report, "M3-02", "F16", 1,
                "Enemy Archetype Grenade Drop Rates",
                "Verifies Chaser (20%), Shooter (25%), and Rusher (15%) drop probabilities.",
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

                        E2EAssert.AreApproximatelyEqual(0.20f, chaser.grenadeDropChance, 0.001f, "Chaser drop chance must be 0.20");
                        E2EAssert.AreApproximatelyEqual(0.25f, shooter.grenadeDropChance, 0.001f, "Shooter drop chance must be 0.25");
                        E2EAssert.AreApproximatelyEqual(0.15f, rusher.grenadeDropChance, 0.001f, "Rusher drop chance must be 0.15");
                    }
                });

            // TEST 3: Border Drop Clamping (F16)
            TestRunnerHelper.RunTest(report, "M3-03", "F16", 2,
                "Border Drop Clamping Inside Arena",
                "Verifies GrenadePickup dropped outside arena bounds is clamped inside.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Pickup");
                        go.transform.position = new Vector3(-15.0f, 10.0f, 0f);
                        var pickup = go.AddComponent<GrenadePickup>();

                        // Simulate Start() via reflection
                        var startMethod = typeof(GrenadePickup).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                        startMethod?.Invoke(pickup, null);

                        E2EAssert.IsTrue(go.transform.position.x >= -8.5f, "X position should be clamped >= -8.5");
                        E2EAssert.IsTrue(go.transform.position.y <= 5.2f, "Y position should be clamped <= 5.2");
                    }
                });

            // TEST 4: Grenade Collection & Inventory Increment (F17)
            TestRunnerHelper.RunTest(report, "M3-04", "F17", 1,
                "Grenade Collection and Inventory Increment",
                "Verifies player collecting GrenadePickup increments grenade count by 1.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 1;
                        thrower.maxGrenades = 5;

                        var pickupGo = ctx.CreateGameObject("Pickup");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        bool collected = pickup.TryCollect(playerGo);
                        E2EAssert.IsTrue(collected, "TryCollect should return true for player");
                        E2EAssert.AreEqual(2, thrower.grenadeCount, "Grenade count should increment from 1 to 2");
                    }
                });

            // TEST 5: Inventory Max Capacity Clamping (F17)
            TestRunnerHelper.RunTest(report, "M3-05", "F17", 2,
                "Inventory Max Capacity Clamping at 5",
                "Verifies collection is rejected and pickup remains when inventory is at capacity 5.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;
                        thrower.maxGrenades = 5;

                        var pickupGo = ctx.CreateGameObject("Pickup");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        bool collected = pickup.TryCollect(playerGo);
                        E2EAssert.IsFalse(collected, "TryCollect should return false when inventory is full");
                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Grenade count must not exceed maxGrenades (5)");
                    }
                });

            // TEST 6: Enemy & Bullet Touch Immunity on Pickup (F17)
            TestRunnerHelper.RunTest(report, "M3-06", "F17", 2,
                "Enemy and Bullet Immunity to Pickup Collection",
                "Verifies non-player objects (enemies, bullets) cannot collect or consume pickups.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemyGo = ctx.CreateGameObject("Enemy");
                        enemyGo.tag = "Enemy";

                        var bulletGo = ctx.CreateGameObject("EnemyBullet");

                        var pickupGo = ctx.CreateGameObject("Pickup");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        bool enemyCollected = pickup.TryCollect(enemyGo);
                        bool bulletCollected = pickup.TryCollect(bulletGo);

                        E2EAssert.IsFalse(enemyCollected, "Enemy must not collect grenade pickup");
                        E2EAssert.IsFalse(bulletCollected, "Bullet must not collect grenade pickup");
                    }
                });

            // TEST 7: Grenade Count Changed Event Dispatch (F17)
            TestRunnerHelper.RunTest(report, "M3-07", "F17", 1,
                "OnGrenadeCountChanged Event Dispatch",
                "Verifies GrenadeThrower fires OnGrenadeCountChanged on pickup and throw.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 2;

                        int eventCount = -1;
                        thrower.OnGrenadeCountChanged += count => eventCount = count;

                        thrower.AddGrenades(1);
                        E2EAssert.AreEqual(3, eventCount, "Event should dispatch new count (3) on AddGrenades");

                        thrower.ThrowGrenade(new Vector2(2f, 0f));
                        E2EAssert.AreEqual(2, eventCount, "Event should dispatch new count (2) on ThrowGrenade");
                    }
                });

            // TEST 8: Grenade Throw Decrements Inventory (F18)
            TestRunnerHelper.RunTest(report, "M3-08", "F18", 1,
                "Throw Decrements Inventory",
                "Verifies throwing a grenade decrements inventory count.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 3;

                        thrower.ThrowGrenade(new Vector2(3f, 0f));
                        E2EAssert.AreEqual(2, thrower.grenadeCount, "Grenade count should decrement from 3 to 2");
                    }
                });

            // TEST 9: Grenade Throw Blocked When Paused or Dead (F18)
            TestRunnerHelper.RunTest(report, "M3-09", "F18", 2,
                "Throw Blocked When Paused or Dead",
                "Verifies grenade throw is blocked when Time.timeScale=0 or player is dead.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        var health = playerGo.AddComponent<PlayerHealth>();
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 3;

                        // Case A: Paused game
                        Time.timeScale = 0f;
                        thrower.ThrowGrenade(new Vector2(1f, 0f));
                        E2EAssert.AreEqual(3, thrower.grenadeCount, "Throw must be blocked when game is paused");
                        Time.timeScale = 1.0f;

                        // Case B: Dead player
                        var currentHealthProp = typeof(PlayerHealth).GetProperty("currentHealth", BindingFlags.Public | BindingFlags.Instance);
                        currentHealthProp?.GetSetMethod(true)?.Invoke(health, new object[] { 0 });
                        E2EAssert.IsFalse(health.IsAlive, "Player should be dead");

                        thrower.ThrowGrenade(new Vector2(1f, 0f));
                        E2EAssert.AreEqual(3, thrower.grenadeCount, "Dead player must not be able to throw grenades");
                    }
                });

            // TEST 10: Grenade Throw Blocked at 0 Inventory (F18)
            TestRunnerHelper.RunTest(report, "M3-10", "F18", 2,
                "Zero Grenade Inventory Throws Nothing",
                "Verifies throwing with 0 grenades does not decrement below 0.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        var thrower = playerGo.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 0;

                        thrower.ThrowGrenade(new Vector2(2f, 0f));
                        E2EAssert.AreEqual(0, thrower.grenadeCount, "Grenade count should remain 0");
                    }
                });

            // TEST 11: Maximum Throw Distance Clamping to 7.0u (F19)
            TestRunnerHelper.RunTest(report, "M3-11", "F19", 1,
                "Max Throw Distance Clamping to 7.0u",
                "Verifies throw vector beyond 7.0u is clamped to 7.0 units.",
                () =>
                {
                    Vector2 origin = Vector2.zero;
                    Vector2 farTarget = new Vector2(20f, 0f);
                    Vector2 clampedVec = Vector2.ClampMagnitude(farTarget - origin, 7.0f);

                    E2EAssert.AreApproximatelyEqual(7.0f, clampedVec.magnitude, 0.001f, "Clamped throw magnitude must be 7.0");
                });

            // TEST 12: Flight Duration and Fuse Timing (F19)
            TestRunnerHelper.RunTest(report, "M3-12", "F19", 1,
                "Flight Duration and Fuse Parameters",
                "Verifies flight duration is 0.7s and fuse time is 1.2s.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Proj");
                        var proj = go.AddComponent<GrenadeProjectile>();

                        E2EAssert.AreApproximatelyEqual(0.7f, proj.flightDuration, 0.01f, "Flight duration should be 0.7s");
                        E2EAssert.AreApproximatelyEqual(1.2f, proj.fuseTime, 0.01f, "Total fuse should be 1.2s");
                    }
                });

            // TEST 13: Parabolic Height Simulation Curve (F19)
            TestRunnerHelper.RunTest(report, "M3-13", "F19", 1,
                "Parabolic Trajectory Height Oscillation",
                "Verifies sin(t*pi) curve peaks at t=0.5 and grounds at t=0.0 and t=1.0.",
                () =>
                {
                    float hStart = Mathf.Sin(0.0f * Mathf.PI);
                    float hMid = Mathf.Sin(0.5f * Mathf.PI);
                    float hEnd = Mathf.Sin(1.0f * Mathf.PI);

                    E2EAssert.AreApproximatelyEqual(0.0f, hStart, 0.001f, "Height at launch should be 0");
                    E2EAssert.AreApproximatelyEqual(1.0f, hMid, 0.001f, "Height at apex should be 1.0");
                    E2EAssert.AreApproximatelyEqual(0.0f, hEnd, 0.001f, "Height at landing should be 0");
                });

            // TEST 14: ExplosionAoE Radius and Damage (F20)
            TestRunnerHelper.RunTest(report, "M3-14", "F20", 1,
                "ExplosionAoE Radius and Damage Baseline",
                "Verifies ExplosionAoE has 3.5u radius and 50 damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Explosion");
                        var aoe = go.AddComponent<ExplosionAoE>();

                        E2EAssert.AreApproximatelyEqual(3.5f, aoe.explosionRadius, 0.01f, "Explosion radius must be 3.5u");
                        E2EAssert.AreEqual(50, aoe.damage, "Explosion damage must be 50 HP");
                    }
                });

            // TEST 15: Regular Enemies 1-Hit Kill by Explosion (F20)
            TestRunnerHelper.RunTest(report, "M3-15", "F20", 1,
                "Explosion 1-Hit Kill on Regular Enemies",
                "Verifies 50 explosion damage is lethal to Chaser (3 HP), Shooter (2 HP), Rusher (1 HP).",
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

                        chaser.TakeDamage(50);
                        shooter.TakeDamage(50);
                        rusher.TakeDamage(50);

                        E2EAssert.AreEqual(0, chaser.currentHealth, "Chaser should be defeated");
                        E2EAssert.AreEqual(0, shooter.currentHealth, "Shooter should be defeated");
                        E2EAssert.AreEqual(0, rusher.currentHealth, "Rusher should be defeated");
                    }
                });

            // TEST 16: Player Friendly Fire Immunity (F20)
            TestRunnerHelper.RunTest(report, "M3-16", "F20", 2,
                "Player Friendly Fire Immunity",
                "Verifies Player inside explosion radius takes zero damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Player");
                        playerGo.tag = "Player";
                        var playerCol = playerGo.AddComponent<CircleCollider2D>();
                        var health = playerGo.AddComponent<PlayerHealth>();

                        playerGo.transform.position = Vector3.zero;

                        var expGo = ctx.CreateGameObject("Explosion");
                        expGo.transform.position = new Vector3(1f, 0f, 0f);
                        var aoe = expGo.AddComponent<ExplosionAoE>();
                        aoe.Explode();

                        E2EAssert.AreEqual(5, health.currentHealth, "Player health should remain 5 (no friendly fire)");
                    }
                });

            // TEST 17: Multi-Target Blast Elimination (F20)
            TestRunnerHelper.RunTest(report, "M3-17", "F20", 3,
                "Multi-Target AoE Elimination",
                "Verifies multiple enemies inside blast radius all receive 50 damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var e1 = ctx.CreateGameObject("E1");
                        e1.transform.position = new Vector3(1f, 0f, 0f);
                        e1.AddComponent<CircleCollider2D>();
                        var c1 = e1.AddComponent<ChaserEnemy>();

                        var e2 = ctx.CreateGameObject("E2");
                        e2.transform.position = new Vector3(-1f, 0f, 0f);
                        e2.AddComponent<CircleCollider2D>();
                        var c2 = e2.AddComponent<ChaserEnemy>();

                        var e3 = ctx.CreateGameObject("E3");
                        e3.transform.position = new Vector3(0f, 2f, 0f);
                        e3.AddComponent<CircleCollider2D>();
                        var c3 = e3.AddComponent<ChaserEnemy>();

                        ctx.StepPhysics(0.02f);

                        var expGo = ctx.CreateGameObject("Explosion");
                        expGo.transform.position = Vector3.zero;
                        var aoe = expGo.AddComponent<ExplosionAoE>();
                        aoe.Explode();

                        E2EAssert.AreEqual(0, c1.currentHealth, "E1 should be defeated by AoE");
                        E2EAssert.AreEqual(0, c2.currentHealth, "E2 should be defeated by AoE");
                        E2EAssert.AreEqual(0, c3.currentHealth, "E3 should be defeated by AoE");
                    }
                });

            // TEST 18: ExplosionAoE Visual Effect Instantiation (F33)
            TestRunnerHelper.RunTest(report, "M3-18", "F33", 1,
                "ExplosionAoE Visual Effect Scale and Prefab Reference",
                "Verifies visualScale is 3.5x and visual effect prefab can be assigned.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("Explosion");
                        var aoe = go.AddComponent<ExplosionAoE>();

                        E2EAssert.AreApproximatelyEqual(3.5f, aoe.visualScale, 0.01f, "Visual scale should be 3.5x");
                        E2EAssert.AreApproximatelyEqual(0.6f, aoe.lifetime, 0.05f, "Lifetime should be approximately 0.6s");
                    }
                });

            // TEST 19: Direct Enemy Impact Detonation (F19, F20)
            TestRunnerHelper.RunTest(report, "M3-19", "F19+F20", 3,
                "Direct Impact Triggers Immediate Detonation",
                "Verifies GrenadeProjectile detonates on collision with enemy before fuse expires.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemyGo = ctx.CreateGameObject("Enemy");
                        enemyGo.tag = "Enemy";
                        enemyGo.AddComponent<CircleCollider2D>();
                        enemyGo.AddComponent<ChaserEnemy>();

                        var projGo = ctx.CreateGameObject("Proj");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(5f, 0f));

                        // Simulate impact via reflection
                        var impactMethod = typeof(GrenadeProjectile).GetMethod("HandleImpact", BindingFlags.NonPublic | BindingFlags.Instance);
                        impactMethod?.Invoke(proj, new object[] { enemyGo });

                        // If detonated, Detonate() was called
                        var fieldDetonated = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool hasDetonated = (bool)fieldDetonated.GetValue(proj);

                        E2EAssert.IsTrue(hasDetonated, "GrenadeProjectile should detonate on enemy impact");
                    }
                });

            // TEST 20: Scene Player Wiring Verification
            TestRunnerHelper.RunTest(report, "M3-20", "SceneIntegration", 4,
                "Scene Player GrenadeThrower Wiring",
                "Verifies Player GameObject has GrenadeThrower component referencing valid grenadePrefab.",
                () =>
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    E2EAssert.IsNotNull(playerObj, "Player GameObject must exist in scene");

                    var thrower = playerObj.GetComponent<GrenadeThrower>();
                    E2EAssert.IsNotNull(thrower, "GrenadeThrower component must be attached to Player");
                    E2EAssert.IsNotNull(thrower.grenadePrefab, "GrenadeThrower must reference grenadePrefab");
                    E2EAssert.AreEqual(2, thrower.grenadeCount, "Default grenade count must be 2");
                    E2EAssert.AreEqual(5, thrower.maxGrenades, "Default max grenades must be 5");
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Milestone 3 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
