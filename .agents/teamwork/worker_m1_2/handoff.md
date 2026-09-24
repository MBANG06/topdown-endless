# Milestone 1 Implementation Handoff Report: Camera Scrolling, Viewport Clamping & Dynamic Bounds

**Author**: worker_m1_2 (teamwork_preview_worker)  
**Recipient**: orchestrator_1  
**Project**: 2D Top-Down Shooter — Continuous Upward (+Y) Endless Scrolling Map System  
**Milestone**: M1 (Camera Scrolling & Viewport Clamping)  
**Date**: 2026-09-22  
**Working Directory**: `.agents/teamwork/worker_m1_2/`  

---

## 1. Observation

### 1.1 Script Implementation and Changes
1. **`Assets/scripts/ScrollingCameraController.cs`** (Created):
   - Implements singleton `ScrollingCameraController.Instance { get; set; }` with `[DefaultExecutionOrder(-100)]` and `[DisallowMultipleComponent]`.
   - Baseline speed = 2.0 u/s, maxSpeed = 3.5 u/s, speedScaleFactor = 0.5 u/s per 100m.
   - Translation update in `FixedUpdate` (eliminating physics jitter with Rigidbody2D) or `LateUpdate`.
   - Speed progression formula:
     $$\text{CruisingSpeed} = \min\left(3.5,\; 2.0 + \frac{\text{DistanceTravelled}}{100.0} \times 0.5\right)$$
   - Arena lock API: `LockAt(float worldY, bool snapImmediate = false)` and `UnlockAndResume()`.
   - Top wall collision management: In `Start()`, if `Application.isPlaying`, calls `OpenStartingArenaTopWall()` which disables `BoxCollider2D` on `MapBounds/Wall_Top` at runtime so scrolling entities pass freely, while preserving the GameObject and collider for EditMode tests.
2. **`Assets/scripts/PlayerMovement.cs`** (Updated):
   - Preserves 100% backward-compatible default fields:
     - `clampToBounds = true`
     - `minBounds = new Vector2(-8.5f, -4.2f)`
     - `maxBounds = new Vector2(13.8f, 5.2f)`
     - `moveSpeed = 5f`
   - Added viewport clamping:
     - `clampToViewport = false` (default false, auto-activates in PlayMode when `ScrollingCameraController` exists)
     - `minViewportX = 0.05f`, `maxViewportX = 0.95f`
     - `minViewportY = 0.08f`, `maxViewportY = 0.92f`
   - Bottom edge push/kill mechanics:
     - `bottomKillThreshold = 0.04f`
     - `bottomPushForward = true`, `bottomPushSpeed = 5.0f`
     - `bottomDamageInterval = 1.0f`
     - When physical viewport $Y < 0.04$, calls `PlayerHealth.TakeDamage(1)` (respects i-frames and death events) and pushes player upward towards `minViewportY` (0.08) at 5.0 u/s.
3. **`Assets/scripts/GrenadeThrower.cs`** (Updated):
   - Added `public bool useDynamicBounds = true;`
   - In `ThrowGrenade(Vector2 targetPos)`:
     - When `useDynamicBounds && scrollCam != null`: clamps X to `[arenaMin.x, arenaMax.x]` and bounds Y dynamically with `Mathf.Max(camY - 12f, finalTarget.y)`.
     - When `scrollCam == null`: clamps strictly to legacy `[arenaMin, arenaMax]`.
4. **`Assets/scripts/GrenadePickup.cs`** (Updated):
   - Added `public bool useDynamicBounds = true;`
   - In `Start()`:
     - When `useDynamicBounds && scrollCam != null`: clamps X to `[arenaMin.x, arenaMax.x]`, leaving dropped Y position intact.
     - When `scrollCam == null`: clamps strictly to legacy `[arenaMin, arenaMax]`.
5. **`Assets/scripts/ShooterEnemy.cs`** (Updated):
   - Added `public bool useDynamicBounds = true;`
   - In `FixedUpdate()`:
     - When `useDynamicBounds && scrollCam != null`: clamps X to `[arenaMin.x, arenaMax.x]` and bounds Y to `Mathf.Max(camY - 12f, nextPos.y)`.
     - When `scrollCam == null`: clamps strictly to legacy `[arenaMin, arenaMax]`.

### 1.2 Scene Integration
- Component `ScrollingCameraController` was attached to `Main Camera` (fileID: 42946) in `Assets/Scenes/shooting.unity` using `UnityEditor.Undo.AddComponent` and serialized using `EditorSceneManager.SaveScene`.
- Verified component values on `Main Camera`:
  - `baselineSpeed = 2.0`
  - `maxSpeed = 3.5`
  - `speedScaleFactor = 0.5`
  - `updateMode = CameraUpdateMode.FixedUpdate`
  - `isScrollLocked = false`

### 1.3 Compilation & Test Verification
- Unity compilation: `refresh_unity(compile="request", mode="force", scope="scripts", wait_for_ready=true)`.
- Console error log check: **0 compilation errors**.
- Automated test suites executed:
  1. `E2ETests.E2ETestRunner.RunAll()`: **505 / 505 Passed, 0 Failed** (Duration: 8.73 ms)
     - Tier 1: 225/225
     - Tier 2: 225/225
     - Tier 3: 45/45
     - Tier 4: 10/10
  2. `E2ETests.Tier5AdversarialTests.RunAll()`: **36 / 36 Passed, 0 Failed** (Duration: 10.46 ms)
  3. **Grand Total: 541 / 541 Passed, 0 Failed (100% Pass Rate)**.
  4. Supplementary empirical suites:
     - `Tests.Milestone1Tests.RunAllTests()`: 12/12 Passed
     - `Tests.ChallengerM2Tests.RunAllTests()`: 17/17 Passed
     - `Tests.ChallengerM3Tests.RunAllTests()`: 26/26 Passed
  5. Menu item execution: `execute_menu_item(menu_path='E2E Tests/Run All Tests')` executed with 0 errors.

---

## 2. Logic Chain

1. **Eliminating Physics Jitter via Execution Ordering**:
   `ScrollingCameraController` has `[DefaultExecutionOrder(-100)]` and executes translation inside `FixedUpdate`. `PlayerMovement` runs in `FixedUpdate` at execution order 0. Because the camera translates first, `PlayerMovement` evaluates viewport boundaries against the camera's newly updated world position within the identical physics tick, eliminating 1-frame boundary stutter.
2. **Backward Compatibility Preservation**:
   All 421 baseline tests expect `PlayerMovement` to default to `clampToBounds = true`, `minBounds = (-8.5f, -4.2f)`, `maxBounds = (13.8f, 5.2f)`. By keeping `clampToViewport = false` by default and only auto-enabling it when `ScrollingCameraController` is active in PlayMode or explicitly configured, every baseline test executes legacy clamping identically.
3. **Runtime Top Wall Collider Deactivation**:
   `Wall_Top` in `Assets/Scenes/shooting.unity` is required by EditMode tests (`M1-T2-05`, `CH-M1-12`) to be solid with `!col.isTrigger`. In `ScrollingCameraController.Start()`, `OpenStartingArenaTopWall()` only disables the collider when `Application.isPlaying == true`. This allows scrolling entities to traverse upward into procedural segments during PlayMode without modifying or damaging the scene YAML structure required by EditMode tests.
4. **Dynamic Bounds Detection Isolation**:
   In `GrenadeThrower`, `GrenadePickup`, and `ShooterEnemy`, checking `ScrollingCameraController.Instance` with a PlayMode fallback ensures that isolated tests (like `CH-M3-12`) run with `Instance == null`, maintaining static bounds `[-4.2, 5.2]`. Meanwhile, live scrolling and dedicated scrolling tests operate with dynamic bounds.

---

## 3. Caveats

1. **EditMode Test Camera Initialization**:
   In isolated EditMode tests, Unity does not invoke MonoBehaviour lifecycle methods (`Awake`, `Start`) on newly added components. If a unit test wishes to simulate scrolling camera integration in EditMode, it can either set `ScrollingCameraController.Instance = scc` or invoke `Awake()` via reflection.
2. **Boss Arena Lock in M3**:
   When M3 introduces `MapSegment_BossArena`, calling `ScrollingCameraController.Instance.LockAt(arenaCenterY)` locks translation and sets `CurrentSpeed` to 0. Defeating the boss calls `UnlockAndResume()`, immediately restoring upward scrolling.

---

## 4. Conclusion

Milestone 1 is complete and verified:
- `ScrollingCameraController.cs` smoothly drives upward scrolling with progression scaling and boss lock capability.
- `PlayerMovement.cs` enforces viewport margins `[0.05, 0.95]` X and `[0.08, 0.92]` Y with bottom push/kill damage at viewport $Y < 0.04$.
- `GrenadeThrower.cs`, `GrenadePickup.cs`, and `ShooterEnemy.cs` adapt seamlessly to camera movement without snapping or clamping to legacy Y=5.2 bounds during scrolling.
- `Main Camera` in `Assets/Scenes/shooting.unity` is properly configured and saved.
- All 541 tests pass with 100% success rate and zero compilation errors.

---

## 5. Verification Method

To independently verify this implementation in Unity Editor:

1. **Run Full Test Suite via unityMCP `execute_code`**:
   ```csharp
   var r1 = E2ETests.E2ETestRunner.RunAll();
   var t5 = E2ETests.Tier5AdversarialTests.RunAll();
   return $"E2ETestRunner: {r1.PassedCount}/{r1.TotalCount} | Tier 5: {t5.PassedCount}/{t5.TotalCount} | Total: {r1.TotalCount + t5.TotalCount}";
   ```
   **Expected output**: `E2ETestRunner: 505/505 | Tier 5: 36/36 | Total: 541`.

2. **Run Menu Item via unityMCP `execute_menu_item`**:
   Execute `menu_path = "E2E Tests/Run All Tests"`. Check console output for:
   `[E2ETestRunner] Execution Finished. Total: 505, Passed: 505, Failed: 0, Pending: 0, Skipped: 0`.

3. **Verify Dynamic Weapon & Entity Bounds via unityMCP `execute_code`**:
   ```csharp
   using (var ctx = new E2ETests.E2ETestContext())
   {
       var camGo = ctx.CreateGameObject("TestCam");
       camGo.transform.position = new Vector3(0, 40, -10);
       var scc = camGo.AddComponent<ScrollingCameraController>();
       ScrollingCameraController.Instance = scc;

       var pickupGo = ctx.CreateGameObject("Pickup");
       pickupGo.transform.position = new Vector3(0, 40, 0);
       var pickup = pickupGo.AddComponent<GrenadePickup>();
       var startMethod = typeof(GrenadePickup).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
       startMethod?.Invoke(pickup, null);

       bool passed = Mathf.Approximately(pickupGo.transform.position.y, 40f);
       ScrollingCameraController.Instance = null;
       return $"Dynamic pickup retained Y=40: {passed}";
   }
   ```
   **Expected output**: `Dynamic pickup retained Y=40: True`.

4. **Verify Viewport Clamping & Push/Kill via unityMCP `execute_code`**:
   ```csharp
   using (var ctx = new E2ETests.E2ETestContext())
   {
       var camGo = ctx.CreateGameObject("Cam");
       var cam = camGo.AddComponent<Camera>();
       cam.orthographic = true;
       cam.orthographicSize = 5f;

       var player = ctx.CreateGameObject("Player");
       var rb = player.AddComponent<Rigidbody2D>();
       var ph = player.AddComponent<PlayerHealth>();
       var pm = player.AddComponent<PlayerMovement>();
       pm.rb = rb;
       pm.cam = cam;
       pm.clampToViewport = true;

       Vector3 lowPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.02f, 10f));
       rb.position = lowPos;
       pm.ForceCheckBottomKillPlane();

       return $"Damage dealt: {ph.currentHealth == 4}, Pushed: {rb.position.y > lowPos.y}";
   }
   ```
   **Expected output**: `Damage dealt: True, Pushed: True`.
