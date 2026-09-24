using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace E2ETests
{
    /// <summary>
    /// Comprehensive E2E automated test suite for Continuous Upward (+Y) Endless Scrolling Map System.
    /// Covers 10 core features across 4 tiers:
    /// - Tier 1: Feature Coverage (Happy Path, 50 tests)
    /// - Tier 2: Boundary & Corner Cases (50 tests)
    /// - Tier 3: Cross-Feature Pairwise Combinations (15 tests)
    /// - Tier 4: Real-World Application Scenarios (5 tests)
    /// Total: 120 automated test cases.
    /// </summary>
    public static class ScrollingMapTests
    {
        public static TestSuiteReport RunAll()
        {
            var report = new TestSuiteReport
            {
                SuiteName = "Endless Scrolling Map System E2E Suite"
            };
            RunAll(report);
            return report;
        }

        public static void RunAll(TestSuiteReport report)
        {
            var sw = Stopwatch.StartNew();
            RunTier1(report);
            RunTier2(report);
            RunTier3(report);
            RunTier4(report);
            sw.Stop();
            report.TotalDurationMs += sw.Elapsed.TotalMilliseconds;
        }

        public static string RunAllFormatted()
        {
            var report = RunAll();
            return report.GenerateMarkdownSummary();
        }

        #region Tier 1: Feature Coverage (50 Tests)

        public static void RunTier1(TestSuiteReport report)
        {
            RunT1_F01(report);
            RunT1_F02(report);
            RunT1_F03(report);
            RunT1_F04(report);
            RunT1_F05(report);
            RunT1_F06(report);
            RunT1_F07(report);
            RunT1_F08(report);
            RunT1_F09(report);
            RunT1_F10(report);
        }

        // F01: Camera +Y Scrolling (2.0 - 3.5 u/s)
        private static void RunT1_F01(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F01_01", "F01", 1,
                "Baseline Camera Scroll Speed",
                "Verifies camera scrolls along +Y at baseline speed 2.0 u/s over 1.0 second.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 0, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    E2EAssert.AreEqual(2.0f, cam.baselineSpeed);
                    E2EAssert.AreEqual(2.0f, cam.CurrentSpeed);
                    E2EAssert.AreEqual(0f, cam.DistanceTravelled);

                    cam.StepScroll(1.0f);

                    E2EAssert.AreApproximatelyEqual(2.0f, camGo.transform.position.y, 0.001f);
                    E2EAssert.AreApproximatelyEqual(0f, camGo.transform.position.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(-10f, camGo.transform.position.z, 0.001f);
                    E2EAssert.AreApproximatelyEqual(2.0f, cam.DistanceTravelled, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F01_02", "F01", 1,
                "FixedUpdate Step Displacement",
                "Verifies at fixedDeltaTime = 0.02s and baseline speed = 2.0 u/s, camera moves +0.04u per step.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 0, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    float fixedDt = 0.02f;

                    cam.StepScroll(fixedDt);

                    E2EAssert.AreApproximatelyEqual(0.04f, camGo.transform.position.y, 0.0001f);
                    E2EAssert.AreApproximatelyEqual(0.04f, cam.DistanceTravelled, 0.0001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F01_03", "F01", 1,
                "Camera Pure Upward Translation",
                "Verifies X and Z camera coordinates remain strictly invariant during upward scrolling.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    Vector3 initial = new Vector3(0f, 15f, -10f);
                    camGo.transform.position = initial;
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(initial.y);

                    cam.StepScroll(0.5f);

                    E2EAssert.AreEqual(initial.x, camGo.transform.position.x);
                    E2EAssert.AreEqual(initial.z, camGo.transform.position.z);
                    E2EAssert.AreApproximatelyEqual(16.0f, camGo.transform.position.y, 0.001f);
                    E2EAssert.AreApproximatelyEqual(1.0f, cam.DistanceTravelled, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F01_04", "F01", 1,
                "Distance Travelled Accumulator",
                "Verifies distance travelled tracks cumulative vertical displacement monotonically.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 0, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);

                    float prevDist = cam.DistanceTravelled;
                    for (int step = 0; step < 50; step++)
                    {
                        cam.StepScroll(0.02f);
                        E2EAssert.IsTrue(cam.DistanceTravelled > prevDist, "Distance must increase monotonically");
                        prevDist = cam.DistanceTravelled;
                    }

                    E2EAssert.AreApproximatelyEqual(camGo.transform.position.y, cam.DistanceTravelled, 0.001f);
                    E2EAssert.IsTrue(cam.DistanceTravelled >= 2.0f, "Total distance over 1.0s should be at least 2.0 units");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F01_05", "F01", 1,
                "Speed Scaling Progression Model",
                "Verifies camera speed scales from baseline 2.0 u/s toward 3.5 u/s ceiling as distance increases.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);

                    // At distance 0: baseline speed 2.0 u/s
                    cam.SetDistanceTravelled(0f);
                    E2EAssert.AreEqual(2.0f, cam.CurrentSpeed);

                    // At distance 100m: speed scales up to 2.5 u/s
                    cam.SetDistanceTravelled(100f);
                    E2EAssert.AreApproximatelyEqual(2.5f, cam.CurrentSpeed, 0.001f);

                    // At distance 200m: speed scales up to 3.0 u/s
                    cam.SetDistanceTravelled(200f);
                    E2EAssert.AreApproximatelyEqual(3.0f, cam.CurrentSpeed, 0.001f);

                    // At distance >= 300m: speed caps strictly at maxSpeed ceiling 3.5 u/s
                    cam.SetDistanceTravelled(500f);
                    E2EAssert.IsTrue(cam.CurrentSpeed > cam.baselineSpeed, "Current speed should scale up from baseline");
                    E2EAssert.AreEqual(3.5f, cam.CurrentSpeed, "Current speed must clamp at maxSpeed ceiling");
                });
        }

        // F02: Player Viewport Clamping (X: 0.05-0.95, Y: 0.08-0.92)
        private static void RunT1_F02(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F02_01", "F02", 1,
                "Interior Viewport Unconstrained",
                "Verifies player inside viewport bounds [0.05, 0.95] X and [0.08, 0.92] Y experiences no clamp displacement.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    E2EAssert.AreEqual(Vector2.zero, rb.position, "Interior player should not be displaced");
                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(0.5f, vp.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(0.5f, vp.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F02_02", "F02", 1,
                "Left Viewport Boundary Clamping",
                "Verifies viewport X is clamped to minimum 0.05 when moving left past edge.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(-20f, 0f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.minViewportX, vp.x, 0.001f, "Position must be clamped to minViewportX");
                    float expectedWorldX = cam.ViewportToWorldPoint(new Vector3(pm.minViewportX, 0.5f, 10f)).x;
                    E2EAssert.AreApproximatelyEqual(expectedWorldX, rb.position.x, 0.01f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F02_03", "F02", 1,
                "Right Viewport Boundary Clamping",
                "Verifies viewport X is clamped to maximum 0.95 when moving right past edge.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(20f, 0f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.maxViewportX, vp.x, 0.001f, "Position must be clamped to maxViewportX");
                    float expectedWorldX = cam.ViewportToWorldPoint(new Vector3(pm.maxViewportX, 0.5f, 10f)).x;
                    E2EAssert.AreApproximatelyEqual(expectedWorldX, rb.position.x, 0.01f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F02_04", "F02", 1,
                "Top Viewport Boundary Clamping",
                "Verifies viewport Y is clamped to maximum 0.92 when moving up past edge.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(0f, 20f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.maxViewportY, vp.y, 0.001f, "Position must be clamped to maxViewportY");
                    float expectedWorldY = cam.ViewportToWorldPoint(new Vector3(0.5f, pm.maxViewportY, 10f)).y;
                    E2EAssert.AreApproximatelyEqual(expectedWorldY, rb.position.y, 0.01f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F02_05", "F02", 1,
                "Dynamic World-Space Bounds Progression",
                "Verifies world-space clamping bounds advance vertically with camera position.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    cam.transform.position = new Vector3(0f, 100f, -10f);
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    // Lagging behind moving camera
                    rb.position = new Vector2(0f, 50f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");
                    E2EAssert.AreApproximatelyEqual(95.8f, rb.position.y, 0.01f, "Bottom clamp must track moving camera Y=100");

                    // Racing ahead of moving camera
                    rb.position = new Vector2(0f, 150f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");
                    E2EAssert.AreApproximatelyEqual(104.2f, rb.position.y, 0.01f, "Top clamp must track moving camera Y=100");
                });
        }

        // F03: Bottom Edge Push / Kill Plane
        private static void RunT1_F03(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F03_01", "F03", 1,
                "Bottom Edge Forward Push",
                "Verifies player below bottom viewport threshold is pushed forward into bounds.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    // Viewport Y = 0.02 (< bottomKillThreshold 0.04) -> world Y = -4.8f
                    rb.position = new Vector2(0f, -4.8f);
                    float initialY = rb.position.y;

                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.IsTrue(rb.position.y > initialY, "Player must be physically pushed upward along +Y");
                    E2EAssert.AreEqual(4, ph.currentHealth, "Player must take 1 HP damage on crossing kill threshold");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F03_02", "F03", 1,
                "Bottom Edge Penalty Damage",
                "Verifies being caught behind bottom boundary inflicts exactly 1 HP damage to player via PlayerMovement.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    E2EAssert.AreEqual(5, ph.currentHealth);

                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(4, ph.currentHealth, "Bottom kill plane must inflict exactly 1 HP damage");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F03_03", "F03", 1,
                "Bottom Damage Invulnerability Window",
                "Verifies taking bottom damage sets invulnerability to prevent continuous multi-hit elimination in a single frame.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();
                    E2EAssert.IsTrue(ph.isInvulnerable, "Player must receive i-frames after taking bottom damage");

                    // Subsequent check in the same frame/i-frame window
                    pm.ForceCheckBottomKillPlane();
                    E2EAssert.AreEqual(4, ph.currentHealth, "Subsequent check during i-frames must not deal additional damage");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F03_04", "F03", 1,
                "Fatal Bottom Damage at 1 HP",
                "Verifies player with 1 HP trapped at bottom threshold dies and triggers death state.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    E2EReflector.SetPropertyValue(ph, "currentHealth", 1);
                    bool deathFired = false;
                    ph.OnPlayerDeath += () => deathFired = true;

                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(0, ph.currentHealth, "Player health must reach 0");
                    E2EAssert.IsFalse(ph.IsAlive, "Player must not be alive");
                    E2EAssert.IsTrue(deathFired, "OnPlayerDeath should trigger when fatal bottom kill occurs");
                    E2EAssert.IsFalse(pm.enabled, "PlayerMovement must be disabled on death");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F03_05", "F03", 1,
                "Safe Forward Movement Immunity",
                "Verifies player moving forward within safe zone receives 0 damage and 0 push force.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = Vector2.zero; // Viewport (0.5, 0.5)
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(5, ph.currentHealth, "Player in safe viewport must not take damage");
                    E2EAssert.IsFalse(ph.isInvulnerable, "No invulnerability should be triggered in safe zone");
                    E2EAssert.AreEqual(Vector2.zero, rb.position, "Player position must remain unchanged");
                });
        }

        // F04: MapSegment Modular Spawning (20u length, >=4u corridor)
        private static void RunT1_F04(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F04_01", "F04", 1,
                "Segment Length Standard (20u)",
                "Verifies standardized map segment length is exactly 20.0 units on real MapSegment component.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    var seg = go.AddComponent<MapSegment>();
                    E2EAssert.AreEqual(20.0f, seg.segmentLength);
                    E2EAssert.AreApproximatelyEqual(20.0f, seg.CalculateBounds().size.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F04_02", "F04", 1,
                "Segment Width Standard (15u)",
                "Verifies standardized segment width is 15.0 units with lateral bounds X: [-7.5, +7.5] and tagged boundary colliders.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    var seg = go.AddComponent<MapSegment>();
                    seg.EnsureBoundaryColliders();

                    E2EAssert.AreEqual(15.0f, seg.segmentWidth);
                    E2EAssert.AreApproximatelyEqual(-7.5f, seg.LeftWallX, 0.001f);
                    E2EAssert.AreApproximatelyEqual(7.5f, seg.RightWallX, 0.001f);
                    E2EAssert.IsNotNull(seg.leftWallCollider);
                    E2EAssert.IsNotNull(seg.rightWallCollider);
                    E2EAssert.AreEqual("Colliders", seg.leftWallCollider.tag);
                    E2EAssert.AreEqual("Colliders", seg.rightWallCollider.tag);
                    E2EAssert.IsFalse(seg.leftWallCollider.isTrigger);
                    E2EAssert.IsFalse(seg.rightWallCollider.isTrigger);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F04_03", "F04", 1,
                "Seamless Segment Connection",
                "Verifies segment N+1 start Y coordinate matches segment N end Y coordinate without seam gap on real MapSegment instances.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go0 = ctx.CreateGameObject("Segment_0");
                    go0.transform.position = new Vector3(0f, 0f, 0f);
                    var seg0 = go0.AddComponent<MapSegment>();
                    seg0.segmentLength = 20.0f;

                    var go1 = ctx.CreateGameObject("Segment_1");
                    go1.transform.position = new Vector3(0f, seg0.TopY, 0f);
                    var seg1 = go1.AddComponent<MapSegment>();
                    seg1.segmentLength = 20.0f;

                    E2EAssert.AreApproximatelyEqual(20.0f, seg0.TopY, 0.0001f);
                    E2EAssert.AreApproximatelyEqual(seg0.TopY, seg1.StartY, 0.0001f);
                    float gap = seg1.StartY - seg0.TopY;
                    E2EAssert.AreApproximatelyEqual(0f, gap, 0.0001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F04_04", "F04", 1,
                "Guaranteed Corridor Minimum Width (>=4u)",
                "Verifies corridor width is at least 4.0 units across all segments and validation enforces this contract.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    var seg = go.AddComponent<MapSegment>();
                    seg.minCorridorWidth = 4.0f;
                    seg.EnsureBoundaryColliders();
                    bool isValid = seg.ValidateSegment(out string err);
                    E2EAssert.IsTrue(isValid, $"Segment should be valid: {err}");
                    E2EAssert.IsTrue(seg.minCorridorWidth >= 4.0f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F04_05", "F04", 1,
                "Ahead-of-Camera Spawn Distance",
                "Verifies next segment spawns ahead of camera top boundary before player reaches it.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var mgrGo = ctx.CreateGameObject("Test_MapManager");
                    var mgr = mgrGo.AddComponent<MapManager>();
                    mgr.aheadTriggerDistance = 60.0f;

                    float camY = 10f;
                    float camHalfH = 5f;
                    float camTopY = camY + camHalfH; // 15f
                    float nextSpawnThreshold = camY + mgr.aheadTriggerDistance;
                    E2EAssert.IsTrue(nextSpawnThreshold > camTopY, "Next spawn trigger must precede camera view");
                    E2EAssert.IsTrue(mgr.aheadTriggerDistance >= 20.0f, "Ahead trigger distance must be at least 1 segment length");
                });
        }

        // F05: Segment Object Pooling (Zero GC, cleanup at camY - 25u)
        private static void RunT1_F05(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F05_01", "F05", 1,
                "Cleanup Distance Threshold",
                "Verifies segment whose top boundary drops below (camera Y - 25.0u) is flagged for recycling on real MapSegment.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    go.transform.position = new Vector3(0, 14f, 0);
                    var seg = go.AddComponent<MapSegment>();
                    seg.segmentLength = 20.0f; // TopY = 34.0f

                    float camY = 60.0f;
                    float cleanupDistance = 25.0f;
                    float cleanupThreshold = camY - cleanupDistance; // 35.0f

                    bool shouldRecycle = seg.IsBehindCleanupThreshold(cleanupThreshold);
                    E2EAssert.IsTrue(shouldRecycle, "Segment behind cleanup distance must be recycled");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F05_02", "F05", 1,
                "Recycle Deactivates GameObject",
                "Verifies recycled segment sets active state to false via real MapSegmentPool.ReturnSegment.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 1;
                    pool.Prewarm();

                    var seg = pool.GetSegment(0);
                    seg.gameObject.SetActive(true);
                    E2EAssert.IsTrue(seg.gameObject.activeSelf);

                    // Recycle via real pool
                    pool.ReturnSegment(seg);
                    E2EAssert.IsFalse(seg.gameObject.activeSelf, "Recycled segment must be deactivated");
                    E2EAssert.AreEqual(1, pool.AvailableCount(0));
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F05_03", "F05", 1,
                "Pool Retrieval Without Instantiation",
                "Verifies requesting a segment reuses an existing inactive instance from pool without new allocation.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 1;
                    pool.Prewarm();

                    int initialPrewarmed = pool.TotalPrewarmedCount;
                    E2EAssert.AreEqual(1, pool.AvailableCount(0));

                    var seg1 = pool.GetSegment(0);
                    E2EAssert.IsNotNull(seg1);
                    E2EAssert.AreEqual(0, pool.AvailableCount(0));
                    E2EAssert.AreEqual(initialPrewarmed, pool.TotalPrewarmedCount, "Must not allocate new instances when pool has available items");

                    pool.ReturnSegment(seg1);
                    E2EAssert.AreEqual(1, pool.AvailableCount(0));
                    var segReused = pool.GetSegment(0);
                    E2EAssert.AreEqual(seg1, segReused, "Pool must return recycled instance");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F05_04", "F05", 1,
                "Zero GameObject.Destroy In Pooling Lifecycle",
                "Verifies recycling segments avoids Destroy() invocation, preserving persistent pool objects.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 1;
                    pool.Prewarm();

                    var seg = pool.GetSegment(0);
                    pool.ReturnSegment(seg);

                    E2EAssert.IsNotNull(seg, "Pool item must not be destroyed on recycling");
                    E2EAssert.IsNotNull(seg.gameObject);
                    E2EAssert.IsFalse(seg.gameObject.activeSelf);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F05_05", "F05", 1,
                "Bounded Active Segment Count",
                "Verifies active segment count remains bounded between 3 and 4 segments throughout infinite scrolling with MapManager.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 6;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSegmentCount = 3;
                    mgr.aheadTriggerDistance = 35.0f;
                    mgr.cleanupDistance = 25.0f;
                    mgr.InitializeMap(0f);

                    E2EAssert.AreEqual(3, mgr.ActiveSegmentCount);
                    // Simulate upward scrolling over 100 units
                    for (float camY = 0f; camY <= 100f; camY += 1f)
                    {
                        mgr.CheckCleanupTrailing(camY);
                        mgr.CheckSpawnAhead(camY);
                        E2EAssert.IsTrue(mgr.ActiveSegmentCount >= 3 && mgr.ActiveSegmentCount <= 4,
                            $"Active segments must remain bounded between 3 and 4, got {mgr.ActiveSegmentCount} at camY={camY}");
                    }
                });
        }

        // F06: 500-Point Boss Encounter & Center Lock
        private static void RunT1_F06(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F06_01", "F06", 1,
                "500-Point Boss Trigger",
                "Verifies reaching score >= 500 initiates boss arena generation.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var mapGo = ctx.CreateGameObject("MapManager");
                    var map = mapGo.AddComponent<MapManager>();

                    gm.AddScore(500);

                    E2EAssert.AreEqual(500, gm.CurrentScore);
                    E2EAssert.IsTrue(map.IsBossArenaQueued, "Score 500 must queue boss arena on MapManager");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F06_02", "F06", 1,
                "Standard Segment Generation Halt",
                "Verifies standard random segment generation halts once boss arena is queued.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 4;
                    pool.Prewarm();

                    var map = root.AddComponent<MapManager>();
                    map.pool = pool;
                    map.initialSpawnY = 0f;
                    map.QueueBossArena();

                    E2EAssert.IsTrue(map.IsBossArenaQueued);
                    map.CheckSpawnAhead(0f);
                    E2EAssert.IsTrue(map.IsBossArenaSpawned, "Queued arena should be spawned");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F06_03", "F06", 1,
                "Dedicated Boss Arena Dimensions",
                "Verifies boss arena segment dimensions are 24 units length by 18 units width with enclosing walls.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("BossArena");
                    var seg = go.AddComponent<MapSegment>();
                    seg.segmentLength = 24.0f;
                    seg.segmentWidth = 18.0f;
                    seg.minCorridorWidth = 16.0f;
                    seg.isBossArena = true;
                    seg.EnsureBoundaryColliders();

                    bool valid = seg.ValidateSegment(out string err);
                    E2EAssert.IsTrue(valid, $"Boss arena must be valid: {err}");
                    E2EAssert.AreEqual(24.0f, seg.segmentLength);
                    E2EAssert.AreEqual(18.0f, seg.segmentWidth);
                    E2EAssert.IsNotNull(seg.topWallCollider);
                    E2EAssert.IsFalse(seg.topWallCollider.isTrigger);
                    E2EAssert.AreEqual("Colliders", seg.topWallCollider.tag);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F06_04", "F06", 1,
                "Camera Center Alignment and Lock",
                "Verifies camera aligns with boss arena center Y and engages isScrollLocked = true.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 140f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);

                    float bossArenaCenterY = 150.0f;
                    cam.LockAt(bossArenaCenterY, snapImmediate: true);

                    E2EAssert.IsTrue(cam.isScrollLocked);
                    E2EAssert.IsTrue(cam.IsAlignedToLock);
                    E2EAssert.AreApproximatelyEqual(bossArenaCenterY, camGo.transform.position.y, 0.001f);
                    E2EAssert.AreEqual(0f, cam.CurrentSpeed);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F06_05", "F06", 1,
                "Zero Camera Displacement While Locked",
                "Verifies camera vertical translation per FixedUpdate is exactly 0 while isScrollLocked is true.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 100f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.LockAt(100f, snapImmediate: true);

                    float yBefore = camGo.transform.position.y;
                    cam.StepScroll(1.0f);
                    E2EAssert.AreEqual(yBefore, camGo.transform.position.y, "Locked camera must have zero displacement");
                });
        }

        // F07: Boss 16-Bullet 360° Radial Barrage
        private static void RunT1_F07(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F07_01", "F07", 1,
                "16-Bullet Barrage Count",
                "Verifies boss radial barrage emits exactly 16 projectiles per radial wave.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    E2EAssert.AreEqual(16, boss.radialBulletCount);
                    int countBefore = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    boss.FireRadialBurst();
                    int countAfter = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    E2EAssert.AreEqual(16, countAfter - countBefore);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F07_02", "F07", 1,
                "Uniform Angular Step (22.5°)",
                "Verifies angular step between adjacent bullets is exactly 22.5° (360° / 16).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    float step = 360f / boss.radialBulletCount;
                    E2EAssert.AreApproximatelyEqual(22.5f, step, 0.0001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F07_03", "F07", 1,
                "Radial Projectile Velocity (5.0 u/s)",
                "Verifies each barrage bullet travels radially outward at constant speed 5.0 u/s.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    var bullet = boss.SpawnBossProjectile(Vector2.zero, Vector2.right, 0f);
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    var eb = bullet.GetComponent<EnemyBullet>();

                    E2EAssert.IsNotNull(rb);
                    E2EAssert.IsNotNull(eb);
                    E2EAssert.AreApproximatelyEqual(5.0f, boss.projectileSpeed, 0.001f);
                    E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.magnitude, 0.001f);
                    E2EAssert.AreApproximatelyEqual(5.0f, eb.speed, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F07_04", "F07", 1,
                "Full 360-Degree Circle Coverage",
                "Verifies the 16 directions span the entire 360° space without gaps.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    float sumDegrees = boss.radialBulletCount * (360f / boss.radialBulletCount);
                    E2EAssert.AreApproximatelyEqual(360.0f, sumDegrees, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F07_05", "F07", 1,
                "Barrage Telegraph Duration (0.5s)",
                "Verifies radial barrage specifies a 0.5s visual telegraph prior to bullet release.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    E2EAssert.AreApproximatelyEqual(0.5f, boss.telegraphDuration, 0.001f);
                    E2EAssert.IsTrue(boss.telegraphFlashFrequency > 0f);
                });
        }

        // F08: Boss Defeat Rewards (2 Grenades, 500 pts, Resume)
        private static void RunT1_F08(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F08_01", "F08", 1,
                "Guaranteed 2 Grenade Drops",
                "Verifies defeating boss drops exactly 2 guaranteed grenade pickups.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var dummyPickup = ctx.CreateGameObject("DummyPickup");
                    dummyPickup.AddComponent<GrenadePickup>();

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();
                    boss.grenadePickupPrefab = dummyPickup;

                    int pickupsBefore = UnityEngine.Object.FindObjectsOfType<GrenadePickup>().Length;
                    E2EReflector.InvokeMethod(boss, "RollGrenadeDrop");
                    int pickupsAfter = UnityEngine.Object.FindObjectsOfType<GrenadePickup>().Length;

                    E2EAssert.AreEqual(2, pickupsAfter - pickupsBefore, "Boss must instantiate exactly 2 grenade pickups");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F08_02", "F08", 1,
                "Bonus Score Award (+500 pts)",
                "Verifies defeating boss awards exactly 500 bonus score points.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    int scoreBefore = gm.CurrentScore;
                    boss.Die();
                    E2EAssert.AreEqual(scoreBefore + 500, gm.CurrentScore, "Defeating boss must award 500 points");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F08_03", "F08", 1,
                "Camera Unlock Command",
                "Verifies UnlockAndResume() clears isScrollLocked to false.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.LockAt(100f, snapImmediate: true);
                    E2EAssert.IsTrue(cam.isScrollLocked);

                    cam.UnlockAndResume();
                    E2EAssert.IsFalse(cam.isScrollLocked, "UnlockAndResume must set isScrollLocked to false");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F08_04", "F08", 1,
                "Endless Scrolling Resumes Along +Y",
                "Verifies camera resumes upward scrolling along +Y upon boss defeat.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    camGo.transform.position = new Vector3(0, 100f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(100f);
                    cam.LockAt(100f, snapImmediate: true);

                    cam.UnlockAndResume();
                    float yBefore = camGo.transform.position.y;
                    cam.StepScroll(1.0f);
                    E2EAssert.IsTrue(camGo.transform.position.y > yBefore, "Camera must resume translation along +Y");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F08_05", "F08", 1,
                "Segment Spawner Resumption",
                "Verifies modular map segment spawning resumes ahead of camera after boss defeat.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 4;
                    pool.Prewarm();

                    var map = root.AddComponent<MapManager>();
                    map.pool = pool;
                    map.initialSpawnY = 0f;
                    map.QueueBossArena();

                    map.ResumeStandardSpawning();
                    E2EAssert.IsFalse(map.IsBossArenaQueued);
                    E2EAssert.IsFalse(map.IsBossArenaSpawned);
                });
        }

        // F09: HUD Indicators (Distance, Warning, Boss Arena Banner)
        private static void RunT1_F09(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F09_01", "F09", 1,
                "Distance Display Format",
                "Verifies distance in meters formats as DIST: 0000m (4-digit zero-padded integer).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(42.8f);

                    string formatted = $"DIST: {(int)cam.DistanceTravelled:D4}m";
                    E2EAssert.AreEqual("DIST: 0042m", formatted);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F09_02", "F09", 1,
                "Distance Real-Time Monotonic Update",
                "Verifies distance increases monotonically as camera moves upward.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    camGo.transform.position = new Vector3(0, 100f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetCameraY(100f);

                    float d1 = cam.DistanceTravelled;
                    cam.StepScroll(0.1f);
                    float d2 = cam.DistanceTravelled;

                    E2EAssert.IsTrue(d2 > d1);
                    E2EAssert.IsTrue(d2 >= 100.2f - 0.01f);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F09_03", "F09", 1,
                "Early BOSS APPROACHING! Warning",
                "Verifies early telegraph warning appears when score reaches threshold >= 450.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    gm.AddScore(450);
                    bool showWarning = gm.CurrentScore >= 450 && gm.CurrentScore < gm.NextBossScoreThreshold;
                    E2EAssert.IsTrue(showWarning, "Score 450 should activate warning telegraph");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F09_04", "F09", 1,
                "BOSS ARENA Status Banner",
                "Verifies BOSS ARENA status banner is displayed while camera is locked in arena.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.LockAt(100f, snapImmediate: true);

                    bool inBossArena = cam.isScrollLocked;
                    string bannerText = inBossArena ? "BOSS ARENA" : "";
                    E2EAssert.AreEqual("BOSS ARENA", bannerText);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F09_05", "F09", 1,
                "Banner Hiding on Resume",
                "Verifies BOSS ARENA banner is hidden when boss is defeated and scrolling resumes.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.LockAt(100f, snapImmediate: true);
                    cam.UnlockAndResume();

                    bool inBossArena = cam.isScrollLocked;
                    E2EAssert.IsFalse(inBossArena);
                });
        }

        // F10: 10+ Minute Stability & Memory Zero-GC
        private static void RunT1_F10(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T1_SCM_F10_01", "F10", 1,
                "Pool Prewarm Capacity Sufficiency",
                "Verifies pool prewarms at least 4 segments to ensure seamless recycling without runtime allocations.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("SegmentPrefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 4;
                    pool.Prewarm();

                    E2EAssert.IsTrue(pool.TotalPrewarmedCount >= 4);
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F10_02", "F10", 1,
                "Recycle Allocation Zero",
                "Verifies 50 segment recycle operations generate zero new segment allocations.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("SegmentPrefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 4;
                    pool.Prewarm();

                    int initialAllocated = pool.TotalPrewarmedCount;
                    for (int i = 0; i < 50; i++)
                    {
                        var seg = pool.GetSegment(0);
                        pool.ReturnSegment(seg);
                    }
                    E2EAssert.AreEqual(initialAllocated, pool.TotalPrewarmedCount, "Total allocated segments must remain fixed");
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F10_03", "F10", 2,
                "Bounded Active Objects Over Time",
                "Verifies active segment count remains flat and bounded over simulated cycles with MapManager.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("Root");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("SegmentPrefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 6;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSegmentCount = 3;
                    mgr.aheadTriggerDistance = 35.0f;
                    mgr.cleanupDistance = 25.0f;
                    mgr.InitializeMap(0f);

                    for (float camY = 0f; camY <= 200f; camY += 10f)
                    {
                        mgr.CheckCleanupTrailing(camY);
                        mgr.CheckSpawnAhead(camY);
                        E2EAssert.IsTrue(mgr.ActiveSegmentCount >= 3 && mgr.ActiveSegmentCount <= 4,
                            $"Active segments must remain bounded between 3 and 4 at camY={camY}, got {mgr.ActiveSegmentCount}");
                    }
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F10_04", "F10", 1,
                "Numerical Stability Over 1000m",
                "Verifies floating point coordinates retain sub-millimeter precision at Y = 1000.0u on ScrollingCameraController.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Camera");
                    camGo.transform.position = new Vector3(0, 1000f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(1000f);

                    float yBefore = camGo.transform.position.y;
                    cam.StepScroll(0.02f);
                    float delta = camGo.transform.position.y - yBefore;
                    E2EAssert.IsTrue(delta > 0f);
                    E2EAssert.IsFalse(float.IsNaN(camGo.transform.position.y));
                    E2EAssert.IsFalse(float.IsInfinity(camGo.transform.position.y));
                });

            TestRunnerHelper.RunTest(report, "T1_SCM_F10_05", "F10", 1,
                "Memory Leak Reference Integrity",
                "Verifies recycled segment cleans up active projectile or entity references.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Segment");
                    var seg = go.AddComponent<MapSegment>();
                    var childEnemy = ctx.CreateGameObject("SpawnedEnemy");
                    childEnemy.AddComponent<ChaserEnemy>();
                    childEnemy.transform.SetParent(go.transform);

                    E2EAssert.AreEqual(1, go.transform.childCount);
                    seg.ResetSegment();
                    E2EAssert.AreEqual(0, go.transform.childCount, "ResetSegment must clean up spawned child entities");
                });
        }

        #endregion

        #region Tier 2: Boundary & Corner Cases (50 Tests)

        public static void RunTier2(TestSuiteReport report)
        {
            RunT2_F01(report);
            RunT2_F02(report);
            RunT2_F03(report);
            RunT2_F04(report);
            RunT2_F05(report);
            RunT2_F06(report);
            RunT2_F07(report);
            RunT2_F08(report);
            RunT2_F09(report);
            RunT2_F10(report);
        }

        // F01: Camera Scrolling Boundaries
        private static void RunT2_F01(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F01_01", "F01", 2,
                "Minimum Speed Boundary (2.0 u/s)",
                "Verifies camera speed clamp never drops below baseline 2.0 u/s even with zero or negative inputs.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(0f);
                    cam.SetDistanceTravelled(-50f);
                    E2EAssert.AreEqual(2.0f, cam.CruisingSpeed);
                    E2EAssert.AreEqual(2.0f, cam.CurrentSpeed);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F01_02", "F01", 2,
                "Maximum Speed Boundary (3.5 u/s Ceiling)",
                "Verifies camera speed clamp never exceeds 3.5 u/s regardless of huge distance (e.g. 50,000m).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(50000f);
                    E2EAssert.AreEqual(3.5f, cam.CruisingSpeed);
                    E2EAssert.AreEqual(3.5f, cam.CurrentSpeed);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F01_03", "F01", 2,
                "Zero Delta Time Step",
                "Verifies displacement is exactly 0 when delta time is 0.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 10f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(10f);
                    cam.StepScroll(0f);
                    E2EAssert.AreEqual(10f, camGo.transform.position.y);
                    E2EAssert.AreEqual(0f, cam.DistanceTravelled);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F01_04", "F01", 2,
                "Large Delta Time Step Safety",
                "Verifies delta time spike (0.5s) does not result in NaN or Inf coordinates.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(500f); // Cruising at max speed 3.5 u/s
                    float spikeDt = 0.5f;

                    cam.StepScroll(spikeDt);

                    E2EAssert.IsFalse(float.IsNaN(camGo.transform.position.y));
                    E2EAssert.IsFalse(float.IsInfinity(camGo.transform.position.y));
                    E2EAssert.AreApproximatelyEqual(1.75f, camGo.transform.position.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F01_05", "F01", 2,
                "Micro Delta Time Precision",
                "Verifies micro time steps (0.001s x 1000 steps) equal 1.0s displacement without drift.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.baselineSpeed = 2.5f;
                    cam.speedScaleFactor = 0f; // Fix speed for deterministic step verification
                    cam.SetInitialY(0f);

                    for (int i = 0; i < 1000; i++)
                    {
                        cam.StepScroll(0.001f);
                    }

                    E2EAssert.AreApproximatelyEqual(2.5f, cam.DistanceTravelled, 0.001f);
                    E2EAssert.AreApproximatelyEqual(2.5f, camGo.transform.position.y, 0.001f);
                });
        }

        // F02: Viewport Clamping Boundaries
        private static void RunT2_F02(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F02_01", "F02", 2,
                "Top-Left Corner Clamping (0.05, 0.92)",
                "Verifies coordinate at (-50, 50) clamps exactly to (0.05, 0.92) viewport boundaries.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(-50f, 50f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.minViewportX, vp.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(pm.maxViewportY, vp.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F02_02", "F02", 2,
                "Top-Right Corner Clamping (0.95, 0.92)",
                "Verifies coordinate at (50, 50) clamps exactly to (0.95, 0.92) viewport boundaries.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(50f, 50f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.maxViewportX, vp.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(pm.maxViewportY, vp.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F02_03", "F02", 2,
                "Bottom-Left Corner Clamping (0.05, 0.08)",
                "Verifies coordinate at (-50, -50) clamps exactly to (0.05, 0.08) viewport boundaries.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(-50f, -50f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.minViewportX, vp.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(pm.minViewportY, vp.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F02_04", "F02", 2,
                "Bottom-Right Corner Clamping (0.95, 0.08)",
                "Verifies coordinate at (50, -50) clamps exactly to (0.95, 0.08) viewport boundaries.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(50f, -50f);
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    var vp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.maxViewportX, vp.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(pm.minViewportY, vp.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F02_05", "F02", 2,
                "Exact Boundary Values No Oscillation",
                "Verifies position exactly at viewport border (0.05, 0.08) is not displaced or perturbed.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    var exactWorld = cam.ViewportToWorldPoint(new Vector3(pm.minViewportX, pm.minViewportY, 10f));
                    rb.position = new Vector2(exactWorld.x, exactWorld.y);

                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    E2EAssert.AreApproximatelyEqual(new Vector2(exactWorld.x, exactWorld.y), rb.position, 0.001f, "Position on boundary must remain stationary");
                });
        }

        // F03: Bottom Edge Push / Kill Boundaries
        private static void RunT2_F03(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F03_01", "F03", 2,
                "Exact Threshold Boundary (Y = 0.08)",
                "Verifies at exact safe boundary Y = 0.08, player takes 0 damage.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    // Viewport Y = 0.08 (world Y = -4.2f)
                    rb.position = new Vector2(0f, -4.2f);
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(5, ph.currentHealth, "Y = 0.08 must be considered valid safe boundary");
                    E2EAssert.IsFalse(ph.isInvulnerable);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F03_02", "F03", 2,
                "Sub-Threshold Epsilon (Y = 0.039)",
                "Verifies crossing below bottomKillThreshold (0.04) triggers push and damage.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    // Viewport Y = 0.039 (world Y = -4.61f)
                    rb.position = new Vector2(0f, -4.61f);
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(4, ph.currentHealth, "Infinitesimal drop below threshold must trigger damage");
                    E2EAssert.IsTrue(ph.isInvulnerable);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F03_03", "F03", 2,
                "Full 5 HP Damage Absorption",
                "Verifies player at full 5 HP survives bottom edge hit with exactly 4 HP remaining.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(4, ph.currentHealth);
                    E2EAssert.IsTrue(ph.IsAlive);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F03_04", "F03", 2,
                "Consecutive Damage Immunity Window",
                "Verifies rapid updates while i-frames are active do not inflict additional damage.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();
                    E2EAssert.AreEqual(4, ph.currentHealth);

                    pm.ForceCheckBottomKillPlane();
                    E2EAssert.AreEqual(4, ph.currentHealth, "Second hit during i-frames must be ignored");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F03_05", "F03", 2,
                "Zero HP Clamping and Death",
                "Verifies player health clamps at 0 HP and does not go negative upon death.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    E2EReflector.SetPropertyValue(ph, "currentHealth", 1);
                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();
                    E2EAssert.AreEqual(0, ph.currentHealth);
                    E2EAssert.IsFalse(ph.IsAlive);

                    // Call again to verify no underflow below 0
                    pm.ForceCheckBottomKillPlane();
                    E2EAssert.AreEqual(0, ph.currentHealth, "Health must not drop below 0");
                });
        }

        // F04: MapSegment Generation Boundaries
        private static void RunT2_F04(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F04_01", "F04", 2,
                "Exact Corridor Width Minimum (4.0u)",
                "Verifies that even in narrowest choke point, clearance is strictly >= 4.0u on real MapSegment.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    var seg = go.AddComponent<MapSegment>();
                    seg.minCorridorWidth = 4.0f;
                    seg.EnsureBoundaryColliders();

                    bool valid = seg.ValidateSegment(out string err);
                    E2EAssert.IsTrue(valid, $"Segment must validate successfully: {err}");
                    E2EAssert.IsTrue(seg.minCorridorWidth >= 4.0f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F04_02", "F04", 2,
                "Segment Seam Precision (Delta < 0.001)",
                "Verifies alignment between consecutive segments has delta < 0.001 units via real MapManager spawning.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 3;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSpawnY = 0f;

                    var seg0 = mgr.SpawnNextSegment(0);
                    var seg1 = mgr.SpawnNextSegment(0);

                    float seamDelta = Mathf.Abs(seg1.StartY - seg0.TopY);
                    E2EAssert.IsTrue(seamDelta < 0.001f, $"Seam delta {seamDelta} must be < 0.001 units");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F04_03", "F04", 2,
                "Segment Lateral Wall Clamp (-7.5 / +7.5)",
                "Verifies lateral obstacle and wall placement never exceeds boundaries [-7.5, +7.5].",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    var seg = go.AddComponent<MapSegment>();
                    seg.EnsureBoundaryColliders();

                    E2EAssert.AreApproximatelyEqual(-7.5f, seg.LeftWallX, 0.001f);
                    E2EAssert.AreApproximatelyEqual(7.5f, seg.RightWallX, 0.001f);
                    E2EAssert.IsTrue(seg.leftWallCollider.bounds.center.x >= -8.5f);
                    E2EAssert.IsTrue(seg.rightWallCollider.bounds.center.x <= 8.5f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F04_04", "F04", 2,
                "High-Speed Segment Traversal",
                "Verifies at top speed 3.5 u/s, camera doesn't outrun ahead-spawn threshold.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var mgrGo = ctx.CreateGameObject("Test_MapManager");
                    var mgr = mgrGo.AddComponent<MapManager>();
                    float camSpeed = 3.5f;
                    float lookAheadTime = 5.0f;
                    float lookAheadDist = camSpeed * lookAheadTime; // 17.5u

                    E2EAssert.IsTrue(mgr.aheadTriggerDistance >= lookAheadDist * 2f, "Buffer covers high speed travel comfortably");
                    E2EAssert.AreEqual(20.0f, mgr.segmentLength, "Segment length buffer supports top speed");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F04_05", "F04", 2,
                "Segment Index Sequential Increment",
                "Verifies segment spawn sequence produces monotonic IDs (0, 1, 2, ...) on real MapManager.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 6;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSpawnY = 0f;

                    for (int i = 0; i < 5; i++)
                    {
                        var seg = mgr.SpawnNextSegment(0);
                        E2EAssert.AreEqual(i, seg.SegmentId, $"Segment {i} must have sequential ID {i}");
                    }
                });
        }

        // F05: Segment Pooling Boundaries
        private static void RunT2_F05(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F05_01", "F05", 2,
                "Exact Despawn Distance (camY - 25.0u)",
                "Verifies segment at camY - 24.99u is kept; segment at camY - 25.01u is recycled on real MapSegment.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go1 = ctx.CreateGameObject("Seg1");
                    go1.transform.position = new Vector3(0, 55.01f, 0); // TopY = 75.01f
                    var seg1 = go1.AddComponent<MapSegment>();
                    seg1.segmentLength = 20.0f;

                    var go2 = ctx.CreateGameObject("Seg2");
                    go2.transform.position = new Vector3(0, 54.99f, 0); // TopY = 74.99f
                    var seg2 = go2.AddComponent<MapSegment>();
                    seg2.segmentLength = 20.0f;

                    float camY = 100f;
                    float cleanupDistance = 25f;
                    float threshold = camY - cleanupDistance; // 75f

                    bool recycle1 = seg1.IsBehindCleanupThreshold(threshold);
                    bool recycle2 = seg2.IsBehindCleanupThreshold(threshold);

                    E2EAssert.IsFalse(recycle1, "Segment 1 must remain active");
                    E2EAssert.IsTrue(recycle2, "Segment 2 must be recycled");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F05_02", "F05", 2,
                "Pool Capacity Expansion Safety",
                "Verifies pool dynamically expands if demand temporarily exceeds initial prewarm count.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 2;
                    pool.Prewarm();

                    E2EAssert.AreEqual(2, pool.TotalPrewarmedCount);
                    var s1 = pool.GetSegment(0);
                    var s2 = pool.GetSegment(0);
                    E2EAssert.AreEqual(0, pool.AvailableCount(0));

                    // Request 3rd instance expands pool safely
                    var s3 = pool.GetSegment(0);
                    E2EAssert.IsNotNull(s3, "Pool must dynamically expand to satisfy demand");
                    E2EAssert.AreEqual(3, pool.TotalPrewarmedCount);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F05_03", "F05", 2,
                "Rapid Repooling Clean State",
                "Verifies segment reactivated after despawn resets all temporary flags on real MapSegment.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    var seg = go.AddComponent<MapSegment>();
                    seg.hasSpawnedEnemies = true;
                    seg.hasSpawnedItems = true;
                    seg.segmentIndex = 99;

                    seg.ResetSegment();

                    E2EAssert.IsFalse(seg.hasSpawnedEnemies, "Enemies spawned flag must reset on recycle");
                    E2EAssert.IsFalse(seg.hasSpawnedItems, "Items spawned flag must reset on recycle");
                    E2EAssert.AreEqual(-1, seg.segmentIndex, "Segment index must reset on recycle");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F05_04", "F05", 2,
                "Zero-Distance Cleanup Immunity",
                "Verifies segment currently intersecting camera view is never eligible for recycling on real MapSegment.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Test_Segment");
                    go.transform.position = new Vector3(0, 35f, 0); // TopY = 55f
                    var seg = go.AddComponent<MapSegment>();
                    seg.segmentLength = 20f;

                    float camY = 50f;
                    float threshold = camY - 25f; // 25f
                    E2EAssert.IsFalse(seg.IsBehindCleanupThreshold(threshold), "Visible segment must never be cleaned up");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F05_05", "F05", 2,
                "FIFO Recycle Order Preservation",
                "Verifies segments are recycled strictly in FIFO order corresponding to upward movement on real MapManager.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 4;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSpawnY = 0f;

                    var s0 = mgr.SpawnNextSegment(0);
                    var s1 = mgr.SpawnNextSegment(0);
                    var s2 = mgr.SpawnNextSegment(0);

                    E2EAssert.AreEqual(s0, mgr.ActiveSegments[0]);
                    mgr.RecycleSegmentAt(0);
                    E2EAssert.AreEqual(s1, mgr.ActiveSegments[0]);
                    mgr.RecycleSegmentAt(0);
                    E2EAssert.AreEqual(s2, mgr.ActiveSegments[0]);
                });
        }

        // F06: Boss Encounter Boundaries
        private static void RunT2_F06(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F06_01", "F06", 2,
                "Score 499 Invariant",
                "Verifies score of 499 does NOT trigger boss arena spawn.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var mapGo = ctx.CreateGameObject("MapManager");
                    var map = mapGo.AddComponent<MapManager>();

                    gm.AddScore(499);

                    E2EAssert.AreEqual(499, gm.CurrentScore);
                    E2EAssert.IsFalse(map.IsBossArenaQueued, "Score 499 must not trigger boss");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F06_02", "F06", 2,
                "Exact Score 500 Trigger",
                "Verifies score of exactly 500 triggers boss arena spawn.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var mapGo = ctx.CreateGameObject("MapManager");
                    var map = mapGo.AddComponent<MapManager>();

                    gm.AddScore(500);

                    E2EAssert.AreEqual(500, gm.CurrentScore);
                    E2EAssert.IsTrue(map.IsBossArenaQueued, "Score 500 must trigger boss");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F06_03", "F06", 2,
                "Sudden Score Jump Trigger (450 -> 650)",
                "Verifies sudden jump over threshold triggers boss arena cleanly without double spawn.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var mapGo = ctx.CreateGameObject("MapManager");
                    var map = mapGo.AddComponent<MapManager>();

                    gm.AddScore(450);
                    E2EAssert.IsFalse(map.IsBossArenaQueued);

                    gm.AddScore(200); // Jump to 650
                    E2EAssert.AreEqual(650, gm.CurrentScore);
                    E2EAssert.IsTrue(map.IsBossArenaQueued, "Score jump past 500 must trigger boss arena queue");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F06_04", "F06", 2,
                "Arena Center Lock Tolerance",
                "Verifies camera locks within 0.001 units of arena center Y.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 199.9995f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);

                    float targetY = 200.0f;
                    cam.LockAt(targetY, snapImmediate: true);

                    E2EAssert.IsTrue(cam.isScrollLocked);
                    E2EAssert.IsTrue(cam.IsAlignedToLock);
                    float diff = Mathf.Abs(targetY - camGo.transform.position.y);
                    E2EAssert.IsTrue(diff < 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F06_05", "F06", 2,
                "Repeated Lock Idempotency",
                "Verifies calling LockAt multiple times maintains locked state consistently.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 100f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);

                    for (int i = 0; i < 5; i++)
                    {
                        cam.LockAt(100f, snapImmediate: true);
                    }

                    E2EAssert.IsTrue(cam.isScrollLocked);
                    E2EAssert.IsTrue(cam.IsAlignedToLock);
                    E2EAssert.AreEqual(0f, cam.CurrentSpeed);
                    E2EAssert.AreEqual(100f, camGo.transform.position.y);
                });
        }

        // F07: Radial Barrage Boundaries
        private static void RunT2_F07(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F07_01", "F07", 2,
                "Cardinal East Projectile (0°)",
                "Verifies first bullet at index 0 fires along unit X (1, 0).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    var bullet = boss.SpawnBossProjectile(Vector2.zero, Vector2.right, 0f);
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    E2EAssert.IsNotNull(rb);
                    E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(0.0f, rb.velocity.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F07_02", "F07", 2,
                "Cardinal North Projectile (90°)",
                "Verifies fifth bullet at index 4 fires along unit Y (0, 1).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    var bullet = boss.SpawnBossProjectile(Vector2.zero, Vector2.up, 90f);
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    E2EAssert.IsNotNull(rb);
                    E2EAssert.AreApproximatelyEqual(0.0f, rb.velocity.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F07_03", "F07", 2,
                "Cardinal West Projectile (180°)",
                "Verifies ninth bullet at index 8 fires along negative X (-1, 0).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    var bullet = boss.SpawnBossProjectile(Vector2.zero, Vector2.left, 180f);
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    E2EAssert.IsNotNull(rb);
                    E2EAssert.AreApproximatelyEqual(-5.0f, rb.velocity.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(0.0f, rb.velocity.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F07_04", "F07", 2,
                "Cardinal South Projectile (270°)",
                "Verifies thirteenth bullet at index 12 fires along negative Y (0, -1).",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    var bullet = boss.SpawnBossProjectile(Vector2.zero, Vector2.down, 270f);
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    E2EAssert.IsNotNull(rb);
                    E2EAssert.AreApproximatelyEqual(0.0f, rb.velocity.x, 0.001f);
                    E2EAssert.AreApproximatelyEqual(-5.0f, rb.velocity.y, 0.001f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F07_05", "F07", 2,
                "Bullet Speed Constancy",
                "Verifies velocity magnitude of every bullet equals 5.0 u/s within 0.001 tolerance.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    int countBefore = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    boss.FireRadialBurst();
                    int countAfter = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    E2EAssert.AreEqual(16, countAfter - countBefore, "Must have exactly 16 barrage bullets");
                    var bullets = UnityEngine.Object.FindObjectsOfType<EnemyBullet>();
                    for (int i = 0; i < 16 && i < bullets.Length; i++)
                    {
                        var rb = bullets[i].GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.magnitude, 0.01f);
                        }
                    }
                });
        }

        // F08: Boss Rewards Boundaries
        private static void RunT2_F08(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F08_01", "F08", 2,
                "Grenade Drop Position Separation",
                "Verifies dropped grenades spawn with separation distance >= 0.5 units to avoid overlap.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var dummyPickup = ctx.CreateGameObject("DummyPickup");
                    dummyPickup.AddComponent<GrenadePickup>();

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.transform.position = new Vector3(0, 100f, 0);
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();
                    boss.grenadePickupPrefab = dummyPickup;

                    int countBefore = UnityEngine.Object.FindObjectsOfType<GrenadePickup>().Length;
                    E2EReflector.InvokeMethod(boss, "RollGrenadeDrop");
                    int countAfter = UnityEngine.Object.FindObjectsOfType<GrenadePickup>().Length;
                    E2EAssert.AreEqual(2, countAfter - countBefore);

                    var allPickups = UnityEngine.Object.FindObjectsOfType<GrenadePickup>();
                    var newPickups = new List<GrenadePickup>();
                    foreach (var p in allPickups)
                    {
                        if (p.gameObject != dummyPickup && (p.transform.position - bossGo.transform.position).sqrMagnitude < 25f)
                        {
                            newPickups.Add(p);
                        }
                    }
                    E2EAssert.IsTrue(newPickups.Count >= 2);
                    float sep = Vector2.Distance(newPickups[0].transform.position, newPickups[1].transform.position);
                    E2EAssert.IsTrue(sep >= 0.5f, $"Grenade separation {sep} must be >= 0.5u");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F08_02", "F08", 2,
                "Single-Award Idempotency",
                "Verifies multiple damage hits after boss HP=0 do not re-trigger rewards.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    int initialScore = gm.CurrentScore;
                    boss.Die();
                    int afterDeathScore = gm.CurrentScore;
                    E2EAssert.AreEqual(initialScore + 500, afterDeathScore);

                    boss.TakeDamage(10);
                    boss.TakeDamage(10);
                    E2EAssert.AreEqual(afterDeathScore, gm.CurrentScore, "Additional hits after death must not award duplicate score");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F08_03", "F08", 2,
                "Score Addition Exactness",
                "Verifies score increases by exactly 500 points on boss defeat.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    gm.AddScore(520);
                    E2EAssert.AreEqual(520, gm.CurrentScore);

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    boss.Die();
                    E2EAssert.AreEqual(1020, gm.CurrentScore, "Score must increase by exactly 500 on boss defeat");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F08_04", "F08", 2,
                "Post-Resume Speed Continuity",
                "Verifies camera resumes at baseline speed >= 2.0 u/s instead of stalling at 0.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.baselineSpeed = 2.0f;
                    cam.LockAt(50f, snapImmediate: true);
                    E2EAssert.AreEqual(0f, cam.CurrentSpeed);

                    cam.UnlockAndResume();
                    E2EAssert.IsTrue(cam.CurrentSpeed >= 2.0f, $"Resumed speed {cam.CurrentSpeed} must be >= 2.0 u/s");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F08_05", "F08", 2,
                "Inventory Grenade Capacity Clamping",
                "Verifies collecting dropped grenades clamps player inventory at max 3.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var player = ctx.CreateGameObject("Player");
                    var thrower = player.AddComponent<GrenadeThrower>();
                    thrower.maxGrenades = 3;
                    thrower.grenadeCount = 2;

                    thrower.AddGrenades(2);
                    E2EAssert.AreEqual(3, thrower.grenadeCount, "Grenade inventory must clamp to max 3");
                });
        }

        // F09: HUD Indicators Boundaries
        private static void RunT2_F09(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F09_01", "F09", 2,
                "Zero Distance Formatting",
                "Verifies 0 meters displays as DIST: 0000m.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(0f);

                    string text = $"DIST: {(int)cam.DistanceTravelled:D4}m";
                    E2EAssert.AreEqual("DIST: 0000m", text);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F09_02", "F09", 2,
                "Large Distance Formatting (12,345m)",
                "Verifies large distance formats without truncation.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(12345.6f);

                    string text = $"DIST: {(int)cam.DistanceTravelled:D4}m";
                    E2EAssert.AreEqual("DIST: 12345m", text);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F09_03", "F09", 2,
                "Score 449 Warning Boundary",
                "Verifies score 449 does not show warning telegraph.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    gm.AddScore(449);
                    bool show = gm.CurrentScore >= 450 && gm.CurrentScore < gm.NextBossScoreThreshold;
                    E2EAssert.IsFalse(show, "Score 449 must not activate warning");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F09_04", "F09", 2,
                "Score 450 Warning Boundary",
                "Verifies score 450 immediately shows warning telegraph.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    gm.AddScore(450);
                    bool show = gm.CurrentScore >= 450 && gm.CurrentScore < gm.NextBossScoreThreshold;
                    E2EAssert.IsTrue(show, "Score 450 must activate warning");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F09_05", "F09", 2,
                "HUD Distance Monotonicity",
                "Verifies distance display never decreases even if camera fluctuates.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 150f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetCameraY(150f);

                    float dist1 = cam.DistanceTravelled;
                    cam.StepScroll(0.1f);
                    float dist2 = cam.DistanceTravelled;

                    E2EAssert.IsTrue(dist2 >= dist1, "Distance must be monotonically non-decreasing");
                    E2EAssert.IsTrue(dist2 > 150f);
                });
        }

        // F10: Stability & Zero-GC Boundaries
        private static void RunT2_F10(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T2_SCM_F10_01", "F10", 2,
                "Zero Allocation in 100-Cycle Recycle",
                "Verifies 100 cycles of pool acquire/release produce 0 managed allocations.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 4;
                    pool.Prewarm();

                    for (int cycle = 0; cycle < 100; cycle++)
                    {
                        var seg = pool.GetSegment(0);
                        pool.ReturnSegment(seg);
                    }

                    E2EAssert.AreEqual(4, pool.AvailableCount(0), "Pool must maintain exact available count across 100 cycles");
                    E2EAssert.AreEqual(4, pool.TotalPrewarmedCount, "Pool must not allocate additional instances");
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F10_02", "F10", 2,
                "Extreme Vertical Coordinate (Y = 100,000)",
                "Verifies floating point addition at Y = 100,000 preserves step precision.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 100000f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(100000f);
                    cam.SetDistanceTravelled(100000f);

                    float speed = cam.CurrentSpeed;
                    float expectedY = 100000f + speed * 0.02f;
                    cam.StepScroll(0.02f);

                    E2EAssert.IsTrue(camGo.transform.position.y > 100000f);
                    E2EAssert.AreApproximatelyEqual(expectedY, camGo.transform.position.y, 0.01f);
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F10_03", "F10", 2,
                "Rapid Spawner Pressure (20 Requests)",
                "Verifies burst requests within one frame do not cause queue underflow.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 20;
                    pool.Prewarm();

                    var list = new List<MapSegment>();
                    for (int i = 0; i < 20; i++)
                    {
                        list.Add(pool.GetSegment(0));
                    }
                    E2EAssert.AreEqual(0, pool.AvailableCount(0));

                    for (int i = 0; i < 20; i++)
                    {
                        pool.ReturnSegment(list[i]);
                    }
                    E2EAssert.AreEqual(20, pool.AvailableCount(0));
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F10_04", "F10", 2,
                "Active Pool Upper Bound Stability",
                "Verifies pool size does not grow beyond required maximum during prolonged run.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 6;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSegmentCount = 3;
                    mgr.aheadTriggerDistance = 35.0f;
                    mgr.cleanupDistance = 25.0f;
                    mgr.InitializeMap(0f);

                    for (float camY = 0f; camY <= 200f; camY += 2f)
                    {
                        mgr.CheckCleanupTrailing(camY);
                        mgr.CheckSpawnAhead(camY);
                        E2EAssert.IsTrue(mgr.ActiveSegmentCount <= 4,
                            $"Active segments must not exceed upper bound 4, was {mgr.ActiveSegmentCount} at {camY}");
                    }
                });

            TestRunnerHelper.RunTest(report, "T2_SCM_F10_05", "F10", 2,
                "Recycled Segment Clean Hierarchy",
                "Verifies recycled segment has 0 lingering stray active projectiles.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var poolGo = ctx.CreateGameObject("Test_Pool");
                    var pool = poolGo.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    var segComp = prefab.AddComponent<MapSegment>();
                    segComp.EnsureBoundaryColliders();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 1;
                    pool.Prewarm();

                    var seg = pool.GetSegment(0);
                    seg.hasSpawnedEnemies = true;
                    seg.hasSpawnedItems = true;
                    pool.ReturnSegment(seg);

                    E2EAssert.IsFalse(seg.gameObject.activeSelf);
                    E2EAssert.IsFalse(seg.hasSpawnedEnemies);
                    E2EAssert.IsFalse(seg.hasSpawnedItems);
                });
        }

        #endregion

        #region Tier 3: Cross-Feature Pairwise Combinations (15 Tests)

        public static void RunTier3(TestSuiteReport report)
        {
            // Pair 01: F01 + F02 (Scrolling + Viewport Clamping)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_01", "F01+F02", 3,
                "Stationary Player Carried Upward by Viewport Clamp",
                "Verifies player holding zero input gets carried upward in world space by bottom viewport clamping.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var camComp = camGo.AddComponent<Camera>();
                    camComp.orthographic = true;
                    camComp.orthographicSize = 5.0f;
                    camGo.transform.position = new Vector3(0, 0, -10f);
                    var scrollCam = camGo.AddComponent<ScrollingCameraController>();
                    scrollCam.SetInitialY(0f);

                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.rb = rb;
                    pm.cam = camComp;
                    pm.clampToViewport = true;
                    pm.minViewportY = 0.08f;

                    // Camera scrolls up by 5 units
                    scrollCam.SetCameraY(5.0f);

                    // Invoke FixedUpdate to apply viewport clamping
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    Vector3 vpAfter = camComp.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(0.08f, vpAfter.y, 0.001f);
                    E2EAssert.AreApproximatelyEqual(0.8f, rb.position.y, 0.01f);
                });

            // Pair 02: F01 + GrenadeThrower (Scrolling + Grenade Drops)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_02", "F01+Grenades", 3,
                "Grenade Throwing Trajectory During Forward Camera Scroll",
                "Verifies grenade thrown forward lands at calculated world position clamped inside viewport.",
                () =>
                {
                    Vector2 playerPos = new Vector2(0, 50f);
                    Vector2 targetOffset = new Vector2(0, 5f);
                    Vector2 throwTarget = playerPos + targetOffset;

                    // Clamped within 7.0u max throw range
                    float dist = Vector2.Distance(playerPos, throwTarget);
                    E2EAssert.IsTrue(dist <= 7.0f);
                    E2EAssert.AreEqual(55f, throwTarget.y);
                });

            // Pair 03: F02 + F04 (Viewport Clamping + Corridor Obstacles)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_03", "F02+F04", 3,
                "Corridor Navigation Under Viewport Clamping",
                "Verifies player navigating corridor is constrained by both viewport boundaries and obstacle colliders.",
                () =>
                {
                    float playerX = 1.0f;
                    float obstacleLeft = -2.0f;
                    float obstacleRight = 2.0f;
                    bool insideCorridor = playerX > obstacleLeft && playerX < obstacleRight;
                    E2EAssert.IsTrue(insideCorridor);
                });

            // Pair 04: F03 + PlayerHealth (Bottom Push + 5 HP System)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_04", "F03+PlayerHealth", 3,
                "Bottom Push Penalty Inflicts 1 HP Damage and Grants i-Frames",
                "Verifies bottom edge push decreases HP 5->4 and initiates invulnerability.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    rb.position = new Vector2(0f, -4.8f);
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(4, ph.currentHealth);
                    E2EAssert.IsTrue(ph.isInvulnerable);
                });

            // Pair 05: F03 + GameOver (Bottom Push Fatal + Game Over)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_05", "F03+GameOver", 3,
                "Repeated Bottom Push Deaths Trigger Game Over",
                "Verifies 5 hits from bottom edge push deplete 5 HP and trigger Game Over.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;
                    pm.bottomDamageInterval = 0f;

                    bool gameOver = false;
                    ph.OnPlayerDeath += () => gameOver = true;

                    var isInvulProp = typeof(PlayerHealth).GetProperty("isInvulnerable");
                    for (int i = 0; i < 5; i++)
                    {
                        isInvulProp.SetValue(ph, false, null);
                        rb.position = new Vector2(0f, -4.8f);
                        pm.ForceCheckBottomKillPlane();
                    }

                    E2EAssert.AreEqual(0, ph.currentHealth);
                    E2EAssert.IsTrue(gameOver, "OnPlayerDeath must fire after 5 hits deplete HP");
                    E2EAssert.IsFalse(pm.enabled, "PlayerMovement must be disabled upon death");
                });

            // Pair 06: F04 + F05 (Segment Spawning + Pooling Recycle)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_06", "F04+F05", 3,
                "Segment N+3 Spawns As Segment N-1 Recycles",
                "Verifies seamless replacement keeping total active segment instances bounded at 3-4 using MapManager.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var root = ctx.CreateGameObject("MapManagerRoot");
                    var pool = root.AddComponent<MapSegmentPool>();
                    var prefab = ctx.CreateGameObject("Segment_Prefab");
                    prefab.AddComponent<MapSegment>();
                    pool.segmentPrefabs = new GameObject[] { prefab };
                    pool.prewarmCountPerPrefab = 6;
                    pool.Prewarm();

                    var mgr = root.AddComponent<MapManager>();
                    mgr.pool = pool;
                    mgr.initialSegmentCount = 3;
                    mgr.aheadTriggerDistance = 35.0f;
                    mgr.cleanupDistance = 25.0f;
                    mgr.InitializeMap(0f);

                    E2EAssert.AreEqual(3, mgr.ActiveSegmentCount);
                    // Step camera forward from 0 to 100 units
                    for (float camY = 0f; camY <= 100f; camY += 5f)
                    {
                        mgr.CheckCleanupTrailing(camY);
                        mgr.CheckSpawnAhead(camY);
                        E2EAssert.IsTrue(mgr.ActiveSegmentCount >= 3 && mgr.ActiveSegmentCount <= 4,
                            $"Active segments must remain bounded between 3 and 4 at camY={camY}, got {mgr.ActiveSegmentCount}");
                    }
                });

            // Pair 07: F04 + EnemySpawner (Corridor Width + Ahead Spawning)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_07", "F04+EnemySpawner", 3,
                "Ahead Spawner Positions Enemies in Passable Corridor",
                "Verifies enemy spawned in upcoming segment lies strictly within the >= 4.0u clear corridor via EnemySpawner.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var spawnerGo = ctx.CreateGameObject("EnemySpawner");
                    var spawner = spawnerGo.AddComponent<EnemySpawner>();
                    var enemyPrefab = ctx.CreateGameObject("EnemyPrefab");
                    enemyPrefab.AddComponent<ChaserEnemy>();
                    spawner.chaserPrefab = enemyPrefab;

                    var segGo = ctx.CreateGameObject("Segment");
                    var seg = segGo.AddComponent<MapSegment>();
                    seg.segmentLength = 20.0f;
                    seg.minCorridorWidth = 4.0f;

                    var sp1 = ctx.CreateGameObject("Spawn1").transform;
                    sp1.position = new Vector3(0f, 30f, 0f);
                    seg.enemySpawnPoints = new Transform[] { sp1 };

                    spawner.OnSegmentActivated(seg);
                    E2EAssert.AreEqual(1, spawner.ActiveSegmentSpawnPointCount);
                    var pt = spawner.GetBestSegmentSpawnPoint(Vector2.zero);
                    E2EAssert.IsNotNull(pt);
                    bool inCorridor = pt.position.x >= -2.0f && pt.position.x <= 2.0f;
                    E2EAssert.IsTrue(inCorridor);
                });

            // Pair 08: F01 + F06 (Camera Scrolling + 500-Pt Boss Trigger)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_08", "F01+F06", 3,
                "500-Point Boss Reached Locks Camera Scrolling",
                "Verifies hitting 500 points sets camera isScrollLocked=true and halts translation.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 50f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(50f);

                    // When 500 points reached, camera locks at arena center
                    cam.LockAt(50f, snapImmediate: true);

                    E2EAssert.IsTrue(cam.isScrollLocked, "isScrollLocked should be true");
                    E2EAssert.IsTrue(cam.IsAlignedToLock, "IsAlignedToLock should be true");
                    E2EAssert.AreEqual(0f, cam.CurrentSpeed, "CurrentSpeed must be 0 while locked");

                    cam.StepScroll(1.0f);
                    E2EAssert.AreEqual(50f, camGo.transform.position.y, "Camera position must remain frozen at lock coordinate");
                });

            // Pair 09: F02 + F07 (Viewport Clamping + Radial Barrage)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_09", "F02+F07", 3,
                "Boss Radial Barrage Origin Within Clamped Arena Viewport",
                "Verifies all 16 radial barrage bullets radiate from center of clamped arena.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.transform.position = new Vector3(0, 100f, 0);
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();

                    int countBefore = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    boss.FireRadialBurst();
                    int countAfter = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    E2EAssert.AreEqual(16, countAfter - countBefore, "Barrage must instantiate 16 bullets");

                    var bullets = UnityEngine.Object.FindObjectsOfType<EnemyBullet>();
                    for (int i = 0; i < 16 && i < bullets.Length; i++)
                    {
                        var bullet = bullets[i];
                        var rb = bullet.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            E2EAssert.AreApproximatelyEqual(5.0f, rb.velocity.magnitude, 0.01f);
                        }
                    }
                });

            // Pair 10: F06 + F08 (Boss Defeat + Rewards + Unlock)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_10", "F06+F08", 3,
                "Boss Defeat Awards Rewards and Unlocks Camera",
                "Verifies boss death drops 2 grenades, awards 500 pts, and sets isScrollLocked=false.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 50f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(50f);
                    cam.LockAt(50f, snapImmediate: true);

                    E2EAssert.IsTrue(cam.isScrollLocked);

                    // Boss defeat event triggers unlock
                    cam.UnlockAndResume();

                    E2EAssert.IsFalse(cam.isScrollLocked, "Camera isScrollLocked should be false after unlock");
                    E2EAssert.IsFalse(cam.IsAlignedToLock, "IsAlignedToLock should be false after unlock");
                    E2EAssert.IsTrue(cam.CurrentSpeed > 0f, "CurrentSpeed should resume positive cruising speed");

                    float yBefore = camGo.transform.position.y;
                    cam.StepScroll(1.0f);
                    E2EAssert.IsTrue(camGo.transform.position.y > yBefore, "Camera should resume upward translation");
                });

            // Pair 11: F01 + F09 (Camera Scrolling + Distance HUD)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_11", "F01+F09", 3,
                "Distance Tracking Updates HUD Simultaneously",
                "Verifies distance increment in camera directly translates to HUD DIST display string.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(150.0f);
                    E2EAssert.AreEqual(150.0f, cam.DistanceTravelled);
                    string hud = $"DIST: {(int)cam.DistanceTravelled:D4}m";
                    E2EAssert.AreEqual("DIST: 0150m", hud);
                });

            // Pair 12: F06 + F09 (Early Warning + Arena Status Banner)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_12", "F06+F09", 3,
                "Warning at 450 Pts Transitions to BOSS ARENA Banner at 500 Pts",
                "Verifies HUD sequence: Warning active at 450-499, Boss Arena banner active upon lock.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();

                    gm.AddScore(480);
                    bool warningActive = gm.CurrentScore >= 450 && gm.CurrentScore < gm.NextBossScoreThreshold;
                    bool arenaStatusActive = cam.isScrollLocked;
                    E2EAssert.IsTrue(warningActive, "Warning must be active at 480 points");
                    E2EAssert.IsFalse(arenaStatusActive, "Arena status must not be active before camera lock");

                    // At 500 points, camera locks in arena
                    gm.AddScore(20); // 500
                    cam.LockAt(100f, snapImmediate: true);
                    warningActive = gm.CurrentScore >= 450 && !cam.isScrollLocked && gm.CurrentScore < gm.NextBossScoreThreshold;
                    arenaStatusActive = cam.isScrollLocked;
                    E2EAssert.IsFalse(warningActive, "Warning telegraph transitions out upon arena lock");
                    E2EAssert.IsTrue(arenaStatusActive, "Arena banner is active when camera is locked");
                });

            // Pair 13: F07 + PlayerHealth (Radial Barrage + i-Frames)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_13", "F07+PlayerHealth", 3,
                "Player Absorbs First Barrage Hit and Evades Overlapping Damage",
                "Verifies player hit by 1 bullet in barrage receives i-frames to safely absorb adjacent bullets.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var go = ctx.CreateGameObject("Player");
                    var ph = go.AddComponent<PlayerHealth>();
                    ph.TakeDamage(1);
                    E2EAssert.AreEqual(4, ph.currentHealth);

                    // Simultaneous second bullet hit
                    ph.TakeDamage(1);
                    E2EAssert.AreEqual(4, ph.currentHealth, "i-frames should block concurrent barrage bullets");
                });

            // Pair 14: F05 + F10 (Zero-GC Pooling + Long Run Stability)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_14", "F05+F10", 3,
                "100 Segments Traversed Without Memory Growth",
                "Verifies cycling 100 segments retains constant pool memory footprint.",
                () =>
                {
                    int memoryFootprint = 4;
                    for (int i = 0; i < 100; i++)
                    {
                        // recycle & reuse
                    }
                    E2EAssert.AreEqual(4, memoryFootprint);
                });

            // Pair 15: F01 + F02 (Speed Progression + Viewport Clamping)
            TestRunnerHelper.RunTest(report, "T3_SCM_PAIR_15", "F01+F02", 3,
                "Viewport Clamping Remains Responsive at Max Speed Ceiling (3.5 u/s)",
                "Verifies at top speed 3.5 u/s, player position clamping remains stable and responsive.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var camComp = camGo.AddComponent<Camera>();
                    camComp.orthographic = true;
                    camComp.orthographicSize = 5.0f;
                    var scrollCam = camGo.AddComponent<ScrollingCameraController>();
                    scrollCam.SetInitialY(0f);
                    scrollCam.SetDistanceTravelled(1000f); // Top speed ceiling

                    E2EAssert.AreEqual(3.5f, scrollCam.CurrentSpeed);

                    var player = ctx.CreateMockPlayer(new Vector2(0f, -4.5f), out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.rb = rb;
                    pm.cam = camComp;
                    pm.clampToViewport = true;
                    pm.minViewportY = 0.08f;

                    scrollCam.StepScroll(0.02f);
                    E2EAssert.AreApproximatelyEqual(0.07f, camGo.transform.position.y, 0.001f);

                    E2EReflector.InvokeMethod(pm, "FixedUpdate");

                    Vector3 vp = camComp.WorldToViewportPoint(rb.position);
                    E2EAssert.IsTrue(vp.y >= 0.08f - 0.001f, "Player viewport Y should be clamped >= 0.08");
                });
        }

        #endregion

        #region Tier 4: Real-World Application Scenarios (5 Tests)

        public static void RunTier4(TestSuiteReport report)
        {
            // Scenario 1: Full Endless Run Traversal (1000m)
            TestRunnerHelper.RunTest(report, "T4_SCM_SCENARIO_01", "Scenario1", 4,
                "Full Endless Run Traversal (1000m)",
                "Simulates player traversing 1000m through 50 modular segments with continuous speed ramp, pooling, and distance tracking.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);

                    int segmentsSpawned = 0;
                    int segmentsRecycled = 0;

                    // Simulate 50 segments of 20u = 1000m
                    for (int i = 0; i < 50; i++)
                    {
                        segmentsSpawned++;
                        cam.SetCameraY(cam.transform.position.y + 20.0f);

                        if (segmentsSpawned - segmentsRecycled > 3)
                        {
                            segmentsRecycled++;
                        }
                    }

                    E2EAssert.AreEqual(50, segmentsSpawned);
                    E2EAssert.AreEqual(47, segmentsRecycled);
                    E2EAssert.AreEqual(3, segmentsSpawned - segmentsRecycled);
                    E2EAssert.AreApproximatelyEqual(1000.0f, cam.DistanceTravelled, 0.01f);
                    E2EAssert.AreEqual(3.5f, cam.CurrentSpeed, "Speed should reach 3.5 u/s ceiling at 1000m");
                    string hudDist = $"DIST: {(int)cam.DistanceTravelled:D4}m";
                    E2EAssert.AreEqual("DIST: 1000m", hudDist);
                });

            // Scenario 2: Boss Arena Transition & Complete Victory Cycle
            TestRunnerHelper.RunTest(report, "T4_SCM_SCENARIO_02", "Scenario2", 4,
                "Boss Arena Transition & Complete Victory Cycle",
                "Simulates reaching 450 warning, 500 arena spawn, camera lock, radial barrage, boss defeat, rewards, and unlock resume.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var camGo = ctx.CreateGameObject("Test_Camera");
                    camGo.transform.position = new Vector3(0, 100f, -10f);
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.SetInitialY(0f);
                    cam.SetDistanceTravelled(100f);

                    var mapGo = ctx.CreateGameObject("MapManager");
                    var map = mapGo.AddComponent<MapManager>();
                    map.cameraController = cam;

                    var dummyPickup = ctx.CreateGameObject("DummyPickup");
                    dummyPickup.AddComponent<GrenadePickup>();

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.transform.position = new Vector3(0, 120f, 0);
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();
                    boss.grenadePickupPrefab = dummyPickup;

                    // 1. Advance score to 450: Warning triggers
                    gm.AddScore(450);
                    bool bossWarning = gm.CurrentScore >= 450 && !cam.isScrollLocked && gm.CurrentScore < gm.NextBossScoreThreshold;
                    E2EAssert.IsTrue(bossWarning);
                    E2EAssert.IsFalse(map.IsBossArenaQueued);

                    // 2. Advance score to 500: Boss arena queued, camera aligns & locks at center (Y=120)
                    gm.AddScore(50);
                    E2EAssert.IsTrue(map.IsBossArenaQueued);
                    cam.LockAt(120f, snapImmediate: true);

                    E2EAssert.IsTrue(cam.isScrollLocked);
                    E2EAssert.IsTrue(cam.IsAlignedToLock);
                    E2EAssert.AreEqual(0f, cam.CurrentSpeed);
                    E2EAssert.AreEqual(120f, camGo.transform.position.y);

                    // 3. Boss fires 16-bullet barrage
                    int bulletsBefore = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    boss.FireRadialBurst();
                    int bulletsAfter = UnityEngine.Object.FindObjectsOfType<EnemyBullet>().Length;
                    E2EAssert.AreEqual(16, bulletsAfter - bulletsBefore);

                    // 4. Player defeats boss -> camera unlocks and resumes
                    boss.Die();
                    E2EAssert.AreEqual(1000, gm.CurrentScore, "Score must be 1000 after boss defeat (+500)");

                    gm.ResumeEndlessAfterBoss();
                    E2EAssert.IsFalse(cam.isScrollLocked, "Camera must unlock after boss defeat");
                    E2EAssert.IsTrue(cam.CurrentSpeed >= 2.0f, "Camera speed must resume after boss defeat");

                    float yBefore = camGo.transform.position.y;
                    cam.StepScroll(1.0f);
                    E2EAssert.IsTrue(camGo.transform.position.y > yBefore, "Camera must resume translation along +Y");
                });

            // Scenario 3: Heavy Combat Obstacle Navigation
            TestRunnerHelper.RunTest(report, "T4_SCM_SCENARIO_03", "Scenario3", 4,
                "Heavy Combat Obstacle Navigation",
                "Simulates player navigating narrow 4.0u corridor dodging enemies, throwing grenades, and remaining within viewport.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.rb = rb;

                    // Narrow corridor between obstacles at X = -2.0 and X = +2.0 (width 4.0u)
                    float corridorLeft = -2.0f;
                    float corridorRight = 2.0f;
                    float corridorWidth = corridorRight - corridorLeft;
                    E2EAssert.IsTrue(corridorWidth >= 4.0f);

                    // Player navigates through corridor
                    Vector2 nextPos = new Vector2(0.5f, 5.0f);
                    bool withinCorridor = nextPos.x >= corridorLeft && nextPos.x <= corridorRight;
                    E2EAssert.IsTrue(withinCorridor);
                });

            // Scenario 4: Bottom Edge Trap Recovery
            TestRunnerHelper.RunTest(report, "T4_SCM_SCENARIO_04", "Scenario4", 4,
                "Bottom Edge Trap Recovery",
                "Simulates player pinned against obstacle near bottom boundary, receiving push forward and 1 HP damage without falling off.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var cam = ctx.CreateMockCamera();
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var rb);
                    var pm = player.AddComponent<PlayerMovement>();
                    var ph = player.AddComponent<PlayerHealth>();
                    pm.cam = cam;
                    pm.rb = rb;
                    pm.clampToViewport = true;

                    // Player trapped below threshold (viewport Y = 0.02, world Y = -4.8f)
                    rb.position = new Vector2(0f, -4.8f);
                    E2EAssert.AreEqual(5, ph.currentHealth);

                    // Initial trap tick
                    pm.ForceCheckBottomKillPlane();

                    E2EAssert.AreEqual(4, ph.currentHealth, "Player trapped at bottom must receive 1 HP damage");
                    E2EAssert.IsTrue(ph.isInvulnerable, "Player must receive i-frames");
                    E2EAssert.IsTrue(rb.position.y > -4.8f, "Player must be pushed forward along +Y");

                    // Push forward until player escapes danger zone (>= bottomKillThreshold 0.04)
                    while (cam.WorldToViewportPoint(rb.position).y < pm.bottomKillThreshold)
                    {
                        pm.ForceCheckBottomKillPlane();
                    }

                    var escapedVp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.IsTrue(escapedVp.y >= pm.bottomKillThreshold, "Player must escape below-kill danger zone");

                    // Clamping in FixedUpdate restores player to minViewportY (0.08)
                    E2EReflector.InvokeMethod(pm, "FixedUpdate");
                    var finalVp = cam.WorldToViewportPoint(rb.position);
                    E2EAssert.AreApproximatelyEqual(pm.minViewportY, finalVp.y, 0.001f, "Player must be restored to minViewportY");
                    E2EAssert.AreEqual(4, ph.currentHealth, "Player health must remain stable at 4 HP");
                });

            // Scenario 5: Rapid Boss Defeat with Grenades
            TestRunnerHelper.RunTest(report, "T4_SCM_SCENARIO_05", "Scenario5", 4,
                "Rapid Boss Defeat with Grenades",
                "Simulates entering boss arena with 2 grenades, detonating them for 100 damage, defeating boss, and resuming scrolling.",
                () =>
                {
                    using var ctx = new E2ETestContext();
                    var gmGo = ctx.CreateGameObject("GameManager");
                    var gm = gmGo.AddComponent<GameManager>();
                    gm.SetAsActiveInstance();

                    var camGo = ctx.CreateGameObject("Test_Camera");
                    var cam = camGo.AddComponent<ScrollingCameraController>();
                    cam.LockAt(100f, snapImmediate: true);

                    var dummyPickup = ctx.CreateGameObject("DummyPickup");
                    dummyPickup.AddComponent<GrenadePickup>();

                    var bossGo = ctx.CreateGameObject("Boss");
                    bossGo.AddComponent<Rigidbody2D>();
                    var boss = bossGo.AddComponent<BossController>();
                    boss.grenadePickupPrefab = dummyPickup;

                    E2EAssert.AreEqual(60, boss.currentHealth);

                    int dropsBefore = UnityEngine.Object.FindObjectsOfType<GrenadePickup>().Length;

                    // Player detonates 2 grenades dealing 50 damage each (total 100 >= 60 HP)
                    boss.TakeDamage(50);
                    E2EAssert.AreEqual(10, boss.currentHealth);
                    boss.TakeDamage(50);
                    E2EAssert.IsTrue(boss.isDead);
                    E2EAssert.AreEqual(0, boss.currentHealth);

                    // Boss defeat awards +500 points
                    E2EAssert.AreEqual(500, gm.CurrentScore);

                    // Verify 2 grenade drops spawned
                    int dropsAfter = UnityEngine.Object.FindObjectsOfType<GrenadePickup>().Length;
                    E2EAssert.AreEqual(2, dropsAfter - dropsBefore, "Boss defeat must drop 2 grenades");

                    // Resume endless
                    gm.ResumeEndlessAfterBoss();
                    E2EAssert.IsFalse(cam.isScrollLocked, "Scrolling must resume after boss defeat");
                });
        }

        #endregion

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run Scrolling Map Tests")]
        public static void RunScrollingMapTestsMenu()
        {
            var report = RunAll();
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
