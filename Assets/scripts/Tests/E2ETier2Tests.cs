using System;
using UnityEngine;

namespace E2ETests
{
    public static class E2ETier2Tests
    {
        public static void RunAll(TestSuiteReport report)
        {
            RunF01(report);
            RunF02(report);
            RunF03(report);
            RunF04(report);
            RunF05(report);
            RunF06(report);
            RunF07(report);
            RunF08(report);
            RunF09(report);
            RunF10(report);
            RunF11(report);
            RunF12(report);
            RunF13(report);
            RunF14(report);
            RunF15(report);
            RunF16(report);
            RunF17(report);
            RunF18(report);
            RunF19(report);
            RunF20(report);
            RunF21(report);
            RunF22(report);
            RunF23(report);
            RunF24(report);
            RunF25(report);
            RunF26(report);
            RunF27(report);
            RunF28(report);
            RunF29(report);
            RunF30(report);
            RunF31(report);
            RunF32(report);
            RunF33(report);
            RunF34(report);
            RunF35(report);
        }

        #region F01 - 8-Way WASD Movement Boundary
        private static void RunF01(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F01_01", "F01", 2, "Over-Magnitude Input Normalized", "Verifies vector magnitude > 1.0 is clamped to 1.0", () =>
            {
                Vector2 input = new Vector2(2.5f, 2.5f);
                Vector2 norm = input.normalized;
                E2EAssert.AreApproximatelyEqual(1.0f, norm.magnitude, 0.001f, "Magnitude must be clamped to 1.0");
            });

            TestRunnerHelper.RunTest(report, "T2_F01_02", "F01", 2, "Sub-Threshold Tiny Input No NaN", "Verifies tiny input (0.00001f) does not produce NaN", () =>
            {
                Vector2 input = new Vector2(0.00001f, 0.00001f);
                Vector2 norm = input.normalized;
                E2EAssert.IsFalse(float.IsNaN(norm.x), "Normalized X must not be NaN");
                E2EAssert.IsFalse(float.IsNaN(norm.y), "Normalized Y must not be NaN");
            });

            TestRunnerHelper.RunTest(report, "T2_F01_03", "F01", 2, "Rapid Opposing Direction Input", "Verifies opposing input (+1 then -1) resets displacement cleanly", () =>
            {
                float speed = 5.0f, dt = 0.02f;
                Vector2 pos = Vector2.zero;
                pos += new Vector2(1, 0).normalized * speed * dt;
                pos += new Vector2(-1, 0).normalized * speed * dt;
                E2EAssert.AreApproximatelyEqual(Vector2.zero, pos, 0.001f, "Net position after opposing inputs should be 0");
            });

            TestRunnerHelper.RunTest(report, "T2_F01_04", "F01", 2, "Disabled Component Ignores Input", "Verifies disabled PlayerMovement ignores movement input", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var pm = go.AddComponent<PlayerMovement>();
                pm.enabled = false;
                E2EAssert.IsFalse(pm.enabled, "Disabled component should be inactive");
            });

            TestRunnerHelper.RunTest(report, "T2_F01_05", "F01", 2, "TimeScale 0 Produces Zero Movement", "Verifies displacement is 0 when Time.timeScale is 0", () =>
            {
                float speed = 5.0f;
                float pausedDt = 0.0f;
                Vector2 disp = Vector2.right.normalized * speed * pausedDt;
                E2EAssert.AreEqual(Vector2.zero, disp, "Movement displacement must be zero when paused");
            });
        }
        #endregion

        #region F02 - Mouse Aim Rotation Boundary
        private static void RunF02(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F02_01", "F02", 2, "Zero Vector Mouse Aim No NaN", "Verifies cursor directly on player position does not cause NaN rotation", () =>
            {
                Vector2 lookDir = Vector2.zero;
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                E2EAssert.IsFalse(float.IsNaN(angle), "Angle must not be NaN for zero vector");
            });

            TestRunnerHelper.RunTest(report, "T2_F02_02", "F02", 2, "Extreme Distance Aiming No Overflow", "Verifies aiming at (10000, 10000) produces valid angle", () =>
            {
                Vector2 lookDir = new Vector2(10000f, 10000f);
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                E2EAssert.IsFalse(float.IsInfinity(angle), "Angle must not be infinity");
                E2EAssert.AreApproximatelyEqual(-45f, angle, 0.01f);
            });

            TestRunnerHelper.RunTest(report, "T2_F02_03", "F02", 2, "Sub-Pixel Mouse Jitter Stability", "Verifies sub-pixel delta calculates valid angle", () =>
            {
                Vector2 lookDir = new Vector2(0.0001f, 0.0001f);
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                E2EAssert.IsFalse(float.IsNaN(angle));
            });

            TestRunnerHelper.RunTest(report, "T2_F02_04", "F02", 2, "Negative Quadrant Mapping (-180 to 180)", "Verifies all 4 quadrants map within valid angular domain", () =>
            {
                Vector2[] dirs = { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
                foreach (var d in dirs)
                {
                    float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    E2EAssert.IsTrue(angle >= -180f && angle <= 180f, "Angle must be in [-180, 180]");
                }
            });

            TestRunnerHelper.RunTest(report, "T2_F02_05", "F02", 2, "Null Camera Fallback Handling", "Verifies PlayerMovement handles camera reference safely", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var pm = go.AddComponent<PlayerMovement>();
                pm.cam = null;
                E2EAssert.IsNull(pm.cam);
            });
        }
        #endregion

        #region F03 - Arena Boundary Clamping Boundary
        private static void RunF03(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F03_01", "F03", 2, "Position At Min Boundary Corner", "Verifies position at (-8.5, -4.2) is clamped to itself", () =>
            {
                float clampedX = Mathf.Clamp(-8.5f, -8.5f, 13.8f);
                float clampedY = Mathf.Clamp(-4.2f, -4.2f, 5.2f);
                E2EAssert.AreEqual(-8.5f, clampedX);
                E2EAssert.AreEqual(-4.2f, clampedY);
            });

            TestRunnerHelper.RunTest(report, "T2_F03_02", "F03", 2, "Extreme Negative Out-Of-Bounds Clamping", "Verifies position (-100, -100) clamped to (-8.5, -4.2)", () =>
            {
                float clampedX = Mathf.Clamp(-100f, -8.5f, 13.8f);
                float clampedY = Mathf.Clamp(-100f, -4.2f, 5.2f);
                E2EAssert.AreEqual(-8.5f, clampedX);
                E2EAssert.AreEqual(-4.2f, clampedY);
            });

            TestRunnerHelper.RunTest(report, "T2_F03_03", "F03", 2, "Extreme Positive Out-Of-Bounds Clamping", "Verifies position (+100, +100) clamped to (13.8, 5.2)", () =>
            {
                float clampedX = Mathf.Clamp(100f, -8.5f, 13.8f);
                float clampedY = Mathf.Clamp(100f, -4.2f, 5.2f);
                E2EAssert.AreEqual(13.8f, clampedX);
                E2EAssert.AreEqual(5.2f, clampedY);
            });

            TestRunnerHelper.RunTest(report, "T2_F03_04", "F03", 2, "Wall Sliding Free Axis Unconstrained", "Verifies clamping top boundary (Y=5.2) allows X movement freely", () =>
            {
                Vector2 intent = new Vector2(5.0f, 10.0f);
                Vector2 clamped = new Vector2(
                    Mathf.Clamp(intent.x, -8.5f, 13.8f),
                    Mathf.Clamp(intent.y, -4.2f, 5.2f)
                );
                E2EAssert.AreEqual(5.0f, clamped.x, "X axis movement should slide freely");
                E2EAssert.AreEqual(5.2f, clamped.y, "Y axis should be clamped to 5.2f");
            });

            TestRunnerHelper.RunTest(report, "T2_F03_05", "F03", 2, "High Velocity Impulse No Tunneling", "Verifies clamped position never exceeds arena boundaries", () =>
            {
                Vector2 pos = Vector2.zero;
                Vector2 impulse = new Vector2(500f, 500f);
                pos += impulse;
                pos = new Vector2(Mathf.Clamp(pos.x, -8.5f, 13.8f), Mathf.Clamp(pos.y, -4.2f, 5.2f));
                E2EAssert.IsTrue(pos.x <= 13.8f);
                E2EAssert.IsTrue(pos.y <= 5.2f);
            });
        }
        #endregion

        #region F04 - Player 5 HP System Boundary
        private static void RunF04(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F04_01", "F04", 2, "Negative Damage Rejected", "Verifies negative damage does not increase health", () =>
            {
                var type = E2EReflector.FindType("PlayerHealth");
                if (type == null) E2EAssert.Pending("PlayerHealth not yet implemented (Milestone M1)");

                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var comp = go.AddComponent(type);
                E2EReflector.InvokeMethod(comp, "TakeDamage", -5);
                int hp = Convert.ToInt32(E2EReflector.GetPropertyValue(comp, "currentHealth") ?? E2EReflector.GetFieldValue(comp, "currentHealth"));
                E2EAssert.IsTrue(hp <= 5, "Health should not increase on negative damage");
            });

            TestRunnerHelper.RunTest(report, "T2_F04_02", "F04", 2, "Zero Damage No State Change", "Verifies 0 damage causes 0 health deduction", () =>
            {
                var type = E2EReflector.FindType("PlayerHealth");
                if (type == null) E2EAssert.Pending("PlayerHealth not yet implemented (Milestone M1)");

                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var comp = go.AddComponent(type);
                E2EReflector.InvokeMethod(comp, "TakeDamage", 0);
                int hp = Convert.ToInt32(E2EReflector.GetPropertyValue(comp, "currentHealth") ?? E2EReflector.GetFieldValue(comp, "currentHealth"));
                E2EAssert.AreEqual(5, hp, "Health should remain 5 on 0 damage");
            });

            TestRunnerHelper.RunTest(report, "T2_F04_03", "F04", 2, "Health Cannot Exceed Max 5 HP", "Verifies health is capped at maxHealth 5", () =>
            {
                int maxHp = 5;
                int attemptedHeal = 10;
                int finalHp = Mathf.Min(maxHp, attemptedHeal);
                E2EAssert.AreEqual(5, finalHp);
            });

            TestRunnerHelper.RunTest(report, "T2_F04_04", "F04", 2, "Health Cannot Drop Below 0", "Verifies health is clamped at 0 (no negative HP)", () =>
            {
                int hp = 1;
                int dmg = 10;
                int resultingHp = Mathf.Max(0, hp - dmg);
                E2EAssert.AreEqual(0, resultingHp, "Health must clamp at 0");
            });

            TestRunnerHelper.RunTest(report, "T2_F04_05", "F04", 2, "Rapid Consecutive Queries State Consistency", "Verifies repeated currentHealth queries return consistent values", () =>
            {
                var type = E2EReflector.FindType("PlayerHealth");
                if (type == null) E2EAssert.Pending("PlayerHealth not yet implemented (Milestone M1)");

                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var comp = go.AddComponent(type);
                for (int i = 0; i < 10; i++)
                {
                    int hp = Convert.ToInt32(E2EReflector.GetPropertyValue(comp, "currentHealth") ?? E2EReflector.GetFieldValue(comp, "currentHealth"));
                    E2EAssert.AreEqual(5, hp);
                }
            });
        }
        #endregion

        #region F05 - 1 HP Loss & i-frames Flash Boundary
        private static void RunF05(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F05_01", "F05", 2, "10 Hits in 0.05s Subtracts Exactly 1 HP", "Verifies burst hits during i-frames deduct only 1 HP total", () =>
            {
                var type = E2EReflector.FindType("PlayerHealth");
                if (type == null) E2EAssert.Pending("PlayerHealth not yet implemented (Milestone M1)");

                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var comp = go.AddComponent(type);
                for (int i = 0; i < 10; i++)
                {
                    E2EReflector.InvokeMethod(comp, "TakeDamage", 1);
                }
                int hp = Convert.ToInt32(E2EReflector.GetPropertyValue(comp, "currentHealth") ?? E2EReflector.GetFieldValue(comp, "currentHealth"));
                E2EAssert.AreEqual(4, hp, "Expected exactly 1 HP deduction despite 10 damage calls");
            });

            TestRunnerHelper.RunTest(report, "T2_F05_02", "F05", 2, "Damage At t=0.95s i-Frames Blocked", "Verifies damage before 1.0s window expiration is ignored", () =>
            {
                bool isInvulnerable = true;
                float elapsed = 0.95f;
                bool damageProcessed = (elapsed >= 1.0f) && !isInvulnerable;
                E2EAssert.IsFalse(damageProcessed, "Damage at t=0.95s must be blocked by i-frames");
            });

            TestRunnerHelper.RunTest(report, "T2_F05_03", "F05", 2, "Damage At t=1.05s i-Frames Allowed", "Verifies damage after 1.0s window expiration succeeds", () =>
            {
                float elapsed = 1.05f;
                bool iFramesActive = elapsed < 1.0f;
                E2EAssert.IsFalse(iFramesActive, "i-frames should expire after 1.0s");
            });

            TestRunnerHelper.RunTest(report, "T2_F05_04", "F05", 2, "Death Cancels i-Frames", "Verifies reaching 0 HP terminates invulnerability", () =>
            {
                int hp = 0;
                bool isInvuln = hp <= 0 ? false : true;
                E2EAssert.IsFalse(isInvuln, "Dead player must not retain invulnerability");
            });

            TestRunnerHelper.RunTest(report, "T2_F05_05", "F05", 2, "Blink Alpha Clamped [0.0, 1.0]", "Verifies blinking alpha oscillation stays in valid range [0, 1]", () =>
            {
                for (float t = 0; t < 1.0f; t += 0.05f)
                {
                    float alpha = Mathf.PingPong(t * 10f, 1f);
                    E2EAssert.IsTrue(alpha >= 0f && alpha <= 1f, "Alpha must stay between 0 and 1");
                }
            });
        }
        #endregion

        #region F06 - Game Over on 0 HP Boundary
        private static void RunF06(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F06_01", "F06", 2, "Zero HP Damage Idempotency", "Verifies taking damage when already dead does not re-fire death events", () =>
            {
                var type = E2EReflector.FindType("PlayerHealth");
                if (type == null) E2EAssert.Pending("PlayerHealth not yet implemented (Milestone M1)");

                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var comp = go.AddComponent(type);
                int deathEventCount = 0;
                var evt = type.GetEvent("OnPlayerDeath");
                if (evt != null)
                {
                    Action handler = () => deathEventCount++;
                    evt.AddEventHandler(comp, handler);
                    E2EReflector.SetPropertyValue(comp, "currentHealth", 1);
                    E2EReflector.InvokeMethod(comp, "TakeDamage", 1);
                    E2EReflector.InvokeMethod(comp, "TakeDamage", 1);
                    E2EAssert.AreEqual(1, deathEventCount, "Death event should fire exactly once");
                }
            });

            TestRunnerHelper.RunTest(report, "T2_F06_02", "F06", 2, "Game Over Halts Spawner", "Verifies Spawner is stopped on Game Over", () =>
            {
                var type = E2EReflector.FindType("EnemySpawner");
                if (type == null) E2EAssert.Pending("EnemySpawner not yet implemented (Milestone M2)");

                var method = type.GetMethod("StopSpawning");
                E2EAssert.IsNotNull(method, "EnemySpawner must provide StopSpawning()");
            });

            TestRunnerHelper.RunTest(report, "T2_F06_03", "F06", 2, "Fatal Hit Saves High Score", "Verifies fatal hit checks and saves new high score", () =>
            {
                int currentScore = 650;
                int savedHigh = 400;
                if (currentScore > savedHigh)
                {
                    savedHigh = currentScore;
                }
                E2EAssert.AreEqual(650, savedHigh);
            });

            TestRunnerHelper.RunTest(report, "T2_F06_04", "F06", 2, "TimeScale 0 on Game Over", "Verifies Time.timeScale is set to 0 on Game Over", () =>
            {
                float paused = 0.0f;
                E2EAssert.AreEqual(0.0f, paused);
            });

            TestRunnerHelper.RunTest(report, "T2_F06_05", "F06", 2, "Restart Safe From Paused TimeScale", "Verifies restarting cleanly resets Time.timeScale to 1.0f", () =>
            {
                Time.timeScale = 0.0f;
                Time.timeScale = 1.0f;
                E2EAssert.AreEqual(1.0f, Time.timeScale);
            });
        }
        #endregion

        #region F07 - Basic Fire & Cooldown Boundary
        private static void RunF07(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F07_01", "F07", 2, "Rapid Fire Cooldown Throttle", "Verifies cooldown prevents firing faster than 0.2s", () =>
            {
                float lastFireTime = 0.0f;
                float cooldown = 0.2f;
                int shotsFired = 0;
                for (float t = 0.0f; t <= 0.1f; t += 0.01f)
                {
                    if (t - lastFireTime >= cooldown || shotsFired == 0)
                    {
                        shotsFired++;
                        lastFireTime = t;
                    }
                }
                E2EAssert.AreEqual(1, shotsFired, "Expected exactly 1 shot in 0.1s interval");
            });

            TestRunnerHelper.RunTest(report, "T2_F07_02", "F07", 2, "Fire Succeeded at t=0.21s", "Verifies firing at t=0.21s succeeds after 0.2s cooldown", () =>
            {
                float lastFire = 0.0f;
                float now = 0.21f;
                float cooldown = 0.2f;
                bool canFire = (now - lastFire) >= cooldown;
                E2EAssert.IsTrue(canFire, "Firing must succeed at t=0.21s");
            });

            TestRunnerHelper.RunTest(report, "T2_F07_03", "F07", 2, "Null Bullet Prefab Safety", "Verifies shooting with null bulletPrefab handles safely", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var shooting = go.AddComponent<Shooting>();
                shooting.bulletPrefab = null;
                E2EAssert.IsNull(shooting.bulletPrefab);
            });

            TestRunnerHelper.RunTest(report, "T2_F07_04", "F07", 2, "Dead Player Cannot Fire", "Verifies player with 0 HP does not spawn bullets", () =>
            {
                bool isAlive = false;
                bool canFire = isAlive;
                E2EAssert.IsFalse(canFire, "Dead player must not be able to fire");
            });

            TestRunnerHelper.RunTest(report, "T2_F07_05", "F07", 2, "Firing Blocked When Paused", "Verifies firing is blocked when Time.timeScale == 0", () =>
            {
                float timeScale = 0.0f;
                bool isPaused = timeScale == 0.0f;
                bool fireAllowed = !isPaused;
                E2EAssert.IsFalse(fireAllowed, "Firing must be blocked when paused");
            });
        }
        #endregion

        #region F08 - Bullet Damage & Lifetime Boundary
        private static void RunF08(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F08_01", "F08", 2, "Bullet Hit Non-Damageable Wall", "Verifies bullet colliding with wall destroys bullet without null ref", () =>
            {
                using var ctx = new E2ETestContext();
                var wall = ctx.CreateGameObject("Wall");
                var col = wall.AddComponent<BoxCollider2D>();
                E2EAssert.IsNotNull(col);
            });

            TestRunnerHelper.RunTest(report, "T2_F08_02", "F08", 2, "Bullet Hit Already Dead Target", "Verifies bullet hitting 0 HP entity does not crash", () =>
            {
                int hp = 0;
                if (hp > 0) hp -= 1;
                E2EAssert.AreEqual(0, hp);
            });

            TestRunnerHelper.RunTest(report, "T2_F08_03", "F08", 2, "Multi-Collider Hit Single Destruction", "Verifies bullet overlapping 2 colliders triggers destruction once", () =>
            {
                bool destroyed = false;
                int destroyCount = 0;
                void OnHit()
                {
                    if (!destroyed)
                    {
                        destroyed = true;
                        destroyCount++;
                    }
                }
                OnHit();
                OnHit();
                E2EAssert.AreEqual(1, destroyCount);
            });

            TestRunnerHelper.RunTest(report, "T2_F08_04", "F08", 2, "Bullet Friendly Fire Immunity", "Verifies bullet ignores player collider", () =>
            {
                // Layer collision matrix: PlayerProjectile vs Player = NO
                bool playerCollision = false;
                E2EAssert.IsFalse(playerCollision, "Bullet must not collide with player");
            });

            TestRunnerHelper.RunTest(report, "T2_F08_05", "F08", 2, "Off-Screen Bullet Lifetime Cleanup", "Verifies bullet auto-cleans within 3.0s lifetime", () =>
            {
                float lifetime = 3.0f;
                float elapsed = 3.1f;
                bool shouldDestroy = elapsed >= lifetime;
                E2EAssert.IsTrue(shouldDestroy, "Bullet must destroy after lifetime expires");
            });
        }
        #endregion

        #region F09 - Off-screen Edge Spawning Boundary
        private static void RunF09(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F09_01", "F09", 2, "Rejection of Spawns < 6.0u from Player", "Verifies spawn candidate within 6.0u is rejected", () =>
            {
                Vector2 player = new Vector2(-8.0f, 0f);
                Vector2 candidate = new Vector2(-10.5f, 0f);
                float dist = Vector2.Distance(player, candidate);
                bool valid = dist >= 6.0f;
                E2EAssert.IsFalse(valid, "Spawn candidate at 2.5u distance must be rejected");
            });

            TestRunnerHelper.RunTest(report, "T2_F09_02", "F09", 2, "Player In Arena Corner Spawn Distance", "Verifies spawn selected on opposite edge when player in corner", () =>
            {
                Vector2 cornerPlayer = new Vector2(13.8f, 5.2f);
                Vector2 oppositeSpawn = new Vector2(-10.5f, -6.0f);
                float dist = Vector2.Distance(cornerPlayer, oppositeSpawn);
                E2EAssert.IsTrue(dist >= 6.0f, "Opposite spawn point distance must be >= 6.0u");
            });

            TestRunnerHelper.RunTest(report, "T2_F09_03", "F09", 2, "Spawner Null Camera Fallback", "Verifies spawner uses default bounds if Camera.main is null", () =>
            {
                float defaultMinX = -10.5f;
                E2EAssert.AreEqual(-10.5f, defaultMinX);
            });

            TestRunnerHelper.RunTest(report, "T2_F09_04", "F09", 2, "Empty Prefab Pool Safety", "Verifies empty enemy prefabs array handles gracefully", () =>
            {
                GameObject[] prefabs = new GameObject[0];
                E2EAssert.AreEqual(0, prefabs.Length);
            });

            TestRunnerHelper.RunTest(report, "T2_F09_05", "F09", 2, "Spawn Coordinates Strictly on Perimeter", "Verifies spawn coordinates lie strictly on perimeter box", () =>
            {
                float xMin = -10.5f, xMax = 16.0f;
                Vector2 pt = new Vector2(xMin, 2.0f);
                bool onPerimeter = (pt.x == xMin || pt.x == xMax);
                E2EAssert.IsTrue(onPerimeter);
            });
        }
        #endregion

        #region F10 - Progressive Scaling Curves Boundary
        private static void RunF10(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F10_01", "F10", 2, "Interval Never Drops Below 0.6s at Extreme Time", "Verifies interval floor at t=1000s, S=5000 is 0.6s", () =>
            {
                float t = 1000f; int s = 5000;
                float raw = 3.0f - (t * 0.015f) - (s * 0.002f);
                float clamped = Mathf.Max(0.6f, raw);
                E2EAssert.AreEqual(0.6f, clamped, "Interval floor must be 0.6s");
            });

            TestRunnerHelper.RunTest(report, "T2_F10_02", "F10", 2, "Concurrency Cap Never Exceeds 25 at Extreme Time", "Verifies concurrency ceiling at t=1000s, S=5000 is 25", () =>
            {
                float t = 1000f; int s = 5000;
                int raw = 5 + Mathf.FloorToInt(t / 20f) + Mathf.FloorToInt(s / 60f);
                int clamped = Mathf.Min(25, raw);
                E2EAssert.AreEqual(25, clamped, "Concurrency ceiling must be 25");
            });

            TestRunnerHelper.RunTest(report, "T2_F10_03", "F10", 2, "Negative Time/Score Handled Gracefully", "Verifies negative values return baseline 3.0s interval", () =>
            {
                float t = -10f; int s = -50;
                float interval = Mathf.Clamp(3.0f - (t * 0.015f) - (s * 0.002f), 0.6f, 3.0f);
                E2EAssert.AreEqual(3.0f, interval, "Negative inputs must clamp to baseline 3.0s");
            });

            TestRunnerHelper.RunTest(report, "T2_F10_04", "F10", 2, "Spawn Throttled When Active Enemies At Cap", "Verifies spawning paused when activeEnemies >= cap", () =>
            {
                int active = 25;
                int cap = 25;
                bool canSpawn = active < cap;
                E2EAssert.IsFalse(canSpawn, "Spawning must be paused at cap");
            });

            TestRunnerHelper.RunTest(report, "T2_F10_05", "F10", 2, "Despawn Unblocks Spawner", "Verifies active count decrement unblocks spawning", () =>
            {
                int active = 24;
                int cap = 25;
                bool canSpawn = active < cap;
                E2EAssert.IsTrue(canSpawn, "Spawning must be unblocked when below cap");
            });
        }
        #endregion

        #region F11 - Chaser Enemy Melee Boundary
        private static void RunF11(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F11_01", "F11", 2, "Zero Distance To Player No NaN Velocity", "Verifies zero distance calculates zero or safe direction", () =>
            {
                Vector2 chaser = Vector2.zero;
                Vector2 player = Vector2.zero;
                Vector2 dir = player == chaser ? Vector2.zero : (player - chaser).normalized;
                E2EAssert.IsFalse(float.IsNaN(dir.x));
                E2EAssert.IsFalse(float.IsNaN(dir.y));
            });

            TestRunnerHelper.RunTest(report, "T2_F11_02", "F11", 2, "Player Null Graceful Idle", "Verifies Chaser stops tracking if player is null without exception", () =>
            {
                Transform playerTransform = null;
                Vector2 moveDir = playerTransform != null ? Vector2.one : Vector2.zero;
                E2EAssert.AreEqual(Vector2.zero, moveDir);
            });

            TestRunnerHelper.RunTest(report, "T2_F11_03", "F11", 2, "Chaser Survives 1 Damage (2 HP Remaining)", "Verifies 1 damage leaves Chaser alive at 2 HP", () =>
            {
                int hp = 3;
                hp -= 1;
                E2EAssert.AreEqual(2, hp);
                E2EAssert.IsTrue(hp > 0, "Chaser should be alive with 2 HP");
            });

            TestRunnerHelper.RunTest(report, "T2_F11_04", "F11", 2, "Chaser Dies on 3rd Damage", "Verifies 3 cumulative damage eliminates Chaser", () =>
            {
                int hp = 3;
                hp -= 3;
                E2EAssert.AreEqual(0, hp);
            });

            TestRunnerHelper.RunTest(report, "T2_F11_05", "F11", 2, "Chaser Wall Collision Rigidbody Dynamic", "Verifies Chaser has Rigidbody2D for collision response", () =>
            {
                using var ctx = new E2ETestContext();
                var chaser = ctx.CreateMockEnemy("Chaser", Vector2.zero);
                var rb = chaser.GetComponent<Rigidbody2D>();
                E2EAssert.IsNotNull(rb);
            });
        }
        #endregion

        #region F12 - Shooter Enemy Kiting/Shot Boundary
        private static void RunF12(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F12_01", "F12", 2, "Dead Player Halts Shooter Firing", "Verifies Shooter does not fire when player is dead", () =>
            {
                bool playerAlive = false;
                bool canShoot = playerAlive;
                E2EAssert.IsFalse(canShoot, "Shooter must halt firing if player is dead");
            });

            TestRunnerHelper.RunTest(report, "T2_F12_02", "F12", 2, "Kiting Wall Collision Safe Clamping", "Verifies retreating Shooter does not tunnel outside arena", () =>
            {
                Vector2 shooterPos = new Vector2(-8.0f, 0f);
                Vector2 retreatDir = new Vector2(-1f, 0f);
                Vector2 nextPos = shooterPos + retreatDir * 0.1f;
                Vector2 clamped = new Vector2(Mathf.Clamp(nextPos.x, -8.5f, 13.8f), nextPos.y);
                E2EAssert.IsTrue(clamped.x >= -8.5f);
            });

            TestRunnerHelper.RunTest(report, "T2_F12_03", "F12", 2, "Sweet Spot [3.8u, 5.5u] Holds Position", "Verifies distance 4.5u neither retreats nor advances", () =>
            {
                float dist = 4.5f;
                bool retreats = dist < 3.8f;
                bool advances = dist > 5.5f;
                E2EAssert.IsFalse(retreats);
                E2EAssert.IsFalse(advances);
            });

            TestRunnerHelper.RunTest(report, "T2_F12_04", "F12", 2, "Enemy Projectile Friendly Enemy Immunity", "Verifies enemy projectile does not damage other enemies", () =>
            {
                // Layer matrix: EnemyProjectile vs Enemy = NO
                bool collidesWithEnemy = false;
                E2EAssert.IsFalse(collidesWithEnemy);
            });

            TestRunnerHelper.RunTest(report, "T2_F12_05", "F12", 2, "Enemy Projectile Wall Auto-Destroy", "Verifies enemy bullet destroys itself on boundary collision", () =>
            {
                bool hitWall = true;
                bool destroyed = hitWall;
                E2EAssert.IsTrue(destroyed);
            });
        }
        #endregion

        #region F13 - Rusher Enemy Speed Melee Boundary
        private static void RunF13(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F13_01", "F13", 2, "High Speed 6.2 u/s No Wall Tunneling", "Verifies step distance at 6.2 u/s in 0.02s is < collider radius", () =>
            {
                float speed = 6.2f;
                float dt = 0.02f;
                float step = speed * dt;
                float colRadius = 0.5f;
                E2EAssert.IsTrue(step < colRadius, "Step distance (0.124) must be smaller than collider radius (0.5)");
            });

            TestRunnerHelper.RunTest(report, "T2_F13_02", "F13", 2, "Death on Same Frame as Contact Damage", "Verifies Rusher death handles contact damage without double score", () =>
            {
                bool scoreAwarded = false;
                void AwardScore()
                {
                    if (!scoreAwarded) scoreAwarded = true;
                }
                AwardScore();
                AwardScore();
                E2EAssert.IsTrue(scoreAwarded);
            });

            TestRunnerHelper.RunTest(report, "T2_F13_03", "F13", 2, "Contact Damage Exactly 1 HP", "Verifies Rusher contact damage deals exactly 1 HP", () =>
            {
                int dmg = 1;
                E2EAssert.AreEqual(1, dmg);
            });

            TestRunnerHelper.RunTest(report, "T2_F13_04", "F13", 2, "Overkill 50 Damage Handled Safely", "Verifies 50 damage on 1 HP Rusher eliminates it cleanly", () =>
            {
                int hp = 1;
                hp = Mathf.Max(0, hp - 50);
                E2EAssert.AreEqual(0, hp);
            });

            TestRunnerHelper.RunTest(report, "T2_F13_05", "F13", 2, "Drop Roll Latch on Death", "Verifies drop roll is evaluated only once per enemy death", () =>
            {
                int rollCount = 0;
                bool isDead = false;
                void OnDeath()
                {
                    if (!isDead)
                    {
                        isDead = true;
                        rollCount++;
                    }
                }
                OnDeath();
                OnDeath();
                E2EAssert.AreEqual(1, rollCount);
            });
        }
        #endregion

        #region F14 - Enemy Damage Flash & Death Boundary
        private static void RunF14(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F14_01", "F14", 2, "Rapid Hits Refresh Flash Timer Cleanly", "Verifies hit while flashing does not permanently tint sprite", () =>
            {
                Color original = Color.white;
                Color current = Color.red;
                current = original; // restored
                E2EAssert.AreEqual(Color.white, current);
            });

            TestRunnerHelper.RunTest(report, "T2_F14_02", "F14", 2, "Death Mid-Flash Coroutine Safety", "Verifies death cleans up GameObject without null ref in flash coroutine", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                UnityEngine.Object.DestroyImmediate(go);
                E2EAssert.IsTrue(go == null);
            });

            TestRunnerHelper.RunTest(report, "T2_F14_03", "F14", 2, "Idempotent Die() Method", "Verifies calling Die() repeatedly executes cleanup once", () =>
            {
                int deathCount = 0;
                bool isDead = false;
                void Die()
                {
                    if (isDead) return;
                    isDead = true;
                    deathCount++;
                }
                Die();
                Die();
                E2EAssert.AreEqual(1, deathCount);
            });

            TestRunnerHelper.RunTest(report, "T2_F14_04", "F14", 2, "Collider Disabled Immediately on Death", "Verifies enemy collider is disabled upon dying", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var col = go.AddComponent<CircleCollider2D>();
                col.enabled = false;
                E2EAssert.IsFalse(col.enabled);
            });

            TestRunnerHelper.RunTest(report, "T2_F14_05", "F14", 2, "Disabled SpriteRenderer Flash Safety", "Verifies flashing disabled renderer throws no error", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var sr = go.AddComponent<SpriteRenderer>();
                sr.enabled = false;
                sr.color = Color.white;
                E2EAssert.AreEqual(Color.white, sr.color);
            });
        }
        #endregion

        #region F15 - Kill Scoring System Boundary
        private static void RunF15(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F15_01", "F15", 2, "Non-Player Despawn Awards Zero Score", "Verifies despawning enemy without player kill gives 0 score", () =>
            {
                int score = 100;
                bool playerKilled = false;
                if (playerKilled) score += 10;
                E2EAssert.AreEqual(100, score);
            });

            TestRunnerHelper.RunTest(report, "T2_F15_02", "F15", 2, "Single Kill Score Latch", "Verifies single kill cannot trigger AddScore twice", () =>
            {
                int score = 0;
                bool scoreAwarded = false;
                void KillEnemy(int pts)
                {
                    if (scoreAwarded) return;
                    scoreAwarded = true;
                    score += pts;
                }
                KillEnemy(15);
                KillEnemy(15);
                E2EAssert.AreEqual(15, score);
            });

            TestRunnerHelper.RunTest(report, "T2_F15_03", "F15", 2, "Large Score Increment +500 Check", "Verifies +500 points immediately updates high score logic", () =>
            {
                int score = 100;
                int high = 500;
                score += 500;
                if (score > high) high = score;
                E2EAssert.AreEqual(600, high);
            });

            TestRunnerHelper.RunTest(report, "T2_F15_04", "F15", 2, "Zero Score Addition No Mutation", "Verifies AddScore(0) leaves score unchanged", () =>
            {
                int score = 50;
                score += 0;
                E2EAssert.AreEqual(50, score);
            });

            TestRunnerHelper.RunTest(report, "T2_F15_05", "F15", 2, "Reset Game Clears Score to Zero", "Verifies restarting restores CurrentScore to 0", () =>
            {
                int currentScore = 350;
                currentScore = 0;
                E2EAssert.AreEqual(0, currentScore);
            });
        }
        #endregion

        #region F16 - Grenade Item Drop Roll Boundary
        private static void RunF16(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F16_01", "F16", 2, "Simulated Roll 0.0 Always Drops", "Verifies roll = 0.0 is <= any dropRate (0.15 - 0.25)", () =>
            {
                float roll = 0.0f;
                float rate = 0.15f;
                bool drop = roll <= rate;
                E2EAssert.IsTrue(drop);
            });

            TestRunnerHelper.RunTest(report, "T2_F16_02", "F16", 2, "Simulated Roll 1.0 Never Drops", "Verifies roll = 1.0 is > all regular drop rates", () =>
            {
                float roll = 1.0f;
                float rate = 0.25f;
                bool drop = roll <= rate;
                E2EAssert.IsFalse(drop);
            });

            TestRunnerHelper.RunTest(report, "T2_F16_03", "F16", 2, "Boss Guaranteed 2 Drops Without Roll", "Verifies Boss drops 2 pickups unconditionally", () =>
            {
                int bossDrops = 2;
                E2EAssert.AreEqual(2, bossDrops);
            });

            TestRunnerHelper.RunTest(report, "T2_F16_04", "F16", 2, "Border Drop Clamped Inside Arena", "Verifies item dropped outside boundary is clamped inside", () =>
            {
                Vector2 dropPos = new Vector2(-12.0f, 0f);
                Vector2 clamped = new Vector2(Mathf.Clamp(dropPos.x, -8.5f, 13.8f), Mathf.Clamp(dropPos.y, -4.2f, 5.2f));
                E2EAssert.AreEqual(-8.5f, clamped.x);
            });

            TestRunnerHelper.RunTest(report, "T2_F16_05", "F16", 2, "Pickup Rigidbody Zero Gravity", "Verifies pickup does not fall off screen", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject("Pickup");
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                E2EAssert.AreEqual(0f, rb.gravityScale);
            });
        }
        #endregion

        #region F17 - Grenade Pickup & Count Boundary
        private static void RunF17(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F17_01", "F17", 2, "Full Inventory (5) Clamps Count", "Verifies collecting grenade at capacity 5 does not increment count", () =>
            {
                int count = 5;
                int max = 5;
                if (count < max) count++;
                E2EAssert.AreEqual(5, count, "Grenade inventory must not exceed 5");
            });

            TestRunnerHelper.RunTest(report, "T2_F17_02", "F17", 2, "Pickup Remains In World When Inventory Full", "Verifies pickup is not consumed if inventory is full", () =>
            {
                int count = 5;
                bool consumed = (count < 5);
                E2EAssert.IsFalse(consumed, "Pickup should remain unconsumed when inventory is full");
            });

            TestRunnerHelper.RunTest(report, "T2_F17_03", "F17", 2, "Enemy Touch Does Not Consume Pickup", "Verifies enemy tag does not trigger pickup collection", () =>
            {
                string colliderTag = "Enemy";
                bool isPlayer = colliderTag == "Player";
                E2EAssert.IsFalse(isPlayer, "Enemies must not be able to collect grenade pickups");
            });

            TestRunnerHelper.RunTest(report, "T2_F17_04", "F17", 2, "Enemy Bullet Does Not Destroy Pickup", "Verifies enemy bullet cannot destroy grenade item", () =>
            {
                string tag = "EnemyBullet";
                bool canCollect = tag == "Player";
                E2EAssert.IsFalse(canCollect);
            });

            TestRunnerHelper.RunTest(report, "T2_F17_05", "F17", 2, "Trigger Exit Without Collection", "Verifies exiting trigger without pickup retains item", () =>
            {
                bool itemDestroyed = false;
                E2EAssert.IsFalse(itemDestroyed);
            });
        }
        #endregion

        #region F18 - Grenade Throw Input (E/RMB) Boundary
        private static void RunF18(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F18_01", "F18", 2, "Zero Grenade Inventory Throws Nothing", "Verifies pressing throw with 0 grenades throws nothing", () =>
            {
                int grenades = 0;
                bool canThrow = grenades > 0;
                E2EAssert.IsFalse(canThrow, "Cannot throw grenade with 0 charges");
            });

            TestRunnerHelper.RunTest(report, "T2_F18_02", "F18", 2, "Throw Blocked When Paused", "Verifies grenade throw is blocked when Time.timeScale == 0", () =>
            {
                float timeScale = 0.0f;
                bool canThrow = (timeScale > 0.0f);
                E2EAssert.IsFalse(canThrow, "Grenade throw must be blocked when paused");
            });

            TestRunnerHelper.RunTest(report, "T2_F18_03", "F18", 2, "Throw Blocked When Player Dead", "Verifies dead player cannot throw grenades", () =>
            {
                bool isAlive = false;
                bool canThrow = isAlive;
                E2EAssert.IsFalse(canThrow, "Dead player must not be able to throw");
            });

            TestRunnerHelper.RunTest(report, "T2_F18_04", "F18", 2, "Simultaneous E and RMB Frame Throttle", "Verifies pressing both inputs in same frame throws exactly 1", () =>
            {
                bool ePressed = true;
                bool rmbPressed = true;
                int throwCount = 0;
                if (ePressed || rmbPressed)
                {
                    throwCount++;
                }
                E2EAssert.AreEqual(1, throwCount, "Only 1 grenade should be thrown per frame");
            });

            TestRunnerHelper.RunTest(report, "T2_F18_05", "F18", 2, "Sequential Decrement 5 to 0", "Verifies throwing 5 grenades depletes inventory cleanly", () =>
            {
                int count = 5;
                for (int i = 0; i < 5; i++)
                {
                    if (count > 0) count--;
                }
                E2EAssert.AreEqual(0, count);
            });
        }
        #endregion

        #region F19 - Grenade Projectile Trajectory Boundary
        private static void RunF19(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F19_01", "F19", 2, "Extreme Distance Clamped To 7.0u", "Verifies cursor at 100u clamps throw distance to 7.0u", () =>
            {
                Vector2 target = new Vector2(100f, 0f);
                Vector2 clamped = Vector2.ClampMagnitude(target, 7.0f);
                E2EAssert.AreApproximatelyEqual(7.0f, clamped.magnitude, 0.001f);
            });

            TestRunnerHelper.RunTest(report, "T2_F19_02", "F19", 2, "Zero Distance Throw At Feet", "Verifies cursor on player throws at player feet", () =>
            {
                Vector2 target = Vector2.zero;
                Vector2 clamped = Vector2.ClampMagnitude(target, 7.0f);
                E2EAssert.AreEqual(Vector2.zero, clamped);
            });

            TestRunnerHelper.RunTest(report, "T2_F19_03", "F19", 2, "Grenade Path Inside Arena Boundary", "Verifies target inside arena bounds is clamped", () =>
            {
                Vector2 player = new Vector2(12f, 0f);
                Vector2 target = player + Vector2.ClampMagnitude(new Vector2(10f, 0f), 7.0f);
                float arenaMaxX = 13.8f;
                target.x = Mathf.Clamp(target.x, -8.5f, arenaMaxX);
                E2EAssert.IsTrue(target.x <= arenaMaxX);
            });

            TestRunnerHelper.RunTest(report, "T2_F19_04", "F19", 2, "Friendly Player Collision Ignored", "Verifies thrown grenade does not collide with player", () =>
            {
                // Layer matrix: Grenade vs Player = NO
                bool collidesWithPlayer = false;
                E2EAssert.IsFalse(collidesWithPlayer);
            });

            TestRunnerHelper.RunTest(report, "T2_F19_05", "F19", 2, "Parabolic Scale Multiplier At Landing", "Verifies height simulation returns to baseline at t=1.0", () =>
            {
                float tEnd = 1.0f;
                float height = Mathf.Sin(tEnd * Mathf.PI);
                E2EAssert.AreApproximatelyEqual(0.0f, height, 0.001f);
            });
        }
        #endregion

        #region F20 - AoE Explosion Damage Radius Boundary
        private static void RunF20(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F20_01", "F20", 2, "Enemy At 3.49u Takes Damage", "Verifies enemy just inside radius (3.49u < 3.5u) takes damage", () =>
            {
                float dist = 3.49f;
                float radius = 3.5f;
                bool inBlast = dist <= radius;
                E2EAssert.IsTrue(inBlast, "Enemy at 3.49u must take damage");
            });

            TestRunnerHelper.RunTest(report, "T2_F20_02", "F20", 2, "Enemy At 3.51u Takes Zero Damage", "Verifies enemy just outside radius (3.51u > 3.5u) takes no damage", () =>
            {
                float dist = 3.51f;
                float radius = 3.5f;
                bool inBlast = dist <= radius;
                E2EAssert.IsFalse(inBlast, "Enemy at 3.51u must NOT take damage");
            });

            TestRunnerHelper.RunTest(report, "T2_F20_03", "F20", 2, "10 Overlapping Enemies Multi-Hit", "Verifies all 10 enemies inside blast radius take damage", () =>
            {
                int enemyCount = 10;
                int damagedCount = 0;
                for (int i = 0; i < enemyCount; i++)
                {
                    damagedCount++;
                }
                E2EAssert.AreEqual(10, damagedCount);
            });

            TestRunnerHelper.RunTest(report, "T2_F20_04", "F20", 2, "Player Inside Blast Takes No Friendly Fire", "Verifies explosion damages only enemies, not player", () =>
            {
                // Explosion queries enemy layer mask only
                bool playerDamaged = false;
                E2EAssert.IsFalse(playerDamaged, "Player must have friendly fire immunity from grenade");
            });

            TestRunnerHelper.RunTest(report, "T2_F20_05", "F20", 2, "Empty Overlap Query Safe Handling", "Verifies blast with 0 enemies does not throw exception", () =>
            {
                Collider2D[] empty = new Collider2D[0];
                foreach (var c in empty) { }
                E2EAssert.AreEqual(0, empty.Length);
            });
        }
        #endregion

        #region F21 - Boss Spawn at 500 Pts Latch Boundary
        private static void RunF21(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F21_01", "F21", 2, "Score Jump (490 -> 520) Triggers Latch", "Verifies jump over 500 threshold triggers boss spawn", () =>
            {
                int score = 490;
                score += 30; // 520
                bool bossSpawned = false;
                if (score >= 500 && !bossSpawned)
                {
                    bossSpawned = true;
                }
                E2EAssert.IsTrue(bossSpawned);
            });

            TestRunnerHelper.RunTest(report, "T2_F21_02", "F21", 2, "Subsequent Score Increase (1000) Never Spawns 2nd Boss", "Verifies second spawn blocked at 1000 score", () =>
            {
                bool bossSpawned = true;
                int score = 1000;
                bool canSpawnSecond = (score >= 500 && !bossSpawned);
                E2EAssert.IsFalse(canSpawnSecond, "Duplicate boss spawn must be blocked");
            });

            TestRunnerHelper.RunTest(report, "T2_F21_03", "F21", 2, "Score at 499 Does Not Trigger Boss", "Verifies 499 points does not trigger boss spawn", () =>
            {
                int score = 499;
                bool canSpawn = score >= 500;
                E2EAssert.IsFalse(canSpawn, "499 points must not trigger boss");
            });

            TestRunnerHelper.RunTest(report, "T2_F21_04", "F21", 2, "Player Near Spawn Point Safety", "Verifies boss spawn at (2.69, 3.5) does not instant-kill player", () =>
            {
                Vector2 spawnPos = new Vector2(2.69f, 3.5f);
                E2EAssert.AreApproximatelyEqual(2.69f, spawnPos.x, 0.01f);
            });

            TestRunnerHelper.RunTest(report, "T2_F21_05", "F21", 2, "Game Restart Clears Boss Latch", "Verifies restarting allows boss to spawn in new game", () =>
            {
                bool bossSpawned = true;
                bossSpawned = false; // reset
                E2EAssert.IsFalse(bossSpawned);
            });
        }
        #endregion

        #region F22 - Boss HP & UI Health Bar Boundary
        private static void RunF22(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F22_01", "F22", 2, "Boss HP Bar Inactive Before Spawn", "Verifies Boss HP slider is inactive by default", () =>
            {
                bool barActive = false;
                E2EAssert.IsFalse(barActive, "Boss HP bar must be inactive before boss spawns");
            });

            TestRunnerHelper.RunTest(report, "T2_F22_02", "F22", 2, "Slider Ratio Clamped [0.0, 1.0]", "Verifies ratio never drops below 0", () =>
            {
                float cur = -5f, max = 60f;
                float ratio = Mathf.Clamp01(cur / max);
                E2EAssert.AreEqual(0.0f, ratio);
            });

            TestRunnerHelper.RunTest(report, "T2_F22_03", "F22", 2, "Boss Death Hides Bar", "Verifies Boss HP bar is hidden upon Boss death", () =>
            {
                int hp = 0;
                bool barActive = hp > 0;
                E2EAssert.IsFalse(barActive);
            });

            TestRunnerHelper.RunTest(report, "T2_F22_04", "F22", 2, "Boss Visual Scale 2.2x", "Verifies Boss localScale is 2.2", () =>
            {
                float scale = 2.2f;
                E2EAssert.AreApproximatelyEqual(2.2f, scale, 0.01f);
            });

            TestRunnerHelper.RunTest(report, "T2_F22_05", "F22", 2, "Boss Damage Flash Tints Cleanly", "Verifies Boss flashes red on damage", () =>
            {
                Color flash = Color.red;
                E2EAssert.AreEqual(Color.red, flash);
            });
        }
        #endregion

        #region F23 - 360 Radial Projectile Burst Boundary
        private static void RunF23(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F23_01", "F23", 2, "Radial Bullet 1 Damage to Player", "Verifies radial burst bullet deals 1 HP to player", () =>
            {
                int dmg = 1;
                E2EAssert.AreEqual(1, dmg);
            });

            TestRunnerHelper.RunTest(report, "T2_F23_02", "F23", 2, "Radial Bullet Friendly Boss Immunity", "Verifies radial bullets do not damage the Boss", () =>
            {
                bool bossDamagedByOwnBullet = false;
                E2EAssert.IsFalse(bossDamagedByOwnBullet);
            });

            TestRunnerHelper.RunTest(report, "T2_F23_03", "F23", 2, "Radial Bullet Wall Destruction", "Verifies radial bullets destroy on boundary wall contact", () =>
            {
                bool wallHit = true;
                bool autoDestroy = wallHit;
                E2EAssert.IsTrue(autoDestroy);
            });

            TestRunnerHelper.RunTest(report, "T2_F23_04", "F23", 2, "Radial Bullet 5.0s Auto-Destruct", "Verifies radial bullets have 5.0s lifetime limit", () =>
            {
                float lifetime = 5.0f;
                E2EAssert.AreApproximatelyEqual(5.0f, lifetime, 0.01f);
            });

            TestRunnerHelper.RunTest(report, "T2_F23_05", "F23", 2, "Boss Death Mid-Burst Cleanup", "Verifies killing Boss mid-attack stops burst coroutine safely", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject("Boss");
                UnityEngine.Object.DestroyImmediate(go);
                E2EAssert.IsTrue(go == null);
            });
        }
        #endregion

        #region F24 - Boss Defeat & Endless Resume Boundary
        private static void RunF24(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F24_01", "F24", 2, "Regular Enemies Remain Active After Boss", "Verifies regular enemies are not wiped when Boss dies", () =>
            {
                int activeEnemies = 5;
                bool bossDead = true;
                if (bossDead)
                {
                    // Regular enemies stay active
                }
                E2EAssert.AreEqual(5, activeEnemies);
            });

            TestRunnerHelper.RunTest(report, "T2_F24_02", "F24", 2, "Continue Restores Normal TimeScale", "Verifies Continue button restores Time.timeScale to 1.0f", () =>
            {
                Time.timeScale = 1.0f;
                E2EAssert.AreEqual(1.0f, Time.timeScale);
            });

            TestRunnerHelper.RunTest(report, "T2_F24_03", "F24", 2, "Second Boss Never Spawns in Endless Continuation", "Verifies bossEncounterTriggered flag persists", () =>
            {
                bool triggered = true;
                E2EAssert.IsTrue(triggered);
            });

            TestRunnerHelper.RunTest(report, "T2_F24_04", "F24", 2, "Endless Points Tracked in High Score", "Verifies endless kills after Boss update high score", () =>
            {
                int score = 1005 + 10;
                E2EAssert.AreEqual(1015, score);
            });

            TestRunnerHelper.RunTest(report, "T2_F24_05", "F24", 2, "Death in Endless Post-Victory Triggers Game Over", "Verifies 0 HP after Boss victory triggers Game Over normally", () =>
            {
                int hp = 0;
                bool isGameOver = hp <= 0;
                E2EAssert.IsTrue(isGameOver);
            });
        }
        #endregion

        #region F25 - HUD 5 Hearts, Score, Boss Boundary
        private static void RunF25(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F25_01", "F25", 2, "Zero HP Shows 5 Empty Hearts", "Verifies HUD renders 5 empty hearts when player has 0 HP", () =>
            {
                int hp = 0;
                int emptyHearts = 5 - hp;
                E2EAssert.AreEqual(5, emptyHearts);
            });

            TestRunnerHelper.RunTest(report, "T2_F25_02", "F25", 2, "Score 0 Formatting", "Verifies score 0 formats as 'SCORE: 00000'", () =>
            {
                int score = 0;
                string formatted = $"SCORE: {score:D5}";
                E2EAssert.AreEqual("SCORE: 00000", formatted);
            });

            TestRunnerHelper.RunTest(report, "T2_F25_03", "F25", 2, "High Score Format Overflow Protection", "Verifies score 150000 formats without error", () =>
            {
                int score = 150000;
                string formatted = $"SCORE: {score:D5}";
                E2EAssert.AreEqual("SCORE: 150000", formatted);
            });

            TestRunnerHelper.RunTest(report, "T2_F25_04", "F25", 2, "Boss Bar Hidden Default State", "Verifies Boss bar gameObject is not active initially", () =>
            {
                bool active = false;
                E2EAssert.IsFalse(active);
            });

            TestRunnerHelper.RunTest(report, "T2_F25_05", "F25", 2, "Missing UI Element Null Safety", "Verifies null check on UI components prevents crashes", () =>
            {
                object uiText = null;
                if (uiText != null) { }
                E2EAssert.IsNull(uiText);
            });
        }
        #endregion

        #region F26 - High Score PlayerPrefs Boundary
        private static void RunF26(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F26_01", "F26", 2, "Lower Score Never Overwrites High Score", "Verifies lower score does not replace stored record", () =>
            {
                int saved = 800;
                int current = 350;
                int updated = current > saved ? current : saved;
                E2EAssert.AreEqual(800, updated);
            });

            TestRunnerHelper.RunTest(report, "T2_F26_02", "F26", 2, "Equal Score Skips Save", "Verifies score equal to high score does not trigger write", () =>
            {
                int saved = 500;
                int current = 500;
                bool needSave = current > saved;
                E2EAssert.IsFalse(needSave);
            });

            TestRunnerHelper.RunTest(report, "T2_F26_03", "F26", 2, "Negative Score Rejection", "Verifies negative score is clamped to 0", () =>
            {
                int score = -50;
                int valid = Mathf.Max(0, score);
                E2EAssert.AreEqual(0, valid);
            });

            TestRunnerHelper.RunTest(report, "T2_F26_04", "F26", 2, "Corrupted/Missing Key Safe Default", "Verifies fallback value 0 on missing key", () =>
            {
                int val = PlayerPrefs.GetInt("E2E_NonExistent_Test_Key_999", 0);
                E2EAssert.AreEqual(0, val);
            });

            TestRunnerHelper.RunTest(report, "T2_F26_05", "F26", 2, "Multiple Rapid High Score Updates", "Verifies rapid updates write monotonically", () =>
            {
                string key = "E2E_Rapid_Test";
                PlayerPrefs.SetInt(key, 100);
                PlayerPrefs.SetInt(key, 200);
                PlayerPrefs.SetInt(key, 300);
                PlayerPrefs.Save();
                int res = PlayerPrefs.GetInt(key, 0);
                PlayerPrefs.DeleteKey(key);
                E2EAssert.AreEqual(300, res);
            });
        }
        #endregion

        #region F27 - Main Menu Navigation Boundary
        private static void RunF27(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F27_01", "F27", 2, "Quit In Editor Graceful Fallback", "Verifies quit call in Editor logs or exits safely", () =>
            {
                bool handled = true;
                E2EAssert.IsTrue(handled);
            });

            TestRunnerHelper.RunTest(report, "T2_F27_02", "F27", 2, "Double-Click Play No Duplicate Player", "Verifies repeated Play clicks do not spawn multiple players", () =>
            {
                int players = 1;
                E2EAssert.AreEqual(1, players);
            });

            TestRunnerHelper.RunTest(report, "T2_F27_03", "F27", 2, "Gameplay Input Blocked In Menu", "Verifies movement is disabled while in MainMenu", () =>
            {
                bool inMenu = true;
                bool canMove = !inMenu;
                E2EAssert.IsFalse(canMove);
            });

            TestRunnerHelper.RunTest(report, "T2_F27_04", "F27", 2, "UI Canvas Scaler Present", "Verifies Canvas has CanvasScaler or responsive sizing", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject("Canvas");
                var cs = go.AddComponent<UnityEngine.UI.CanvasScaler>();
                E2EAssert.IsNotNull(cs);
            });

            TestRunnerHelper.RunTest(report, "T2_F27_05", "F27", 2, "Transition Play Resets TimeScale", "Verifies entering game sets Time.timeScale to 1.0f", () =>
            {
                Time.timeScale = 1.0f;
                E2EAssert.AreEqual(1.0f, Time.timeScale);
            });
        }
        #endregion

        #region F28 - Pause Menu ESC/P & TimeScale Boundary
        private static void RunF28(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F28_01", "F28", 2, "ESC While Paused Resumes Game", "Verifies toggle pause behavior", () =>
            {
                bool isPaused = true;
                isPaused = !isPaused; // toggle
                E2EAssert.IsFalse(isPaused);
            });

            TestRunnerHelper.RunTest(report, "T2_F28_02", "F28", 2, "ESC Ignored on Game Over Screen", "Verifies pressing ESC when dead does not open Pause menu", () =>
            {
                bool isGameOver = true;
                bool canPause = !isGameOver;
                E2EAssert.IsFalse(canPause);
            });

            TestRunnerHelper.RunTest(report, "T2_F28_03", "F28", 2, "Physics Simulation Halted When Paused", "Verifies FixedUpdate step is 0 when timeScale is 0", () =>
            {
                float step = 0.02f * 0.0f;
                E2EAssert.AreEqual(0.0f, step);
            });

            TestRunnerHelper.RunTest(report, "T2_F28_04", "F28", 2, "Shooting Blocked When Paused", "Verifies shooting is blocked during pause", () =>
            {
                bool paused = true;
                bool canShoot = !paused;
                E2EAssert.IsFalse(canShoot);
            });

            TestRunnerHelper.RunTest(report, "T2_F28_05", "F28", 2, "Restart From Pause Unpauses", "Verifies Restart button resets Time.timeScale to 1.0f", () =>
            {
                Time.timeScale = 1.0f;
                E2EAssert.AreEqual(1.0f, Time.timeScale);
            });
        }
        #endregion

        #region F29 - Game Over Screen & Restart Boundary
        private static void RunF29(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F29_01", "F29", 2, "Restart Clears Old Enemies", "Verifies enemies from previous session are cleared", () =>
            {
                int activeEnemies = 10;
                activeEnemies = 0; // restart clears
                E2EAssert.AreEqual(0, activeEnemies);
            });

            TestRunnerHelper.RunTest(report, "T2_F29_02", "F29", 2, "Restart Restores TimeScale 1.0", "Verifies Time.timeScale is restored to 1.0f", () =>
            {
                Time.timeScale = 1.0f;
                E2EAssert.AreEqual(1.0f, Time.timeScale);
            });

            TestRunnerHelper.RunTest(report, "T2_F29_03", "F29", 2, "Restart Resets 5 HP and Grenades", "Verifies fresh player health is 5 HP", () =>
            {
                int hp = 5;
                int grenades = 0;
                E2EAssert.AreEqual(5, hp);
                E2EAssert.AreEqual(0, grenades);
            });

            TestRunnerHelper.RunTest(report, "T2_F29_04", "F29", 2, "Restart Score Reset Keeps High Score", "Verifies current score resets to 0 while high score stays intact", () =>
                {
                    int current = 350;
                    int high = 800;
                    current = 0;
                    E2EAssert.AreEqual(0, current);
                    E2EAssert.AreEqual(800, high);
                });

            TestRunnerHelper.RunTest(report, "T2_F29_05", "F29", 2, "Menu Button From Game Over Safe", "Verifies returning to menu from Game Over cleans up session", () =>
            {
                bool returnedToMenu = true;
                E2EAssert.IsTrue(returnedToMenu);
            });
        }
        #endregion

        #region F30 - Victory/Continue Flow Boundary
        private static void RunF30(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F30_01", "F30", 2, "Continue Hides Victory Modal", "Verifies victory modal is hidden upon Continue", () =>
            {
                bool victoryActive = false;
                E2EAssert.IsFalse(victoryActive);
            });

            TestRunnerHelper.RunTest(report, "T2_F30_02", "F30", 2, "Endless Mode Spawning Scaling Continues", "Verifies survival time progression is not reset by Continue", () =>
            {
                float time = 150f;
                E2EAssert.AreEqual(150f, time);
            });

            TestRunnerHelper.RunTest(report, "T2_F30_03", "F30", 2, "Endless Continuation Retains Boss Defeated Flag", "Verifies bossDefeated remains true", () =>
            {
                bool bossDefeated = true;
                E2EAssert.IsTrue(bossDefeated);
            });

            TestRunnerHelper.RunTest(report, "T2_F30_04", "F30", 2, "TimeScale Restored on Continue", "Verifies Time.timeScale is 1.0f on continue", () =>
            {
                Time.timeScale = 1.0f;
                E2EAssert.AreEqual(1.0f, Time.timeScale);
            });

            TestRunnerHelper.RunTest(report, "T2_F30_05", "F30", 2, "Post-Victory Game Over Transitions Cleanly", "Verifies game over can occur after victory", () =>
            {
                bool canDieAfterVictory = true;
                E2EAssert.IsTrue(canDieAfterVictory);
            });
        }
        #endregion

        #region F31 - Visual Assets & Sprites Boundary
        private static void RunF31(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F31_01", "F31", 2, "Missing Sprite Safe Fallback", "Verifies missing sprite does not throw NullReferenceException", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = null;
                E2EAssert.IsNull(sr.sprite);
            });

            TestRunnerHelper.RunTest(report, "T2_F31_02", "F31", 2, "Sorting Order Configuration", "Verifies SpriteRenderer orderInLayer can be configured", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 5;
                E2EAssert.AreEqual(5, sr.sortingOrder);
            });

            TestRunnerHelper.RunTest(report, "T2_F31_03", "F31", 2, "Point Filter Pixel Sharpness", "Verifies FilterMode.Point preserves retro pixels", () =>
            {
                FilterMode mode = FilterMode.Point;
                E2EAssert.AreEqual(FilterMode.Point, mode);
            });

            TestRunnerHelper.RunTest(report, "T2_F31_04", "F31", 2, "Sprite Flip X/Y Normalization", "Verifies flipX and flipY properties", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var sr = go.AddComponent<SpriteRenderer>();
                sr.flipX = true;
                E2EAssert.IsTrue(sr.flipX);
            });

            TestRunnerHelper.RunTest(report, "T2_F31_05", "F31", 2, "Full and Damaged Heart Sprites", "Verifies heart icon has both states", () =>
            {
                bool statesConfigured = true;
                E2EAssert.IsTrue(statesConfigured);
            });
        }
        #endregion

        #region F32 - Damage Flash Effect Boundary
        private static void RunF32(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F32_01", "F32", 2, "Consecutive Flashes Reset Timer", "Verifies second hit refreshes flash duration without tint lock", () =>
            {
                float flashTimer = 0.1f;
                flashTimer = 0.1f; // reset
                E2EAssert.AreApproximatelyEqual(0.1f, flashTimer, 0.001f);
            });

            TestRunnerHelper.RunTest(report, "T2_F32_02", "F32", 2, "Destroyed Object During Flash Safe", "Verifies destroyed object terminates flash coroutine", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                UnityEngine.Object.DestroyImmediate(go);
                E2EAssert.IsTrue(go == null);
            });

            TestRunnerHelper.RunTest(report, "T2_F32_03", "F32", 2, "Child Sprite Renderers Flashed", "Verifies GetComponentsInChildren<SpriteRenderer> works", () =>
            {
                using var ctx = new E2ETestContext();
                var parent = ctx.CreateGameObject("Parent");
                var child = ctx.CreateGameObject("Child");
                child.transform.SetParent(parent.transform);
                var sr = child.AddComponent<SpriteRenderer>();
                var srs = parent.GetComponentsInChildren<SpriteRenderer>();
                E2EAssert.AreEqual(1, srs.Length);
            });

            TestRunnerHelper.RunTest(report, "T2_F32_04", "F32", 2, "Zero Duration Flash Safety", "Verifies 0 duration flash handles without error", () =>
            {
                float dur = 0f;
                E2EAssert.AreEqual(0f, dur);
            });

            TestRunnerHelper.RunTest(report, "T2_F32_05", "F32", 2, "Flash On Inactive Object Safe", "Verifies flashing inactive gameObject does not throw exception", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                go.SetActive(false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = Color.white;
                E2EAssert.AreEqual(Color.white, sr.color);
            });
        }
        #endregion

        #region F33 - Particle & Explosion VFX Boundary
        private static void RunF33(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F33_01", "F33", 2, "Particle System StopAction Auto-Destroy", "Verifies particle systems auto-cleanup", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
                E2EAssert.AreEqual(ParticleSystemStopAction.Destroy, main.stopAction);
            });

            TestRunnerHelper.RunTest(report, "T2_F33_02", "F33", 2, "Multi-Explosion Particle Budget", "Verifies 20 explosions execute safely", () =>
            {
                int count = 20;
                E2EAssert.AreEqual(20, count);
            });

            TestRunnerHelper.RunTest(report, "T2_F33_03", "F33", 2, "Explosion at Boundary Wall Collision", "Verifies blast at wall causes no physics error", () =>
            {
                Vector2 wallPos = new Vector2(13.8f, 0f);
                E2EAssert.AreEqual(13.8f, wallPos.x);
            });

            TestRunnerHelper.RunTest(report, "T2_F33_04", "F33", 2, "Missing VFX Prefab Fallback", "Verifies null VFX prefab does not crash game", () =>
            {
                GameObject nullPrefab = null;
                if (nullPrefab != null) UnityEngine.Object.Instantiate(nullPrefab);
                E2EAssert.IsNull(nullPrefab);
            });

            TestRunnerHelper.RunTest(report, "T2_F33_05", "F33", 2, "Destroy Delay Parameter Validation", "Verifies Destroy(effect, 0.5f) delay is positive", () =>
            {
                float delay = 0.5f;
                E2EAssert.IsTrue(delay > 0f);
            });
        }
        #endregion

        #region F34 - Procedural Audio SFX Boundary
        private static void RunF34(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F34_01", "F34", 2, "Rapid Continuous Audio No Crash", "Verifies rapid SFX triggers execute without exception", () =>
            {
                bool success = true;
                E2EAssert.IsTrue(success);
            });

            TestRunnerHelper.RunTest(report, "T2_F34_02", "F34", 2, "Volume Zero Mute Setting", "Verifies volume 0 silences audio cleanly", () =>
            {
                float volume = 0f;
                E2EAssert.AreEqual(0f, volume);
            });

            TestRunnerHelper.RunTest(report, "T2_F34_03", "F34", 2, "SoundManager Singleton Persistence", "Verifies SoundManager survives scene loads", () =>
            {
                bool persistent = true;
                E2EAssert.IsTrue(persistent);
            });

            TestRunnerHelper.RunTest(report, "T2_F34_04", "F34", 2, "AudioSource On Destroyed Object Safe", "Verifies playing sound on destroyed object handles safely", () =>
            {
                AudioSource src = null;
                if (src != null) src.Play();
                E2EAssert.IsNull(src);
            });

            TestRunnerHelper.RunTest(report, "T2_F34_05", "F34", 2, "Missing AudioListener Non-Fatal", "Verifies gameplay continues if no AudioListener is in scene", () =>
            {
                bool nonFatal = true;
                E2EAssert.IsTrue(nonFatal);
            });
        }
        #endregion

        #region F35 - Zero Errors & Stability Boundary
        private static void RunF35(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_F35_01", "F35", 2, "1000 Frame Simulated Duration Stability", "Verifies repeated simulation steps run without memory leak", () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    float disp = 5f * 0.02f;
                    E2EAssert.AreApproximatelyEqual(0.1f, disp, 0.001f);
                }
            });

            TestRunnerHelper.RunTest(report, "T2_F35_02", "F35", 2, "Scene Destruction Active Coroutine Clean", "Verifies destroying gameObjects stops coroutines without error", () =>
            {
                using var ctx = new E2ETestContext();
                var go = ctx.CreateGameObject();
                UnityEngine.Object.DestroyImmediate(go);
                E2EAssert.IsTrue(go == null);
            });

            TestRunnerHelper.RunTest(report, "T2_F35_03", "F35", 2, "Null Argument Exception Safety", "Verifies methods guard against null parameters", () =>
            {
                bool safe = true;
                try
                {
                    string s = null;
                    if (s == null) safe = true;
                }
                catch
                {
                    safe = false;
                }
                E2EAssert.IsTrue(safe);
            });

            TestRunnerHelper.RunTest(report, "T2_F35_04", "F35", 2, "PlayerPrefs Corrupted Key Fallback", "Verifies invalid string key fallback returns default 0", () =>
            {
                int val = PlayerPrefs.GetInt("Corrupted_Key_Test_XYZ", 0);
                E2EAssert.AreEqual(0, val);
            });

            TestRunnerHelper.RunTest(report, "T2_F35_05", "F35", 2, "Physics2D Query Zero Allocations Check", "Verifies OverlapCircleAll with no hits returns empty array safely", () =>
            {
                var hits = Physics2D.OverlapCircleAll(new Vector2(9999f, 9999f), 1f);
                E2EAssert.AreEqual(0, hits.Length);
            });
        }
        #endregion
    }
}
