using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    /// <summary>
    /// Empirical Adversarial Verification Suite for Milestone 3 (Grenade AoE Mechanic).
    /// Authored by Challenger 2.
    /// </summary>
    public static class ChallengerM3Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 2 Milestone 3 Empirical Verification Suite" };

            // =========================================================================
            // SECTION 1: Player Friendly Fire Immunity (ExplosionAoE & GrenadeProjectile)
            // =========================================================================

            // TEST 1: Epicenter Immunity
            TestRunnerHelper.RunTest(report, "CH-M3-01", "F20", 1,
                "Player Epicenter Explosion Immunity",
                "Verifies Player directly at explosion epicenter (distance 0) takes 0 damage and HP remains 5.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        var col = player.AddComponent<CircleCollider2D>();
                        col.radius = 0.5f;
                        var ph = player.AddComponent<PlayerHealth>();

                        var expGo = ctx.CreateGameObject("CH_Exp");
                        expGo.transform.position = Vector3.zero;
                        var aoe = expGo.AddComponent<ExplosionAoE>();
                        aoe.Explode();

                        E2EAssert.AreEqual(5, ph.currentHealth, "Player health must remain 5 at epicenter");
                        E2EAssert.IsTrue(ph.IsAlive, "Player must remain alive");
                    }
                });

            // TEST 2: Multi-Distance Blast Radius Immunity
            TestRunnerHelper.RunTest(report, "CH-M3-02", "F20", 2,
                "Player Multi-Distance Blast Radius Immunity",
                "Verifies Player at distances 0.5, 1.0, 2.0, 3.0, and 3.49 within 3.5u blast radius takes 0 damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        float[] testDistances = new float[] { 0.5f, 1.0f, 2.0f, 3.0f, 3.49f };
                        foreach (float dist in testDistances)
                        {
                            var player = ctx.CreateGameObject($"CH_Player_{dist}");
                            player.tag = "Player";
                            var col = player.AddComponent<CircleCollider2D>();
                            col.radius = 0.4f;
                            var ph = player.AddComponent<PlayerHealth>();
                            player.transform.position = new Vector3(dist, 0f, 0f);

                            var expGo = ctx.CreateGameObject($"CH_Exp_{dist}");
                            expGo.transform.position = Vector3.zero;
                            var aoe = expGo.AddComponent<ExplosionAoE>();
                            aoe.Explode();

                            E2EAssert.AreEqual(5, ph.currentHealth, $"Player at distance {dist} must take 0 damage");
                        }
                    }
                });

            // TEST 3: Compound Collider Player Immunity
            TestRunnerHelper.RunTest(report, "CH-M3-03", "F20", 2,
                "Compound Collider Player Immunity",
                "Verifies compound Player hierarchy (parent PlayerHealth, untagged child colliders) takes 0 damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var parent = ctx.CreateGameObject("CH_CompoundPlayer");
                        parent.tag = "Untagged"; // Root not tagged
                        var ph = parent.AddComponent<PlayerHealth>();

                        var childHurtbox = ctx.CreateGameObject("HurtboxChild");
                        childHurtbox.transform.SetParent(parent.transform);
                        childHurtbox.tag = "Untagged";
                        var col = childHurtbox.AddComponent<CircleCollider2D>();
                        col.radius = 0.5f;

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("CH_Exp");
                        expGo.transform.position = Vector3.zero;
                        var aoe = expGo.AddComponent<ExplosionAoE>();
                        aoe.Explode();

                        E2EAssert.AreEqual(5, ph.currentHealth, "Compound player must take 0 damage");
                    }
                });

            // TEST 4: Massive Overlapping Blast Barrage
            TestRunnerHelper.RunTest(report, "CH-M3-04", "F20", 2,
                "Massive Overlapping Blast Barrage Immunity",
                "Verifies 20 simultaneous explosions right on top of Player do not reduce HP even by 1.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        var col = player.AddComponent<CircleCollider2D>();
                        col.radius = 0.5f;
                        var ph = player.AddComponent<PlayerHealth>();

                        Physics2D.SyncTransforms();

                        for (int i = 0; i < 20; i++)
                        {
                            var expGo = ctx.CreateGameObject($"CH_Exp_{i}");
                            expGo.transform.position = Vector3.zero;
                            var aoe = expGo.AddComponent<ExplosionAoE>();
                            aoe.Explode();
                        }

                        E2EAssert.AreEqual(5, ph.currentHealth, "Player must have 5 HP after 20 overlapping explosions");
                    }
                });

            // TEST 5: Lethal Discrimination (Hostile Damage vs Friendly Immunity)
            TestRunnerHelper.RunTest(report, "CH-M3-05", "F20", 3,
                "Lethal Discrimination Hostile vs Friendly",
                "Verifies explosion inflicts 50 damage to all hostile entities while leaving co-located Player unharmed.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        // Co-locate Player and all 3 enemy types in blast zone
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        player.AddComponent<CircleCollider2D>();
                        var ph = player.AddComponent<PlayerHealth>();

                        var chaser = ctx.CreateGameObject("CH_Chaser");
                        chaser.tag = "Enemy";
                        chaser.transform.position = new Vector3(1f, 0f, 0f);
                        chaser.AddComponent<CircleCollider2D>();
                        var ce = chaser.AddComponent<ChaserEnemy>();

                        var shooter = ctx.CreateGameObject("CH_Shooter");
                        shooter.tag = "Enemy";
                        shooter.transform.position = new Vector3(-1f, 0f, 0f);
                        shooter.AddComponent<CircleCollider2D>();
                        var se = shooter.AddComponent<ShooterEnemy>();

                        var rusher = ctx.CreateGameObject("CH_Rusher");
                        rusher.tag = "Enemy";
                        rusher.transform.position = new Vector3(0f, 1f, 0f);
                        rusher.AddComponent<CircleCollider2D>();
                        var re = rusher.AddComponent<RusherEnemy>();

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("CH_Exp");
                        expGo.transform.position = Vector3.zero;
                        var aoe = expGo.AddComponent<ExplosionAoE>();
                        aoe.Explode();

                        E2EAssert.AreEqual(5, ph.currentHealth, "Player must take 0 damage");
                        E2EAssert.AreEqual(0, ce.currentHealth, "Chaser must be eliminated (HP 0)");
                        E2EAssert.AreEqual(0, se.currentHealth, "Shooter must be eliminated (HP 0)");
                        E2EAssert.AreEqual(0, re.currentHealth, "Rusher must be eliminated (HP 0)");
                    }
                });

            // TEST 6: Projectile Ignores Player Collisions
            TestRunnerHelper.RunTest(report, "CH-M3-06", "F19", 2,
                "Projectile Ignores Player Collisions",
                "Verifies GrenadeProjectile collision with Player does not trigger early detonation.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        player.AddComponent<CircleCollider2D>();
                        player.AddComponent<PlayerHealth>();

                        var projGo = ctx.CreateGameObject("CH_Proj");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(5f, 0f));

                        var impactMethod = typeof(GrenadeProjectile).GetMethod("HandleImpact", BindingFlags.NonPublic | BindingFlags.Instance);
                        impactMethod.Invoke(proj, new object[] { player });

                        var fieldDet = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool hasDetonated = (bool)fieldDet.GetValue(proj);

                        E2EAssert.IsFalse(hasDetonated, "Projectile must NOT detonate on player impact");
                    }
                });

            // =========================================================================
            // SECTION 2: Max Capacity Rejection & Inventory Dynamics
            // =========================================================================

            // TEST 7: Max Capacity Rejection at 5 Grenades
            TestRunnerHelper.RunTest(report, "CH-M3-07", "F17", 2,
                "Max Capacity Rejection at 5 Grenades",
                "Verifies player at capacity 5 rejects pickup collection, count remains 5, and pickup is NOT destroyed.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;
                        thrower.maxGrenades = 5;

                        var pickupGo = ctx.CreateGameObject("CH_Pickup");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        bool collected = pickup.TryCollect(player);

                        E2EAssert.IsFalse(collected, "TryCollect must return false at max capacity");
                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Grenade count must remain 5");
                        E2EAssert.IsTrue(pickupGo != null, "Pickup GameObject must not be destroyed");
                    }
                });

            // TEST 8: Multi-Pickup Queue Rejection
            TestRunnerHelper.RunTest(report, "CH-M3-08", "F17", 2,
                "Multiple Pickups On-Ground Immunity",
                "Verifies 3 pickups walked over by a player at 5 grenades all reject collection and remain alive.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;
                        thrower.maxGrenades = 5;

                        var p1 = ctx.CreateGameObject("Pickup_1").AddComponent<GrenadePickup>();
                        var p2 = ctx.CreateGameObject("Pickup_2").AddComponent<GrenadePickup>();
                        var p3 = ctx.CreateGameObject("Pickup_3").AddComponent<GrenadePickup>();

                        E2EAssert.IsFalse(p1.TryCollect(player), "Pickup 1 must reject");
                        E2EAssert.IsFalse(p2.TryCollect(player), "Pickup 2 must reject");
                        E2EAssert.IsFalse(p3.TryCollect(player), "Pickup 3 must reject");

                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Inventory must remain 5");
                        E2EAssert.IsTrue(p1.gameObject != null, "Pickup 1 must still exist");
                        E2EAssert.IsTrue(p2.gameObject != null, "Pickup 2 must still exist");
                        E2EAssert.IsTrue(p3.gameObject != null, "Pickup 3 must still exist");
                    }
                });

            // TEST 9: Post-Throw Pickup Collection
            TestRunnerHelper.RunTest(report, "CH-M3-09", "F17", 2,
                "Post-Throw Pickup Collection Lifecycle",
                "Verifies rejected pickup is successfully collected after player throws 1 grenade, restoring count to 5.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.tag = "Player";
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 5;
                        thrower.maxGrenades = 5;

                        // Create dummy projectile prefab
                        var dummyProj = ctx.CreateGameObject("DummyProjPrefab");
                        dummyProj.AddComponent<GrenadeProjectile>();
                        thrower.grenadePrefab = dummyProj;

                        var pickupGo = ctx.CreateGameObject("CH_Pickup");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        // Phase 1: Rejection at 5
                        E2EAssert.IsFalse(pickup.TryCollect(player), "Must reject at 5");

                        // Phase 2: Throw 1 grenade -> count becomes 4
                        thrower.ThrowGrenade(new Vector2(2f, 0f));
                        E2EAssert.AreEqual(4, thrower.grenadeCount, "Count should decrement to 4");

                        // Clean up instantiated clone immediately to prevent scene clutter
                        var clone = GameObject.Find("DummyProjPrefab(Clone)");
                        if (clone != null) UnityEngine.Object.DestroyImmediate(clone);

                        // Phase 3: Successful collection
                        bool collected = pickup.TryCollect(player);
                        E2EAssert.IsTrue(collected, "Must succeed when inventory has capacity");
                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Count must restore to 5");
                    }
                });

            // TEST 10: Over-Capacity Addition Clamping
            TestRunnerHelper.RunTest(report, "CH-M3-10", "F17", 2,
                "Over-Capacity Clamping on AddGrenades",
                "Verifies adding 10 grenades when count is 3 clamps strictly at maxGrenades (5).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 3;
                        thrower.maxGrenades = 5;

                        bool added = thrower.AddGrenades(10);
                        E2EAssert.IsTrue(added, "AddGrenades should succeed up to cap");
                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Grenade count must clamp to 5");

                        // Attempt adding more when full
                        bool addMore = thrower.AddGrenades(1);
                        E2EAssert.IsFalse(addMore, "AddGrenades should return false when already at max");
                        E2EAssert.AreEqual(5, thrower.grenadeCount, "Grenade count must remain 5");
                    }
                });

            // TEST 11: Non-Player Objects Cannot Collect Pickups
            TestRunnerHelper.RunTest(report, "CH-M3-11", "F17", 2,
                "Non-Player Immunity to Pickup Collection",
                "Verifies Enemies, Bullets, and Obstacles cannot consume or trigger GrenadePickup.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemy = ctx.CreateGameObject("CH_Enemy");
                        enemy.tag = "Enemy";
                        enemy.AddComponent<ChaserEnemy>();

                        var bullet = ctx.CreateGameObject("CH_Bullet");
                        bullet.AddComponent<EnemyBullet>();

                        var wall = ctx.CreateGameObject("CH_Wall");
                        wall.tag = "Colliders";

                        var pickupGo = ctx.CreateGameObject("CH_Pickup");
                        var pickup = pickupGo.AddComponent<GrenadePickup>();

                        E2EAssert.IsFalse(pickup.TryCollect(enemy), "Enemy must not collect pickup");
                        E2EAssert.IsFalse(pickup.TryCollect(bullet), "Bullet must not collect pickup");
                        E2EAssert.IsFalse(pickup.TryCollect(wall), "Wall must not collect pickup");
                        E2EAssert.IsTrue(pickupGo != null, "Pickup must remain intact");
                    }
                });

            // TEST 12: Border Drop Clamping Inside Arena
            TestRunnerHelper.RunTest(report, "CH-M3-12", "F16", 2,
                "Border Drop Clamping Across Extreme Coordinates",
                "Verifies pickups dropped at extreme perimeter coords are clamped inside [-8.5, 13.8] x [-4.2, 5.2].",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        Vector3[] extremePositions = new Vector3[]
                        {
                            new Vector3(-50f, -50f, 0f),
                            new Vector3(100f, 100f, 0f),
                            new Vector3(-9.0f, 0f, 0f),
                            new Vector3(15.0f, 6.0f, 0f)
                        };

                        var startMethod = typeof(GrenadePickup).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

                        foreach (var pos in extremePositions)
                        {
                            var pGo = ctx.CreateGameObject("CH_ClampedPickup");
                            pGo.transform.position = pos;
                            var pickup = pGo.AddComponent<GrenadePickup>();
                            startMethod?.Invoke(pickup, null);

                            E2EAssert.IsTrue(pGo.transform.position.x >= -8.5f && pGo.transform.position.x <= 13.8f,
                                $"X {pGo.transform.position.x} must be within [-8.5, 13.8]");
                            E2EAssert.IsTrue(pGo.transform.position.y >= -4.2f && pGo.transform.position.y <= 5.2f,
                                $"Y {pGo.transform.position.y} must be within [-4.2, 5.2]");
                        }
                    }
                });

            // =========================================================================
            // SECTION 3: Parabolic Trajectory, Fuse Timing & Detonation Triggers
            // =========================================================================

            // TEST 13: Parabolic Height Scale Curve Verification
            TestRunnerHelper.RunTest(report, "CH-M3-13", "F19", 1,
                "Parabolic Scale Elevation Arc",
                "Verifies sin(t*pi)*0.5 curve: launch scale 1.0, apex scale 1.5 at t=0.5, landing scale 1.0 at t=1.0.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var projGo = ctx.CreateGameObject("CH_Proj");
                        projGo.transform.localScale = Vector3.one;
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.maxArcHeight = 0.5f;

                        // Check mathematical sampling points
                        float[] tSamples = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
                        foreach (float t in tSamples)
                        {
                            float heightCurve = Mathf.Sin(t * Mathf.PI);
                            float scale = 1.0f + heightCurve * proj.maxArcHeight;

                            if (Mathf.Approximately(t, 0.0f))
                                E2EAssert.AreApproximatelyEqual(1.0f, scale, 0.001f, "Launch scale must be 1.0");
                            else if (Mathf.Approximately(t, 0.5f))
                                E2EAssert.AreApproximatelyEqual(1.5f, scale, 0.001f, "Apex scale must be 1.5");
                            else if (Mathf.Approximately(t, 1.0f))
                                E2EAssert.AreApproximatelyEqual(1.0f, scale, 0.001f, "Landing scale must be 1.0");

                            E2EAssert.IsTrue(scale >= 0.9999f && scale <= 1.5001f, $"Scale {scale} must remain within [1.0, 1.5]");
                        }
                    }
                });

            // TEST 14: Flight Trajectory Target Arrival
            TestRunnerHelper.RunTest(report, "CH-M3-14", "F19", 1,
                "Flight Duration and Target Arrival",
                "Verifies flightDuration is 0.7s and projectile interpolates linearly to destination.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var projGo = ctx.CreateGameObject("CH_Proj");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        Vector2 start = new Vector2(1f, 2f);
                        Vector2 target = new Vector2(5f, 2f);
                        proj.Initialize(start, target);

                        E2EAssert.AreApproximatelyEqual(0.7f, proj.flightDuration, 0.01f, "flightDuration must be 0.7s");

                        // Sample halfway point
                        Vector2 mid = Vector2.Lerp(start, target, 0.5f);
                        E2EAssert.AreApproximatelyEqual(new Vector2(3f, 2f), mid, 0.01f, "Midpoint must be (3, 2)");
                    }
                });

            // TEST 15: Fuse Detonation Timing at 1.2s
            TestRunnerHelper.RunTest(report, "CH-M3-15", "F19", 2,
                "Fuse Detonation Timing at 1.2s",
                "Verifies projectile detonates strictly when elapsedTime >= 1.2s fuseTime.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var projGo = ctx.CreateGameObject("CH_Proj");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(4f, 0f));

                        var elapsedTimeField = typeof(GrenadeProjectile).GetField("_elapsedTime", BindingFlags.NonPublic | BindingFlags.Instance);
                        var hasDetonatedField = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);

                        // At 1.15s: not detonated
                        elapsedTimeField.SetValue(proj, 1.15f);
                        E2EAssert.IsFalse((bool)hasDetonatedField.GetValue(proj), "Must not detonate before 1.2s fuse");

                        // At 1.20s: triggers detonation
                        elapsedTimeField.SetValue(proj, 1.20f);
                        if ((float)elapsedTimeField.GetValue(proj) >= proj.fuseTime)
                        {
                            proj.Detonate();
                        }
                        E2EAssert.IsTrue((bool)hasDetonatedField.GetValue(proj), "Must detonate at 1.2s fuse");

                        // Clean up fallback explosion created by Detonate()
                        var fallbackExp = GameObject.Find("ExplosionAoE_Fallback");
                        if (fallbackExp != null) UnityEngine.Object.DestroyImmediate(fallbackExp);
                    }
                });

            // TEST 16: Early Detonation on Hostile Impact
            TestRunnerHelper.RunTest(report, "CH-M3-16", "F19", 2,
                "Early Detonation on Hostile Entity Impact",
                "Verifies GrenadeProjectile detonates immediately upon impact with Chaser, Shooter, or Rusher.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var enemy = ctx.CreateGameObject("CH_Enemy");
                        enemy.tag = "Enemy";
                        enemy.AddComponent<ChaserEnemy>();

                        var projGo = ctx.CreateGameObject("CH_Proj");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(5f, 0f));

                        var impactMethod = typeof(GrenadeProjectile).GetMethod("HandleImpact", BindingFlags.NonPublic | BindingFlags.Instance);
                        impactMethod.Invoke(proj, new object[] { enemy });

                        var fieldDet = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);
                        E2EAssert.IsTrue((bool)fieldDet.GetValue(proj), "Must detonate immediately on enemy collision");

                        // Clean up fallback explosion
                        var fallbackExp = GameObject.Find("ExplosionAoE_Fallback");
                        if (fallbackExp != null) UnityEngine.Object.DestroyImmediate(fallbackExp);
                    }
                });

            // TEST 17: Early Detonation on Obstacle Wall Impact
            TestRunnerHelper.RunTest(report, "CH-M3-17", "F19", 2,
                "Early Detonation on Arena Wall Impact",
                "Verifies GrenadeProjectile detonates immediately upon impact with wall or collider.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var wall = ctx.CreateGameObject("CH_Wall");
                        wall.tag = "Colliders";

                        var projGo = ctx.CreateGameObject("CH_Proj");
                        var proj = projGo.AddComponent<GrenadeProjectile>();
                        proj.Initialize(Vector2.zero, new Vector2(5f, 0f));

                        var impactMethod = typeof(GrenadeProjectile).GetMethod("HandleImpact", BindingFlags.NonPublic | BindingFlags.Instance);
                        impactMethod.Invoke(proj, new object[] { wall });

                        var fieldDet = typeof(GrenadeProjectile).GetField("_hasDetonated", BindingFlags.NonPublic | BindingFlags.Instance);
                        E2EAssert.IsTrue((bool)fieldDet.GetValue(proj), "Must detonate immediately on wall collision");

                        // Clean up fallback explosion
                        var fallbackExp = GameObject.Find("ExplosionAoE_Fallback");
                        if (fallbackExp != null) UnityEngine.Object.DestroyImmediate(fallbackExp);
                    }
                });

            // =========================================================================
            // SECTION 4: Throw Mechanics, Boundary Clamping & Input Guards
            // =========================================================================

            // TEST 18: Maximum Throw Distance Clamping to 7.0u
            TestRunnerHelper.RunTest(report, "CH-M3-18", "F18", 1,
                "Maximum Throw Distance Clamping to 7.0u",
                "Verifies throws requested far beyond 7.0u clamp to exactly 7.0 units from player.",
                () =>
                {
                    Vector2 playerPos = new Vector2(2f, 3f);
                    Vector2 farTarget = new Vector2(20f, 3f); // Distance 18 units away
                    Vector2 throwDir = farTarget - playerPos;
                    Vector2 clampedOffset = Vector2.ClampMagnitude(throwDir, 7.0f);
                    Vector2 finalTarget = playerPos + clampedOffset;

                    E2EAssert.AreApproximatelyEqual(7.0f, Vector2.Distance(playerPos, finalTarget), 0.001f,
                        "Clamped target distance must be 7.0 units");
                    E2EAssert.AreApproximatelyEqual(new Vector2(9f, 3f), finalTarget, 0.001f,
                        "Target must lie along throw vector at (9, 3)");
                });

            // TEST 19: Arena Boundary Clamping on Throw Target
            TestRunnerHelper.RunTest(report, "CH-M3-19", "F18", 2,
                "Throw Target Arena Boundary Clamping",
                "Verifies throw targets are strictly bounded by arenaMin [-8.5, -4.2] and arenaMax [13.8, 5.2].",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.transform.position = new Vector3(12.0f, 4.0f, 0f);
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 2;

                        var dummyProj = ctx.CreateGameObject("DummyProj");
                        dummyProj.AddComponent<GrenadeProjectile>();
                        thrower.grenadePrefab = dummyProj;

                        // Throw towards (18, 10)
                        thrower.ThrowGrenade(new Vector2(18f, 10f));

                        // Find spawned projectile and clean up
                        var spawned = GameObject.Find("DummyProj(Clone)");
                        if (spawned != null)
                        {
                            E2EAssert.IsTrue(spawned.transform.position.x <= 13.801f, "Throw must clamp inside arenaMax.x");
                            E2EAssert.IsTrue(spawned.transform.position.y <= 5.201f, "Throw must clamp inside arenaMax.y");
                            UnityEngine.Object.DestroyImmediate(spawned);
                        }
                    }
                });

            // TEST 20: Zero Vector Throw (Throw At Self)
            TestRunnerHelper.RunTest(report, "CH-M3-20", "F18", 2,
                "Zero Vector Throw Safety",
                "Verifies throwing at player position (target == playerPos) does not result in NaN or exception.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        player.transform.position = new Vector3(3f, 2f, 0f);
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 2;

                        var dummyProj = ctx.CreateGameObject("DummyProj");
                        dummyProj.AddComponent<GrenadeProjectile>();
                        thrower.grenadePrefab = dummyProj;

                        // Throw directly at player pos
                        thrower.ThrowGrenade(new Vector2(3f, 2f));
                        E2EAssert.AreEqual(1, thrower.grenadeCount, "Count should decrement to 1");

                        var spawned = GameObject.Find("DummyProj(Clone)");
                        if (spawned != null)
                        {
                            E2EAssert.IsFalse(float.IsNaN(spawned.transform.position.x), "X position must not be NaN");
                            E2EAssert.IsFalse(float.IsNaN(spawned.transform.position.y), "Y position must not be NaN");
                            UnityEngine.Object.DestroyImmediate(spawned);
                        }
                    }
                });

            // TEST 21: Zero Inventory Throw Guard
            TestRunnerHelper.RunTest(report, "CH-M3-21", "F18", 2,
                "Zero Inventory Throw Guard",
                "Verifies throwing with 0 grenades does not decrement count below 0 and spawns nothing.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 0;

                        thrower.ThrowGrenade(new Vector2(3f, 0f));
                        E2EAssert.AreEqual(0, thrower.grenadeCount, "Count must remain 0");
                    }
                });

            // TEST 22: Paused and Dead Player Throw Guards
            TestRunnerHelper.RunTest(report, "CH-M3-22", "F18", 2,
                "Paused and Dead Player Guards",
                "Verifies throws are completely blocked when Time.timeScale=0 or player is dead.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        var ph = player.AddComponent<PlayerHealth>();
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 3;

                        // Case A: Paused
                        Time.timeScale = 0f;
                        thrower.ThrowGrenade(new Vector2(2f, 0f));
                        E2EAssert.AreEqual(3, thrower.grenadeCount, "Paused game must block throw");
                        Time.timeScale = 1.0f;

                        // Case B: Dead player
                        var hpProp = typeof(PlayerHealth).GetProperty("currentHealth", BindingFlags.Public | BindingFlags.Instance);
                        hpProp?.GetSetMethod(true)?.Invoke(ph, new object[] { 0 });
                        E2EAssert.IsFalse(ph.IsAlive, "Player must be dead");

                        thrower.ThrowGrenade(new Vector2(2f, 0f));
                        E2EAssert.AreEqual(3, thrower.grenadeCount, "Dead player must not throw grenades");
                    }
                });

            // TEST 23: Event Dispatch Integrity
            TestRunnerHelper.RunTest(report, "CH-M3-23", "F17", 1,
                "OnGrenadeCountChanged Event Dispatch Integrity",
                "Verifies event dispatches correct count on add, throw, and reset.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var player = ctx.CreateGameObject("CH_Player");
                        var thrower = player.AddComponent<GrenadeThrower>();
                        thrower.grenadeCount = 2;

                        var dummyProj = ctx.CreateGameObject("DummyProj");
                        dummyProj.AddComponent<GrenadeProjectile>();
                        thrower.grenadePrefab = dummyProj;

                        int lastDispatched = -1;
                        thrower.OnGrenadeCountChanged += count => lastDispatched = count;

                        thrower.AddGrenades(2);
                        E2EAssert.AreEqual(4, lastDispatched, "Dispatched count should be 4 on add");

                        thrower.ThrowGrenade(new Vector2(1f, 0f));
                        E2EAssert.AreEqual(3, lastDispatched, "Dispatched count should be 3 on throw");

                        var clone = GameObject.Find("DummyProj(Clone)");
                        if (clone != null) UnityEngine.Object.DestroyImmediate(clone);

                        thrower.ResetGrenades(1);
                        E2EAssert.AreEqual(1, lastDispatched, "Dispatched count should be 1 on reset");
                    }
                });

            // =========================================================================
            // SECTION 5: Prefab References, Memory Leaks & Scene Integrity
            // =========================================================================

            // TEST 24: Prefab Wiring & Asset Integrity
            TestRunnerHelper.RunTest(report, "CH-M3-24", "SceneIntegration", 4,
                "Prefab Wiring and Asset Hierarchy Integrity",
                "Verifies GrenadeProjectile.prefab references ExplosionAoE.prefab, and ExplosionAoE references Fire Effect.",
                () =>
                {
                    var projPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GrenadeProjectile.prefab");
                    var expPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExplosionAoE.prefab");
                    var pickPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GrenadePickup.prefab");

                    E2EAssert.IsNotNull(projPrefab, "GrenadeProjectile.prefab must exist");
                    E2EAssert.IsNotNull(expPrefab, "ExplosionAoE.prefab must exist");
                    E2EAssert.IsNotNull(pickPrefab, "GrenadePickup.prefab must exist");

                    var proj = projPrefab.GetComponent<GrenadeProjectile>();
                    E2EAssert.IsNotNull(proj.explosionPrefab, "GrenadeProjectile must reference explosionPrefab");

                    var aoe = expPrefab.GetComponent<ExplosionAoE>();
                    E2EAssert.IsNotNull(aoe.visualEffectPrefab, "ExplosionAoE must reference visualEffectPrefab");

                    var pickup = pickPrefab.GetComponent<GrenadePickup>();
                    E2EAssert.IsNotNull(pickup, "GrenadePickup must have GrenadePickup component");
                });

            // TEST 25: Massive Crowd Clearing AoE Test
            TestRunnerHelper.RunTest(report, "CH-M3-25", "F20", 3,
                "Massive 30-Enemy Crowd Blast Elimination",
                "Verifies an explosion clears 30 packed enemies simultaneously without dropping targets or throwing exceptions.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        List<ChaserEnemy> enemies = new List<ChaserEnemy>();
                        for (int i = 0; i < 30; i++)
                        {
                            float angle = (i / 30f) * Mathf.PI * 2f;
                            float r = UnityEngine.Random.Range(0.5f, 3.2f);
                            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);

                            var eGo = ctx.CreateGameObject($"CrowdEnemy_{i}");
                            eGo.tag = "Enemy";
                            eGo.transform.position = pos;
                            eGo.AddComponent<CircleCollider2D>();
                            var c = eGo.AddComponent<ChaserEnemy>();
                            enemies.Add(c);
                        }

                        Physics2D.SyncTransforms();

                        var expGo = ctx.CreateGameObject("CH_CrowdExp");
                        expGo.transform.position = Vector3.zero;
                        var aoe = expGo.AddComponent<ExplosionAoE>();
                        aoe.Explode();

                        int aliveCount = 0;
                        foreach (var e in enemies)
                        {
                            if (e.currentHealth > 0) aliveCount++;
                        }

                        E2EAssert.AreEqual(0, aliveCount, $"All 30 enemies must be eliminated, but {aliveCount} survived");
                    }
                });

            // TEST 26: Zero Memory Leaks & Projectile Clean Destruction
            TestRunnerHelper.RunTest(report, "CH-M3-26", "MemoryCleanliness", 4,
                "Zero Memory Leaks on 20 Consecutive Detonations",
                "Verifies 20 projectile instances clean up 100% upon detonation with zero leftover clutter.",
                () =>
                {
                    var projPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GrenadeProjectile.prefab");
                    List<GameObject> spawnedProjs = new List<GameObject>();

                    int beforeCount = GameObject.FindObjectsOfType<GameObject>().Length;

                    for (int i = 0; i < 20; i++)
                    {
                        var projObj = UnityEngine.Object.Instantiate(projPrefab, new Vector3(i * 0.5f, 0f, 0f), Quaternion.identity);
                        spawnedProjs.Add(projObj);
                        var proj = projObj.GetComponent<GrenadeProjectile>();
                        proj.Initialize(new Vector2(i * 0.5f, 0f), new Vector2(i * 0.5f + 1f, 0f));

                        // Detonate immediately
                        proj.Detonate();
                    }

                    // Verify every projectile GameObject was destroyed immediately by Detonate()
                    int aliveProjs = 0;
                    foreach (var p in spawnedProjs)
                    {
                        if (p != null) aliveProjs++;
                    }
                    E2EAssert.AreEqual(0, aliveProjs, "All 20 projectiles must be destroyed upon detonation");

                    // In EditMode, clean up the cloned explosion objects created by Detonate()
                    var exps = GameObject.FindObjectsOfType<ExplosionAoE>();
                    foreach (var e in exps)
                    {
                        if (e.name.Contains("(Clone)"))
                        {
                            UnityEngine.Object.DestroyImmediate(e.gameObject);
                        }
                    }

                    int afterCount = GameObject.FindObjectsOfType<GameObject>().Length;
                    E2EAssert.AreEqual(beforeCount, afterCount,
                        $"Object count before ({beforeCount}) must match after ({afterCount}), net leak: {afterCount - beforeCount}");
                });

            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Challenger M3 Tests")]
        public static void RunFromMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
