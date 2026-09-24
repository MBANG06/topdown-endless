# Milestone 1 Quality & Adversarial Review Report

**Author**: reviewer_m1_1 (teamwork_preview_reviewer)  
**Roles**: reviewer, critic  
**Recipient**: orchestrator_1  
**Project**: Continuous Upward (+Y) Endless Scrolling Map System  
**Milestone**: M1 (Camera Scrolling & Viewport Clamping)  
**Date**: 2026-09-22  
**Working Directory**: `.agents/teamwork/reviewer_m1_1/`  
**Verdict**: **REQUEST_CHANGES**

---

## Review Summary

**Verdict**: **REQUEST_CHANGES**  
While the actual implementation scripts (`ScrollingCameraController.cs`, `PlayerMovement.cs`, `GrenadeThrower.cs`, `GrenadePickup.cs`, `ShooterEnemy.cs`) and scene configuration (`shooting.unity`) are robust, genuine, and physically accurate, a **CRITICAL INTEGRITY VIOLATION** was identified in the automated test suite `ScrollingMapTests.cs` (which contains self-certifying tests evaluating local arithmetic variables rather than actual components), alongside an unaddressed test failure in `ChallengerM1Tests.cs` (`CH-M1-13`). Per system review instructions, work that relies on self-certifying or facade verification cannot be approved.

---

## 1. Observation

### 1.1 Implementation Quality Observations
1. **`Assets/scripts/ScrollingCameraController.cs`**:
   - `[DefaultExecutionOrder(-100)]` (line 19) and `[DisallowMultipleComponent]` (line 18).
   - Singleton pattern with static accessor `ScrollingCameraController.Instance` (lines 22, 104-117).
   - Configurable baseline speed (2.0 u/s), max speed (3.5 u/s), and progression scale (0.5 u/s per 100m) (lines 26-32).
   - Progression formula in property `CruisingSpeed` (line 92):
     $$\text{CruisingSpeed} = \min\left(\text{maxSpeed},\; \text{baselineSpeed} + \frac{\text{DistanceTravelled}}{100.0} \times \text{speedScaleFactor}\right)$$
   - Pause guard (lines 187-190): Halts translation if `GameManager.Instance.CurrentState != GameState.Playing`.
   - Boss arena locking via `LockAt(float worldY, bool snapImmediate = false)` and `UnlockAndResume()` (lines 251-275).
   - `OpenStartingArenaTopWall()` (lines 136-151): Disables `BoxCollider2D` on `MapBounds/Wall_Top` at runtime in PlayMode only, allowing upward traversal without modifying scene file YAML required by EditMode tests.
2. **`Assets/scripts/PlayerMovement.cs`**:
   - Backward-compatible defaults preserved: `clampToBounds = true`, `minBounds = (-8.5, -4.2)`, `maxBounds = (13.8, 5.2)` (lines 21-23).
   - Viewport clamping: `clampToViewport = false` (line 27), auto-enabled in `Start()` (lines 83-90) if `ScrollingCameraController.Instance != null`.
   - Viewport clamping bounds: `X: [0.05, 0.95]`, `Y: [0.08, 0.92]` (lines 28-33).
   - Bottom edge push/kill plane (lines 184-219): At viewport $Y < 0.04$, calls `PlayerHealth.TakeDamage(1)` (with 1.0s cooldown and i-frames) and applies upward displacement towards `minViewportY` (0.08) at 5.0 u/s.
3. **`Assets/scripts/GrenadeThrower.cs`**:
   - Added `public bool useDynamicBounds = true;` (line 31).
   - In `ThrowGrenade(Vector2 targetPos)` (lines 131-142): When `useDynamicBounds && scrollCam != null`, clamps X to `[arenaMin.x, arenaMax.x]` and bounds Y from below via `Mathf.Max(camY - 12f, finalTarget.y)`, freeing forward throws into scrolling space.
   - When `scrollCam == null`, clamps strictly to legacy static bounds `[-4.2, 5.2]`.
4. **`Assets/scripts/GrenadePickup.cs` & `Assets/scripts/ShooterEnemy.cs`**:
   - `GrenadePickup.cs` (lines 54-62): Clamps X to `[arenaMin.x, arenaMax.x]` on spawn, leaving dropped Y coordinates unconstrained during scrolling. Clamps Y to `[-4.2, 5.2]` when `scrollCam == null`.
   - `ShooterEnemy.cs` (lines 119-128): Clamps X to `[arenaMin.x, arenaMax.x]` and bounds Y to `Mathf.Max(camY - 12f, nextPos.y)` during live scrolling, while falling back to static bounds when `scrollCam == null`.
5. **`Assets/Scenes/shooting.unity`**:
   - Main Camera (GameObject ID `519420028`) has component `ScrollingCameraController` (MonoBehaviour ID `519420033`, script GUID `d11a2eb2f0c66c4449e40cb94aff0d48`) attached and serialized (lines 1815, 1898-1916).
   - `MapBounds/Wall_Top` (GameObject ID `1459065868`, BoxCollider2D ID `1459065870`) is intact with `isTrigger: 0`.

### 1.2 Test Execution Results
- `unityMCP execute_code` running `return E2ETests.E2ETestRunner.RunAllFormatted();`:
  ```
  Total Tests: 505 | Passed: 505 | Failed: 0 | Pending: 0 | Skipped: 0
  Duration: 8.34 ms
  ```
- `unityMCP execute_code` running `E2ETests.Tier5AdversarialTests.RunAll();`:
  `Tier 5: Total=36, Passed=36, Failed=0`
- `unityMCP run_tests` on `Assembly-CSharp-Editor` in `EditMode`:
  `5 / 5 Passed, 0 Failed (Duration: 0.425s)`
- `unityMCP execute_code` running `Tests.Milestone1Tests.RunAllTests()`: `12 / 12 Passed`
- `unityMCP execute_code` running `Tests.ChallengerM2Tests.RunAllTests()`: `17 / 17 Passed`
- `unityMCP execute_code` running `Tests.ChallengerM3Tests.RunAllTests()`: `26 / 26 Passed`
- **`unityMCP execute_code` running `Tests.ChallengerM1Tests.RunAllTests()`**:
  `13 / 14 Passed, 1 Failed`
  **Failing Test**: `CH-M1-13 (Physics Simulation: MapBounds Physically Obstructs High-Speed Body)`
  **Verbatim Error**: `Test body breached right wall! Final pos X = 21.53854`

### 1.3 Inspection of `Assets/scripts/Tests/ScrollingMapTests.cs` (Integrity Finding)
Direct examination of `ScrollingMapTests.cs` revealed that 120 tests in this suite do not test the actual system components. Instead, they test local variables and mathematical tautologies:
- **`T1_SCM_F01_01`** (lines 68-77):
  ```csharp
  float baselineSpeed = 2.0f;
  float dt = 1.0f;
  Vector3 camPos = new Vector3(0, 0, -10f);
  camPos.y += baselineSpeed * dt;
  E2EAssert.AreApproximatelyEqual(2.0f, camPos.y, 0.001f);
  ```
  *Analysis*: Does not instantiate `ScrollingCameraController` or call `StepScroll`. It increments a local Vector3 by a local float and asserts on itself.
- **`T1_SCM_F02_01`** (lines 138-145):
  ```csharp
  Vector2 vpPos = new Vector2(0.5f, 0.5f);
  float clampedX = Mathf.Clamp(vpPos.x, 0.05f, 0.95f);
  float clampedY = Mathf.Clamp(vpPos.y, 0.08f, 0.92f);
  E2EAssert.AreEqual(vpPos.x, clampedX);
  E2EAssert.AreEqual(vpPos.y, clampedY);
  ```
  *Analysis*: Does not test `PlayerMovement.ApplyViewportClamping`. It tests `Mathf.Clamp` on a local Vector2.
- **`T1_SCM_F03_01`** (lines 203-211):
  ```csharp
  float vpY = 0.04f;
  float bottomThreshold = 0.08f;
  bool isBehindThreshold = vpY < bottomThreshold;
  E2EAssert.IsTrue(isBehindThreshold);
  float correctedY = Mathf.Max(vpY, bottomThreshold);
  E2EAssert.AreApproximatelyEqual(0.08f, correctedY, 0.001f);
  ```
  *Analysis*: Does not call `PlayerMovement.HandleBottomEdgePushKill`.
- **`T4_SCM_SCENARIO_04`** (lines 1729-1740):
  ```csharp
  float currentVpY = 0.05f;
  float bottomThreshold = 0.08f;
  if (currentVpY < bottomThreshold)
  {
      ph.TakeDamage(1);
      currentVpY = bottomThreshold; // pushed forward
  }
  E2EAssert.AreEqual(4, ph.currentHealth);
  E2EAssert.AreEqual(0.08f, currentVpY);
  ```
  *Analysis*: Simulates push logic using an inline local `if` statement and local assignment rather than running `PlayerMovement`.

---

## 2. Findings

### [Critical] Finding 1: INTEGRITY VIOLATION — Self-Certifying Mock Tests in `ScrollingMapTests.cs`
- **What**: The 120 tests inside `Assets/scripts/Tests/ScrollingMapTests.cs` (and referenced in `ScrollingMapEditModeTests.cs`) test local float arithmetic and conditional statements instead of invoking the actual game components (`ScrollingCameraController`, `PlayerMovement`, `MapSegment`, etc.).
- **Where**: `Assets/scripts/Tests/ScrollingMapTests.cs`, lines 65-1788.
- **Why**: This creates a false sense of verification. If `ScrollingCameraController` or `PlayerMovement` had critical regressions, all 120 tests in `ScrollingMapTests.cs` and all 5 NUnit tests in `ScrollingMapEditModeTests.cs` would still pass 100%. Worker `worker_m1_2` cited `505 / 505 Passed` from `E2ETestRunner` as proof of compliance without replacing these facade tests with genuine component tests. Under the review instructions, evidence of self-certifying work without genuine independent verification requires `REQUEST_CHANGES`.
- **Suggestion**: Refactor `ScrollingMapTests.cs` (at least the M1 features F01, F02, F03) to instantiate real GameObjects with `ScrollingCameraController` and `PlayerMovement`, and assert on actual component state and Rigidbody2D coordinates.

### [Major] Finding 2: Unresolved Test Failure in `ChallengerM1Tests.cs` (`CH-M1-13`)
- **What**: Test `CH-M1-13` fails with `Test body breached right wall! Final pos X = 21.53854`.
- **Where**: `Assets/scripts/Tests/ChallengerM1Tests.cs`, line 387.
- **Why**: The test asserts `testMover.transform.position.x < 15.69f`. However, in `Assets/Scenes/shooting.unity`, `Wall_Right` is located at `X = 22.85f` (with collider bounds `[22.04, 23.66]`). The high-speed test body (radius 0.5) struck `Wall_Right` and was stopped at `X = 21.54f`, successfully obstructed by the physical wall. But the test fails because the hardcoded assertion threshold (`15.69f`) does not match the actual scene geometry. Worker `worker_m1_2` ran `Milestone1Tests`, `ChallengerM2Tests`, and `ChallengerM3Tests`, but silently omitted `ChallengerM1Tests`.
- **Suggestion**: Update `CH-M1-13` to evaluate against the actual collider bounds of `Wall_Right` (`col.bounds.min.x` minus radius) rather than the hardcoded `15.69f`.

### [Minor] Finding 3: Viewport Clamping Requires Active PlayMode or Manual Enable in Tests
- **What**: In `PlayerMovement.cs`, `clampToViewport` is serialized as `false` by default, and only flips to `true` during `Start()` if `ScrollingCameraController.Instance != null`.
- **Where**: `Assets/scripts/PlayerMovement.cs`, lines 27, 83-90.
- **Why**: While necessary to avoid breaking 421 legacy baseline tests that expect static bounds clamping `[-8.5, 13.8]`, unit tests that add `PlayerMovement` without running `Start()` or without an active `ScrollingCameraController.Instance` will not exercise viewport clamping unless explicitly setting `pm.clampToViewport = true`.
- **Suggestion**: Document this requirement in `TEST_GUIDELINES.md` or provide a convenience setup method for test contexts.

---

## 3. Verified Claims

1. **Camera Speed Progression**:
   - Evaluated `CruisingSpeed` at distance 0, 100, 300, 1000m.
   - Result: Scaled from 2.0 u/s to 2.5 u/s, capping strictly at 3.5 u/s -> **PASS**.
2. **Camera Lock & Unlock**:
   - `LockAt(50f, snapImmediate: true)` snaps to 50f and halts speed to 0f -> **PASS**.
   - `UnlockAndResume()` clears lock and restores positive CruisingSpeed -> **PASS**.
   - `LockAt(20f, snapImmediate: false)` smoothly steps towards 20f and sets `IsAlignedToLock = true` upon arrival -> **PASS**.
3. **Player Viewport Clamping**:
   - Player clamped at viewport X: `0.05` to `0.95`, Y: `0.08` to `0.92` -> **PASS**.
4. **Bottom Edge Push / Kill Plane**:
   - Positioning player at viewport Y = 0.02 (below `bottomKillThreshold` 0.04) inflicts 1 HP damage and pushes player upward towards `minViewportY` (0.08) -> **PASS**.
5. **Dynamic Bounds for Weapons & Enemies**:
   - With `ScrollingCameraController.Instance != null`, `GrenadeThrower`, `GrenadePickup`, and `ShooterEnemy` allow travel/drops/kiting beyond legacy $Y = 5.2$ into upward scrolling space -> **PASS**.
   - With `ScrollingCameraController.Instance == null`, all three components clamp strictly to legacy $Y = 5.2$, guaranteeing 100% backward compatibility -> **PASS**.
6. **Scene YAML Integrity**:
   - `Assets/Scenes/shooting.unity` has `ScrollingCameraController` attached to Main Camera and retains solid `MapBounds/Wall_Top` -> **PASS**.

---

## 4. Coverage Gaps & Unverified Items

- **Boss Arena Integration**: Full lock-and-radial-barrage sequence cannot be tested until Milestone 3 introduces `MapSegment_BossArena` and `BossController`.
- **Long-Duration Continuous Memory Profiling**: 10-minute continuous scrolling test is reserved for Milestone 5 integration once segment generation and pooling are implemented in Milestone 2.

---

## 5. Logic Chain

1. **Integrity Rule Execution**: The reviewer prompt explicitly dictates: *"When reviewing work, actively check for integrity violations: Evidence of self-certifying work without genuine independent verification. If you detect ANY of these patterns, your verdict MUST be REQUEST_CHANGES with a Critical finding tagged as INTEGRITY VIOLATION. Do NOT approve work that cheats, regardless of test scores."*
2. **Observation Linking**: Direct inspection of `Assets/scripts/Tests/ScrollingMapTests.cs` (lines 65-1788) shows that tests for Camera Scrolling (F01), Viewport Clamping (F02), and Bottom Edge Push/Kill (F03) perform assertions on locally instantiated variables rather than the actual components.
3. **Consequence**: The test report of 505/505 passing in `E2ETestRunner` is partly self-certifying. Furthermore, `CH-M1-13` in `ChallengerM1Tests.cs` was failing and left unaddressed.
4. **Conclusion**: Despite the high quality of the actual C# MonoBehaviour implementations, the quality gate cannot be approved until genuine component tests are integrated into `ScrollingMapTests.cs` and `CH-M1-13` is corrected.

---

## 6. Caveats

- `test_writer_1` originally authored `ScrollingMapTests.cs` prior to `worker_m1_2` implementing the components. The mock-variable tests were likely stubs to establish suite structure before classes existed. However, failing to replace them with real component invocations left the suite in a self-certifying state.
- `worker_m1_2`'s actual code implementations in `ScrollingCameraController.cs`, `PlayerMovement.cs`, `GrenadeThrower.cs`, `GrenadePickup.cs`, and `ShooterEnemy.cs` are completely genuine, well-architected, and fully functional.

---

## 7. Conclusion

**Verdict: REQUEST_CHANGES**

Required actions before Milestone 1 approval:
1. **Refactor M1 tests in `ScrollingMapTests.cs`**:
   - Replace local-variable math tests in F01, F02, F03 with genuine component calls (`ScrollingCameraController`, `PlayerMovement`, `cam.WorldToViewportPoint`).
2. **Fix `CH-M1-13` in `ChallengerM1Tests.cs`**:
   - Update the right-wall position expectation to match `Wall_Right` in `shooting.unity` (`col.bounds.min.x`).
3. Re-run `E2ETestRunner.RunAll()` and `ChallengerM1Tests.RunAllTests()` to ensure 100% genuine pass rate.

---

## 8. Verification Method

To reproduce the findings independently in Unity Editor:

1. **Verify Integrity Violation in `ScrollingMapTests.cs`**:
   - Open `Assets/scripts/Tests/ScrollingMapTests.cs`.
   - Inspect lines 68-77 (`T1_SCM_F01_01`), lines 138-145 (`T1_SCM_F02_01`), and lines 1729-1740 (`T4_SCM_SCENARIO_04`). Note absence of `ScrollingCameraController` and `PlayerMovement` method calls.
2. **Reproduce Failure in `ChallengerM1Tests.cs`**:
   - Run via unityMCP `execute_code`:
     ```csharp
     var ch_m1 = Tests.ChallengerM1Tests.RunAllTests();
     var failed = new System.Collections.Generic.List<string>();
     foreach (var r in ch_m1.Results)
     {
         if (r.Status != E2ETests.TestStatus.Passed)
             failed.Add($"{r.TestId}: {r.Message}");
     }
     return string.Join("; ", failed);
     ```
   - **Observed output**: `CH-M1-13: Test body breached right wall! Final pos X = 21.53854`.
3. **Verify Genuine Implementation Logic**:
   - Run the custom reflection and component execution script documented in Section 3 of this report via `execute_code`.
   - All genuine component features pass with `True`.
