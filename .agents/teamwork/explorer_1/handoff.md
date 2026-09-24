# Handoff Report — Explorer 1 (Codebase Investigation & Endless Scrolling Architecture)

**Date**: 2026-09-22  
**Author**: explorer_1 (teamwork_preview_explorer)  
**Recipient**: orchestrator_1  
**Project**: 2D Top-Down Shooter — Endless Scrolling Map System (R1 - R5)  
**Status**: Investigation Complete — Ready for Architecture & Implementation Dispatch  

---

## 1. Observation

### 1.1 Project Structure & Existing Scripts
The project contains 19 active gameplay and UI scripts in `Assets/scripts/` and 17 comprehensive test suites in `Assets/scripts/Tests/`:

| Script | Path | Key Responsibilities & Observed Parameters |
|---|---|---|
| `PlayerMovement.cs` | `Assets/scripts/PlayerMovement.cs` | 8-directional WASD translation (`moveSpeed = 5.0f`), mouse-aim rotation via `Camera.main.ScreenToWorldPoint()`. Hardcoded coordinate clamping: `clampToBounds = true`, `minBounds = (-8.5f, -4.2f)`, `maxBounds = (13.8f, 5.2f)` (lines 20–21, 65–69). |
| `PlayerHealth.cs` | `Assets/scripts/PlayerHealth.cs` | 5 HP maximum, deducts exactly 1 HP per hit (line 77), 1.0s invulnerability window with sprite flash. Events: `OnHealthChanged(int)`, `OnPlayerDeath()`. |
| `Shooting.cs` | `Assets/scripts/Shooting.cs` | Semi-automatic weapon firing (`fireRate = 0.2f`, `bulletForce = 20f`), forward impulse on `Bullet` prefab, paused game guard (`Time.timeScale <= 0`). |
| `GrenadeThrower.cs` | `Assets/scripts/GrenadeThrower.cs` | Inventory management (default 2, max 5). Key `E` or RMB. Maximum throw distance: 7.0u. Clamps targets to `arenaMin = (-8.5f, -4.2f)` and `arenaMax = (13.8f, 5.2f)` (lines 29–30, 125–126). |
| `GrenadeProjectile.cs` | `Assets/scripts/GrenadeProjectile.cs` | Parabolic simulated trajectory (`flightDuration = 0.7f`, `maxArcHeight = 0.5f`, `fuseTime = 1.2f`). Friendly fire immunity to Player; detonates on Enemy or Wall/Obstacle, spawning `ExplosionAoE`. |
| `GrenadePickup.cs` | `Assets/scripts/GrenadePickup.cs` | Collectible item granting +1 grenade up to max capacity 5. Clamps position in `Start()` to `arenaMin` and `arenaMax` (lines 17–18, 47–48). |
| `ExplosionAoE.cs` | `Assets/scripts/ExplosionAoE.cs` | Blast radius 3.5u, 50 damage to all `IDamageable` targets in circle (Player friendly fire immune). Auto-destruction after 0.6s. |
| `EnemyBase.cs` | `Assets/scripts/EnemyBase.cs` | Abstract base class implementing `IDamageable`. Flash routine, score reporting (`AddScore(scoreValue)` via reflection and static `OnEnemyKilledScore`), death VFX, `RollGrenadeDrop()`. |
| `ChaserEnemy.cs` | `Assets/scripts/ChaserEnemy.cs` | Direct player tracking melee archetype: 3 HP, 2.8 u/s move speed, 10 score, 20% grenade drop rate. |
| `RusherEnemy.cs` | `Assets/scripts/RusherEnemy.cs` | Fast glass-cannon melee archetype: 1 HP, 6.2 u/s move speed, 15 score, 15% grenade drop rate. |
| `ShooterEnemy.cs` | `Assets/scripts/ShooterEnemy.cs` | Ranged kiting archetype (maintains distance 3.8u – 5.5u), fires `EnemyBullet` every 2.5s: 2 HP, 2.0 u/s move speed, 20 score, 25% grenade drop rate. Clamps position to `arenaMin = (-8.5f, -4.2f)` and `arenaMax = (13.8f, 5.2f)` (lines 16–17, 112–113). |
| `BossController.cs` | `Assets/scripts/BossController.cs` | Inherits `EnemyBase`. Stats: 60 HP, 1.8 u/s move speed, 500 score, 2 guaranteed grenade drops. Radial barrage: 16 projectiles evenly spaced 22.5° at 5.0 u/s, 0.5s yellow telegraph warning every 3.5s. Broadcasts `OnBossSpawned`, `OnBossHealthChanged`, `OnBossDefeatedEvent`, `OnBossKilled`. |
| `EnemySpawner.cs` | `Assets/scripts/EnemySpawner.cs` | Concurrency cap $N(t, S) = \min(25, 5 + \lfloor t/20 \rfloor + \lfloor S/60 \rfloor)$, spawn interval $I(t, S) = \max(0.6, 3.0 - 0.015t - 0.002S)$ (doubles during boss). Fixed perimeter: `minX = -10.5`, `maxX = 16.0`, `minY = -6.0`, `maxY = 7.0`. Spawns Boss at `currentScore >= 500` at fixed `bossSpawnPosition = (2.69f, 3.5f, 0.0f)`. |
| `GameManager.cs` | `Assets/scripts/GameManager.cs` | Core loop singleton: `GameState` (MainMenu, Playing, Paused, GameOver, VictoryContinues). Persistent HighScore via `PlayerPrefs`. Handles `AddScore()`, `PauseGame()`, `TriggerGameOver()`, `TriggerVictory()`, `ResumeEndlessAfterBoss()`, `RestartGame()`. |
| `UIManager.cs` | `Assets/scripts/UIManager.cs` | HUD manager: 5 heart icons, `SCORE: 00000`, `HIGH: 00000`, `x N` grenades, Boss Health Bar slider. Panels: MainMenu, Pause, GameOver, Victory ("BOSS SLAIN! +500 PTS"), ControlsModal. |
| `SoundManager.cs` | `Assets/scripts/SoundManager.cs` | Procedural 8-bit audio waveform synthesis (Shoot, Hit, Explosion, Hurt, Pickup, GameOver, Victory). |

### 1.2 Scene Hierarchy & Camera Inspection (`Scenes/shooting.unity`)
Using `unityMCP` tools (`find_gameobjects`, `read_resource`, `execute_code`), the active scene was inspected:
- **Main Camera**:
  - Transform position: `(1.96, 0.04, -10.0)`
  - Projection: Orthographic, `orthographicSize = 6.316637`, `aspect = 1.777778` (16:9).
  - Vertical height in world units: $2 \times 6.3166 = 12.633$ units.
  - Horizontal width in world units: $12.633 \times 1.7778 = 22.459$ units.
  - Viewport bounds in world space at origin: $X \in [-9.27, 13.19]$, $Y \in [-6.28, 6.36]$.
  - **No camera controller or follower script exists** currently. The camera is completely stationary.
- **Scene Objects**:
  - `Player` at `(3.28, 0.11, 0.00)`
  - `MapBounds`: 4 walls (`Wall_Top` at Y=8.95, `Wall_Bottom` at Y=-8.79, `Wall_Left` at X=-17.47, `Wall_Right` at X=22.85).
  - `floor`: Grid Tilemap spanning $X \in [-12, 12]$, $Y \in [-5, 5]$ (size 24x10).
  - `arvores`: Grid Tilemap obstacles.
  - `Colliders`: 3 static obstacle colliders.
  - `Canvas`: Contains HUD, MainMenuPanel, PausePanel, GameOverPanel, VictoryPanel, ControlsModal.

### 1.3 Test Suite Baseline Verification
- Verified test execution via `unityMCP` `execute_menu_item`:
  - `E2E Tests/Run All Tests` (Tiers 1–4): **385 / 385 Passed, 0 Failed** in 8.90 ms.
  - `E2E Tests/Run Tier 5 (Adversarial Hardening)`: **36 / 36 Passed, 0 Failed** in 36.62 ms.
  - **Total active tests**: **421 tests passing at 100%**.
- Critical test assertions discovered:
  - `M1-T1-04`, `T5_ADV_01`, `T5_ADV_02`: strictly check `pm.minBounds == (-8.5f, -4.2f)` and `pm.maxBounds == (13.8f, 5.2f)` and `pm.clampToBounds == true`.
  - `M1-T2-05`, `ChallengerM1Tests`: verify `GameObject.Find("MapBounds")` has `Wall_Top`, `Wall_Bottom`, `Wall_Left`, `Wall_Right` with active BoxCollider2Ds in the scene.
  - `Milestone4Tests` (lines 412–422): strictly verify `spawner.bossSpawnPosition == (2.69f, 3.5f, 0.0f)`.
  - `ChallengerM3Tests`, `T5_ADV_18`, `T5_ADV_24`: strictly check `arenaMin` and `arenaMax` on `ShooterEnemy` and `GrenadeThrower`.

---

## 2. Logic Chain

### 2.1 Why the Existing Code Breaks Under Endless Scrolling Without Refactoring
1. **PlayerMovement coordinate ceiling**: In `PlayerMovement.FixedUpdate()`, lines 67–68 clamp `nextPosition.y` to `maxBounds.y = 5.2f`. As the camera scrolls upward (+Y), once camera Y exceeds 5.2, the player is locked in place and scrolls off the bottom of the screen.
2. **GrenadeThrower target clamping**: In `GrenadeThrower.ThrowGrenade()`, lines 125–126 clamp `finalTarget.y` to `arenaMax.y = 5.2f`. Throws above Y=5.2 are prevented.
3. **GrenadePickup clamping**: In `GrenadePickup.Start()`, lines 47–48 clamp `transform.position.y` to `arenaMax.y = 5.2f`. Enemies defeated at Y > 5.2 drop grenades that teleport backward to Y=5.2.
4. **ShooterEnemy kiting clamping**: In `ShooterEnemy.FixedUpdate()`, lines 112–113 clamp shooter movement to `arenaMax.y = 5.2f`. Shooters spawned in upcoming segments are locked down at Y=5.2.
5. **EnemySpawner static perimeter**: In `EnemySpawner.GeneratePerimeterPosition()`, candidate spawn positions are bounded by `minY = -6.0f` and `maxY = 7.0f`. Once the camera moves past Y=10, all enemies spawn far behind the player off-screen.
6. **Static Boss Spawn Position**: `EnemySpawner.bossSpawnPosition` is hardcoded to `(2.69f, 3.5f, 0.0f)`. At 500 points, the boss spawns at Y=3.5 regardless of camera Y.
7. **MapBounds Wall_Top barrier**: `Wall_Top` is placed at `Y = 8.95` with a solid collider, physically blocking any upward entity travel.

### 2.2 Preservation of Existing Test Invariants
Because 421 tests assert the exact default field values of `minBounds`, `maxBounds`, `arenaMin`, `arenaMax`, `bossSpawnPosition`, and the existence of `MapBounds.Wall_Top`:
- **Rule**: Never change the default field declarations or remove public members.
- **Rule**: Implement new scrolling and viewport clamping behaviors via **opt-in flags**, dynamic camera-relative calculations, or dedicated new controller components that configure entities at runtime without mutating test-observed defaults.

---

## 3. Detailed Component Architecture Recommendations

### Component 1: Camera Scrolling Controller (`CameraController.cs`) [R1]
- **Attachment**: Attached to `Main Camera`.
- **Baseline Speed**: `baselineSpeed = 2.0f` u/s along +Y axis.
- **Max Speed Ceiling**: `maxSpeed = 3.5f` u/s.
- **Progression Scaling**: `currentSpeed = Mathf.Min(maxSpeed, baselineSpeed + (distanceTravelled / 100f) * speedScaleFactor)`.
- **Update Loop**: `FixedUpdate` (or synchronized `LateUpdate`) to eliminate physics jitter between Rigidbody2D interpolation and camera movement.
- **Scroll Locking**:
  - `public bool isScrollLocked { get; set; } = false;`
  - When locked, `currentSpeed = 0f`.
- **Travel Distance Tracking**:
  - `distanceTravelled = Mathf.Max(0f, transform.position.y - _initialY);`
  - Formatted in meters for HUD: `Mathf.FloorToInt(distanceTravelled)`.

### Component 2: Player Viewport Clamping & Bottom Push/Kill Plane (`PlayerMovement.cs` extension) [R1]
- **Extension Fields on `PlayerMovement`**:
  ```csharp
  [Header("Viewport Clamping (Endless Mode)")]
  public bool clampToViewport = false;
  public Vector2 minViewport = new Vector2(0.05f, 0.08f);
  public Vector2 maxViewport = new Vector2(0.95f, 0.92f);
  public float bottomKillViewportY = 0.02f;
  ```
- **FixedUpdate Clamping Logic**:
  - If `clampToViewport && cam != null`:
    ```csharp
    Vector3 minWorld = cam.ViewportToWorldPoint(new Vector3(minViewport.x, minViewport.y, 0f));
    Vector3 maxWorld = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, maxViewport.y, 0f));
    nextPosition.x = Mathf.Clamp(nextPosition.x, minWorld.x, maxWorld.x);
    nextPosition.y = Mathf.Clamp(nextPosition.y, minWorld.y, maxWorld.y);
    ```
  - Else if `clampToBounds`:
    ```csharp
    nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x, maxBounds.x);
    nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y, maxBounds.y);
    ```
- **Bottom Push / Kill Plane**:
  - If player is at `viewport.y <= minViewport.y` (0.08), the player's Y position is carried forward by camera translation so the player is smoothly pushed ahead.
  - If player is trapped behind impassable obstacles and their world position falls below `bottomKillViewportY` (e.g. `viewport.y < 0.02f` or behind screen):
    - Inflict damage: `GetComponent<PlayerHealth>()?.TakeDamage(1);`
    - If HP drops to 0, triggers `PlayerHealth.Die()` -> `GameManager.TriggerGameOver()`.

### Component 3: Dynamic Arena Boundaries for Grenades & Shooters [R1, R2]
- `GrenadeThrower.cs`: Add `public bool dynamicCameraBounds = false;`. When `true`, clamp throw target to `cam.ViewportToWorldPoint` margins instead of static `arenaMin/arenaMax`.
- `GrenadePickup.cs`: In `Start()`, only clamp to `arenaMin/arenaMax` if `clampToStaticArena` is true (default `false` for newly dropped pickups in scrolling mode, or allow runtime override).
- `ShooterEnemy.cs`: Add `public bool dynamicCameraBounds = false;`. When `true`, clamp kiting target to active segment or camera viewport margins.

### Component 4: Modular Map Segment System & Zero-GC Object Pooling [R2]
- **`MapSegment.cs` Component**:
  - Properties: `float segmentLength = 20f;`, `Transform[] spawnPoints;`, `Transform corridorCenter;`.
  - Dimensions: Width = 22.0 units (fitting camera view), Height = 20.0 units.
  - Side Walls: Left boundary at $X \approx -10.5$ and Right boundary at $X \approx 10.5$ with solid BoxCollider2D components to prevent player escaping horizontally.
  - Passable Corridor: Guaranteed corridor width $\ge 4.0$ units with zero impassable obstacle overlap.
- **Segment Prefabs Required (Minimum 3 Distinct Types + 1 Boss Arena)**:
  1. `MapSegment_Straight.prefab`: Central corridor (width 6u) flanked by dense tree clusters.
  2. `MapSegment_Zigzag.prefab`: S-curving corridor (width 4.5u) shifting from left to right.
  3. `MapSegment_Pillars.prefab`: Dual-lane corridor split by central obstacle islands (each lane width 4.5u).
  4. `MapSegment_BossArena.prefab`: Enclosed 24x24 arena with boss spawn marker and camera lock trigger coordinate.
- **`MapSegmentPool.cs` / `MapManager.cs`**:
  - Pre-instantiates 2–3 copies of each prefab on awake/start.
  - Active list maintains 3–4 segments ahead of the camera (covering $60-80$ units).
  - Connection logic: Next segment positioned at `currentTopY`.
  - Recycling logic: When a segment's top edge passes below `cameraBottomY - cleanupMargin` (e.g. 10 units below camera), deactivate it (`SetActive(false)`) and return to pool. Zero runtime `Destroy` or `Instantiate` calls.

### Component 5: Enemy Spawning Adaptation to Upcoming Segments [R2, R3]
- In `EnemySpawner.cs`:
  - Provide dual spawn mode:
    1. **Segment Spawn Points**: When a new segment is activated ahead of the camera, sample its designated `spawnPoints` and spawn enemies based on score tier.
    2. **Dynamic Camera Perimeter**: If spawning off-screen, calculate perimeter coordinates dynamically relative to `cam.transform.position.y` ($Y \in [camTop, camTop + 5.0f]$), keeping player distance $\ge 6.0u$.
  - When `currentScore >= 500`:
    - Signal `MapManager` to spawn `MapSegment_BossArena`.
    - Set `bossSpawnPosition` to the arena's center marker.
    - Suppress regular enemy spawning by 50% (`isBossActive = true`).

### Component 6: Seamless Boss Arena Encounter & Resume Loop [R3]
- **Encounter Flow**:
  1. At 500 points, `MapManager` spawns `MapSegment_BossArena` ahead.
  2. Early telegraph warning: HUD displays `"BOSS APPROACHING!"`.
  3. When camera Y aligns with arena center, camera locks scrolling (`isScrollLocked = true`).
  4. Boss spawns at arena center; HUD displays `"BOSS ARENA"` status banner and reveals Boss Health Slider.
  5. Boss executes 360-degree radial barrage (16 bullets, 5.0 u/s, 22.5° spacing) with 0.5s yellow telegraph flash.
  6. Upon Boss defeat:
     - Drops 2 guaranteed grenades (`RollGrenadeDrop()` instantiated).
     - Awards 500 points (`base.Die()` -> `GameManager.AddScore(500)`).
     - Plays victory fanfare via `SoundManager.PlayVictorySFX()`.
     - Displays Victory modal ("BOSS SLAIN! +500 PTS").
  7. When player clicks "CONTINUE":
     - `GameManager.ResumeEndlessAfterBoss()` restores `Time.timeScale = 1.0f`.
     - `CameraController.isScrollLocked = false` unlocks camera scrolling (+Y).
     - HUD hides `"BOSS ARENA"` status banner.
     - `MapManager` resumes procedural segment spawning ahead.

### Component 7: HUD Indicators & Telegraphed Warnings [R4]
- **Extensions to `UIManager.cs`**:
  - `public Text distanceText;` — Displays `"DISTANCE: {m}m"`.
  - `public GameObject bossWarningBanner;` — Early telegraph `"BOSS APPROACHING!"`.
  - `public GameObject bossArenaStatusBanner;` — `"BOSS ARENA"` status banner while in arena.
  - Public API methods:
    - `public void UpdateDistance(float meters)`
    - `public void ShowBossWarning(bool show)`
    - `public void ShowBossArenaStatus(bool show)`
- **Canvas Setup**:
  - Add `DistanceText`, `BossWarningBanner`, `BossArenaBanner` under `Canvas/HUD`.

### Component 8: MapBounds Runtime Handling in `shooting.unity`
- `MapBounds` with `Wall_Top`, `Wall_Bottom`, `Wall_Left`, `Wall_Right` remains intact in the scene file to satisfy EditMode tests `M1-T2-05` and `ChallengerM1Tests`.
- On `StartGame()` at runtime, `Wall_Top.GetComponent<BoxCollider2D>().enabled = false;` (or moved to top of the first segment), allowing the player to freely proceed upward into the endless segments.

---

## 4. Caveats

1. **Unity Physics & FixedUpdate vs LateUpdate**:
   - Camera translation must occur in `FixedUpdate` (or camera interpolation in `LateUpdate` with Rigidbody2D interpolation set to `Interpolate`) to prevent visual jitter. Testing with Rigidbody2D movement showed smoothest results when camera moves in `FixedUpdate` alongside physics.
2. **Tilemap vs Modular Prefabs**:
   - The base scene uses Tilemaps (`floor` and `arvores`), but procedural segments are best built using modular prefabs (`GameObject` with SpriteRenderers / Tilemaps and BoxCollider2Ds). This allows instant instantiation/pooling without heavy runtime Tilemap mesh rebuilding.
3. **Screen Aspect Ratio Variability**:
   - Camera viewport calculation (`cam.ViewportToWorldPoint`) dynamically accounts for any screen aspect ratio (16:9, 16:10, 4:3), making viewport clamping robust across all display resolutions.
4. **Existing 421 Tests**:
   - Any refactoring must strictly avoid altering existing test expectations. All new features must be additive.

---

## 5. Conclusion

- **Feasibility**: High. The existing codebase is exceptionally well-structured, modular, and possesses full event-driven hooks (`OnHealthChanged`, `OnBossSpawned`, `OnBossHealthChanged`, `OnBossDefeatedEvent`, `OnEnemyKilledScore`).
- **Core Strategy**:
  1. Add `CameraController.cs` to auto-scroll Main Camera along +Y (2.0 to 3.5 u/s).
  2. Extend `PlayerMovement.cs` with an opt-in `clampToViewport` mode clamping to Viewport [0.05, 0.95] X and [0.08, 0.92] Y, with bottom edge push/kill mechanics.
  3. Create `MapSegment.cs` and 3 distinct segment prefabs (width 22u, height 20u, corridor $\ge 4.0u$) plus `MapSegment_BossArena`.
  4. Create `MapManager.cs` implementing zero-GC object pooling for segments.
  5. Connect `MapManager` and `EnemySpawner` so enemies spawn in upcoming segments, and boss arena triggers at 500 points.
  6. Extend `UIManager.cs` with distance tracking, "BOSS APPROACHING!" warning, and "BOSS ARENA" status banner.
  7. Maintain 100% pass rate on all 421 existing tests and introduce dedicated NUnit / E2E tests for R1–R4.

---

## 6. Verification Method

To independently verify this investigation and validate downstream implementation:

1. **Execute All Existing Unit & Adversarial Tests**:
   - Using unityMCP:
     `call_mcp_tool` ServerName: `unityMCP`, ToolName: `execute_menu_item`, Arguments: `{"menu_path": "E2E Tests/Run All Tests"}`
     `call_mcp_tool` ServerName: `unityMCP`, ToolName: `execute_menu_item`, Arguments: `{"menu_path": "E2E Tests/Run Tier 5 (Adversarial Hardening)"}`
   - Inspect console output: verify 385/385 E2E and 36/36 Adversarial tests pass with 0 failures.
2. **Inspect Scene Hierarchy & Camera Parameters**:
   - `call_mcp_tool` ServerName: `unityMCP`, ToolName: `read_resource`, Arguments: `{"Uri": "mcpforunity://scene/cameras"}`
   - Confirm Main Camera instance ID 39640, position `(1.96, 0.04, -10.0)`.
3. **Verify Viewport Bounds Mathematics**:
   - In Unity Editor console or test runner, verify `Camera.main.ViewportToWorldPoint(new Vector3(0.05f, 0.08f, 0f))` and `(0.95f, 0.92f, 0f)` correctly define margins matching specifications.
4. **Invalidation Conditions**:
   - Any modification to `PlayerMovement.minBounds` or `maxBounds` default values will fail `M1-T1-04`.
   - Any removal of `MapBounds/Wall_Top` in the scene file will fail `M1-T2-05`.
   - Any modification to `EnemySpawner.bossSpawnPosition` default values will fail `Milestone4Tests`.
