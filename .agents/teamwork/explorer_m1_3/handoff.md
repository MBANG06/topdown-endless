# Milestone 1 Dynamic Weapon & Entity Bounds Handoff Report

## 1. Observation

### 1.1 Baseline Test Suite Status
- Verified via `unityMCP` `execute_code`:
  ```csharp
  var report = E2ETests.E2ETestRunner.RunAll(); // Total: 385, Passed: 385, Failed: 0
  var t5Report = new E2ETests.TestSuiteReport();
  E2ETests.Tier5AdversarialTests.RunAll(t5Report); // Total: 36, Passed: 36, Failed: 0
  ```
  Total baseline tests: **421 tests, 100% passing (0 failed, 0 pending, 0 skipped)**.

### 1.2 `GrenadeThrower.cs`
- File path: `Assets/scripts/GrenadeThrower.cs`
- Lines 28-31:
  ```csharp
  [Header("Arena Boundaries")]
  public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
  public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
  ```
- Lines 124-127:
  ```csharp
  // Ensure target is clamped within arena bounds
  finalTarget.x = Mathf.Clamp(finalTarget.x, arenaMin.x, arenaMax.x);
  finalTarget.y = Mathf.Clamp(finalTarget.y, arenaMin.y, arenaMax.y);
  ```
- Test dependencies:
  - `Assets/scripts/Tests/ChallengerM3Tests.cs` (lines 557-585, `CH-M3-19`):
    Asserts throw target clamped to `arenaMax.x <= 13.801f` and `arenaMax.y <= 5.201f`.
  - `Assets/scripts/Tests/Tier5AdversarialTests.cs` (lines 583-601, `T5_ADV_24`):
    Directly clamps using `thrower.arenaMin` and `thrower.arenaMax` and asserts equality to `(13.8f, 5.2f)`.

### 1.3 `GrenadePickup.cs`
- File path: `Assets/scripts/GrenadePickup.cs`
- Lines 16-18:
  ```csharp
  [Header("Arena Boundaries")]
  public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
  public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
  ```
- Lines 43-52:
  ```csharp
  private void Start()
  {
      // Clamp position inside arena bounds to ensure drops near perimeter stay inside playable arena
      Vector3 pos = transform.position;
      pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
      pos.y = Mathf.Clamp(pos.y, arenaMin.y, arenaMax.y);
      transform.position = pos;

      _startPosition = transform.position;
  }
  ```
- Test dependencies:
  - `Assets/scripts/Tests/Milestone3Tests.cs` (lines 58-77, `M3-03`):
    Spawns pickup at `(-15f, 10f, 0f)`, invokes `Start()`, asserts `pos.x >= -8.5f` and `pos.y <= 5.2f`.
  - `Assets/scripts/Tests/ChallengerM3Tests.cs` (lines 359-386, `CH-M3-12`):
    Iterates over extreme positions including `(100f, 100f, 0f)`, invokes `Start()`, asserts `pos.y in [-4.2, 5.2]`.

### 1.4 `ShooterEnemy.cs`
- File path: `Assets/scripts/ShooterEnemy.cs`
- Lines 13-18:
  ```csharp
  [Header("Shooter Distance & Kiting")]
  public float retreatDistance = 3.8f;
  public float advanceDistance = 5.5f;
  public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
  public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
  ```
- Lines 109-114 in `FixedUpdate()`:
  ```csharp
  Vector2 nextPos = currentPos + moveDir * moveSpeed * Time.fixedDeltaTime;

  // Clamp position inside arena boundaries
  nextPos.x = Mathf.Clamp(nextPos.x, arenaMin.x, arenaMax.x);
  nextPos.y = Mathf.Clamp(nextPos.y, arenaMin.y, arenaMax.y);
  ```
- Test dependencies:
  - `Assets/scripts/Tests/Tier5AdversarialTests.cs` (lines 449-468, `T5_ADV_18`):
    Directly checks `Mathf.Clamp(overKitePos.x, shooter.arenaMin.x, shooter.arenaMax.x)` equals `13.8f` and `5.2f`.
  - `Assets/scripts/Tests/ChallengerM2Tests.cs` (lines 495-528, `CH-M2-13`):
    Evaluates AI kiting thresholds (3.8u / 5.5u) without scrolling camera.

### 1.5 `MapBounds` and `Wall_Top` in `Assets/Scenes/shooting.unity`
- Scene YAML inspection:
  - `MapBounds` (fileID 1754246110): Local position `(-1.6485, -0.049026, 0)`, Scale `(1.6128, 1.6128, 1.6128)`.
  - `Wall_Top` (fileID 1459065868): Child of `MapBounds`, Local position `(2.69, 5.58, 0)`.
  - World Y bounds of `Wall_Top`: `min.y ≈ 8.14f`, `center.y ≈ 8.95f`, `max.y ≈ 9.76f`.
  - Components on `Wall_Top`: Only `Transform` and `BoxCollider2D` (no `SpriteRenderer`).
  - Collider: `BoxCollider2D` with size `(26, 1)`, `isTrigger = false`.
- Test assertions requiring `Wall_Top`:
  - `Assets/scripts/Tests/Milestone1Tests.cs` (lines 204-221, `M1-T2-05`):
    Asserts `mapBounds.transform.Find("Wall_Top") != null`, has `BoxCollider2D`, and `!col.isTrigger`.
  - `Assets/scripts/Tests/ChallengerM1Tests.cs` (lines 323-356, `CH-M1-12`):
    Asserts `Wall_Top` has `!col.isTrigger` and `topCol.bounds.min.y >= 5.0f`.
  - `Assets/scripts/Tests/ChallengerM1Tests.cs` (lines 412-434, `CH-M1-14`):
    Invokes `Bullet.HandleHit(topWall)`.
  - `Assets/scripts/Tests/ChallengerM2Tests.cs` (lines 124-148, `CH-M2-04`):
    Invokes `EnemyBullet.HandleHit(topWall)`.

---

## 2. Logic Chain

1. **Failure Mode under Scrolling**:
   In continuous upward (+Y) scrolling, camera and entities advance to coordinates far above Y = 5.2 (e.g. Y = 20, 50, 200, 1000):
   - In `GrenadeThrower`, any forward throw will be capped to `finalTarget.y <= 5.2f`, forcing grenades to fly backwards or clamp at 5.2f.
   - In `GrenadePickup`, any enemy killed at Y > 5.2 dropping a pickup will cause the pickup to snap from Y = 20+ down to Y = 5.2f in `Start()`.
   - In `ShooterEnemy`, as the player moves upward, the shooter trying to kite in `FixedUpdate()` will clamp `nextPos.y <= 5.2f`, pinning the shooter at Y = 5.2f.
   - In `shooting.unity`, `Wall_Top`'s solid `BoxCollider2D` spans across the entire room at Y ≈ 8.14f to 9.76f, which will physically block the player, enemies, and projectiles from moving into upcoming scrolling segments.

2. **Test Invariant Requirements**:
   - All 421 baseline tests MUST remain 100% passing.
   - None of the 421 existing tests instantiate `ScrollingCameraController`.
   - Tests `T5_ADV_24` and `T5_ADV_18` directly read public fields `arenaMin` and `arenaMax`. They must exist with default values `(-8.5f, -4.2f)` and `(13.8f, 5.2f)`.
   - Tests `CH-M3-12` and `M3-03` invoke `GrenadePickup.Start()` in isolation and assert Y <= 5.2f.
   - Tests `M1-T2-05` and `CH-M1-12` inspect `Wall_Top` in EditMode and require `col != null`, `!col.isTrigger`, and `bounds.min.y >= 5.0f`.

3. **Reconciliation Strategy**:
   - When `ScrollingCameraController.Instance != null` (active scrolling mode):
     - `GrenadeThrower`: clamps X to arena/segment width, but allows Y to scale dynamically with the scrolling camera (`Mathf.Max(camY - 12f, finalTarget.y)`).
     - `GrenadePickup`: clamps X to horizontal boundaries, but leaves Y at the dropped world coordinate (does not clamp to 5.2f).
     - `ShooterEnemy`: clamps X to corridor/arena limits, but allows Y to kite forward with camera progression (`Mathf.Max(camY - 12f, nextPos.y)`).
   - When `ScrollingCameraController.Instance == null` (isolated unit test mode):
     - `GrenadeThrower`, `GrenadePickup`, and `ShooterEnemy` execute legacy `Mathf.Clamp(..., arenaMin.y, arenaMax.y)`.
   - `Wall_Top` Handling:
     - DO NOT delete or alter `Wall_Top` in `Assets/Scenes/shooting.unity` scene YAML.
     - In `ScrollingCameraController.Start()` at runtime (`Application.isPlaying`), locate `Wall_Top` under `MapBounds` and set `topCol.enabled = false`.
     - In EditMode (`!Application.isPlaying`), `ScrollingCameraController.Start()` never runs, leaving `Wall_Top` solid with `bounds.min.y >= 5.0f`, completely satisfying `M1-T2-05`, `CH-M1-12`, `CH-M1-14`, and `CH-M2-04`.

---

## 3. Caveats

1. **Boss Arena (M3) Enclosure**:
   When the Boss Arena spawns in M3, `ScrollingCameraController.isScrollLocked` locks camera scrolling. The Boss Arena segment (`MapSegment_BossArena.prefab`) has its own side and top colliders. No manual hardcoded coordinate overrides are needed in `ShooterEnemy` or `GrenadeThrower` because physical obstacle colliders in the arena prefab naturally bound movement.
2. **Horizontal Segment Width**:
   Corridor width in standard segments is 15.0u (`X: -7.5` to `+7.5`). Clamping X to `[arenaMin.x, arenaMax.x]` (`[-8.5, 13.8]`) is safe and preserves existing bounds, while segment wall colliders physically enforce the corridor width.
3. **Unity Test Mode**:
   EditMode test runs do not invoke `MonoBehaviour.Start()` or `Update()` unless specifically called via reflection. Thus runtime deactivation of `Wall_Top` collider will never bleed into EditMode test executions.

---

## 4. Conclusion & Recommended Worker Implementation Plan

### 4.1 Changes to `Assets/scripts/GrenadeThrower.cs`
Add a public toggle `public bool useDynamicBounds = true;` and update `ThrowGrenade`:

```csharp
// In GrenadeThrower.cs
[Header("Arena Boundaries")]
public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
public bool useDynamicBounds = true;

// In ThrowGrenade(Vector2 targetPos):
Vector2 playerPos = transform.position;
Vector2 throwDirection = targetPos - playerPos;
Vector2 clampedOffset = Vector2.ClampMagnitude(throwDirection, maxThrowDistance);
Vector2 finalTarget = playerPos + clampedOffset;

var scrollCam = ScrollingCameraController.Instance ?? FindObjectOfType<ScrollingCameraController>();
if (useDynamicBounds && scrollCam != null)
{
    finalTarget.x = Mathf.Clamp(finalTarget.x, arenaMin.x, arenaMax.x);
    float camY = scrollCam.transform.position.y;
    finalTarget.y = Mathf.Max(camY - 12f, finalTarget.y);
}
else
{
    finalTarget.x = Mathf.Clamp(finalTarget.x, arenaMin.x, arenaMax.x);
    finalTarget.y = Mathf.Clamp(finalTarget.y, arenaMin.y, arenaMax.y);
}
```

### 4.2 Changes to `Assets/scripts/GrenadePickup.cs`
Add a public toggle `public bool useDynamicBounds = true;` and update `Start()`:

```csharp
// In GrenadePickup.cs
[Header("Arena Boundaries")]
public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
public bool useDynamicBounds = true;

private void Start()
{
    Vector3 pos = transform.position;

    var scrollCam = ScrollingCameraController.Instance ?? FindObjectOfType<ScrollingCameraController>();
    if (useDynamicBounds && scrollCam != null)
    {
        pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
    }
    else
    {
        pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
        pos.y = Mathf.Clamp(pos.y, arenaMin.y, arenaMax.y);
    }

    transform.position = pos;
    _startPosition = transform.position;
}
```

### 4.3 Changes to `Assets/scripts/ShooterEnemy.cs`
Add a public toggle `public bool useDynamicBounds = true;` and update `FixedUpdate()`:

```csharp
// In ShooterEnemy.cs
[Header("Shooter Distance & Kiting")]
public float retreatDistance = 3.8f;
public float advanceDistance = 5.5f;
public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
public bool useDynamicBounds = true;

// In FixedUpdate():
if (moveDir != Vector2.zero)
{
    Vector2 nextPos = currentPos + moveDir * moveSpeed * Time.fixedDeltaTime;

    var scrollCam = ScrollingCameraController.Instance ?? FindObjectOfType<ScrollingCameraController>();
    if (useDynamicBounds && scrollCam != null)
    {
        nextPos.x = Mathf.Clamp(nextPos.x, arenaMin.x, arenaMax.x);
        float camY = scrollCam.transform.position.y;
        nextPos.y = Mathf.Max(camY - 12f, nextPos.y);
    }
    else
    {
        nextPos.x = Mathf.Clamp(nextPos.x, arenaMin.x, arenaMax.x);
        nextPos.y = Mathf.Clamp(nextPos.y, arenaMin.y, arenaMax.y);
    }

    if (rb != null)
    {
        rb.MovePosition(nextPos);
    }
    else
    {
        transform.position = nextPos;
    }
    ...
```

### 4.4 Handling `Wall_Top` in `ScrollingCameraController.cs`
In `ScrollingCameraController.cs`:
```csharp
public static ScrollingCameraController Instance { get; private set; }

private void Awake()
{
    if (Instance == null) Instance = this;
}

private void OnDestroy()
{
    if (Instance == this) Instance = null;
}

private void Start()
{
    OpenStartingArenaTopWall();
}

public void OpenStartingArenaTopWall()
{
    var mapBounds = GameObject.Find("MapBounds");
    if (mapBounds != null)
    {
        var topWall = mapBounds.transform.Find("Wall_Top");
        if (topWall != null)
        {
            var col = topWall.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }
}
```

---

## 5. Verification Method

### 5.1 Verification Commands via `unityMCP` `execute_code`

1. **Verify Baseline Test Compatibility (421/421 Pass)**:
   ```csharp
   var report = E2ETests.E2ETestRunner.RunAll();
   var t5Report = new E2ETests.TestSuiteReport();
   E2ETests.Tier5AdversarialTests.RunAll(t5Report);
   return $"Tiers 1-4: {report.PassedCount}/{report.TotalCount}, Tier 5: {t5Report.PassedCount}/{t5Report.TotalCount}";
   // Expect: Tiers 1-4: 385/385, Tier 5: 36/36 -> Total: 421/421
   ```

2. **Verify `Wall_Top` EditMode Integrity**:
   ```csharp
   var report = Tests.Milestone1Tests.RunAllTests();
   return $"M1 Tests: {report.PassedCount}/{report.TotalCount}";
   // Expect: 12/12 Passed (includes M1-T2-05 Arena Boundaries Enclosure)
   ```

3. **Verify Dynamic Grenade Throw Beyond Y=5.2**:
   ```csharp
   using (var ctx = new E2ETests.E2ETestContext())
   {
       var camGo = ctx.CreateGameObject("TestCam");
       camGo.transform.position = new Vector3(0, 50, -10);
       var scc = camGo.AddComponent<ScrollingCameraController>();

       var playerGo = ctx.CreateGameObject("Player");
       playerGo.transform.position = new Vector3(0, 50, 0);
       var thrower = playerGo.AddComponent<GrenadeThrower>();
       thrower.grenadeCount = 2;

       var dummyProj = ctx.CreateGameObject("DummyProj");
       dummyProj.AddComponent<GrenadeProjectile>();
       thrower.grenadePrefab = dummyProj;

       thrower.ThrowGrenade(new Vector2(0, 56));

       var spawned = GameObject.Find("DummyProj(Clone)");
       bool passed = spawned != null && spawned.transform.position.y > 5.2f;
       if (spawned != null) UnityEngine.Object.DestroyImmediate(spawned);
       return $"Throw at Y=50 clamped correctly: {passed}";
   }
   ```

4. **Verify Dynamic Grenade Pickup at Y=40**:
   ```csharp
   using (var ctx = new E2ETests.E2ETestContext())
   {
       var camGo = ctx.CreateGameObject("TestCam");
       camGo.transform.position = new Vector3(0, 40, -10);
       var scc = camGo.AddComponent<ScrollingCameraController>();

       var pickupGo = ctx.CreateGameObject("Pickup");
       pickupGo.transform.position = new Vector3(0, 40, 0);
       var pickup = pickupGo.AddComponent<GrenadePickup>();

       var startMethod = typeof(GrenadePickup).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
       startMethod?.Invoke(pickup, null);

       bool passed = Mathf.Approximately(pickupGo.transform.position.y, 40f);
       return $"Pickup stayed at Y=40: {passed} (pos: {pickupGo.transform.position})";
   }
   ```

5. **Verify Shooter Kiting at Y=40**:
   ```csharp
   using (var ctx = new E2ETests.E2ETestContext())
   {
       var camGo = ctx.CreateGameObject("TestCam");
       camGo.transform.position = new Vector3(0, 40, -10);
       var scc = camGo.AddComponent<ScrollingCameraController>();

       var playerGo = ctx.CreateGameObject("Player");
       playerGo.transform.position = new Vector3(0, 38, 0);
       playerGo.AddComponent<PlayerHealth>();

       var shooterGo = ctx.CreateGameObject("Shooter");
       shooterGo.transform.position = new Vector3(0, 40, 0);
       var rb = shooterGo.AddComponent<Rigidbody2D>();
       var shooter = shooterGo.AddComponent<ShooterEnemy>();
       shooter.SetPlayer(playerGo.transform);

       var fu = typeof(ShooterEnemy).GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
       fu?.Invoke(shooter, null);

       bool passed = shooterGo.transform.position.y > 5.2f;
       return $"Shooter kited at Y=40 without snapping: {passed} (pos: {shooterGo.transform.position})";
   }
   ```

6. **Invalidation Conditions**:
   - Any baseline test in `E2ETestRunner.RunAll()` or `Tier5AdversarialTests.RunAll()` fails.
   - `Wall_Top` is modified or deleted in `shooting.unity` scene YAML, breaking `M1-T2-05` or `CH-M1-12`.
   - `GrenadePickup.Start()` or `ShooterEnemy.FixedUpdate()` snaps entity position back to 5.2f when `ScrollingCameraController` is active.
