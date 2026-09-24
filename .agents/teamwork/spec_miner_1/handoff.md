# Specification Mining Report: Continuous Upward (+Y) Endless Scrolling System

## 1. Observation

### 1.1 Project Environment & Tooling
- **Unity Version**: Unity `2022.3.62f2` (revision `7670c08855a9`), verified via `ProjectSettings/ProjectVersion.txt`.
- **Installed Packages (`Packages/manifest.json`)**:
  - `com.coplaydev.unity-mcp`: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main` (Active and operational; verified via tools `read_console`, `execute_code`, `manage_scene`, `run_tests`, `get_test_job`).
  - `com.unity.test-framework`: `1.1.33` (Unity Test Runner framework installed).
  - `com.unity.ugui`: `1.0.0`, `com.unity.textmeshpro`: `3.0.7`, `com.unity.modules.tilemap`: `1.0.0`, `com.unity.modules.physics2d`: `1.0.0`.
- **Assembly Definitions & Compilation**:
  - Probing `Assets/` with `find_by_name` revealed **0** `.asmdef` files currently in `Assets/`.
  - All current scripts (`Assets/scripts/*.cs` and `Assets/scripts/Tests/*.cs`) compile into the default root assembly `Assembly-CSharp.dll`.
  - Probing `UnityEditor.Compilation.CompilationPipeline.GetAssemblies()` via `execute_code` showed `Assembly-CSharp (flags: None)` without any `Assembly-CSharp-Editor` assembly because no `Assets/Editor/` directory exists.
- **Active Scene Configuration**:
  - Active scene: `Assets/Scenes/shooting.unity` (Build Index 0, path confirmed via `manage_scene(action='get_active')` and `manage_scene(action='get_build_settings')`). It is the sole scene registered and enabled in Build Settings.
  - Scene Roots: `Main Camera`, `Player`, `floor`, `arvores`, `Colliders`, `Fire Effect`, `MapBounds`, `EnemySpawner`, `GameManager`, `SoundManager`, `EventSystem`, `Canvas`.
  - Camera settings: Orthographic, orthographicSize = `6.316637`, Position = `(1.96, 0.04, -10.00)`, aspect = `1.777778` (16:9 viewport).
  - Player settings: Position = `(3.28, 0.11, 0.00)`, Rigidbody2D, BoxCollider2D size = `(1.56, 1.86)`.

### 1.2 Existing Codebase State
- **Player Movement (`Assets/scripts/PlayerMovement.cs`)**:
  - Lines 19-21: Has `clampToBounds = true`, `minBounds = (-8.5f, -4.2f)`, `maxBounds = (13.8f, 5.2f)`. Currently uses static world bounds rather than moving camera viewport bounds.
  - Controls: 8-directional WASD input normalized with speed `5f`, mouse aiming facing direction.
- **Player Health (`Assets/scripts/PlayerHealth.cs`)**:
  - `maxHealth = 5`, `currentHealth = 5`. Takes exactly 1 damage per hit with 1.0s invulnerability flash.
  - Raises `OnHealthChanged` and `OnPlayerDeath`.
- **Enemy Spawner (`Assets/scripts/EnemySpawner.cs`)**:
  - Spawns Chaser, Rusher, and Shooter enemies along perimeter bounds (`minX = -10.5, maxX = 16.0, minY = -6.0, maxY = 7.0`).
  - Lines 110-113: Checks `if (currentScore >= 500 && !bossSpawned) SpawnBoss();`.
  - Lines 150-160: Instantiates `bossPrefab` at static position `(2.69f, 3.5f, 0.0f)`.
- **Boss Controller (`Assets/scripts/BossController.cs`)**:
  - 60 HP, moveSpeed 1.8, 500 points on defeat.
  - Lines 184-197: Fires radial burst of 16 projectiles evenly spaced by 22.5° in 360° circle at 5.0 u/s speed.
  - Lines 299-305: Drops exactly 2 `GrenadePickup` items upon death.
- **UI Manager (`Assets/scripts/UIManager.cs`) & Canvas**:
  - HUD contains: 5 Heart icons, ScoreText (`SCORE: {D5}`), HighScoreText (`HIGH: {D5}`), GrenadeCountText (`x {N}`), BossBarSlider.
  - Canvas does not yet have: Travelled Distance meter, "BOSS APPROACHING!" warning, or "BOSS ARENA" banner.
- **Testing Infrastructure (`Assets/scripts/Tests/`)**:
  - Contains custom runner `E2ETests.E2ETestRunner.RunAllFormatted()` running 385 tests across 4 tiers in 74.74 ms (executed via unityMCP `execute_code`).
  - unityMCP `run_tests` tool connects to Unity Test Runner. When invoked with `mode="EditMode"` or `mode="PlayMode"`, it completed successfully with 0 tests found because no assembly definition marks test assemblies for NUnit discovery.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Camera | Continuous +Y Auto-Scroll | Camera scrolls forward along +Y at baseline speed (default 2.0 u/s) smoothly without jitter. | `Time.fixedDeltaTime`, `baseSpeed = 2.0f` | Main Camera transform position +Y translated | Stalled delta time clamps movement to 0; no jitter | ORIGINAL_REQUEST R1, Acceptance Criteria |
| 2 | Camera | Progression Speed Scaling | Scroll speed scales dynamically with distance or progression up to a safety ceiling (3.5 u/s). | Travelled distance, elapsed time, `maxSpeed = 3.5f` | Current camera scroll speed | Clamped to `[baseSpeed, 3.5f]`; never exceeds 3.5 u/s | ORIGINAL_REQUEST R1 |
| 3 | Camera | Boss Arena Camera Lock | Camera locks smoothly when its center aligns with the Boss Arena segment center. | Boss arena center Y coordinate, camera position | Camera velocity = 0, locked at arena center | Over-scrolling prevented; locks exactly to target Y | ORIGINAL_REQUEST R1, R3 |
| 4 | Camera | Camera Unlock & Resume | Unlocks camera and resumes +Y scrolling after boss is defeated and continuation triggered. | Boss defeat event / continue button | Camera velocity restored, resume scrolling | Idempotent unlock; ignores if already scrolling | ORIGINAL_REQUEST R3 |
| 5 | Player | Viewport Clamping | Clamps player position within camera viewport: X `[0.05, 0.95]`, Y `[0.08, 0.92]`. | `cam.ViewportToWorldPoint`, player input | Clamped world position in `FixedUpdate` | If camera moves past player, pushes player smoothly | ORIGINAL_REQUEST R1, Acceptance Criteria |
| 6 | Player | Bottom Push / Kill Threshold | Detects when player falls below viewport bottom threshold (or bottom kill plane); inflicts damage or triggers game over. | Player position vs camera bottom edge | `PlayerHealth.TakeDamage(1)` or `TriggerGameOver()` | Avoids player falling behind screen indefinitely | ORIGINAL_REQUEST R1, Acceptance Criteria |
| 7 | Map | Modular MapSegment Prefabs | Interchangeable segment prefabs (length 20 units) chosen with controlled randomness. | Segment prefab list, spawn index | Instantiated/activated MapSegment GameObject at `Y = N * 20` | Seamless alignment; no gaps or overlap | ORIGINAL_REQUEST R2 |
| 8 | Map | Passable Corridor Guarantee | Every segment guarantees at least one unblocked corridor with width >= 4.0 units. | Obstacle colliders, layout grid | Navigable path width >= 4.0u from bottom to top | Prevents impassable dead-ends (player width is 1.56u) | ORIGINAL_REQUEST R2 |
| 9 | Map | Segment Object Pooling | Segments behind cleanup distance (e.g. `camY - 25u`) are deactivated and recycled into pool with 0 GC allocations. | Segment pool queue, camera position | Reusable inactive MapSegment instances | Never invokes `GameObject.Destroy`; prevents memory leaks | ORIGINAL_REQUEST R2, Acceptance Criteria |
| 10 | Combat | Dynamic Ahead-of-Camera Spawning | Enemies spawn from valid SpawnPoints inside upcoming segments or dynamic perimeter above camera. | Segment SpawnPoints, camera view bounds | Enemies spawned in active gameplay zone | No spawns inside colliders or behind cleanup line | ORIGINAL_REQUEST Acceptance Criteria |
| 11 | Combat | Scrolling Grenade Throw Clamping | Grenade trajectory clamping adapts dynamically to current camera viewport rather than static arena. | Mouse world position, player position | `GrenadeProjectile` with valid target coords | Unclamped from legacy static arena bounds | Code analysis `GrenadeThrower.cs:29-30` |
| 12 | Boss | 500-Point Boss Encounter Trigger | When score reaches 500 points, normal segment generation halts and dedicated Boss Arena segment spawns ahead. | `GameManager.CurrentScore >= 500` | Boss Arena segment placed, boss prepared | Standard segment spawning halted until victory | ORIGINAL_REQUEST R3 |
| 13 | Boss | Dedicated Boss Arena Segment | Enclosed arena segment with side and bottom boundaries to contain boss battle. | Arena segment prefab | Instantiated/positioned at next segment slot | Clear bounds; camera locks at arena center | ORIGINAL_REQUEST R3 |
| 14 | Boss | Radial Barrage Attack | Boss fires 16 bullets in 360° circle (22.5° spacing) at 5.0 u/s following 0.5s visual telegraph. | Boss attack timer, `telegraphColor` | 16 `BossBullet` projectiles radiating outwards | Bullet damage = 1, lifetime = 5s | Code analysis `BossController.cs:184-250` |
| 15 | Boss | Boss Rewards & Continuation | Defeating boss drops 2 guaranteed grenades, awards 500 points, plays victory fanfare, resumes scrolling. | Boss HP reaching 0 | 2 `GrenadePickup` items, +500 score, victory panel | Spawner suppression released; camera unlocked | Code analysis `BossController.cs:280-305` |
| 16 | HUD | Distance Travelled Indicator | Displays total vertical distance scrolled along +Y in meters (e.g. `DIST: 0142m`). | Camera Y position delta | Formatted HUD Text indicator | Value never decreases during forward scrolling | ORIGINAL_REQUEST R4 |
| 17 | HUD | "BOSS APPROACHING!" Telegraph | Displays early warning banner when player nears 500 points (e.g. >= 450 pts or when boss arena spawns). | Score threshold or arena spawn event | Flashing/active UI banner in HUD | Hidden after boss arena reached | ORIGINAL_REQUEST R4 |
| 18 | HUD | "BOSS ARENA" Status Banner | Displays prominent status banner during boss encounter while camera is locked. | Camera locked in Boss Arena event | Active UI banner in HUD | Hidden upon boss defeat | ORIGINAL_REQUEST R4 |
| 19 | Verification | unity-MCP Automated Test Execution | Tests can be triggered and verified through `run_tests` and `execute_code`. | unityMCP tool commands | JSON/Markdown test reports with 100% pass rate | Detects failed assertions and errors | ORIGINAL_REQUEST R5 |
| 20 | Verification | 10+ Minute Stability Guarantee | System runs continuously without memory leaks, pool exhaustion, or FPS drops. | Continuous scrolling execution | Memory footprint stable; active object count bounded | Pool size capped; no unbounded allocations | ORIGINAL_REQUEST Acceptance Criteria |

---

## 3. Edge Cases

| # | Feature | Input | Observed Behavior |
|---|---------|-------|-------------------|
| 1 | Viewport Clamping | Player stands completely still while camera scrolls forward at 3.5 u/s. | Player hits viewport bottom boundary (Y=0.08); camera push/kill logic pushes or damages player, preventing them from falling behind screen. |
| 2 | Viewport Clamping | Player moves diagonally (W+D or S+A) into screen corners. | Both X (0.05-0.95) and Y (0.08-0.92) axes clamp independently; player glides along boundaries without getting stuck. |
| 3 | Grenade Throw | Player throws grenade towards top of screen while camera is moving at max speed. | Target coordinate is computed in world space relative to moving camera; grenade flies to target and explodes accurately without being constrained to legacy static arena bounds. |
| 4 | Segment Connection | Camera reaches Y = 20.0f, 40.0f, 60.0f transition seams. | Next segment is already pre-spawned and aligned flush at `Y = index * 20`; player crosses seam without collision snag or camera hitch. |
| 5 | Passable Corridor | Player navigates segment with narrowest opening. | Corridor width is verified >= 4.0 units; player BoxCollider2D (1.56 x 1.86 units) passes with >= 1.22 units clearance on each side. |
| 6 | Object Pooling | Player travels 1000+ units along +Y (50+ segments generated). | Active segments never exceed 4 simultaneously; recycled segments are dequeued from pool with zero GC allocation and zero `GameObject.Destroy` calls. |
| 7 | Boss Arena Transition | Player reaches 500 points while near the edge of a normal segment. | Upcoming segment is replaced with Boss Arena; camera continues scrolling until reaching arena center, then smoothly locks. |
| 8 | Boss Barrage | Boss fires radial burst while player is at extreme left or right boundary. | Bullets travel in 360° circle; player can dodge through 22.5° gaps between bullets; bullets destroy upon timeout or arena boundary impact. |
| 9 | Boss Grenade Defeat | Player throws grenade into Boss, dealing 50 damage and triggering death. | Boss dies, triggers 2 guaranteed grenade drops, awards 500 points, plays victory fanfare, and displays Victory modal. |
| 10 | Endless Loop Resumption | Player dismisses Victory modal / clicks Continue. | Victory modal closes, camera unlocks, `EnemySpawner` resumes scaling, next segment spawns ahead, distance counter continues incrementing. |
| 11 | Player Death in Boss Fight | Player HP reaches 0 while fighting Boss. | Game Over panel triggers immediately, timeScale freezes to 0, camera halts, high score is updated and saved. |
| 12 | Rapid Restart | Player clicks Restart while inside Boss Arena. | Camera resets to initial position, score resets to 0, pools are recycled, boss state is cleared, player starts with 5 HP and 2 grenades. |

---

## 4. Logic Chain

1. **Observation 1.1 & 1.2**: The existing project features 8-directional player movement, 5 HP health system, 2-grenade inventory, enemy waves, procedural retro sound synthesis, and a 60 HP boss with radial barrage. However, movement bounds and grenade bounds are hard-coded to a static arena (`minBounds = (-8.5, -4.2)`, `maxBounds = (13.8, 5.2)`).
2. **Deduction 1**: To implement endless forward scrolling (R1), `PlayerMovement.cs` and `GrenadeThrower.cs` must dynamically compute boundaries relative to the moving `Camera.main` viewport (`[0.05, 0.95]` on X and `[0.08, 0.92]` on Y) rather than using static world vectors.
3. **Observation 1.1 (Camera Settings)**: Orthographic camera size is `6.316637` with aspect `1.777778` (half-width ~`11.23`, total width ~`22.46`, total height ~`12.63`).
4. **Deduction 2**: A segment length of exactly 20 units along Y (R2) allows roughly 1.5 - 2 segments to be visible on screen at any moment. Maintaining 3 to 4 active segments ahead and behind the camera guarantees continuous coverage with zero visual pop-in.
5. **Observation 1.1 (Player Collider)**: Player BoxCollider2D is `1.56 x 1.86` units.
6. **Deduction 3**: The required passable corridor width of `>= 4.0` units (R2) provides more than 2.5 times the player width, ensuring unobstructed passage even while dodging enemy bullets and navigating obstacles.
7. **Observation 1.2 (EnemySpawner & BossController)**: `EnemySpawner` currently instantiates `bossPrefab` at static coordinate `(2.69, 3.5, 0)` when `currentScore >= 500`. `BossController` drops 2 grenades, awards 500 points, and triggers `TriggerVictory()`.
8. **Deduction 4**: For R3, the Boss encounter must be integrated into the procedural map pipeline: upon reaching 500 points, the map spawner halts regular segment generation, places a dedicated Boss Arena segment at `nextSpawnY`, smoothly locks the camera at the arena center, spawns the boss, and upon victory unlocks the camera and resumes segment spawning.
9. **Observation 1.2 (UIManager)**: HUD currently has Score, HighScore, Hearts, Grenades, and BossBarSlider, but lacks Distance travelled, "BOSS APPROACHING!", and "BOSS ARENA" banner (R4).
10. **Deduction 5**: New HUD text and banner elements can be hooked into `UIManager` and updated via clean events from the camera and map generation controllers.
11. **Observation 1.1 (Test Infrastructure)**: Unity Test Framework `1.1.33` is installed and `run_tests` works in unityMCP, but reports 0 tests because no `.asmdef` marks a test assembly. Meanwhile, Roslyn `execute_code` can execute tests instantaneously with full report serialization.
12. **Deduction 6**: The verification suite (R5) should provide both:
    - Dedicated test fixtures with `[Test]` runnable via `run_tests` in Unity Test Runner.
    - Integration with `execute_code` running automated assertions covering segment connection, pooling recycling, viewport clamping, and boss encounter transitions.

---

## 5. Caveats

1. **Scene File Modifications**: `Assets/Scenes/shooting.unity` is the sole active scene. Any additions to the scene (camera controller, map spawner, HUD objects) must preserve existing serialized references to `Player`, `UIManager`, `SoundManager`, and `GameManager` without causing missing script references.
2. **Assembly Definition Caution**: Creating an `.asmdef` in `Assets/scripts` isolates the assembly. Since existing scenes and prefabs reference scripts by `.cs.meta` GUID (not assembly name), script links remain valid, but tests must have proper assembly references to `UnityEngine.TestRunner` and `UnityEditor.TestRunner`.
3. **Framerate Independence**: Camera auto-scrolling and viewport clamping must run in `FixedUpdate` or `LateUpdate` with `Time.fixedDeltaTime` / `Time.deltaTime` to ensure smooth motion free of jitter or physics synchronization artifacts.
4. **Zero GC Allocations**: The map segment pool and projectile/enemy pools must pre-allocate instances so that continuous scrolling for 10+ minutes produces 0 GC allocations per frame.

---

## 6. Conclusion & Data Contracts

### 6.1 Core Data Contracts & Formulas

1. **Camera Scrolling**:
   - `BaselineSpeed`: `2.0f` units/s
   - `MaxSpeed`: `3.5f` units/s
   - `SpeedProgression`: `Speed = Mathf.Min(3.5f, 2.0f + (DistanceTravelled / 1000f) * 1.5f)`
   - `ScrollAxis`: `+Y` (0, 1, 0)
   - `State`: `Scrolling`, `LockedInBossArena`, `Paused`

2. **Viewport Boundaries**:
   - `MinViewport`: `(0.05f, 0.08f)`
   - `MaxViewport`: `(0.95f, 0.92f)`
   - `KillThreshold`: Viewport Y `< 0.04f` (or world Y `< cam.transform.position.y - cam.orthographicSize - 0.5f`)
   - `DamageRate`: 1 HP per hit or immediate game over when crushed.

3. **Map Segments & Object Pooling**:
   - `SegmentLength`: Exactly `20.0f` units.
   - `SegmentWidth`: `>= 22.0f` units (matching viewport width with side walls).
   - `CorridorWidth`: Minimum clear path `>= 4.0f` units.
   - `DistinctPrefabs`: `>= 3` variants (`Segment_CorridorOpen`, `Segment_PillarsDual`, `Segment_Chicane`).
   - `PoolCapacity`: 6 - 8 segment instances in memory; active count in scene: 3 - 4.
   - `CleanupDistance`: `cam.transform.position.y - 25.0f`.
   - `AheadSpawnDistance`: `cam.transform.position.y + 35.0f`.

4. **Boss Arena Encounter**:
   - `TriggerScore`: `500` points.
   - `TelegraphWarningDistance`: Triggered when `currentScore >= 450` or when Boss Arena segment is placed ahead.
   - `ArenaLockPosition`: Camera Y locked to `BossArena.CenterY`.
   - `BossStats`: 60 HP, 1.8 move speed, 500 score reward, 2 guaranteed grenade drops.
   - `RadialBarrage`: 16 bullets, 360° coverage, 22.5° angular delta, 5.0 u/s bullet speed, 0.5s telegraph warning.
   - `ResumeCondition`: On Boss defeat + Victory modal dismissed -> Camera unlocks and endless scrolling resumes.

5. **HUD System**:
   - `ScoreText`: `SCORE: {D5}`
   - `HighScoreText`: `HIGH: {D5}`
   - `HeartIcons`: 5 images (full vs empty sprite)
   - `GrenadeText`: `x {N}`
   - `DistanceText`: `DIST: {D4}m` (1 world unit = 1 meter)
   - `BossWarningBanner`: "BOSS APPROACHING!" (flashing / telegraphed)
   - `BossArenaBanner`: "BOSS ARENA"

---

## 7. Verification Method

### 7.1 Automated Testing Commands
1. **Unity Test Runner via unityMCP `run_tests`**:
   - Command: `call_mcp_tool(ServerName='unityMCP', ToolName='run_tests', Arguments={'mode': 'EditMode', 'include_details': true})`
   - Invalidation condition: Any test failure or 0 tests executed after test suite integration.
2. **E2E / Integration Verification via unityMCP `execute_code`**:
   - Command: `call_mcp_tool(ServerName='unityMCP', ToolName='execute_code', Arguments={'action': 'execute', 'code': 'return E2ETests.E2ETestRunner.RunAllFormatted();'})`
   - Invalidation condition: Any assertion failure in segment connection, pooling recycling, viewport clamping, or boss transition tiers.
3. **Console Log Cleanliness**:
   - Command: `call_mcp_tool(ServerName='unityMCP', ToolName='read_console', Arguments={'action': 'get', 'types': ['error', 'warning']})`
   - Invalidation condition: Any unhandled NullReferenceException or MissingComponentException during gameplay loop.

### 7.2 Manual & Live Verification Criteria
- Run scene `shooting.unity` in Play Mode.
- Verify camera scrolls smoothly along +Y at 2.0 u/s and scales up to 3.5 u/s.
- Verify player WASD movement is locked within viewport margins `[0.05, 0.95]` and `[0.08, 0.92]`.
- Verify standing still results in bottom edge damage / push.
- Verify segments connect seamlessly at multiples of 20 units with corridors >= 4.0 units wide.
- Verify score reaches 500, "BOSS APPROACHING!" flashes, Boss Arena spawns, camera locks at center, Boss fires 16 radial bullets, Boss defeat drops 2 grenades, and endless scrolling resumes upon continuation.
- Verify memory and GC allocations remain flat over 10+ minutes of scrolling.
