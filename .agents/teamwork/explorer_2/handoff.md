# Investigation & Architecture Handoff Report — Explorer 2

**Author**: `explorer_2` (teamwork_preview_explorer)  
**Date**: 2026-09-22  
**Target Milestone**: Endless Scrolling Map System, Modular Segments, Pooling, & Boss Arena Architecture  
**Working Directory**: `.agents/teamwork/explorer_2/`  
**Reference Request**: `.agents/teamwork/ORIGINAL_REQUEST.md`  

---

## 1. Observation

### 1.1 Existing Prefabs Catalog
Investigation across the project (`Assets/`, `Assets/Prefabs/`, `Assets/Scenes/`, `Assets/tilemap/`) revealed **13 prefabs**:

| Asset Path | Root Object | Key Components | 2D Colliders & Rigidbodies | Tags & Layers |
|---|---|---|---|---|
| `Assets/Prefabs/BossEnemy.prefab` | `BossEnemy` | `Transform`, `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `BossController` | `CircleCollider2D` (Radius: 0.9, IsTrigger: False), `Rigidbody2D` (Mass: 5, Gravity: 0, FreezeRotation, Continuous) | Tag: `Enemy`, Layer: `Default` (0) |
| `Assets/Prefabs/ChaserEnemy.prefab` | `ChaserEnemy` | `Transform`, `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `ChaserEnemy` | `CircleCollider2D` (Radius: 0.5, IsTrigger: False), `Rigidbody2D` (Mass: 1, Gravity: 0, FreezeRotation) | Tag: `Enemy`, Layer: `Default` (0) |
| `Assets/Prefabs/ShooterEnemy.prefab` | `ShooterEnemy` | `Transform`, `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `ShooterEnemy` | `CircleCollider2D` (Radius: 0.5, IsTrigger: False), `Rigidbody2D` (Mass: 1, Gravity: 0, FreezeRotation) | Tag: `Enemy`, Layer: `Default` (0) |
| `Assets/Prefabs/RusherEnemy.prefab` | `RusherEnemy` | `Transform`, `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `RusherEnemy` | `CircleCollider2D` (Radius: 0.5, IsTrigger: False), `Rigidbody2D` (Mass: 1, Gravity: 0, FreezeRotation) | Tag: `Enemy`, Layer: `Default` (0) |
| `Assets/Prefabs/EnemyBullet.prefab` | `EnemyBullet` | `Transform`, `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `EnemyBullet` | `CircleCollider2D` (Radius: 0.25, IsTrigger: True), `Rigidbody2D` (Mass: 1, Gravity: 0, Continuous) | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/Scenes/BossBullet.prefab` | `BossBullet` | `Transform`, `SpriteRenderer`, `CircleCollider2D`, `Rigidbody2D`, `EnemyBullet` | `CircleCollider2D` (Radius: 0.25, IsTrigger: True), `Rigidbody2D` (Mass: 1, Gravity: 0, Continuous) | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/Bullet.prefab` | `Bullet` | `Transform`, `SpriteRenderer`, `Rigidbody2D`, `BoxCollider2D`, `Bullet` | `BoxCollider2D` (Size: 1.0x1.0, IsTrigger: False), `Rigidbody2D` (Mass: 1, Gravity: 0) | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/Prefabs/GrenadeProjectile.prefab` | `GrenadeProjectile` | `Transform`, `SpriteRenderer`, `CircleCollider2D`, `Rigidbody2D`, `GrenadeProjectile` | `CircleCollider2D` (Radius: 0.25, IsTrigger: True), `Rigidbody2D` (BodyType: Kinematic, Gravity: 0) | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/Prefabs/GrenadePickup.prefab` | `GrenadePickup` | `Transform`, `SpriteRenderer`, `CircleCollider2D`, `Rigidbody2D`, `GrenadePickup` | `CircleCollider2D` (Radius: 0.5, IsTrigger: True), `Rigidbody2D` (BodyType: Dynamic, Gravity: 0) | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/Prefabs/ExplosionAoE.prefab` | `ExplosionAoE` | `Transform`, `ExplosionAoE` | No attached collider on root; queries via `Physics2D.OverlapCircleAll(origin, 3.5f)` | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/Fire Effect.prefab` | `Fire Effect` | `Transform`, `Animator`, `SpriteRenderer` | None (visual effect only) | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/tilemap/environment.prefab` | `environment` | `Transform`, `Grid`, Child: `Layer1` (`Tilemap`, `TilemapRenderer`) | No colliders | Tag: `Untagged`, Layer: `Default` (0) |
| `Assets/tilemap/floor.prefab` | `floor` | `Transform`, `Grid`, Child: `Layer1` (`Tilemap`, `TilemapRenderer`) | No colliders | Tag: `Untagged`, Layer: `Default` (0) |

*Note on Player*: The `Player` entity currently exists **directly in the scene `Assets/Scenes/shooting.unity`** (instanceID 39754 / root item 2), rather than as a standalone prefab asset in `Assets/Prefabs/`.

### 1.2 Scene Hierarchy & Existing Environment Construction
Inspected `Assets/Scenes/shooting.unity` via `manage_scene` and `read_resource`:
- **Main Camera**: Orthographic = true, `orthographicSize = 6.3166`, Aspect Ratio = 1.7778 (16:9), Position = `(1.96, 0.04, -10.0)`. Visible vertical span = `2 * 6.3166 ≈ 12.63` units; visible horizontal span = `12.63 * 1.7778 ≈ 22.46` units.
- **Player Setup**:
  - Components: `Transform`, `SpriteRenderer` (order 1), `Rigidbody2D` (Dynamic, freeze rotation, constraints = 4), `BoxCollider2D` (size `(1.56, 1.86)`, `isTrigger = false`), `PlayerMovement`, `Shooting`, `PlayerHealth` (maxHealth = 5), `GrenadeThrower`.
  - Child: `Fire Point` at local position `(0.35, 1.18, 0.0)`.
- **Environment & Colliders**:
  - `floor` (Grid -> Layer1 with Tilemap): Background ground tiles.
  - `arvores` (Grid -> Layer1 with Tilemap): Visual decorative trees.
  - `Colliders` (GameObject tagged `Colliders`):
    - `Colliders/Arvore`: `SpriteRenderer`, `CapsuleCollider2D` (world scale: `(3.68, 2.22, 1.0)`).
    - `Colliders/Arbusto`: `SpriteRenderer`, `CapsuleCollider2D` (world scale: `(1.06, 0.58, 1.0)`).
    - `Colliders/Cerca`: `SpriteRenderer`, `BoxCollider2D` (world scale: `(3.53, 0.45, 1.0)`).
  - `MapBounds` (GameObject tagged `Colliders`, parent scale: `1.6128`):
    - `Wall_Top`: Position `(2.69, 8.95, 0)`, `BoxCollider2D` size `(26.0, 1.0)` -> world width `41.93`.
    - `Wall_Bottom`: Position `(2.69, -8.79, 0)`, `BoxCollider2D` size `(26.0, 1.0)` -> world width `41.93`.
    - `Wall_Left`: Position `(-17.47, 0.08, 0)`, `BoxCollider2D` size `(1.0, 12.0)` -> world height `19.35`.
    - `Wall_Right`: Position `(22.85, 0.08, 0)`, `BoxCollider2D` size `(1.0, 12.0)` -> world height `19.35`.
  - Arena dimensions: X from `-17.47` to `22.85` (total width = `40.32`, center X = `2.69`); Y from `-8.79` to `8.95` (total height = `17.74`, center Y = `0.08`).

### 1.3 ProjectSettings & Physics Collision Matrix
Inspected `ProjectSettings/TagManager.asset` and `ProjectSettings/Physics2DSettings.asset`:
- **Tags**: `Colliders`, `Enemy`, plus standard built-ins (`Player`, `MainCamera`, `Untagged`).
- **Layers**: Only built-in layers are assigned (`Default`: 0, `TransparentFX`: 1, `Ignore Raycast`: 2, `Water`: 4, `UI`: 5). User layers 8-31 are blank.
- **Sorting Layers**: Only `Default` (uniqueID: 0) exists.
- **Physics2D**: `m_Gravity: {x: 0, y: 0}`, `m_QueriesHitTriggers: 1`, `m_QueriesStartInColliders: 1`.
- **Collision Matrix**: All `0xFFFFFFFF` (all layers interact by default).

### 1.4 Hardcoded Static Arena Bounds in Codebase
The following scripts contain hardcoded static boundaries that assume the legacy static arena:
1. `PlayerMovement.cs` (lines 20-21):
   ```csharp
   public Vector2 minBounds = new Vector2(-8.5f, -4.2f);
   public Vector2 maxBounds = new Vector2(13.8f, 5.2f);
   ```
2. `GrenadeThrower.cs` (lines 29-30):
   ```csharp
   public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
   public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
   ```
3. `ShooterEnemy.cs` (lines 16-17):
   ```csharp
   public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
   public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
   ```
4. `EnemySpawner.cs` (lines 15-18, 39):
   ```csharp
   public float minX = -10.5f; public float maxX = 16.0f;
   public float minY = -6.0f;  public float maxY = 7.0f;
   public Vector3 bossSpawnPosition = new Vector3(2.69f, 3.5f, 0.0f);
   ```

### 1.5 Baseline Test Suite Verification
Executed menu item `"E2E Tests/Run All Tests"` via `unityMCP.execute_menu_item`:
- **Total Tests**: 385
- **Passed**: 385 (100%)
- **Failed**: 0, **Skipped**: 0
- **Duration**: 77.32 ms
- All tiers (Tier 1: 175, Tier 2: 175, Tier 3: 30, Tier 4: 5) passed without error.

---

## 2. Logic Chain

```
[Observation 1.1 - 1.3: Scene & Physics Setup]
  │
  ├─► All projectile and damage systems rely on 2D non-trigger vs trigger colliders
  │   - Bullets destroy on non-trigger colliders (walls, obstacles tagged "Colliders")
  │   - Bullets damage targets implementing IDamageable
  │   - Player & Enemies have Rigidbody2D (Dynamic) blocked by non-trigger Box/Capsule/Circle 2D Colliders
  │
  ├─► Existing 385/385 tests check exact tag names ("Player", "Enemy", "Colliders")
  │   and exact component types (IDamageable, PlayerHealth, EnemyBase, etc.)
  │   => Any new segment and obstacle prefabs MUST maintain these exact tags and collider types!
  │
[Observation 1.4: Hardcoded Bounds in 4 Scripts]
  │
  ├─► Requirement R1 demands continuous upward (+Y) camera scrolling (2.0 - 3.5 u/s)
  │   and dynamic viewport clamping (X: 0.05-0.95, Y: 0.08-0.92) with bottom kill/push threshold
  │   => Static Vector2 minBounds/maxBounds must be superseded by dynamic viewport/camera calculations!
  │
[Observation 1.1 - 1.2: Environment Assets & Dimensions]
  │
  ├─► Camera visible vertical height is ~12.6 units (ortho size 6.32).
  │   Requirement R2 specifies segment length = 20 units, corridor >= 4.0 units wide.
  │   A 20-unit segment comfortably spans ~1.6 screen heights, ideal for smooth streaming.
  │   Keeping 2-3 active segments ahead of camera ensures zero pop-in.
  │
  ├─► Sprites in Assets/Tiny RPG Forest/Artwork/Environment/sliced-objects/ (rock, bush, tree, trunk)
  │   provide native art assets for modular obstacle components.
  │
[Requirement R2: Object Pooling & Zero Runtime GC Allocations]
  │
  ├─► Instantiating/Destroying multi-collider GameObjects causes GC spikes.
  │   => MapSegmentPool prewarms segment prefabs into disabled queues.
  │   => Recycled segments are deactivated, repositioned to next top Y, and re-activated.
  │
[Requirement R3: 500-Point Boss Encounter & Radial Barrage]
  │
  └─► BossController already implements 16-bullet 360-degree radial barrage (22.5° step, 5.0 u/s)
      and 2 guaranteed grenade drops on defeat.
      => Triggering at 500 points requires:
         1. Halting standard segment generation and spawning BossArena segment ahead.
         2. Locking camera scrolling when camera aligns with BossArena center.
         3. Spawning Boss at arena center.
         4. On Boss defeat: award 500 pts, drop 2 grenades, unlock camera scrolling, resume generation.
```

---

## 3. Caveats

1. **Test Suite Invariants**: 385 existing tests pass against the current codebase. Any modification to `PlayerMovement.cs`, `EnemySpawner.cs`, `GrenadeThrower.cs`, or `BossController.cs` must remain 100% backward compatible with existing parameter defaults and public API signatures.
2. **Tag vs Layer Filtering**: While adding custom physics layers (e.g. `Obstacle`, `SegmentWall`) is cleaner, existing scripts (`Bullet.cs`, `EnemyBullet.cs`) directly check `hitObj.CompareTag("Colliders")` and `hitObj.CompareTag("Player")`. Therefore, **all segment boundary walls and obstacle colliders MUST retain tag `Colliders`**.
3. **Player Prefab Status**: Currently, `Player` is only stored in `shooting.unity`. For modularity and testing, creating `Assets/Prefabs/Player.prefab` is strongly recommended, while keeping the scene's Player linked.
4. **Tilemap Performance vs Sprite Obstacles**: Re-generating dynamic tilemaps at runtime can trigger chunk recalculations. Using modular sprite obstacles with 2D colliders grouped under each `MapSegment` prefab is significantly lighter, completely predictable, and pools with zero GC.

---

## 4. Conclusion & Design Recommendations

### 4.1 Prefab Modularity Architecture

Each map segment must be a self-contained prefab inheriting from a standardized structure:

```
MapSegmentPrefab (Root with MapSegment component)
├── FloorTiles / Background (SpriteRenderer or Tilemap, Sorting Layer: Default, Order: -10)
├── Boundaries (Tag: "Colliders")
│   ├── Wall_Left  (BoxCollider2D: X = -7.5, width 1.0, length 20.0, isTrigger: false)
│   └── Wall_Right (BoxCollider2D: X = +7.5, width 1.0, length 20.0, isTrigger: false)
├── Obstacles (Container)
│   ├── Obstacle_Tree (CapsuleCollider2D, Tag: "Colliders", Sprite: tree-orange.png)
│   ├── Obstacle_Rock (CircleCollider2D,  Tag: "Colliders", Sprite: rock.png)
│   └── Obstacle_Bush (CapsuleCollider2D, Tag: "Colliders", Sprite: bush.png)
└── SpawnPoints (Container)
    ├── EnemySpawn_1 (Transform, offset for Chaser/Shooter/Rusher)
    ├── EnemySpawn_2 (Transform)
    └── ItemSpawn_1  (Transform, optional GrenadePickup)
```

#### Blueprint for the 3 Required MapSegment Prefabs (Length: 20 units)
All segments have standardized width = 15.0 units between walls (`X: -7.5` to `+7.5`):

1. **`MapSegment_Corridor` (Open Flanked Highway)**:
   - Left Wall at `X = -7.5`, Right Wall at `X = +7.5`.
   - Flanking obstacles placed along margins (`X: -6.0 to -5.0` and `X: +5.0 to +6.0`).
   - Central passable corridor: `X: -4.0` to `+4.0` (width = **8.0 units** > 4.0u requirement).
   - 3 Enemy SpawnPoints: `(0, 5, 0)`, `(-3, 12, 0)`, `(3, 16, 0)`.
   - 1 Grenade Pickup SpawnPoint: `(0, 10, 0)` (30% spawn chance).

2. **`MapSegment_ChokePoint` (Split Dual-Corridor / Central Island)**:
   - Central obstacle group (Rock Monument / dense grove): `X: -1.2` to `+1.2`, `Y: 7.0` to `13.0` (width 2.4u).
   - Dual corridors:
     - Left Corridor: `X: -7.0` to `-1.5` (width = **5.5 units** > 4.0u requirement).
     - Right Corridor: `X: +1.5` to `+7.0` (width = **5.5 units** > 4.0u requirement).
   - 4 Enemy SpawnPoints: Left lane `(-4, 6, 0)`, `(-4, 14, 0)`; Right lane `(4, 6, 0)`, `(4, 14, 0)`.

3. **`MapSegment_Slalom` (Winding S-Curve / Alternating Deflectors)**:
   - Lower Deflector Wall at `Y = 6.0`: Extends from Left Wall `X: -7.5` inward to `X: -1.0`. Passable opening on right is `X: -1.0` to `+7.5` (width = **8.5 units**).
   - Upper Deflector Wall at `Y = 14.0`: Extends from Right Wall `X: +7.5` inward to `X: +1.0`. Passable opening on left is `X: -7.5` to `+1.0` (width = **8.5 units**).
   - Guarantees continuous navigable route with minimum corridor width **>= 6.0 units** everywhere.
   - 3 Enemy SpawnPoints at turn apexes: `(3, 5, 0)`, `(-3, 13, 0)`, `(0, 18, 0)`.

---

### 4.2 Boss Arena Prefab Specification

**Prefab Name**: `MapSegment_BossArena.prefab`
- **Dimensions**: Length = 24.0 units, Width = 18.0 units (`X: -9.0` to `+9.0`, `Y: 0.0` to `24.0`).
- **Arena Center**: Local position `(0.0, 12.0, 0.0)` — target camera lock point.
- **Boundaries**:
  - `Wall_Left`: `X = -9.0`, length = 24.0 units, Tag: `Colliders`.
  - `Wall_Right`: `X = +9.0`, length = 24.0 units, Tag: `Colliders`.
  - `Wall_Top`: `Y = 24.0`, width = 18.0 units, Tag: `Colliders`.
  - `Gate_Bottom`: `Y = 0.0`, width = 18.0 units, initially inactive or passable; closes when camera locks at center to prevent player retreat.
- **Boss Spawn Point**: Local position `(0.0, 16.0, 0.0)`.
- **Combat & Radial Barrage Integration**:
  - Center position provides `18 x 24` open area, giving player ample room to dodge the 16-bullet radial barrage (bullets travel outward at 5.0 u/s).
  - Boss uses existing `BossController.cs`:
    - HP = 60
    - Radial burst: 16 bullets at 22.5° intervals, 0.5s yellow telegraph flash (`Color(1f, 0.9f, 0.2f, 1f)`).
    - Defeat: Spawns 2 guaranteed grenades, invokes `OnBossDefeatedEvent`, unlocks camera scrolling, awards 500 points.

---

### 4.3 Object Pooling & Memory Stability Strategy

To satisfy R2 ("guarantee zero runtime GC allocations and memory stability over long play sessions"):

1. **Segment Pooling Contract**:
   - `MapSegmentPool` manages prewarmed queues:
     - 2 instances of `MapSegment_Corridor`
     - 2 instances of `MapSegment_ChokePoint`
     - 2 instances of `MapSegment_Slalom`
     - 1 instance of `MapSegment_BossArena`
   - Total pre-instantiated segments = 7.
2. **Segment Lifecycle & Alignment**:
   - Sequential Y alignment formula:
     $$\text{SegmentSpawnY}_{k} = \text{SegmentSpawnY}_{k-1} + 20.0\text{f}$$
   - When segment origin is at its bottom edge (`Y = 0`), adjacent segments connect seamlessly at `Y = 20k` with zero seam gaps and zero collision overlap.
   - Active list tracks currently enabled segments (typically 3 active segments: previous, current, upcoming).
3. **Recycling Condition**:
   - When `camera.transform.position.y - segment.TopY > cleanupDistance` (e.g. `cleanupDistance = 15.0f`):
     - Return all surviving segment enemies/items to their respective pools (or deactivate).
     - Deactivate segment (`gameObject.SetActive(false)`).
     - Enqueue segment back into `MapSegmentPool`.
     - Zero `GameObject.Destroy()` calls at runtime.

---

### 4.4 Viewport Clamping & Camera Scrolling Architecture

1. **Camera Controller (`ScrollingCameraController.cs`)**:
   - Moves along +Y in `FixedUpdate` or `LateUpdate`:
     $$\text{speed} = \min(2.0\text{f} + \text{progressionFactor} \times \text{distance}, 3.5\text{f})$$
   - State machine: `AutoScrolling`, `LockingToArena`, `LockedInArena`, `ResumingScroll`.
2. **Player Viewport Clamping (`PlayerMovement.cs` upgrade)**:
   - Clamps player world position each frame based on camera viewport:
     ```csharp
     Vector3 vp = cam.WorldToViewportPoint(rb.position);
     vp.x = Mathf.Clamp(vp.x, 0.05f, 0.95f);
     vp.y = Mathf.Clamp(vp.y, 0.08f, 0.92f);
     Vector3 world = cam.ViewportToWorldPoint(vp);
     rb.position = new Vector2(world.x, world.y);
     ```
   - **Bottom Kill/Push Plane**:
     - If player's viewport Y drops below `0.04f` (e.g. pinned against an obstacle while camera scrolls):
     - Trigger 1 HP damage via `PlayerHealth.TakeDamage(1)`.
     - Push player forward to `viewport.y = 0.12f`.
     - If HP reaches 0, standard Game Over triggers.

---

## 5. Verification Method

To independently verify the observations, contracts, and recommendations:

1. **Run Full Test Suite Baseline**:
   - Via Unity MCP tool:
     ```json
     {
       "tool": "execute_menu_item",
       "arguments": { "menu_path": "E2E Tests/Run All Tests" }
     }
     ```
   - Expected output in `read_console`:
     `[E2ETestRunner] Execution Finished. Total: 385, Passed: 385, Failed: 0, Pending: 0, Skipped: 0`

2. **Inspect Existing Prefabs**:
   - Use `manage_prefabs(action="get_info", prefab_path="Assets/Prefabs/BossEnemy.prefab")`.
   - Verify `CircleCollider2D`, `Rigidbody2D`, `BossController` component assignments.

3. **Verify Boundary Collider Alignment**:
   - Formula check: Corridor width = `Wall_Right.minX - Wall_Left.maxX >= 4.0u`.
   - Segment alignment: `NextSegment.Y = CurrentSegment.Y + 20.0u`.
   - Verify no gaps: Both walls share continuous vertical X coordinates (`X = -7.5` and `X = +7.5`) across segments.

4. **Invalidation Conditions**:
   - If any new segment prefab reduces corridor width below 4.0u.
   - If segment recycling uses `GameObject.Destroy()` instead of `SetActive(false)` + object pool queue.
   - If any of the existing 385 baseline unit/integration tests fail.
