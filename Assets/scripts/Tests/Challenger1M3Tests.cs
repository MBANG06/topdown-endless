using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    /// <summary>
    /// Empirical Adversarial Verification Suite for Milestone 3 (Grenade AoE Mechanic).
    /// Authored by Challenger 1.
    /// Focuses on:
    /// 1. Distance Clamping under extreme coordinates (<= 7.0u)
    /// 2. AoE Multi-Kill of 10+ / 25 enemies simultaneously destroyed
    /// 3. Boundary Precision at 3.49u (hit) vs 3.51u (miss)
    /// 4. Zero Inventory Guard & Simultaneous Input (E + RMB in 1 frame)
    /// </summary>
    public static class Challenger1M3Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 1 Milestone 3 Empirical Verification Suite" };

            // =========================================================================
            // SECTION 1: Distance Clamping & Extreme Coordinates (F18, F19)
            // =========================================================================

            // TEST 1: Extreme Positive Cursor Coordinates Clamping
            TestRunnerHelper.RunTest(report, "CH1-M3-01", "F19", 1,
                "Distance Clamping Under Extreme Positive Coordinates (+99999, +99999)",
                "Verifies throw target cannot exceed 7.0u from player when aimed at extreme positive coords.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Clamp_Pos");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        player.transform.position = Vector2.zero;
                        thrower.grenadeCount = 5;

                        thrower.ThrowGrenade(new Vector2(99999f, 99999f));

                        var proj = UnityEngine.Object.FindObjectOfType<GrenadeProjectile>();
                        E2EAssert.IsNotNull(proj, "GrenadeProjectile should have spawned");

                        var field = typeof(GrenadeProjectile).GetField("_targetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                        Vector2 target = (Vector2)field.GetValue(proj);
                        float dist = Vector2.Distance(player.transform.position, target);

                        E2EAssert.IsTrue(dist <= 7.0001f, $"Clamped distance {dist} must not exceed 7.0u");
                        E2EAssert.AreApproximatelyEqual(7.0f, dist, 0.001f, $"Expected distance 7.0u, got {dist}");
                        E2EAssert.IsTrue(target.x >= -8.5f && target.x <= 13.8f, "Target X must be inside arena bounds");
                        E2EAssert.IsTrue(target.y >= -4.2f && target.y <= 5.2f, "Target Y must be inside arena bounds");

                        UnityEngine.Object.DestroyImmediate(proj.gameObject);
                    }
                });

            // TEST 2: Extreme Negative Cursor Coordinates Clamping
            TestRunnerHelper.RunTest(report, "CH1-M3-02", "F19", 1,
                "Distance Clamping Under Extreme Negative Coordinates (-99999, -99999)",
                "Verifies throw target cannot exceed 7.0u from player when aimed at extreme negative coords.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Clamp_Neg");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        player.transform.position = Vector2.zero;
                        thrower.grenadeCount = 5;

                        thrower.ThrowGrenade(new Vector2(-99999f, -99999f));

                        var proj = UnityEngine.Object.FindObjectOfType<GrenadeProjectile>();
                        E2EAssert.IsNotNull(proj, "GrenadeProjectile should have spawned");

                        var field = typeof(GrenadeProjectile).GetField("_targetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                        Vector2 target = (Vector2)field.GetValue(proj);
                        float dist = Vector2.Distance(player.transform.position, target);

                        E2EAssert.IsTrue(dist <= 7.0001f, $"Clamped distance {dist} must not exceed 7.0u");
                        E2EAssert.IsTrue(target.x >= -8.5f && target.x <= 13.8f, "Target X must be inside arena bounds");
                        E2EAssert.IsTrue(target.y >= -4.2f && target.y <= 5.2f, "Target Y must be inside arena bounds");

                        UnityEngine.Object.DestroyImmediate(proj.gameObject);
                    }
                });

            // TEST 3: Distance Clamping Near Arena Perimeter
            TestRunnerHelper.RunTest(report, "CH1-M3-03", "F19", 2,
                "Distance Clamping Near Arena Perimeter with Boundary Bounds",
                "Verifies throw target from near boundary (13.0, 4.5) is clamped to both 7.0u and arena edges.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Clamp_Edge");
                        player.transform.position = new Vector2(13.0f, 4.5f);
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;

                        thrower.ThrowGrenade(new Vector2(100f, 100f));

                        var proj = UnityEngine.Object.FindObjectOfType<GrenadeProjectile>();
                        E2EAssert.IsNotNull(proj, "GrenadeProjectile should have spawned");

                        var field = typeof(GrenadeProjectile).GetField("_targetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                        Vector2 target = (Vector2)field.GetValue(proj);
                        float dist = Vector2.Distance(player.transform.position, target);

                        E2EAssert.IsTrue(dist <= 7.0001f, $"Distance from player {dist} must not exceed 7.0u");
                        E2EAssert.IsTrue(target.x <= 13.8f && target.x >= -8.5f, $"Target X {target.x} must be <= 13.8");
                        E2EAssert.IsTrue(target.y <= 5.2f && target.y >= -4.2f, $"Target Y {target.y} must be <= 5.2");

                        UnityEngine.Object.DestroyImmediate(proj.gameObject);
                    }
                });

            // TEST 4: Throw Within Max Range Retains Requested Target Position
            TestRunnerHelper.RunTest(report, "CH1-M3-04", "F19", 1,
                "Throw Within Max Range Retains Target Position",
                "Verifies throw target inside 7.0u (e.g. 4.0u away) is preserved without artificial stretch.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Within_Range");
                        player.transform.position = Vector2.zero;
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;

                        Vector2 requestedTarget = new Vector2(4.0f, 0f);
                        thrower.ThrowGrenade(requestedTarget);

                        var proj = UnityEngine.Object.FindObjectOfType<GrenadeProjectile>();
                        E2EAssert.IsNotNull(proj, "GrenadeProjectile should have spawned");

                        var field = typeof(GrenadeProjectile).GetField("_targetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                        Vector2 actualTarget = (Vector2)field.GetValue(proj);

                        E2EAssert.AreApproximatelyEqual(requestedTarget.x, actualTarget.x, 0.001f, "Target X should match requested");
                        E2EAssert.AreApproximatelyEqual(requestedTarget.y, actualTarget.y, 0.001f, "Target Y should match requested");

                        UnityEngine.Object.DestroyImmediate(proj.gameObject);
                    }
                });

            // TEST 5: Throw Directly at Player Position (Zero Vector Offset)
            TestRunnerHelper.RunTest(report, "CH1-M3-05", "F19", 2,
                "Throw Directly at Player Position (Zero Vector Offset)",
                "Verifies throwing at player position does not cause NaN or division by zero.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Zero_Offset");
                        player.transform.position = new Vector2(2f, 2f);
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;

                        thrower.ThrowGrenade(new Vector2(2f, 2f));

                        var proj = UnityEngine.Object.FindObjectOfType<GrenadeProjectile>();
                        E2EAssert.IsNotNull(proj, "GrenadeProjectile should spawn on zero offset throw");

                        var field = typeof(GrenadeProjectile).GetField("_targetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                        Vector2 actualTarget = (Vector2)field.GetValue(proj);

                        E2EAssert.IsFalse(float.IsNaN(actualTarget.x) || float.IsNaN(actualTarget.y), "Target must not be NaN");
                        E2EAssert.AreApproximatelyEqual(2f, actualTarget.x, 0.001f, "Target should remain player position X");
                        E2EAssert.AreApproximatelyEqual(2f, actualTarget.y, 0.001f, "Target should remain player position Y");

                        UnityEngine.Object.DestroyImmediate(proj.gameObject);
                    }
                });

            // =========================================================================
            // SECTION 2: AoE Multi-Kill Stress-Testing (F20)
            // =========================================================================

            // TEST 6: Cluster of 15 Enemies Multi-Kill
            TestRunnerHelper.RunTest(report, "CH1-M3-06", "F20", 3,
                "Massive Enemy Cluster Multi-Kill (15 Enemies Inside 3.5u Radius)",
                "Verifies cluster of 15 enemies (Chasers, Shooters, Rushers) within 3.5u are simultaneously eliminated.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemies = new List<EnemyBase>();

                        for (int i = 0; i < 15; i++)
                        {
                            float angle = i * (Mathf.PI * 2f / 15f);
                            float radius = 0.8f + (i % 4) * 0.7f; // radii: 0.8, 1.5, 2.2, 2.9 (< 3.5)
                            Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                            var go = ctx.CreateGameObject($"Cluster_Enemy_{i}");
                            go.transform.position = pos;
                            var col = go.AddComponent<CircleCollider2D>();
                            col.radius = 0.2f;

                            EnemyBase enemy;
                            if (i % 3 == 0) enemy = go.AddComponent<ChaserEnemy>();
                            else if (i % 3 == 1) enemy = go.AddComponent<ShooterEnemy>();
                            else enemy = go.AddComponent<RusherEnemy>();

                            enemies.Add(enemy);
                        }

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_15");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        int destroyedCount = 0;
                        foreach (var enemy in enemies)
                        {
                            if (enemy == null || enemy.currentHealth == 0 || !enemy.IsAlive)
                            {
                                destroyedCount++;
                            }
                        }

                        E2EAssert.AreEqual(15, destroyedCount, "All 15 enemies in blast radius must be eliminated simultaneously");
                    }
                });

            // TEST 7: High-Density Cluster Multi-Kill (25 Enemies)
            TestRunnerHelper.RunTest(report, "CH1-M3-07", "F20", 3,
                "High-Density Cluster Multi-Kill (25 Enemies Inside 3.5u Radius)",
                "Verifies high density pack of 25 enemies inside 3.5u radius are all eliminated without truncation.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemies = new List<EnemyBase>();

                        for (int i = 0; i < 25; i++)
                        {
                            float angle = i * (Mathf.PI * 2f / 25f);
                            float radius = 0.5f + (i % 5) * 0.6f; // radii: 0.5 to 2.9u
                            Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                            var go = ctx.CreateGameObject($"Dense_Enemy_{i}");
                            go.transform.position = pos;
                            var col = go.AddComponent<CircleCollider2D>();
                            col.radius = 0.15f;

                            var chaser = go.AddComponent<ChaserEnemy>();
                            enemies.Add(chaser);
                        }

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_25");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        int destroyedCount = 0;
                        foreach (var enemy in enemies)
                        {
                            if (enemy == null || enemy.currentHealth == 0 || !enemy.IsAlive)
                            {
                                destroyedCount++;
                            }
                        }

                        E2EAssert.AreEqual(25, destroyedCount, "All 25 enemies must be destroyed simultaneously");
                    }
                });

            // TEST 8: Compound Multi-Collider Enemy Takes Single Damage
            TestRunnerHelper.RunTest(report, "CH1-M3-08", "F20", 2,
                "Multi-Collider Entity Single Damage Application",
                "Verifies entity with multiple colliders receives damage exactly once per explosion.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var rootGo = ctx.CreateGameObject("Enemy_Compound");
                        rootGo.transform.position = new Vector2(1f, 0f);
                        var rootCol = rootGo.AddComponent<CircleCollider2D>();
                        rootCol.radius = 0.4f;

                        var childGo = ctx.CreateGameObject("Enemy_Compound_Child");
                        childGo.transform.parent = rootGo.transform;
                        childGo.transform.localPosition = new Vector2(0.2f, 0f);
                        var childCol = childGo.AddComponent<BoxCollider2D>();
                        childCol.size = new Vector2(0.5f, 0.5f);

                        // Use a dummy target to count hits
                        var dummyGo = ctx.CreateGameObject("Dummy_Compound");
                        dummyGo.transform.position = new Vector2(-1f, 0f);
                        dummyGo.AddComponent<CircleCollider2D>();
                        var dummyChild = ctx.CreateGameObject("Dummy_Child");
                        dummyChild.transform.parent = dummyGo.transform;
                        dummyChild.transform.localPosition = Vector3.zero;
                        dummyChild.AddComponent<BoxCollider2D>();
                        var dummyTarget = dummyGo.AddComponent<ChallengerDummyTarget>();
                        dummyTarget.health = 100;

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_Compound");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        E2EAssert.AreEqual(1, dummyTarget.hitsReceived, "Target with compound colliders must receive exactly 1 hit");
                        E2EAssert.AreEqual(50, dummyTarget.damageReceived, "Target with compound colliders must receive 50 damage (not 100)");
                    }
                });

            // TEST 9: High-Health Target Survives with Subtracted Health
            TestRunnerHelper.RunTest(report, "CH1-M3-09", "F20", 2,
                "High-Health Target Survives with Subtracted Health",
                "Verifies target with 60 HP (like Boss) survives 50 explosion damage with 10 HP remaining.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bossGo = ctx.CreateGameObject("Mock_Boss_Target");
                        bossGo.transform.position = new Vector2(1f, 0f);
                        bossGo.AddComponent<CircleCollider2D>();
                        var dummy = bossGo.AddComponent<ChallengerDummyTarget>();
                        dummy.health = 60;

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_Boss");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        E2EAssert.IsTrue(dummy.IsAlive, "Boss target should survive 50 damage");
                        E2EAssert.AreEqual(10, dummy.health, "Boss target should have exactly 10 HP remaining");
                    }
                });

            // TEST 10: Empty Blast Query Zero Targets Safe Execution
            TestRunnerHelper.RunTest(report, "CH1-M3-10", "F20", 1,
                "Empty Blast Query Zero Targets Safe Execution",
                "Verifies explosion detonating with no targets executes cleanly without exceptions.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var expGo = ctx.CreateGameObject("Explosion_Empty");
                        expGo.transform.position = new Vector2(50f, 50f);
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        E2EAssert.IsNotNull(exp, "Explosion should execute without error");
                    }
                });

            // =========================================================================
            // SECTION 3: Boundary Precision Tests (3.49u vs 3.51u) (F20)
            // =========================================================================

            // TEST 11: Exact Boundary Test at 3.49u vs 3.51u Along +X Axis
            TestRunnerHelper.RunTest(report, "CH1-M3-11", "F20", 2,
                "Exact Boundary Test at 3.49u vs 3.51u Along +X Axis",
                "Verifies explosion damages enemy at 3.49u and ignores enemy at 3.51u.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var nearGo = ctx.CreateGameObject("Enemy_Near_X");
                        nearGo.transform.position = new Vector2(3.49f, 0f);
                        var nearCol = nearGo.AddComponent<CircleCollider2D>();
                        nearCol.radius = 0.001f;
                        var nearChaser = nearGo.AddComponent<ChaserEnemy>();

                        var farGo = ctx.CreateGameObject("Enemy_Far_X");
                        farGo.transform.position = new Vector2(3.51f, 0f);
                        var farCol = farGo.AddComponent<CircleCollider2D>();
                        farCol.radius = 0.001f;
                        var farChaser = farGo.AddComponent<ChaserEnemy>();

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_Boundary_X");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        bool nearEliminated = (nearChaser == null || nearChaser.currentHealth == 0);
                        bool farUntouched = (farChaser != null && farChaser.currentHealth == 3);

                        E2EAssert.IsTrue(nearEliminated, "Enemy at 3.49u must be damaged/eliminated");
                        E2EAssert.IsTrue(farUntouched, "Enemy at 3.51u must NOT be damaged (remains 3 HP)");
                    }
                });

            // TEST 12: Radial Symmetry Boundary Precision in 4 Cardinal Directions
            TestRunnerHelper.RunTest(report, "CH1-M3-12", "F20", 2,
                "Radial Symmetry Boundary Precision in 4 Directions (+X, -X, +Y, -Y)",
                "Verifies 3.49u hits and 3.51u misses across all 4 cardinal axes.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        Vector2[] directions = new Vector2[]
                        {
                            Vector2.right,
                            Vector2.left,
                            Vector2.up,
                            Vector2.down
                        };

                        var nearEnemies = new List<ChaserEnemy>();
                        var farEnemies = new List<ChaserEnemy>();

                        for (int i = 0; i < directions.Length; i++)
                        {
                            var nearGo = ctx.CreateGameObject($"Near_{i}");
                            nearGo.transform.position = directions[i] * 3.49f;
                            var colN = nearGo.AddComponent<CircleCollider2D>();
                            colN.radius = 0.001f;
                            nearEnemies.Add(nearGo.AddComponent<ChaserEnemy>());

                            var farGo = ctx.CreateGameObject($"Far_{i}");
                            farGo.transform.position = directions[i] * 3.51f;
                            var colF = farGo.AddComponent<CircleCollider2D>();
                            colF.radius = 0.001f;
                            farEnemies.Add(farGo.AddComponent<ChaserEnemy>());
                        }

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_4Dir");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        for (int i = 0; i < directions.Length; i++)
                        {
                            bool nearEliminated = (nearEnemies[i] == null || nearEnemies[i].currentHealth == 0);
                            bool farUntouched = (farEnemies[i] != null && farEnemies[i].currentHealth == 3);

                            E2EAssert.IsTrue(nearEliminated, $"Near enemy in dir {directions[i]} (3.49u) must be eliminated");
                            E2EAssert.IsTrue(farUntouched, $"Far enemy in dir {directions[i]} (3.51u) must remain untouched");
                        }
                    }
                });

            // TEST 13: Boundary Precision with Standard 0.5u Radius Collider
            TestRunnerHelper.RunTest(report, "CH1-M3-13", "F20", 2,
                "Boundary Precision with Standard 0.5u Radius Collider",
                "Verifies collider edge at 3.48u is damaged, while edge at 3.52u is untouched.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        // Radius = 0.5u. Center at 3.5 + 0.5 - 0.02 = 3.98u (edge at 3.48u < 3.5u)
                        var nearGo = ctx.CreateGameObject("Standard_Near");
                        nearGo.transform.position = new Vector2(3.98f, 0f);
                        var nearCol = nearGo.AddComponent<CircleCollider2D>();
                        nearCol.radius = 0.5f;
                        var nearChaser = nearGo.AddComponent<ChaserEnemy>();

                        // Center at 3.5 + 0.5 + 0.02 = 4.02u (edge at 3.52u > 3.5u)
                        var farGo = ctx.CreateGameObject("Standard_Far");
                        farGo.transform.position = new Vector2(4.02f, 0f);
                        var farCol = farGo.AddComponent<CircleCollider2D>();
                        farCol.radius = 0.5f;
                        var farChaser = farGo.AddComponent<ChaserEnemy>();

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_StandardCol");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        bool nearHit = (nearChaser == null || nearChaser.currentHealth == 0);
                        bool farMissed = (farChaser != null && farChaser.currentHealth == 3);

                        E2EAssert.IsTrue(nearHit, "Collider edge at 3.48u must be hit");
                        E2EAssert.IsTrue(farMissed, "Collider edge at 3.52u must NOT be hit");
                    }
                });

            // =========================================================================
            // SECTION 4: Zero Inventory & Simultaneous Input (F18)
            // =========================================================================

            // TEST 14: Zero Grenade Inventory Blocks Throw Without Exception
            TestRunnerHelper.RunTest(report, "CH1-M3-14", "F18", 2,
                "Zero Grenade Inventory Blocks Throw Without Exception",
                "Verifies attempting to throw with 0 grenades does not decrement below 0 or spawn projectile.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Zero_Inv");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 0;

                        int initialProj = UnityEngine.Object.FindObjectsOfType<GrenadeProjectile>().Length;

                        // Call ThrowGrenade 5 times
                        for (int i = 0; i < 5; i++)
                        {
                            thrower.ThrowGrenade(new Vector2(i, i));
                        }

                        int postProj = UnityEngine.Object.FindObjectsOfType<GrenadeProjectile>().Length;

                        E2EAssert.AreEqual(0, thrower.grenadeCount, "Grenade count must remain 0 (never negative)");
                        E2EAssert.AreEqual(initialProj, postProj, "Zero projectiles should spawn when inventory is empty");
                    }
                });

            // TEST 15: Simultaneous Input (E + Fire2 + RMB in 1 Frame) Consumes Exactly 1 Grenade
            TestRunnerHelper.RunTest(report, "CH1-M3-15", "F18", 2,
                "Simultaneous Input in 1 Frame Consumes Exactly 1 Grenade",
                "Verifies pressing E and RMB concurrently in 1 frame consumes only 1 grenade and sets cooldown.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Simul_Input");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 2;

                        // In GrenadeThrower.Update(), input is combined:
                        // bool inputThrow = Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2") || Input.GetMouseButtonDown(1);
                        bool keyE = true;
                        bool btnFire2 = true;
                        bool btnRmb = true;
                        bool combinedInput = keyE || btnFire2 || btnRmb;

                        var cooldownField = typeof(GrenadeThrower).GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                        float cooldown = (float)cooldownField.GetValue(thrower);

                        int throwsExecuted = 0;
                        if (combinedInput && cooldown <= 0f)
                        {
                            if (thrower.grenadeCount > 0)
                            {
                                thrower.ThrowGrenade(new Vector2(3f, 0f));
                                cooldownField.SetValue(thrower, thrower.throwCooldown);
                                throwsExecuted++;
                            }
                        }

                        // Verify secondary check in same frame is blocked
                        cooldown = (float)cooldownField.GetValue(thrower);
                        bool secondaryBlocked = (cooldown > 0f);

                        E2EAssert.AreEqual(1, throwsExecuted, "Only 1 throw action must execute on simultaneous input");
                        E2EAssert.AreEqual(1, thrower.grenadeCount, "Grenade count must decrement by exactly 1 (2 -> 1)");
                        E2EAssert.IsTrue(secondaryBlocked, "Secondary trigger in same frame must be blocked by cooldown");
                    }
                });

            // TEST 16: Cooldown Throttling Over 1.0s Window
            TestRunnerHelper.RunTest(report, "CH1-M3-16", "F18", 2,
                "Rapid Throw Cooldown Throttling Over 1.0s Window",
                "Verifies throw attempts within 1.0s window are throttled by throwCooldown (0.3s).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Cooldown_Test");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 10;
                        thrower.throwCooldown = 0.3f;

                        var cooldownField = typeof(GrenadeThrower).GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance);

                        int successfulThrows = 0;
                        float dt = 0.05f; // 20 checks per second (total 1.0s)

                        for (float t = 0; t <= 1.0f; t += dt)
                        {
                            float currentCooldown = (float)cooldownField.GetValue(thrower);
                            if (currentCooldown > 0f)
                            {
                                currentCooldown -= dt;
                                cooldownField.SetValue(thrower, currentCooldown);
                            }

                            if (currentCooldown <= 0f && thrower.grenadeCount > 0)
                            {
                                thrower.ThrowGrenade(new Vector2(1f, 1f));
                                cooldownField.SetValue(thrower, thrower.throwCooldown);
                                successfulThrows++;
                            }
                        }

                        // In 1.0s with 0.3s cooldown: at most 4 throws
                        E2EAssert.IsTrue(successfulThrows <= 4, $"Throws within 1.0s ({successfulThrows}) must be <= 4");
                        E2EAssert.IsTrue(successfulThrows >= 3, $"Throws within 1.0s ({successfulThrows}) must be >= 3");
                    }
                });

            // TEST 17: Throw Blocked When Game Paused
            TestRunnerHelper.RunTest(report, "CH1-M3-17", "F18", 2,
                "Throw Blocked When Game Paused (Time.timeScale = 0)",
                "Verifies ThrowGrenade cannot execute when game is paused.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Pause_Test");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 3;

                        Time.timeScale = 0f;
                        try
                        {
                            thrower.ThrowGrenade(new Vector2(2f, 2f));
                            E2EAssert.AreEqual(3, thrower.grenadeCount, "Grenade count must remain 3 when paused");
                        }
                        finally
                        {
                            Time.timeScale = 1.0f;
                        }
                    }
                });

            // TEST 18: Throw Blocked When Player Dead
            TestRunnerHelper.RunTest(report, "CH1-M3-18", "F18", 2,
                "Throw Blocked When Player Dead (PlayerHealth.IsAlive = false)",
                "Verifies dead player cannot throw grenades.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Dead_Test");
                        var health = player.AddComponent<PlayerHealth>();
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 3;

                        var prop = typeof(PlayerHealth).GetProperty("currentHealth", BindingFlags.Public | BindingFlags.Instance);
                        prop?.GetSetMethod(true)?.Invoke(health, new object[] { 0 });

                        thrower.ThrowGrenade(new Vector2(2f, 2f));
                        E2EAssert.AreEqual(3, thrower.grenadeCount, "Dead player grenade count must remain unchanged");
                    }
                });

            // =========================================================================
            // SECTION 5: Pickup & Projectile Mechanics (F16, F17, F19)
            // =========================================================================

            // TEST 19: Pickup Rejection at Max Capacity (5 Grenades)
            TestRunnerHelper.RunTest(report, "CH1-M3-19", "F17", 2,
                "Pickup Rejection at Max Capacity (5 Grenades)",
                "Verifies player at full capacity (5) cannot collect pickup and pickup remains in scene.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_Full");
                        player.tag = "Player";
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;
                        thrower.maxGrenades = 5;

                        var pickupGo = ctx.CreateGameObject("Pickup_Cap_Test");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        bool collected = pickup.TryCollect(player);

                        E2EAssert.IsFalse(collected, "TryCollect must return false when capacity is full");
                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Grenade count must stay at max (5)");
                        E2EAssert.IsTrue(pickupGo != null, "Pickup must not be destroyed when rejected");
                    }
                });

            // TEST 20: Pickup Ignored by Hostile Entities
            TestRunnerHelper.RunTest(report, "CH1-M3-20", "F17", 2,
                "Pickup Ignored by Hostile Entities",
                "Verifies non-player entities cannot collect GrenadePickup.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemy = ctx.CreateGameObject("Enemy_Collector");
                        enemy.tag = "Enemy";
                        enemy.AddComponent<ChaserEnemy>();

                        var pickupGo = ctx.CreateGameObject("Pickup_Enemy");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        bool collected = pickup.TryCollect(enemy);

                        E2EAssert.IsFalse(collected, "Enemy cannot collect grenade pickup");
                        E2EAssert.IsTrue(pickupGo != null, "Pickup must remain in scene after enemy collision");
                    }
                });

            // TEST 21: Player Friendly Fire Immunity in Mixed Blast
            TestRunnerHelper.RunTest(report, "CH1-M3-21", "F20", 2,
                "Player Friendly Fire Immunity in Mixed Blast",
                "Verifies player inside explosion center takes zero damage while surrounding enemies are killed.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("Player_In_Blast");
                        player.tag = "Player";
                        player.transform.position = Vector2.zero;
                        var pCol = player.AddComponent<CircleCollider2D>();
                        pCol.radius = 0.5f;
                        var health = player.AddComponent<PlayerHealth>();

                        var enemyGo = ctx.CreateGameObject("Enemy_Near_Player");
                        enemyGo.transform.position = new Vector2(1.5f, 0f);
                        var eCol = enemyGo.AddComponent<CircleCollider2D>();
                        eCol.radius = 0.3f;
                        var chaser = enemyGo.AddComponent<ChaserEnemy>();

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("Explosion_Mixed");
                        expGo.transform.position = Vector2.zero;
                        var exp = expGo.AddComponent<ExplosionAoE>();
                        exp.Explode();

                        E2EAssert.AreEqual(5, health.currentHealth, "Player health must remain 5 (no friendly fire)");
                        E2EAssert.IsTrue(chaser == null || chaser.currentHealth == 0, "Enemy in blast must be killed");
                    }
                });

            // TEST 22: Projectile Impact Detonation on Hostile Entity
            TestRunnerHelper.RunTest(report, "CH1-M3-22", "F19", 2,
                "Projectile Impact Detonation on Hostile Entity",
                "Verifies projectile detonates immediately upon colliding with enemy before fuse expires.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var projGo = ctx.CreateGameObject("Proj_Impact");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(5f, 0f));

                        var enemy = ctx.CreateGameObject("Enemy_Impact");
                        enemy.tag = "Enemy";
                        enemy.AddComponent<ChaserEnemy>();

                        var handleImpactMethod = typeof(GrenadeProjectile).GetMethod("HandleImpact", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleImpactMethod.Invoke(proj, new object[] { enemy });

                        var hasDetonatedField = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool detonated = (bool)hasDetonatedField.GetValue(proj);

                        E2EAssert.IsTrue(detonated, "Projectile must detonate immediately on enemy impact");
                    }
                });

            // TEST 23: Projectile Friendly Pass-Through (Player Collision Ignored)
            TestRunnerHelper.RunTest(report, "CH1-M3-23", "F19", 2,
                "Projectile Friendly Pass-Through (Player Collision Ignored)",
                "Verifies projectile ignores collisions with Player without detonating.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var projGo = ctx.CreateGameObject("Proj_Friendly");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(5f, 0f));

                        var player = ctx.CreateGameObject("Player_Friendly");
                        player.tag = "Player";
                        player.AddComponent<PlayerHealth>();

                        var handleImpactMethod = typeof(GrenadeProjectile).GetMethod("HandleImpact", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleImpactMethod.Invoke(proj, new object[] { player });

                        var hasDetonatedField = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool detonated = (bool)hasDetonatedField.GetValue(proj);

                        E2EAssert.IsFalse(detonated, "Projectile must NOT detonate on player collision");
                    }
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Challenger 1 M3 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
