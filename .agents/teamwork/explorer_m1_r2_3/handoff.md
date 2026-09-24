# Investigation Report: MapBounds Boundary Discrepancies & CH-M1-13 Fix

**Author**: explorer_m1_r2_3 (teamwork_preview_explorer)  
**Roles**: explorer, investigator, analyst  
**Recipient**: orchestrator_1  
**Project**: Continuous Upward (+Y) Endless Scrolling Map System  
**Milestone**: M1 (Camera Scrolling & Viewport Clamping)  
**Working Directory**: `.agents/teamwork/explorer_m1_r2_3/`  
**Date**: 2026-09-22  

---

## Executive Summary
Test `CH-M1-13` in `Assets/scripts/Tests/ChallengerM1Tests.cs` failed with `Test body breached right wall! Final pos X = 21.53854` because the assertion hardcoded `15.69f` based on the unscaled local position (`15.19f + 0.5f`) of child GameObject `MapBounds/Wall_Right`. In `Assets/Scenes/shooting.unity`, parent `MapBounds` has world scale `(1.6128, 1.6128, 1.6128)` and position `(-1.6485, -0.049026, 0)`, placing the actual inner collider boundary of `Wall_Right` at `X = 22.04353f` (inner edge) and outer boundary at `X = 23.65633f`. During continuous physics simulation, the test mover (radius 0.5f) was successfully stopped by `Wall_Right` at `X = 21.53854f` (exactly `col.bounds.min.x - radius`). 

Furthermore, our investigation revealed a second latent failure in `CH-M1-13`: line 407 contains the exact same flaw for `Wall_Left`, asserting `testMoverLeft.transform.position.x > -10.31f` (unscaled local outer edge), whereas the actual world inner boundary stops the left mover at `X = -16.15868f` (with outer edge `X = -18.27647f`). This left wall assertion did not trigger previously solely because the right wall assertion threw an exception first. Both right and left wall assertions must be updated to evaluate against the live `BoxCollider2D.bounds` in the scene.

A ready-to-apply patch has been produced at `.agents/teamwork/explorer_m1_r2_3/CH-M1-13_boundary_fix.patch`.

---

## 1. Observation

### 1.1 Verbatim Test Failure in `ChallengerM1Tests.cs`
- Executed via `unityMCP execute_code`:
  `var report = Tests.ChallengerM1Tests.RunAllTests();`
- **Output**:
  ```text
  Total: 14, Passed: 13, Failed: 1
  [Failed] CH-M1-13: Physics Simulation: MapBounds Physically Obstructs High-Speed Body -> Test body breached right wall! Final pos X = 21.53854
  ```
- **File location**: `Assets/scripts/Tests/ChallengerM1Tests.cs`, lines 358-410.
  Line 386-388:
  ```csharp
  // Right wall outer edge is X = 15.69. Body must NOT breach outer wall boundary!
  E2EAssert.IsTrue(testMover.transform.position.x < 15.69f,
      $"Test body breached right wall! Final pos X = {testMover.transform.position.x}");
  ```
  Line 406-408:
  ```csharp
  // Left wall outer edge is X = -10.31. Body must NOT breach left wall boundary!
  E2EAssert.IsTrue(testMoverLeft.transform.position.x > -10.31f,
      $"Test body breached left wall! Final pos X = {testMoverLeft.transform.position.x}");
  ```

### 1.2 Scene Hierarchy & Transform Data (`Assets/Scenes/shooting.unity`)
Direct inspection of `Assets/Scenes/shooting.unity` and query of scene GameObjects in Unity Editor revealed:
1. **Parent GameObject `MapBounds`** (ID `1754246110` / Transform ID `1754246111`, lines 8902-8936):
   - `m_LocalPosition`: `{x: -1.6485, y: -0.049026, z: 0}`
   - `m_LocalScale`: `{x: 1.6128, y: 1.6128, z: 1.6128}`
2. **Child GameObject `Wall_Right`** (ID `1151536761` / Transform ID `1151536762` / BoxCollider2D ID `1151536763`, lines 6480-6555):
   - `m_LocalPosition`: `{x: 15.19, y: 0.08, z: 0}`
   - `m_LocalScale`: `{x: 1, y: 1, z: 1}`
   - `BoxCollider2D.m_Size`: `{x: 1, y: 12}`
   - `BoxCollider2D.m_Offset`: `{x: 0, y: 0}`
   - `BoxCollider2D.m_IsTrigger`: `0` (solid physical collider)
3. **Child GameObject `Wall_Left`** (ID `839087692` / Transform ID `839087693` / BoxCollider2D ID `839087694`, lines 2411-2487):
   - `m_LocalPosition`: `{x: -9.81, y: 0.08, z: 0}`
   - `m_LocalScale`: `{x: 1, y: 1, z: 1}`
   - `BoxCollider2D.m_Size`: `{x: 1, y: 12}`
   - `BoxCollider2D.m_Offset`: `{x: 0, y: 0}`
   - `BoxCollider2D.m_IsTrigger`: `0` (solid physical collider)
4. **Child GameObject `Wall_Top`** (ID `1459065868` / Transform ID `1459065869`, lines 7853-7929):
   - `m_LocalPosition`: `{x: 2.69, y: 5.58, z: 0}`, `BoxCollider2D.m_Size`: `{x: 26, y: 1}`
5. **Child GameObject `Wall_Bottom`** (ID `1135806207` / Transform ID `1135806208`, lines 6263-6333):
   - `m_LocalPosition`: `{x: 2.69, y: -5.42, z: 0}`, `BoxCollider2D.m_Size`: `{x: 26, y: 1}`

### 1.3 Live Collider Bounds Extracted from Unity Editor
Query executed via `execute_code`:
| Collider Name | Local Position | World Position | World Bounds `min` | World Bounds `max` | World Bounds `size` |
|---|---|---|---|---|---|
| `Wall_Right` | `(15.19, 0.08, 0)` | `(22.85, 0.08, 0)` | `(22.04353, -9.59680, 0)` | `(23.65633, 9.75680, 0)` | `(1.6128, 19.3536, 0)` |
| `Wall_Left` | `(-9.81, 0.08, 0)` | `(-17.47, 0.08, 0)` | `(-18.27647, -9.59680, 0)` | `(-16.66367, 9.75680, 0)` | `(1.6128, 19.3536, 0)` |
| `Wall_Top` | `(2.69, 5.58, 0)` | `(2.69, 8.95, 0)` | `(-18.27647, 8.14400, 0)` | `(23.65633, 9.75680, 0)` | `(41.9328, 1.6128, 0)` |
| `Wall_Bottom` | `(2.69, -5.42, 0)` | `(2.69, -8.79, 0)` | `(-18.27647, -9.59680, 0)` | `(23.65633, -7.98400, 0)` | `(41.9328, 1.6128, 0)` |

### 1.4 Physics Simulation Measurement
1. **Right Wall Simulation**:
   - `testMover` starts at $X = 13.0f, Y = 0f$, with `CircleCollider2D` ($r = 0.5f$) and velocity $V = (40, 0)$.
   - Stepped for 30 physics steps ($\Delta t = 0.02s$, total $0.6s$, unconstrained distance would be $13.0 + 24.0 = 37.0f$).
   - Measured resting position: $X = 21.53854f$.
   - Theoretical inner collision position: $\text{rightCol.bounds.min.x} - r = 22.04353 - 0.5 = 21.54353f$.
   - Difference: $|21.53854 - 21.54353| = 0.00499f$ (within physics contact skin tolerance).
2. **Left Wall Simulation**:
   - `testMoverLeft` starts at $X = -8.0f, Y = 0f$, with `CircleCollider2D` ($r = 0.5f$) and velocity $V = (-40, 0)$.
   - Stepped for 30 physics steps ($\Delta t = 0.02s$, total $0.6s$, unconstrained distance would be $-8.0 - 24.0 = -32.0f$).
   - Measured resting position: $X = -16.15868f$.
   - Theoretical inner collision position: $\text{leftCol.bounds.max.x} + r = -16.66367 + 0.5 = -16.16367f$.
   - Difference: $|-16.15868 - (-16.16367)| = 0.00499f$ (within physics contact skin tolerance).
   - Assertion evaluation under current line 407 (`x > -10.31f`):
     `-16.15868 > -10.31f` evaluates to `FALSE`.

### 1.5 Scan of Other Tests in the Codebase
All 22 test files in `Assets/scripts/Tests/` were audited:
- `CH-M1-12`: Asserts `topCol.bounds.min.y >= 5.0f`, `btmCol.bounds.max.y <= -4.8f`, `leftCol.bounds.max.x <= -9.0f`, `rightCol.bounds.min.x >= 14.5f`. Because it uses loose inequality bounds, it passes with current scaled values (8.14 >= 5.0, -7.98 <= -4.8, -16.66 <= -9.0, 22.04 >= 14.5).
- `CH-M1-14`: Tests bullet destruction on `Wall_Top` via `HandleHit(topWall)` direct reflection. Passes.
- `CH-M2-04`: Tests `EnemyBullet` destruction on all 4 walls via `HandleHit(wall)` direct reflection. Passes.
- `M1-T2-05`: Checks that 4 walls exist and are solid BoxCollider2Ds. Passes.
- No other tests in the repository assert hardcoded coordinates against `MapBounds`.

---

## 2. Logic Chain

1. **Origin of Hardcoded 15.69f & -10.31f**:
   - The author of `ChallengerM1Tests.cs` computed the bounding coordinates using `Wall_Right` and `Wall_Left`'s **local** Transform positions and BoxCollider2D dimensions:
     - Right wall local X: $15.19$. Local half-width: $1.0 / 2 = 0.5$. Local outer edge: $15.19 + 0.5 = 15.69$.
     - Left wall local X: $-9.81$. Local half-width: $1.0 / 2 = 0.5$. Local outer edge: $-9.81 - 0.5 = -10.31$.
   - The author mistakenly assumed `MapBounds` was located at $(0, 0, 0)$ with scale $(1, 1, 1)$.

2. **The World Coordinate Reality**:
   - `MapBounds` in `shooting.unity` is scaled by $1.6128$ and translated to $X = -1.6485$:
     $$\text{World X}_{\text{Wall\_Right}} = -1.6485 + (15.19 \times 1.6128) = 22.849932$$
     $$\text{World Width} = 1.0 \times 1.6128 = 1.6128 \implies \text{Half-width} = 0.8064$$
     $$\text{Inner Edge} = 22.849932 - 0.8064 = 22.043532$$
     $$\text{Outer Edge} = 22.849932 + 0.8064 = 23.656332$$
   - Similarly for `Wall_Left`:
     $$\text{World X}_{\text{Wall\_Left}} = -1.6485 + (-9.81 \times 1.6128) = -17.470068$$
     $$\text{Inner Edge} = -17.470068 + 0.8064 = -16.663668$$
     $$\text{Outer Edge} = -17.470068 - 0.8064 = -18.276468$$

3. **Physics Containment Mechanism**:
   - When a body of radius $r = 0.5$ travels into the wall, collision occurs at the inner face:
     - Right: resting center is at $\text{col.bounds.min.x} - r = 22.04353 - 0.5 = 21.54353$.
     - Left: resting center is at $\text{col.bounds.max.x} + r = -16.66367 + 0.5 = -16.16367$.
   - Continuous collision physics stops both bodies at $X = 21.53854$ and $X = -16.15868$.
   - The physical walls function properly: the test mover never penetrates or breaches either wall.

4. **Failure Cause**:
   - The test evaluated the world position of the body against local coordinate constants:
     - $21.53854 < 15.69f \implies \text{FALSE}$.
     - $-16.15868 > -10.31f \implies \text{FALSE}$.
   - Line 387 failed immediately. Line 407 was never reached due to the assertion exception, masking the identical left-wall flaw.

5. **Resolution Requirement**:
   - The test must dynamically query `rightCol.bounds.min.x` and `leftCol.bounds.max.x` from `mapBounds.transform.Find(...)`.
   - The assertion must verify that the body center does not penetrate past the inner surface ($\text{bounds.min.x} - r + \epsilon$ for right wall, $\text{bounds.max.x} + r - \epsilon$ for left wall), and does not breach the outer boundary ($\text{bounds.max.x}$ for right wall, $\text{bounds.min.x}$ for left wall).

---

## 3. Caveats

- **Scene Serialization**: We verified that `Assets/Scenes/shooting.unity` must NOT be modified to alter `MapBounds` position or scale, as doing so would invalidate the background tile grid (`floor` Grid at `X = 2.69, Y = 0.08`), player starting positions, camera centering, and 421 legacy baseline tests. The correction must occur within the test assertion in `ChallengerM1Tests.cs`.
- **Collider Hierarchy Assumptions**: The fix assumes `MapBounds` has child GameObjects named `"Wall_Right"` and `"Wall_Left"`, each equipped with a `BoxCollider2D`. This is already guaranteed and asserted by `CH-M1-12` and `Milestone1Tests.M1-T2-05`.
- **Physics Contact Skin Tolerance**: A contact tolerance $\epsilon = 0.05f$ is incorporated in the proposed assertion to account for standard PhysX/Box2D contact skin offset ($~0.005f$), avoiding brittle floating-point equality failures across different Unity engine versions.

---

## 4. Conclusion & Recommended Fix

### 4.1 Root Cause
`CH-M1-13` hardcoded local transform coordinates (`15.69f` and `-10.31f`) rather than querying the world `BoxCollider2D.bounds` of `Wall_Right` and `Wall_Left`. The parent `MapBounds` has a non-identity scale (`1.6128`) and position (`-1.6485, -0.049026`), causing an apparent breach when the body was in fact properly obstructed by the wall.

### 4.2 Latent Defect Identified
Fixing line 387 alone is insufficient: line 407 contains the exact mirror bug for `Wall_Left` (`x > -10.31f`), which would immediately fail on the next test execution. Both right and left wall assertions must be patched simultaneously.

### 4.3 Exact Before / After Code Change

**Target File**: `Assets/scripts/Tests/ChallengerM1Tests.cs`  
**Target Lines**: 365–409  

#### Before:
```csharp
                        var mapBounds = GameObject.Find("MapBounds");
                        E2EAssert.IsNotNull(mapBounds, "MapBounds required for physics test");

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

                        // Right wall outer edge is X = 15.69. Body must NOT breach outer wall boundary!
                        E2EAssert.IsTrue(testMover.transform.position.x < 15.69f,
                            $"Test body breached right wall! Final pos X = {testMover.transform.position.x}");

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

                        // Left wall outer edge is X = -10.31. Body must NOT breach left wall boundary!
                        E2EAssert.IsTrue(testMoverLeft.transform.position.x > -10.31f,
                            $"Test body breached left wall! Final pos X = {testMoverLeft.transform.position.x}");
```

#### After:
```csharp
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
```

---

## 5. Verification Method

### 5.1 Verification Commands
1. **Apply the patch**:
   Apply `.agents/teamwork/explorer_m1_r2_3/CH-M1-13_boundary_fix.patch` to `Assets/scripts/Tests/ChallengerM1Tests.cs`.
2. **Execute `ChallengerM1Tests` via unityMCP `execute_code`**:
   ```csharp
   var report = Tests.ChallengerM1Tests.RunAllTests();
   return $"Total: {report.TotalCount}, Passed: {report.PassedCount}, Failed: {report.FailedCount}";
   ```
   **Expected output**:
   `Total: 14, Passed: 14, Failed: 0`
3. **Execute all Challenger suites via unityMCP `execute_code`**:
   ```csharp
   var r1 = Tests.ChallengerM1Tests.RunAllTests();
   var r2 = Tests.ChallengerM2Tests.RunAllTests();
   var r3 = Tests.ChallengerM3Tests.RunAllTests();
   return $"M1: {r1.PassedCount}/{r1.TotalCount}, M2: {r2.PassedCount}/{r2.TotalCount}, M3: {r3.PassedCount}/{r3.TotalCount}";
   ```
   **Expected output**:
   `M1: 14/14, M2: 17/17, M3: 26/26`
4. **Adversarial Negative Check**:
   If `Wall_Right` collider is disabled (`rightCol.enabled = false`), the test mover travels to $X = 36.99998f$, properly triggering assertion failure (`Test body penetrated right wall inner surface! Final pos X = 36.99998, max allowed = 22.09353`). This confirms the test is physically discriminative and not self-certifying.

### 5.2 Files to Inspect
- `Assets/scripts/Tests/ChallengerM1Tests.cs`: lines 365-415
- `.agents/teamwork/explorer_m1_r2_3/CH-M1-13_boundary_fix.patch`
