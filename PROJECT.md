# Project: Continuous Upward (+Y) Endless Scrolling Map System

## Architecture
- **Camera System**: `ScrollingCameraController.cs` attached to Main Camera. Translates camera along +Y in `FixedUpdate` from baseline `2.0f` u/s up to `3.5f` u/s. Supports `isScrollLocked` state for Boss Arena.
- **Player Viewport Clamping**: `PlayerMovement.cs` extension with `clampToViewport` mode. Clamps player to viewport bounds `X: [0.05, 0.95]`, `Y: [0.08, 0.92]`. Bottom push/kill threshold damages player (1 HP) or triggers game over if trapped behind moving screen. Preserves legacy `minBounds`/`maxBounds` defaults to keep 421 existing tests passing.
- **Modular Map Segments & Pooling**: `MapSegment.cs` and `MapManager.cs`. Standardized segment dimensions: Length = 20.0u, Width = 15.0u (`X: -7.5` to `+7.5`), guaranteed corridor width >= 4.0u. Prewarmed object pool (`MapSegmentPool`) recycles off-screen segments at `camY - 25u` with zero runtime GC allocation.
- **Boss Arena Encounter Loop**: At 500 points, normal segment generation halts, spawning `MapSegment_BossArena` (24x18). Camera aligns and locks to arena center. Boss spawns, executes 16-bullet 360° radial barrage (22.5° spacing, 5.0 u/s), drops 2 guaranteed grenades, awards 500 points, unlocks camera, and resumes endless scrolling on continue.
- **HUD & Telegraph Warnings**: `UIManager.cs` extension displaying distance travelled in meters (`DIST: 0000m`), early flashing warning `"BOSS APPROACHING!"` at >= 450 points / arena spawn, and `"BOSS ARENA"` status banner during locked encounter.
- **Test Automation & Quality Gate**: Dual verification via unityMCP `execute_menu_item` / `execute_code` (custom runner) and Unity Test Runner (`run_tests`). 100% pass on baseline 421 tests plus dedicated test suites for R1-R5.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Camera +Y Auto-Scroll | Main camera scrolls continuously along +Y at baseline 2.0 u/s smoothly in FixedUpdate | M1 | ORIGINAL_REQUEST R1 |
| 2 | Camera Speed Progression | Camera speed scales with distance travelled up to safety ceiling 3.5 u/s | M1 | ORIGINAL_REQUEST R1 |
| 3 | Camera Boss Arena Lock | Camera smoothly aligns and locks at Boss Arena center coordinate | M3 | ORIGINAL_REQUEST R1, R3 |
| 4 | Camera Unlock & Resume | Camera unlocks and resumes +Y scrolling after boss defeat | M3 | ORIGINAL_REQUEST R3 |
| 5 | Player Viewport Clamping | Clamps player position to camera viewport X [0.05, 0.95], Y [0.08, 0.92] | M1 | ORIGINAL_REQUEST R1 |
| 6 | Bottom Edge Push / Kill | Pushes player forward or inflicts 1 HP damage / Game Over if trapped at bottom | M1 | ORIGINAL_REQUEST R1 |
| 7 | Dynamic Weapon Bounds | Grenade throwing, pickup clamping, and shooter kiting adapt to moving camera viewport | M1 | Explorer 1 & 2 analysis |
| 8 | Modular MapSegment Prefabs | Interchangeable segment prefabs (length 20u, >= 3 distinct variants) | M2 | ORIGINAL_REQUEST R2 |
| 9 | Passable Corridor Guarantee | Every segment guarantees at least one unblocked corridor with width >= 4.0 units | M2 | ORIGINAL_REQUEST R2 |
| 10 | Zero-GC Segment Pooling | Segments behind cleanup distance (camY - 25u) deactivate and recycle into pool | M2 | ORIGINAL_REQUEST R2 |
| 11 | Ahead-of-Camera Enemy Spawning | Enemies spawn from segment SpawnPoints or dynamic perimeter ahead of camera | M2 | ORIGINAL_REQUEST AC |
| 12 | 500-Point Boss Trigger | Reaching 500 points halts standard segment generation and spawns Boss Arena ahead | M3 | ORIGINAL_REQUEST R3 |
| 12b | Prompt Boss Approach | `MapManager.QueueBossArena()` pulls the arena to 12u ahead of the camera (recycling unseen ahead-segments) so the boss arrives ~5s after 500 instead of ~15s later at 800 | M3 | Follow-up fix |
| 13 | Dedicated Boss Arena Segment | Enclosed arena segment (24u x 18u) with side and top walls and boss spawn point | M3 | ORIGINAL_REQUEST R3 |
| 14 | Radial Barrage Attack | Boss fires 16 bullets in 360° circle (22.5° step, 5.0 u/s) with 0.5s telegraph | M3 | ORIGINAL_REQUEST R3 |
| 15 | Boss Rewards & Continuation | Defeating boss drops 2 guaranteed grenades, +500 points, victory fanfare, resume loop. Victory auto-continues after 3s realtime (manual CONTINUE skips the wait); play-mode only so EditMode tests still assert the frozen modal | M3 | ORIGINAL_REQUEST R3 |
| 16 | Travelled Distance HUD Indicator | HUD displays vertical distance in meters (`DIST: {D4}m`) | M4 | ORIGINAL_REQUEST R4 |
| 17 | "BOSS APPROACHING!" Telegraph | Early flashing HUD banner appears when approaching 500 points | M4 | ORIGINAL_REQUEST R4 |
| 18 | "BOSS ARENA" Status Banner | Prominent HUD banner displayed while camera is locked in boss fight | M4 | ORIGINAL_REQUEST R4 |
| 19 | Test Automation Suite | Automated unit/integration tests for R1-R5 passing 100% via unityMCP | M5 | ORIGINAL_REQUEST R5 |
| 20 | 10+ Min Memory Stability | Zero runtime GC allocations and memory stability over long play sessions | M5 | ORIGINAL_REQUEST AC |
| 21 | Endless Floor Tiling | `EndlessFloor.cs` on `EndlessMapManager`: chunk 0 keeps full hand-designed floor art; chunks 1-7 are pure base-grass (1 tile pattern) for guaranteed-seamless endless ground, recycled by repositioning around camera (behind 20u / ahead 40u), zero runtime alloc. Chunks parented under the source Grid (Tilemap outside Grid does not render) | — | Follow-up request |
| 22 | GameOver/Victory Dim Fix | `GameOverPanel`/`VictoryPanel` background changed from opaque dark red/green (0.92) to neutral black 0.6 so the world stays visible behind end screens | — | Follow-up bugfix |
| 23 | Test Isolation Hardening | `E2ETestContext` snapshots scene + normalizes `Time.timeScale=1` on creation, sweeps untracked Instantiate orphans and restores clock on Dispose (kills order-dependence + EditMode scene trash like DummyPickup/BossBullet clones). Refuses to run in PlayMode (fail-fast guard) | — | Follow-up bugfix |
| 24 | GameOver/Victory Panels Rebuild | Panels were missing from scene file; rebuilt `GameOverPanel` (title/score/record/PLAY AGAIN/MAIN MENU) + `VictoryPanel` (banner/CONTINUE) cloning PausePanel styling, rewired all `UIManager` refs, removed 2 dead script slots on `EndlessMapManager` | — | Follow-up bugfix |
| 25 | Visible Segment Walls + 16:9 Lock | `MapSegmentPrefabBuilder.DressBoundaryWall` plants deterministic tree/rock lines over invisible side walls (16 per standard segment, 18 arena; visual-only, no colliders); duplicate root-level walls stripped. `ScrollingCameraController` letterbox/pillarbox locks 16:9 on any screen | — | Follow-up request |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Camera Scrolling & Viewport Clamping (R1) | `ScrollingCameraController.cs`, `PlayerMovement.cs` viewport clamping, dynamic weapon bounds | None | DONE |
| M2 | Modular Map Segments & Object Pooling (R2) | `MapSegment.cs`, 3 distinct segment prefabs, `MapManager.cs`, `MapSegmentPool.cs`, dynamic enemy spawning | M1 | DONE |
| M3 | Boss Arena Encounter & Resume Loop (R3) | `MapSegment_BossArena.prefab`, 500-pt trigger, camera lock/unlock, 16-bullet barrage, rewards, resume endless | M1, M2 | DONE |
| M4 | HUD Indicators & Telegraphed Warnings (R4) | `UIManager.cs` extensions, Distance meter, Boss Warning banner, Boss Arena banner | M1, M3 | DONE |
| M5 | E2E Integration & Verification (R5) | Comprehensive E2E test suite (Tiers 1-5), memory stability validation, 100% test pass | M1, M2, M3, M4 | DONE |

## Interface Contracts

### `ScrollingCameraController` ↔ `PlayerMovement`
```csharp
public class ScrollingCameraController : MonoBehaviour
{
    public float baselineSpeed = 2.0f;
    public float maxSpeed = 3.5f;
    public float CurrentSpeed { get; }
    public float DistanceTravelled { get; }
    public bool isScrollLocked { get; set; }
    public void LockAt(float worldY);
    public void UnlockAndResume();
}
```

### `MapManager` ↔ `ScrollingCameraController`
- `MapManager` queries `camera.transform.position.y` to determine `nextSpawnY` and `cleanupDistance`.
- When Boss Arena is placed, `MapManager` informs `ScrollingCameraController` of `bossArenaCenterY`.

### `MapManager` ↔ `EnemySpawner`
- When a new `MapSegment` is activated, `EnemySpawner` receives `segment.enemySpawnPoints`.
- When score reaches 500, `EnemySpawner` / `GameManager` triggers `MapManager.SpawnBossArena()`.

### `BossController` ↔ `GameManager` ↔ `ScrollingCameraController`
- `BossController.OnBossDefeatedEvent` invokes `GameManager.TriggerVictory()`.
- Dismissing Victory or clicking Continue invokes `GameManager.ResumeEndlessAfterBoss()`, which calls `cameraController.UnlockAndResume()` and resumes `MapManager` segment spawning.

### `UIManager` HUD Contracts
```csharp
public void UpdateDistance(float meters);
public void ShowBossWarning(bool show);
public void ShowBossArenaStatus(bool show);
```

## Code Layout
- `Assets/scripts/ScrollingCameraController.cs`: Camera scrolling, speed scaling, arena locking.
- `Assets/scripts/PlayerMovement.cs`: Additive viewport clamping and bottom kill plane.
- `Assets/scripts/MapSegment.cs`: Component defining segment length (20u), corridor width (>=4u), and spawn points.
- `Assets/scripts/MapSegmentPool.cs`: Zero-GC prewarmed object pool for segments.
- `Assets/scripts/MapManager.cs`: Spawns, aligns, recycles segments, handles 500-pt boss arena spawn.
- `Assets/scripts/UIManager.cs`: Extended with Distance, Boss Warning, and Boss Arena banner UI bindings.
- `Assets/scripts/EndlessFloor.cs`: Pooled endless floor tiling following the scrolling camera.
- `Assets/Prefabs/MapSegments/`:
  - `MapSegment_Corridor.prefab`
  - `MapSegment_ChokePoint.prefab`
  - `MapSegment_Slalom.prefab`
  - `MapSegment_BossArena.prefab`
- `Assets/scripts/Tests/ScrollingMapTests.cs`: Dedicated automated unit and integration tests covering R1-R5.
