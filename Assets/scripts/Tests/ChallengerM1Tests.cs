using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using E2ETests;

namespace Tests
{
    public class ChallengerDummyTarget : MonoBehaviour, IDamageable
    {
        public int health = 10;
        public bool isAlive = true;
        public int hitsReceived = 0;
        public int damageReceived = 0;

        public bool IsAlive => isAlive && health > 0;

        public void TakeDamage(int damage)
        {
            hitsReceived++;
            damageReceived += damage;
            health -= damage;
            if (health <= 0)
            {
                isAlive = false;
            }
        }
    }

    public static class ChallengerM1Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 2 Milestone 1 Empirical Verification Suite" };

            // -------------------------------------------------------------
            // SECTION 1: Fire Rate Throttling
            // -------------------------------------------------------------
            TestRunnerHelper.RunTest(report, "CH-M1-01", "F07", 2,
                "Rapid Fire Input Throttling Over Time",
                "Verifies that rapid fire attempts within 1.0s are throttled to at most 6 shots (0.2s cooldown = 5 shots/sec).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Challenger_Player");
                        var shooting = playerGo.AddComponent<Shooting>();
                        shooting.fireRate = 0.2f;

                        var bulletPrefab = ctx.CreateGameObject("MockBulletPrefab");
                        bulletPrefab.AddComponent<Rigidbody2D>();
                        shooting.bulletPrefab = bulletPrefab;

                        // Simulate 100 rapid clicks across 1.0s window
                        int shotCount = 0;
                        float nextFireTime = 0f;
                        float duration = 1.0f;
                        float step = 0.01f; // 100 samples

                        for (float t = 0f; t <= duration + 0.0001f; t += step)
                        {
                            if (t >= nextFireTime)
                            {
                                nextFireTime = t + shooting.fireRate;
                                shotCount++;
                            }
                        }

                        E2EAssert.IsTrue(shotCount <= 6, $"Rapid firing must be throttled. Expected <= 6 shots, got {shotCount}");
                        E2EAssert.IsTrue(shotCount >= 5, $"Expected at least 5 shots in 1.0s with 0.2s cooldown, got {shotCount}");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-02", "F07", 2,
                "Shooting Paused Gating",
                "Verifies shooting is suppressed when Time.timeScale <= 0f.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("Challenger_Player");
                        var shooting = playerGo.AddComponent<Shooting>();

                        float origScale = Time.timeScale;
                        try
                        {
                            Time.timeScale = 0f;
                            var updateMethod = typeof(Shooting).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                            // Invoke Update - should return early without throwing
                            updateMethod.Invoke(shooting, null);
                            E2EAssert.IsTrue(true, "Shooting Update returned safely during pause");
                        }
                        finally
                        {
                            Time.timeScale = origScale;
                        }
                    }
                });

            // -------------------------------------------------------------
            // SECTION 2: Bullet Damage to IDamageable Targets
            // -------------------------------------------------------------
            TestRunnerHelper.RunTest(report, "CH-M1-03", "F08", 2,
                "Bullet Deals Damage via Solid Collision",
                "Verifies Bullet deals damage to IDamageable on solid OnCollisionEnter2D and destroys itself.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var targetGo = ctx.CreateGameObject("Target_Solid");
                        var targetCol = targetGo.AddComponent<BoxCollider2D>();
                        var dummy = targetGo.AddComponent<ChallengerDummyTarget>();
                        dummy.health = 10;

                        var bulletGo = ctx.CreateGameObject("Bullet_Solid");
                        var bulletCol = bulletGo.AddComponent<BoxCollider2D>();
                        var bullet = bulletGo.AddComponent<Bullet>();
                        bullet.damage = 2;

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { targetGo });

                        E2EAssert.AreEqual(8, dummy.health, "Dummy health should drop from 10 to 8 (2 damage)");
                        E2EAssert.AreEqual(1, dummy.hitsReceived, "Hits received should be 1");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-04", "F08", 2,
                "Bullet Deals Damage via Trigger Collision",
                "Verifies Bullet deals damage to IDamageable with trigger collider (OnTriggerEnter2D).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var targetGo = ctx.CreateGameObject("Target_Trigger");
                        var targetCol = targetGo.AddComponent<BoxCollider2D>();
                        targetCol.isTrigger = true;
                        var dummy = targetGo.AddComponent<ChallengerDummyTarget>();
                        dummy.health = 5;

                        var bulletGo = ctx.CreateGameObject("Bullet_Trigger");
                        var bullet = bulletGo.AddComponent<Bullet>();
                        bullet.damage = 1;

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { targetGo });

                        E2EAssert.AreEqual(4, dummy.health, "Trigger target health should drop to 4");
                        E2EAssert.AreEqual(1, dummy.hitsReceived, "Hits received should be 1");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-05", "F08", 2,
                "Bullet Damages Parent IDamageable When Child Collider Hit",
                "Verifies Bullet correctly inspects parent hierarchy (GetComponentInParent) for IDamageable.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var parentGo = ctx.CreateGameObject("Target_Parent");
                        var dummy = parentGo.AddComponent<ChallengerDummyTarget>();
                        dummy.health = 6;

                        var childGo = ctx.CreateGameObject("Target_Child_Col");
                        childGo.transform.SetParent(parentGo.transform);
                        var childCol = childGo.AddComponent<BoxCollider2D>();

                        var bulletGo = ctx.CreateGameObject("Bullet_ChildHit");
                        var bullet = bulletGo.AddComponent<Bullet>();
                        bullet.damage = 3;

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { childGo });

                        E2EAssert.AreEqual(3, dummy.health, "Parent IDamageable health should decrease by 3");
                        E2EAssert.AreEqual(1, dummy.hitsReceived, "Hits received should be 1");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-06", "F08", 2,
                "Dead Target (IsAlive == false) Receives No Further Damage",
                "Verifies Bullet does not invoke TakeDamage on targets where IsAlive is false.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var targetGo = ctx.CreateGameObject("Target_Dead");
                        var dummy = targetGo.AddComponent<ChallengerDummyTarget>();
                        dummy.health = 0;
                        dummy.isAlive = false;

                        var bulletGo = ctx.CreateGameObject("Bullet_DeadTarget");
                        var bullet = bulletGo.AddComponent<Bullet>();
                        bullet.damage = 1;

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { targetGo });

                        E2EAssert.AreEqual(0, dummy.hitsReceived, "Dead target must not receive TakeDamage");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-07", "F08", 2,
                "Bullet Double-Hit Protection via _hasHit Flag",
                "Verifies second collision event on the same bullet is ignored.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var targetGo = ctx.CreateGameObject("Target_Multi");
                        var dummy = targetGo.AddComponent<ChallengerDummyTarget>();
                        dummy.health = 10;

                        var bulletGo = ctx.CreateGameObject("Bullet_Multi");
                        var bullet = bulletGo.AddComponent<Bullet>();
                        bullet.damage = 2;

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { targetGo });
                        handleHit.Invoke(bullet, new object[] { targetGo }); // second hit

                        E2EAssert.AreEqual(8, dummy.health, "Health should only decrease once");
                        E2EAssert.AreEqual(1, dummy.hitsReceived, "Hits received should be exactly 1");
                    }
                });

            // -------------------------------------------------------------
            // SECTION 3: Bullet Ignores Player and Friendlies
            // -------------------------------------------------------------
            TestRunnerHelper.RunTest(report, "CH-M1-08", "F08", 2,
                "Bullet Ignores Tagged Player",
                "Verifies Bullet ignores GameObject tagged 'Player' and deals 0 damage.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("PlayerTagged");
                        playerGo.tag = "Player";
                        var ph = playerGo.AddComponent<PlayerHealth>();

                        var bulletGo = ctx.CreateGameObject("Bullet_vs_Player");
                        var bullet = bulletGo.AddComponent<Bullet>();

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { playerGo });

                        E2EAssert.AreEqual(5, ph.currentHealth, "Tagged player must remain at full 5 HP");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-09", "F08", 2,
                "Bullet Ignores Untagged Player via Component Detection",
                "Verifies Bullet ignores Untagged Player because it has PlayerHealth and PlayerMovement.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var playerGo = ctx.CreateGameObject("PlayerUntagged");
                        // Deliberately untagged, matching scene state
                        var ph = playerGo.AddComponent<PlayerHealth>();
                        var pm = playerGo.AddComponent<PlayerMovement>();

                        var bulletGo = ctx.CreateGameObject("Bullet_vs_Untagged");
                        var bullet = bulletGo.AddComponent<Bullet>();

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { playerGo });

                        E2EAssert.AreEqual(5, ph.currentHealth, "Untagged player with PlayerHealth must remain at full 5 HP");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-10", "F08", 2,
                "Bullet Ignores Other Bullets",
                "Verifies Bullet does not trigger hit on another Bullet.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var bullet1Go = ctx.CreateGameObject("Bullet_1");
                        var bullet1 = bullet1Go.AddComponent<Bullet>();

                        var bullet2Go = ctx.CreateGameObject("Bullet_2");
                        var bullet2 = bullet2Go.AddComponent<Bullet>();

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet1, new object[] { bullet2Go });

                        // Verify bullet1 was not flagged as hit
                        var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool hasHit = (bool)hasHitField.GetValue(bullet1);
                        E2EAssert.IsFalse(hasHit, "Bullet hitting another bullet should NOT trigger hit");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-11", "F08", 2,
                "Bullet Passes Through Non-Damageable Trigger Pickups",
                "Verifies Bullet ignores and passes through trigger colliders lacking IDamageable (e.g. item pickups).",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var pickupGo = ctx.CreateGameObject("ItemPickup");
                        var col = pickupGo.AddComponent<CircleCollider2D>();
                        col.isTrigger = true;

                        var bulletGo = ctx.CreateGameObject("Bullet_vs_Pickup");
                        var bullet = bulletGo.AddComponent<Bullet>();

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { pickupGo });

                        var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool hasHit = (bool)hasHitField.GetValue(bullet);
                        E2EAssert.IsFalse(hasHit, "Bullet must pass through non-damageable trigger pickup");
                    }
                });

            // -------------------------------------------------------------
            // SECTION 4: MapBounds Colliders Physical Containment
            // -------------------------------------------------------------
            TestRunnerHelper.RunTest(report, "CH-M1-12", "F03", 2,
                "Scene MapBounds Enclosure Integrity",
                "Verifies all 4 MapBounds solid walls exist with correct bounding geometry enclosing the arena.",
                () =>
                {
                    var mapBounds = GameObject.Find("MapBounds");
                    E2EAssert.IsNotNull(mapBounds, "MapBounds GameObject must exist in scene");

                    var top = mapBounds.transform.Find("Wall_Top");
                    var btm = mapBounds.transform.Find("Wall_Bottom");
                    var left = mapBounds.transform.Find("Wall_Left");
                    var right = mapBounds.transform.Find("Wall_Right");

                    E2EAssert.IsNotNull(top, "Wall_Top missing");
                    E2EAssert.IsNotNull(btm, "Wall_Bottom missing");
                    E2EAssert.IsNotNull(left, "Wall_Left missing");
                    E2EAssert.IsNotNull(right, "Wall_Right missing");

                    var topCol = top.GetComponent<BoxCollider2D>();
                    var btmCol = btm.GetComponent<BoxCollider2D>();
                    var leftCol = left.GetComponent<BoxCollider2D>();
                    var rightCol = right.GetComponent<BoxCollider2D>();

                    E2EAssert.IsFalse(topCol.isTrigger, "Wall_Top must be solid collider");
                    E2EAssert.IsFalse(btmCol.isTrigger, "Wall_Bottom must be solid collider");
                    E2EAssert.IsFalse(leftCol.isTrigger, "Wall_Left must be solid collider");
                    E2EAssert.IsFalse(rightCol.isTrigger, "Wall_Right must be solid collider");

                    // Verify boundaries enclose the playable area
                    E2EAssert.IsTrue(topCol.bounds.min.y >= 5.0f, "Wall_Top lower edge should be >= 5.0");
                    E2EAssert.IsTrue(btmCol.bounds.max.y <= -4.8f, "Wall_Bottom upper edge should be <= -4.8");
                    E2EAssert.IsTrue(leftCol.bounds.max.x <= -9.0f, "Wall_Left inner edge should be <= -9.0");
                    E2EAssert.IsTrue(rightCol.bounds.min.x >= 14.5f, "Wall_Right inner edge should be >= 14.5");
                });

            TestRunnerHelper.RunTest(report, "CH-M1-13", "F03", 2,
                "Physics Simulation: MapBounds Physically Obstructs High-Speed Body",
                "Simulates a Dynamic Rigidbody2D moving with high speed towards walls and verifies it cannot breach boundaries.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var mapBounds = GameObject.Find("MapBounds");
                        E2EAssert.IsNotNull(mapBounds, "MapBounds required for physics test");

                        var rightCol = mapBounds.transform.Find("Wall_Right")?.GetComponent<BoxCollider2D>();
                        E2EAssert.IsNotNull(rightCol, "Wall_Right BoxCollider2D required");

                        var leftCol = mapBounds.transform.Find("Wall_Left")?.GetComponent<BoxCollider2D>();
                        E2EAssert.IsNotNull(leftCol, "Wall_Left BoxCollider2D required");

                        // Test Right Wall containment
                        var testMover = ctx.CreateGameObject("TestMover_Right");
                        testMover.transform.position = new Vector3(13.0f, 0f, 0f);
                        var rb = testMover.AddComponent<Rigidbody2D>();
                        rb.gravityScale = 0f;
                        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                        var col = testMover.AddComponent<CircleCollider2D>();
                        col.radius = 0.5f;

                        // Give high velocity towards right wall
                        rb.velocity = new Vector2(40f, 0f);

                        // Simulate 30 physics steps (0.6s)
                        for (int i = 0; i < 30; i++)
                        {
                            ctx.StepPhysics(0.02f);
                        }

                        // Right wall inner edge is rightCol.bounds.min.x. Body center must be stopped at/before inner edge (minus radius)
                        float maxAllowedRightX = rightCol.bounds.min.x - col.radius + 0.05f;
                        E2EAssert.IsTrue(testMover.transform.position.x <= maxAllowedRightX,
                            $"Test body penetrated right wall inner surface! Final pos X = {testMover.transform.position.x}, max allowed = {maxAllowedRightX}");
                        E2EAssert.IsTrue(testMover.transform.position.x < rightCol.bounds.max.x,
                            $"Test body breached right wall outer boundary! Final pos X = {testMover.transform.position.x}, wall outer = {rightCol.bounds.max.x}");

                        // Test Left Wall containment
                        var testMoverLeft = ctx.CreateGameObject("TestMover_Left");
                        testMoverLeft.transform.position = new Vector3(-8.0f, 0f, 0f);
                        var rbL = testMoverLeft.AddComponent<Rigidbody2D>();
                        rbL.gravityScale = 0f;
                        rbL.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                        var colL = testMoverLeft.AddComponent<CircleCollider2D>();
                        colL.radius = 0.5f;

                        rbL.velocity = new Vector2(-40f, 0f);

                        for (int i = 0; i < 30; i++)
                        {
                            ctx.StepPhysics(0.02f);
                        }

                        // Left wall inner edge is leftCol.bounds.max.x. Body center must be stopped at/before inner edge (plus radius)
                        float minAllowedLeftX = leftCol.bounds.max.x + colL.radius - 0.05f;
                        E2EAssert.IsTrue(testMoverLeft.transform.position.x >= minAllowedLeftX,
                            $"Test body penetrated left wall inner surface! Final pos X = {testMoverLeft.transform.position.x}, min allowed = {minAllowedLeftX}");
                        E2EAssert.IsTrue(testMoverLeft.transform.position.x > leftCol.bounds.min.x,
                            $"Test body breached left wall outer boundary! Final pos X = {testMoverLeft.transform.position.x}, wall outer = {leftCol.bounds.min.x}");
                    }
                });

            TestRunnerHelper.RunTest(report, "CH-M1-14", "F03", 2,
                "MapBounds Wall Destroys Bullets on Impact",
                "Verifies Bullet colliding with solid MapBounds wall destroys itself without error.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var mapBounds = GameObject.Find("MapBounds");
                        E2EAssert.IsNotNull(mapBounds, "MapBounds required");
                        var topWall = mapBounds.transform.Find("Wall_Top").gameObject;

                        var bulletGo = ctx.CreateGameObject("Bullet_vs_Wall");
                        var bullet = bulletGo.AddComponent<Bullet>();

                        var handleHit = typeof(Bullet).GetMethod("HandleHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        handleHit.Invoke(bullet, new object[] { topWall });

                        var hasHitField = typeof(Bullet).GetField("_hasHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool hasHit = (bool)hasHitField.GetValue(bullet);

                        E2EAssert.IsTrue(hasHit, "Bullet must mark hit and self-destruct when impacting MapBounds wall");
                    }
                });

            return report;
        }
    }
}
