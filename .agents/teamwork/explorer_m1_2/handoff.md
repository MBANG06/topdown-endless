# Handoff Report — Milestone 1: Player Viewport Clamping & Bottom Edge Push/Kill

**Author**: explorer_m1_2 (teamwork_preview_explorer)  
**Recipient**: orchestrator_1  
**Project**: 2D Top-Down Shooter — Continuous Upward (+Y) Endless Scrolling Map System  
**Milestone**: M1 (Camera Scrolling & Viewport Clamping)  
**Date**: 2026-09-22  
**Working Directory**: `.agents/teamwork/explorer_m1_2/`  

---

## 1. Observation

### 1.1 Existing `PlayerMovement.cs` Implementation
- **File path**: `Assets/scripts/PlayerMovement.cs` (lines 1–82)
- **Current fields & defaults** (lines 11–25):
  ```csharp
  [Header("Movement Settings")]
  public float moveSpeed = 5f;

  [Header("References")]
  public Rigidbody2D rb;
  public Camera cam;

  [Header("Arena Boundary Clamping")]
  public bool clampToBounds = true;
  public Vector2 minBounds = new Vector2(-8.5f, -4.2f);
  public Vector2 maxBounds = new Vector2(13.8f, 5.2f);
  ```
- **Current update pipeline** (lines 39–80):
  - In `Update()`: Samples WASD input via `Input.GetAxisRaw("Horizontal")` and `"Vertical"`. Safely falls back to `Camera.main` if `cam == null`. Calculates `mousePos = cam.ScreenToWorldPoint(Input.mousePosition)`.
  - In `FixedUpdate()`:
    ```csharp
    Vector2 nextPosition = rb.position + movement.normalized * (moveSpeed * Time.fixedDeltaTime);
    if (clampToBounds)
    {
        nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x, maxBounds.x);
        nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y, maxBounds.y);
    }
    rb.MovePosition(nextPosition);
    ```
    Computes mouse rotation via `Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;` and sets `rb.rotation`.
- **Observations on current behavior**:
  1. `clampToBounds` rigidly clamps player Y to `maxBounds.y = 5.2f`. In an upward scrolling game where camera Y advances past `5.2f`, this would freeze the player at $Y = 5.2$, forcing them to get crushed by the camera.
  2. `PlayerMovement` currently has NO awareness of camera scrolling, camera viewport boundaries, or bottom edge push/kill mechanics.

### 1.2 Baseline Test Suite Dependencies on `PlayerMovement`
- Ran baseline tests via `unityMCP` `execute_code`:
  - `E2ETests.E2ETestRunner.RunAllFormatted()`: **385 / 385 Passed, 0 Failed**.
  - `E2ETests.Tier5AdversarialTests.RunAll()`: **36 / 36 Passed, 0 Failed**.
  - `E2ETests.ScrollingMapTests.RunAllFormatted()`: **120 / 120 Passed, 0 Failed**.
  - **Total active tests: 541 / 541 tests passing at 100%**.
- Key baseline tests directly verifying `PlayerMovement` properties:
  - `Milestone1Tests.cs` (lines 81–96, `M1-T1-04`):
    Asserts: `pm.moveSpeed == 5f`, `pm.clampToBounds == true`, `pm.minBounds == new Vector2(-8.5f, -4.2f)`, `pm.maxBounds == new Vector2(13.8f, 5.2f)`.
  - `Milestone1Tests.cs` (lines 184–202, `M1-T2-04`) and `Tier5AdversarialTests.cs` (lines 147–161, `T5_ADV_04`):
    Sets `pm.cam = null` and invokes `Update()` via reflection; asserts no `NullReferenceException`.
  - `Tier5AdversarialTests.cs` (lines 79–119, `T5_ADV_01`, `T5_ADV_02`):
    Directly tests clamping against `pm.minBounds` and `pm.maxBounds`.
  - `Tier5AdversarialTests.cs` (lines 121–145, `T5_ADV_03`):
    Verifies rotation calculation stability when mouse direction vector has zero magnitude (`sqrMagnitude <= 0.0001f`).
  - `E2ETier1Tests.cs` (line 378), `Milestone5Tests.cs` (line 546), `Challenger1M5Tests.cs` (line 862):
    Asserts `pm.enabled == false` on player death and `pm.enabled == true` on `ResetHealth()`/game start.

### 1.3 `PlayerHealth.cs` & `GameManager.cs` Combat Integration
- `Assets/scripts/PlayerHealth.cs`:
  - `public void TakeDamage(int damage)` (lines 69–92):
    - Rejects if `!IsAlive`, `isInvulnerable`, or `damage <= 0`.
    - Exactly 1 HP deducted per valid hit (`currentHealth = Mathf.Max(0, currentHealth - 1)`).
    - If `currentHealth <= 0`: calls `Die()`, which disables `PlayerMovement` and `Shooting`, and raises `OnPlayerDeath`.
    - Else: starts `InvulnerabilityRoutine()`, setting `isInvulnerable = true` for `invulnerabilityDuration = 1.0f` while flashing sprite red.
- `Assets/scripts/GameManager.cs` (lines 136–150, 170–181):
  - Subscribes `TriggerGameOver` to `_playerHealth.OnPlayerDeath`.
  - Thus, bottom kill plane reducing HP to 0 automatically triggers Game Over with zero additional wiring needed.

### 1.4 Player Collider & Camera Geometry
- Queried active scene via `execute_code`:
  - Camera: Orthographic, `orthographicSize = 6.316637`, Aspect = `1.777778` (16:9).
    - Total visible height = $2 \times 6.316637 = 12.633$ units.
    - Total visible width = $12.633 \times 1.777778 = 22.459$ units.
  - Player: `BoxCollider2D` size = `(1.56, 1.86)`, half-height = `0.93`, half-width = `0.78`.
  - Viewport margin analysis:
    - Viewport $Y = 0.08$: distance from bottom edge is $0.08 \times 12.633 = 1.01$ units.
      Since player half-height is $0.93$, the bottom of the player's collider sits at $1.01 - 0.93 = 0.08$ units *above* the bottom screen edge. Fully visible.
    - Viewport $Y = 0.92$: distance from top edge is $(1.0 - 0.92) \times 12.633 = 1.01$ units.
      Player top collider sits at $0.08$ units *below* the top screen edge. Fully visible.
    - Viewport $X = 0.05$: distance from left edge is $0.05 \times 22.459 = 1.12$ units.
      Player left collider sits at $1.12 - 0.78 = 0.34$ units *inside* the screen edge.
    - Viewport $X = 0.95$: distance from right edge is $1.12$ units.
      Player right collider sits at $0.34$ units *inside* the screen edge.
    - `bottomKillThreshold = 0.04`: distance from bottom edge is $0.04 \times 12.633 = 0.505$ units.
      Since player half-height is $0.93$, at viewport $Y = 0.04$ the player's lower half has crossed *below* the camera's bottom edge, making it the ideal danger threshold.

---

## 2. Logic Chain

### 2.1 Preserving 100% Backward Compatibility
1. **Rule**: Every existing test creates `PlayerMovement` expecting default field values (`clampToBounds = true`, `minBounds = (-8.5f, -4.2f)`, `maxBounds = (13.8f, 5.2f)`).
2. **Strategy**:
   - Keep all legacy fields and exact defaults intact.
   - Introduce `public bool clampToViewport = false;` with default `false`.
   - In `FixedUpdate()`:
     - If `clampToViewport && cam != null`: execute viewport clamping and bottom edge checks.
     - Else if `clampToBounds`: execute legacy arena clamping.
   - In `Start()`: Auto-detect whether a `ScrollingCameraController` exists in the scene via reflection. If present, set `clampToViewport = true`.
   - Result:
     - In all 421 baseline tests, no `ScrollingCameraController` exists, `clampToViewport` remains `false`, and legacy clamping executes identically to before.
     - In the live scrolling scene (and new M1 tests), `clampToViewport` is active.

### 2.2 Viewport Clamping Mathematics
1. Given candidate `nextPosition = rb.position + movement.normalized * (moveSpeed * Time.fixedDeltaTime)`:
2. Convert candidate position to viewport coordinates:
   $$\mathbf{vp} = \text{cam.WorldToViewportPoint}\left(\text{new Vector3}(nextPosition.x, nextPosition.y, 0)\right)$$
3. Clamp viewport coordinates within margins:
   $$\mathbf{vp}.x = \text{Mathf.Clamp}(\mathbf{vp}.x, \text{minViewportX}, \text{maxViewportX})$$
   $$\mathbf{vp}.y = \text{Mathf.Clamp}(\mathbf{vp}.y, \text{minViewportY}, \text{maxViewportY})$$
4. Convert back to world coordinates:
   $$\text{clampedWorld} = \text{cam.ViewportToWorldPoint}(\mathbf{vp})$$
   $$nextPosition.x = \text{clampedWorld}.x,\quad nextPosition.y = \text{clampedWorld}.y$$
5. **Auto-Carry Behavior**:
   - If the player is stationary at the bottom viewport boundary ($vp.y = 0.08$) while the camera scrolls $+Y$ at $v_{\text{cam}}$:
   - On the next physics tick, the player's candidate position is at $vp.y < 0.08$.
   - Clamping $vp.y$ to $0.08$ automatically advances $nextPosition.y$ by the exact distance the camera moved.
   - The player is automatically carried forward with the scrolling camera without requiring continuous WASD input.

### 2.3 Bottom Edge Push / Kill Plane
1. **Physical Position Evaluation**:
   - While `nextPosition` is clamped to $vp.y \ge 0.08$, a physical obstacle (e.g. MapSegment wall or enemy) might block `rb.MovePosition` from advancing.
   - As the camera scrolls away, the player's *actual physical position* `rb.position` drops behind the camera.
2. **Threshold Trigger**:
   - In `FixedUpdate()` after `rb.MovePosition()`, compute:
     $$\mathbf{currentVp} = \text{cam.WorldToViewportPoint}\left(\text{new Vector3}(rb.position.x, rb.position.y, 0)\right)$$
   - If $\mathbf{currentVp}.y < \text{bottomKillThreshold}$ (0.04):
     1. **Damage Ingestion**:
        - Check `playerHealth != null && playerHealth.IsAlive`.
        - If `Time.time >= lastBottomDamageTime + bottomDamageInterval` (1.0s):
          - Set `lastBottomDamageTime = Time.time`.
          - Call `playerHealth.TakeDamage(1)`.
          - `PlayerHealth` handles 1.0s i-frame window, red flashing sprite, and death callback.
          - If `currentHealth` drops to 0, `OnPlayerDeath` fires and `GameManager.TriggerGameOver()` is called.
     2. **Forward Push / Unstuck Assist**:
        - If `bottomPushForward == true`:
          - Determine world target $Y$ corresponding to $vp.y = \text{minViewportY}$ (0.08).
          - Nudge `rb.position.y` upward:
            $$\text{newY} = \text{Mathf.MoveTowards}(rb.position.y, \text{targetWorldY}, \text{bottomPushSpeed} \times \Delta t_{\text{fixed}})$$
            $$rb.position = \text{new Vector2}(rb.position.x, \text{newY})$$
          - With `bottomPushSpeed = 5.0f` u/s (exceeding camera max speed of 3.5 u/s), this upward thrust actively pushes the player out of the crush zone if a path or sliding angle exists.
          - If completely blocked by an impassable obstacle, the player takes 1 damage per second and dies in 5 seconds.

### 2.4 Physics Synchronization & Jitter Elimination
1. `explorer_m1_1` established that `ScrollingCameraController` runs in `FixedUpdate()` with `[DefaultExecutionOrder(-100)]`.
2. `PlayerMovement` runs in `FixedUpdate()` with standard execution order `0`.
3. In each physics tick:
   - Camera translates $+Y$ at tick $T$.
   - `PlayerMovement` evaluates viewport bounds against the camera's *already-updated* position at tick $T$.
   - Rigidbody2D translates and collides.
   - Zero 1-frame boundary lag, zero jitter, zero visual stutter.

---

## 3. Caveats

1. **Camera Reference Null Safety**:
   - In adversarial tests (`M1-T2-04`, `T5_ADV_04`), `cam` may be explicitly set to `null` or unassigned in test harnesses.
   - If `cam == null`, the script must query `Camera.main`. If still null, it must gracefully fall back to `clampToBounds` without throwing any exceptions.
2. **Compilation Order Independence**:
   - To prevent compilation failure if `PlayerMovement.cs` is edited before `ScrollingCameraController.cs` is created, use `System.Type.GetType("ScrollingCameraController")` in `Start()` rather than a direct compile-time type reference.
3. **TimeScale & Pause Safety**:
   - `FixedUpdate()` does not tick when `Time.timeScale == 0f`, naturally pausing both camera scrolling and viewport damage during pause menus and death screens.
4. **Instant Kill Option**:
   - Added `public bool instantKillBelowScreen = false;` (defaults to false). If enabled and player completely falls below screen ($vp.y \le 0$), deals lethal damage. Kept false by default to preserve 1 HP damage absorption tests.

---

## 4. Conclusion & Recommended Implementation Strategy

### 4.1 Recommended Code Replacement for `Assets/scripts/PlayerMovement.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls 8-directional WASD player movement with diagonal normalization,
/// mouse-aim rotation, safe camera reference fallback, arena coordinate clamping,
/// dynamic viewport clamping, and bottom edge push/kill plane for scrolling gameplay.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Rigidbody2D rb;
    public Camera cam;

    [Header("Arena Boundary Clamping (Legacy Static Arena)")]
    public bool clampToBounds = true;
    public Vector2 minBounds = new Vector2(-8.5f, -4.2f);
    public Vector2 maxBounds = new Vector2(13.8f, 5.2f);

    [Header("Viewport Clamping Settings (Milestone 1 Scrolling System)")]
    [Tooltip("If enabled, player position is restricted within the moving camera viewport.")]
    public bool clampToViewport = false;
    [Tooltip("Normalized camera viewport X bounds (default [0.05, 0.95]).")]
    public float minViewportX = 0.05f;
    public float maxViewportX = 0.95f;
    [Tooltip("Normalized camera viewport Y bounds (default [0.08, 0.92]).")]
    public float minViewportY = 0.08f;
    public float maxViewportY = 0.92f;

    [Header("Bottom Edge Push / Kill Settings")]
    [Tooltip("Viewport Y threshold below which player takes damage or is pushed forward (default 0.04).")]
    public float bottomKillThreshold = 0.04f;
    [Tooltip("Whether to push player forward along +Y when crossing below bottomKillThreshold.")]
    public bool bottomPushForward = true;
    [Tooltip("Upward speed applied when pushing player forward from bottom danger zone.")]
    public float bottomPushSpeed = 5.0f;
    [Tooltip("Cooldown between consecutive bottom edge damage ticks in seconds.")]
    public float bottomDamageInterval = 1.0f;
    [Tooltip("Instant kill if player falls completely below camera viewport (Y <= 0).")]
    public bool instantKillBelowScreen = false;

    private Vector2 movement;
    private Vector2 mousePos;
    private float lastBottomDamageTime = -999f;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        // Auto-activate viewport clamping if ScrollingCameraController exists in the active scene
        if (!clampToViewport)
        {
            Type scrollingCamType = Type.GetType("ScrollingCameraController");
            if (scrollingCamType != null && FindObjectOfType(scrollingCamType) != null)
            {
                clampToViewport = true;
            }
        }
    }

    private void Update()
    {
        // 8-directional input sampling
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Safe camera fallback
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Normalized 8-direction movement translation
        Vector2 nextPosition = rb.position + movement.normalized * (moveSpeed * Time.fixedDeltaTime);

        // Coordinate clamping: viewport clamping takes precedence when enabled and camera is available
        if (clampToViewport)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam != null)
            {
                ApplyViewportClamping(ref nextPosition);
            }
            else if (clampToBounds)
            {
                // Safe fallback to static bounds if camera reference is unavailable
                nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x, maxBounds.x);
                nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y, maxBounds.y);
            }
        }
        else if (clampToBounds)
        {
            // Legacy arena bounds clamping (100% backward compatible with 421 tests)
            nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x, maxBounds.x);
            nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y, maxBounds.y);
        }

        rb.MovePosition(nextPosition);

        // Check bottom edge push/kill plane against physical position
        if (clampToViewport && cam != null)
        {
            HandleBottomEdgePushKill();
        }

        // Mouse-aim rotation
        Vector2 lookDir = mousePos - rb.position;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }
    }

    /// <summary>
    /// Clamps candidate world position to the camera's viewport margins.
    /// Also naturally carries the player along +Y as camera scrolls upward.
    /// </summary>
    private void ApplyViewportClamping(ref Vector2 position)
    {
        Vector3 vp = cam.WorldToViewportPoint(new Vector3(position.x, position.y, 0f));
        vp.x = Mathf.Clamp(vp.x, minViewportX, maxViewportX);
        vp.y = Mathf.Clamp(vp.y, minViewportY, maxViewportY);
        Vector3 clampedWorld = cam.ViewportToWorldPoint(vp);
        position.x = clampedWorld.x;
        position.y = clampedWorld.y;
    }

    /// <summary>
    /// Checks if player's physical viewport Y has dropped below bottomKillThreshold.
    /// Inflicts 1 HP damage (with cooldown & i-frames) and pushes player forward towards minViewportY.
    /// </summary>
    private void HandleBottomEdgePushKill()
    {
        Vector3 currentVp = cam.WorldToViewportPoint(new Vector3(rb.position.x, rb.position.y, 0f));

        if (currentVp.y < bottomKillThreshold)
        {
            // 1. Inflict damage via PlayerHealth
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (playerHealth != null && playerHealth.IsAlive)
            {
                if (instantKillBelowScreen && currentVp.y <= 0f)
                {
                    playerHealth.TakeDamage(playerHealth.currentHealth);
                }
                else if (Time.time >= lastBottomDamageTime + bottomDamageInterval)
                {
                    lastBottomDamageTime = Time.time;
                    playerHealth.TakeDamage(1);
                }
            }

            // 2. Push player forward along +Y towards minViewportY
            if (bottomPushForward)
            {
                Vector3 targetVp = new Vector3(currentVp.x, minViewportY, currentVp.z);
                Vector3 targetWorld = cam.ViewportToWorldPoint(targetVp);

                float newY = Mathf.MoveTowards(rb.position.y, targetWorld.y, bottomPushSpeed * Time.fixedDeltaTime);
                rb.position = new Vector2(rb.position.x, newY);
            }
        }
    }

    /// <summary>
    /// Programmatically triggers bottom edge evaluation (for automated testing).
    /// </summary>
    public void ForceCheckBottomKillPlane()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            HandleBottomEdgePushKill();
        }
    }

    /// <summary>
    /// Configures viewport boundaries programmatically (for automated testing).
    /// </summary>
    public void SetViewportBounds(float minX, float maxX, float minY, float maxY)
    {
        minViewportX = minX;
        maxViewportX = maxX;
        minViewportY = minY;
        maxViewportY = maxY;
    }
}
```

### 4.2 Step-by-Step Instructions for Worker
1. Edit `Assets/scripts/PlayerMovement.cs` with the complete implementation above.
2. Trigger compilation via `refresh_unity(compile="request", mode="force", scope="scripts", wait_for_ready=true)`.
3. Check `read_console` to verify 0 compilation errors.
4. Run all baseline tests (`E2ETestRunner.RunAllFormatted()` and `Tier5AdversarialTests.RunAll()`) to verify all 421 baseline tests pass 100%.
5. Run `ScrollingMapTests.RunAllFormatted()` to verify all 120 scrolling map tests pass 100%.

---

## 5. Verification Method

### 5.1 Automated Unit Tests for Worker Verification Suite
The worker can append the following test suite to `Assets/scripts/Tests/ScrollingMapTests.cs` (or create a dedicated test method):

```csharp
// 1. Default Field Values & Backward Compatibility
TestRunnerHelper.RunTest(report, "M1_PM_01", "PlayerMovement", 1,
    "PlayerMovement Viewport Defaults & Legacy Preservation",
    "Verifies legacy clamp defaults are preserved while viewport fields are initialized correctly.",
    () =>
    {
        using var ctx = new E2ETestContext();
        var go = ctx.CreateGameObject("TestPlayer");
        var pm = go.AddComponent<PlayerMovement>();

        // Legacy defaults intact
        E2EAssert.AreEqual(5f, pm.moveSpeed);
        E2EAssert.IsTrue(pm.clampToBounds);
        E2EAssert.AreEqual(new Vector2(-8.5f, -4.2f), pm.minBounds);
        E2EAssert.AreEqual(new Vector2(13.8f, 5.2f), pm.maxBounds);

        // Viewport fields
        E2EAssert.IsFalse(pm.clampToViewport, "clampToViewport must be false by default");
        E2EAssert.AreEqual(0.05f, pm.minViewportX);
        E2EAssert.AreEqual(0.95f, pm.maxViewportX);
        E2EAssert.AreEqual(0.08f, pm.minViewportY);
        E2EAssert.AreEqual(0.92f, pm.maxViewportY);
        E2EAssert.AreEqual(0.04f, pm.bottomKillThreshold);
    });

// 2. Viewport Clamping Bounds Enforcement
TestRunnerHelper.RunTest(report, "M1_PM_02", "PlayerMovement", 1,
    "Viewport Clamping Enforces Margins",
    "Verifies position outside viewport [0.05, 0.95] X and [0.08, 0.92] Y is clamped.",
    () =>
    {
        using var ctx = new E2ETestContext();
        var camGo = ctx.CreateGameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10f);

        var player = ctx.CreateGameObject("Player");
        var rb = player.AddComponent<Rigidbody2D>();
        var pm = player.AddComponent<PlayerMovement>();
        pm.rb = rb;
        pm.cam = cam;
        pm.clampToViewport = true;

        // Position far out to bottom-left: viewport (-1, -1)
        Vector3 outPos = cam.ViewportToWorldPoint(new Vector3(-1f, -1f, 10f));
        rb.position = outPos;

        // Call FixedUpdate via reflection
        var fixedUpdate = typeof(PlayerMovement).GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fixedUpdate.Invoke(pm, null);

        Vector3 clampedVp = cam.WorldToViewportPoint(new Vector3(rb.position.x, rb.position.y, 0f));
        E2EAssert.AreApproximatelyEqual(0.05f, clampedVp.x, 0.01f);
        E2EAssert.AreApproximatelyEqual(0.08f, clampedVp.y, 0.01f);
    });

// 3. Bottom Edge Damage & Push on Trap
TestRunnerHelper.RunTest(report, "M1_PM_03", "PlayerMovement", 1,
    "Bottom Edge Push and Damage on Threshold Breach",
    "Verifies player below 0.04 viewport Y takes 1 damage and is pushed forward.",
    () =>
    {
        using var ctx = new E2ETestContext();
        var camGo = ctx.CreateGameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10f);

        var player = ctx.CreateGameObject("Player");
        var rb = player.AddComponent<Rigidbody2D>();
        var ph = player.AddComponent<PlayerHealth>();
        var pm = player.AddComponent<PlayerMovement>();
        pm.rb = rb;
        pm.cam = cam;
        pm.clampToViewport = true;

        // Place player at viewport Y = 0.02 (below bottomKillThreshold = 0.04)
        Vector3 lowPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.02f, 10f));
        rb.position = lowPos;

        pm.ForceCheckBottomKillPlane();

        E2EAssert.AreEqual(4, ph.currentHealth, "Player should take 1 damage at bottom threshold");
        E2EAssert.IsTrue(ph.isInvulnerable, "Player should receive i-frames");
        E2EAssert.IsTrue(rb.position.y > lowPos.y, "Player should be pushed forward along +Y");
    });

// 4. Null Camera Graceful Fallback
TestRunnerHelper.RunTest(report, "M1_PM_04", "PlayerMovement", 2,
    "Null Camera Graceful Fallback in Viewport Mode",
    "Verifies PlayerMovement does not throw NullReferenceException when clampToViewport is true but cam is null.",
    () =>
    {
        using var ctx = new E2ETestContext();
        var player = ctx.CreateGameObject("Player");
        var rb = player.AddComponent<Rigidbody2D>();
        var pm = player.AddComponent<PlayerMovement>();
        pm.rb = rb;
        pm.cam = null;
        pm.clampToViewport = true;

        var fixedUpdate = typeof(PlayerMovement).GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fixedUpdate.Invoke(pm, null);
        E2EAssert.IsTrue(true, "FixedUpdate completed safely without NullReferenceException");
    });
```

### 5.2 Independent Test Commands
Execute in Unity Editor via `unityMCP` `execute_code`:
```csharp
// Verify 100% baseline test stability
return E2ETests.E2ETestRunner.RunAllFormatted();
// Verify Tier 5 adversarial tests
var t5 = E2ETests.Tier5AdversarialTests.RunAll();
return t5.GenerateMarkdownSummary();
// Verify Scrolling Map test suite
return E2ETests.ScrollingMapTests.RunAllFormatted();
```
All suites must return 100% Passed with 0 Failed.
