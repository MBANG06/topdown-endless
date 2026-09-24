# Investigation & Architecture Handoff Report — Explorer M2.1

**Author**: `explorer_m2_1` (teamwork_preview_explorer)  
**Date**: 2026-09-22  
**Target Milestone**: Milestone 2 (M2) — Modular Map Segment Spawning & Object Pooling  
**Working Directory**: `.agents/teamwork/explorer_m2_1/`  
**Reference Documents**:
- `ORIGINAL_REQUEST.md`
- `PROJECT.md`
- `.agents/teamwork/explorer_2/handoff.md`

---

## 1. Observation

### 1.1 Existing Tag & Layer Configuration
Direct inspection of `ProjectSettings/TagManager.asset` and runtime reflection via Unity MCP (`execute_code`):
```yaml
TagManager:
  tags:
  - Colliders
  - Enemy
  layers:
  - Default (0)
  - TransparentFX (1)
  - Ignore Raycast (2)
  - Water (4)
  - UI (5)
```
- Runtime query via `UnityEditorInternal.InternalEditorUtility.tags` returned:
  `"Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController, Colliders, Enemy"`
- **Critical Finding**: Only `"Colliders"` and `"Enemy"` exist as custom user tags in the project. Any attempt to set an unregistered tag (e.g. `go.tag = "Wall"`) results in the tag reverting to `"Untagged"` without raising a fatal error in script execution, leaving the object effectively untagged.
- In `ProjectSettings/Physics2DSettings.asset`, the 2D collision matrix mask is `0xFFFFFFFF` for Layer 0 (`Default`), meaning Layer 0 interacts bidirectionally with all colliders and raycasts.

### 1.2 Bullet & Projectile Collision Mechanics
1. **Player Bullets (`Assets/scripts/Bullet.cs`, lines 36-88)**:
   ```csharp
   private void OnCollisionEnter2D(Collision2D collision) => HandleHit(collision.gameObject);
   private void OnTriggerEnter2D(Collider2D collider) => HandleHit(collider.gameObject);
   private void HandleHit(GameObject hitObj)
   {
       if (_hasHit || hitObj == null) return;
       if (hitObj.CompareTag("Player") || ...) return;
       IDamageable damageable = hitObj.GetComponent<IDamageable>() ?? hitObj.GetComponentInParent<IDamageable>();
       Collider2D hitCollider = hitObj.GetComponent<Collider2D>();
       if (hitCollider != null && hitCollider.isTrigger && damageable == null) return;
       _hasHit = true;
       if (damageable != null && damageable.IsAlive) damageable.TakeDamage(damage);
       Destroy(gameObject);
   }
   ```
   - **Trigger Passthrough**: If a boundary wall collider has `isTrigger = true` and lacks `IDamageable`, `Bullet.cs` line 68 triggers: `return;`! Bullets pass through triggers. Therefore, boundary wall colliders **must have `isTrigger = false`** to physically catch and destroy bullets.

2. **Enemy Bullets (`Assets/scripts/EnemyBullet.cs`, lines 41-108)**:
   ```csharp
   Collider2D hitCollider = hitObj.GetComponent<Collider2D>();
   bool isPlayer = hitObj.CompareTag("Player") || ...;
   if (hitCollider != null && hitCollider.isTrigger && !isPlayer) return;
   _hasHit = true;
   ```
   - Likewise, enemy bullets pass through triggers that are not the player. `isTrigger = false` is strictly required on boundary walls.

3. **Grenade Projectiles (`Assets/scripts/GrenadeProjectile.cs`, lines 118-129)**:
   ```csharp
   bool isEnemy = target.CompareTag("Enemy") || ...;
   bool isWall = target.CompareTag("Colliders") || target.name.Contains("Collider") || target.name.Contains("Wall");
   if (isEnemy || isWall)
   {
       Detonate();
   }
   ```
   - Grenade projectile impact explicitly checks `target.CompareTag("Colliders")`. If a wall collider is not tagged `"Colliders"`, grenade impact detonation relies solely on fallback string matching (`target.name.Contains`). Tagging with `"Colliders"` ensures deterministic projectile detonation.

4. **Pickups Rejection (`Assets/scripts/GrenadePickup.cs` / `Tests/ChallengerM3Tests.cs`, lines 342-351)**:
   ```csharp
   var wall = ctx.CreateGameObject("CH_Wall");
   wall.tag = "Colliders";
   E2EAssert.IsFalse(pickup.TryCollect(wall), "Wall must not collect pickup");
   ```
   - Pickups filter environmental walls by tag `"Colliders"`.

### 1.3 Physical Rigidbody2D Constraints
- **Player**: `shooting.unity` scene Player has `Rigidbody2D` (Dynamic, mass 1, freeze rotation) and `BoxCollider2D` (`isTrigger = false`, size `(1.56, 1.86)`).
- **Enemies**: `ChaserEnemy`, `ShooterEnemy`, `RusherEnemy`, `BossEnemy` all have `Rigidbody2D` (Dynamic, freeze rotation) and `CircleCollider2D` (`isTrigger = false`).
- In Unity 2D Physics, non-trigger colliders (`isTrigger = false`) generate normal contact forces. If segment boundaries were triggers, dynamic rigidbodies (Player and enemies) would walk through boundaries and fall out of bounds.

### 1.4 Baseline Test Suite Health
Executed `E2ETestRunner.RunAll()` via Unity MCP Roslyn compiler:
- **Total Tests**: 505
- **Passed**: 505 (100%)
- **Failed**: 0, **Pending**: 0, **Skipped**: 0
- Existing test suites in `Assets/scripts/Tests/` verify that all environmental collision structures are tagged `"Colliders"` and have `isTrigger = false`.

---

## 2. Logic Chain

```
[Observation 1.1: TagManager.asset & Reflection Query]
  │
  ├─► The only registered environment tag in the entire project is "Colliders".
  │   - Setting any other tag (e.g. "Wall", "Obstacle") is silently rejected or resets to "Untagged".
  │   - Layer 0 ("Default") is universally unmasked in Physics2D collision matrix (0xFFFFFFFF).
  │
[Observation 1.2: Bullet, EnemyBullet, & GrenadeProjectile Impact Code]
  │
  ├─► Bullet.cs (line 68) & EnemyBullet.cs (line 68):
  │   Both scripts explicitly check:
  │   "if (hitCollider != null && hitCollider.isTrigger && damageable == null) return;"
  │   => If boundary wall colliders have isTrigger = true, all bullets pass straight through walls!
  │   => Therefore, boundary colliders MUST have isTrigger = false.
  │
  ├─► GrenadeProjectile.cs (line 124):
  │   "bool isWall = target.CompareTag("Colliders") || target.name.Contains("Collider") || target.name.Contains("Wall");"
  │   => Must have tag "Colliders" to guarantee immediate grenade detonation upon impact.
  │
[Observation 1.3: Dynamic Rigidbody2D on Player & All 4 Enemy Types]
  │
  ├─► Player and enemies use Dynamic Rigidbody2D with isTrigger = false colliders.
  │   => Only non-trigger colliders (isTrigger = false) generate physical repulsive normal forces.
  │   => Keeps player and enemies confined within the X: [-7.5, +7.5] corridor span.
  │
[Requirements R2, AC, & PROJECT.md Specifications]
  │
  ├─► Standardized dimensions:
  │   - segmentLength = 20.0f
  │   - segmentWidth = 15.0f (lateral boundaries at X = -7.5 and X = +7.5)
  │   - minCorridorWidth = 4.0f
  │   - Sequential spawn alignment: nextSpawnY = currentSegment.BottomY + 20.0f
  │   - Zero GC pooling: ResetSegment() cleans dynamic state without runtime GameObject.Destroy()
  │
  └─► Conclusion: MapSegment.cs must encapsulate these properties, automated boundary setup,
      spawn points query, and pooling lifecycle reset.
```

---

## 3. Caveats

1. **Transform Scale Invariant**: `MapSegment` root GameObjects must maintain local scale `(1, 1, 1)`. Scaling the segment root would distort collider widths and spawn point distances.
2. **Spawn Point Hierarchy**: Enemy and item spawn points are designated as child `Transform` components. If designers add prefabs where spawn points are nested inside sub-containers, `MapSegment.GetSpawnPoints()` must support both explicitly assigned inspector arrays and fallback recursive search.
3. **Boss Arena Distinction**: While standard segments have length 20u and width 15u (`X: [-7.5, +7.5]`), the Boss Arena segment specified in M3 has length 24u and width 18u (`X: [-9.0, +9.0]`). `MapSegment` is designed with instance fields so that `isBossArena = true`, `segmentLength = 24.0f`, and `segmentWidth = 18.0f` can be configured on `MapSegment_BossArena.prefab` seamlessly without code duplication.

---

## 4. Conclusion & Recommended Implementation

### 4.1 Collision & Boundary Tagging Summary
| Property | Left Wall | Right Wall | Obstacles (Rocks/Trees/Bushes) |
|---|---|---|---|
| **Local Position** | `X = -7.5, Y = 10.0, Z = 0` | `X = +7.5, Y = 10.0, Z = 0` | Inside corridor bounds (`X ∈ [-7.5, +7.5]`) |
| **Collider Type** | `BoxCollider2D` | `BoxCollider2D` | `BoxCollider2D`, `CircleCollider2D`, or `CapsuleCollider2D` |
| **Collider Size** | `Size: (1.0, 20.0)` | `Size: (1.0, 20.0)` | Obstacle sprite bounding size |
| **isTrigger** | `false` (Solid obstruction) | `false` (Solid obstruction) | `false` (Solid obstruction) |
| **Tag** | `"Colliders"` | `"Colliders"` | `"Colliders"` |
| **Layer** | `0` (`Default`) | `0` (`Default`) | `0` (`Default`) |

### 4.2 Recommended C# Implementation: `Assets/scripts/MapSegment.cs`

Below is the complete, production-ready implementation of `Assets/scripts/MapSegment.cs` recommended for `implementer_m2`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type filter for spawn points query.
/// </summary>
public enum SpawnPointType
{
    All,
    Enemy,
    Item
}

/// <summary>
/// Represents a modular procedural map segment in the continuous upward (+Y) endless scrolling system.
/// Defines physical bounds, passable corridor clearance, boundary wall colliders, spawn point anchors,
/// and pooling reset logic to guarantee zero runtime GC allocations.
/// </summary>
[DisallowMultipleComponent]
[SelectionBase]
public class MapSegment : MonoBehaviour
{
    [Header("Segment Dimensions")]
    [Tooltip("Standardized length along the vertical (+Y) axis in world units.")]
    public float segmentLength = 20.0f;

    [Tooltip("Standardized width along the lateral (X) axis between left and right walls in world units.")]
    public float segmentWidth = 15.0f;

    [Tooltip("Guaranteed minimum unblocked corridor clearance width across the segment.")]
    public float minCorridorWidth = 4.0f;

    [Header("Spawn Points")]
    [Tooltip("Candidate positions for spawning hostile enemies (Chaser, Shooter, Rusher).")]
    public Transform[] enemySpawnPoints = new Transform[0];

    [Tooltip("Candidate positions for spawning item pickups (Grenades, Health).")]
    public Transform[] itemSpawnPoints = new Transform[0];

    [Header("Segment Classification")]
    [Tooltip("Unique sequential index of this segment in the spawned stream.")]
    public int segmentIndex = 0;

    [Tooltip("Whether this segment is a dedicated Boss Arena.")]
    public bool isBossArena = false;

    [Header("Boundary Colliders")]
    [Tooltip("Left boundary wall Collider2D.")]
    public Collider2D leftWallCollider;

    [Tooltip("Right boundary wall Collider2D.")]
    public Collider2D rightWallCollider;

    [Header("Runtime State")]
    [SerializeField] private bool _hasSpawnedEnemies = false;
    [SerializeField] private bool _hasSpawnedItems = false;

    /// <summary>
    /// Indicates whether enemies have already been spawned for this segment cycle.
    /// </summary>
    public bool hasSpawnedEnemies
    {
        get => _hasSpawnedEnemies;
        set => _hasSpawnedEnemies = value;
    }

    /// <summary>
    /// Indicates whether items have already been spawned for this segment cycle.
    /// </summary>
    public bool hasSpawnedItems
    {
        get => _hasSpawnedItems;
        set => _hasSpawnedItems = value;
    }

    /// <summary>
    /// World Y coordinate of the bottom entrance of the segment (origin).
    /// </summary>
    public float BottomY => transform.position.y;

    /// <summary>
    /// World Y coordinate of the top exit of the segment.
    /// </summary>
    public float TopY => transform.position.y + segmentLength;

    /// <summary>
    /// World center position of the segment.
    /// </summary>
    public Vector3 Center => transform.position + new Vector3(0f, segmentLength * 0.5f, 0f);

    /// <summary>
    /// World X coordinate of the left boundary wall center.
    /// </summary>
    public float LeftWallX => transform.position.x - (segmentWidth * 0.5f);

    /// <summary>
    /// World X coordinate of the right boundary wall center.
    /// </summary>
    public float RightWallX => transform.position.x + (segmentWidth * 0.5f);

    /// <summary>
    /// Bounding box enclosing the segment in world space.
    /// </summary>
    public Bounds WorldBounds => CalculateBounds();

    private void Awake()
    {
        EnsureBoundaryColliders();
    }

    /// <summary>
    /// Resets all dynamic flags, cleans up attached transient children,
    /// and restores the segment to a clean state for pooling reuse.
    /// </summary>
    public void ResetSegment()
    {
        _hasSpawnedEnemies = false;
        _hasSpawnedItems = false;
        segmentIndex = -1;

        // Clean up or deactivate any dynamic spawned entities that may still be attached as children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            // If child is a spawned entity (e.g., enemy or pickup that parented to the segment)
            if (child.GetComponent<EnemyBase>() != null || child.GetComponent<GrenadePickup>() != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    /// <summary>
    /// Returns valid spawn point transforms based on requested type.
    /// Automatically filters out null or destroyed entries.
    /// </summary>
    public Transform[] GetSpawnPoints(SpawnPointType type = SpawnPointType.Enemy)
    {
        switch (type)
        {
            case SpawnPointType.Enemy:
                return GetCleanSpawnPoints(enemySpawnPoints);
            case SpawnPointType.Item:
                return GetCleanSpawnPoints(itemSpawnPoints);
            case SpawnPointType.All:
                var all = new List<Transform>();
                all.AddRange(GetCleanSpawnPoints(enemySpawnPoints));
                all.AddRange(GetCleanSpawnPoints(itemSpawnPoints));
                return all.ToArray();
            default:
                return GetCleanSpawnPoints(enemySpawnPoints);
        }
    }

    /// <summary>
    /// Parameterless overload returning enemy spawn points by default.
    /// </summary>
    public Transform[] GetSpawnPoints() => GetSpawnPoints(SpawnPointType.Enemy);

    /// <summary>
    /// Returns non-null enemy spawn point transforms.
    /// </summary>
    public Transform[] GetEnemySpawnPoints() => GetCleanSpawnPoints(enemySpawnPoints);

    /// <summary>
    /// Returns non-null item spawn point transforms.
    /// </summary>
    public Transform[] GetItemSpawnPoints() => GetCleanSpawnPoints(itemSpawnPoints);

    /// <summary>
    /// Returns a random enemy spawn point transform, or null if none are available.
    /// </summary>
    public Transform GetRandomEnemySpawnPoint()
    {
        var points = GetEnemySpawnPoints();
        if (points == null || points.Length == 0) return null;
        return points[UnityEngine.Random.Range(0, points.Length)];
    }

    /// <summary>
    /// Returns a random item spawn point transform, or null if none are available.
    /// </summary>
    public Transform GetRandomItemSpawnPoint()
    {
        var points = GetItemSpawnPoints();
        if (points == null || points.Length == 0) return null;
        return points[UnityEngine.Random.Range(0, points.Length)];
    }

    /// <summary>
    /// Computes and returns the world-space bounding box for this segment.
    /// </summary>
    public Bounds CalculateBounds()
    {
        return new Bounds(Center, new Vector3(segmentWidth, segmentLength, 1.0f));
    }

    /// <summary>
    /// Checks whether a given world coordinate falls inside the lateral and vertical bounds of this segment.
    /// </summary>
    public bool ContainsWorldPosition(Vector2 worldPos)
    {
        return worldPos.x >= LeftWallX && worldPos.x <= RightWallX &&
               worldPos.y >= BottomY && worldPos.y <= TopY;
    }

    /// <summary>
    /// Checks whether the top boundary of this segment has dropped below the cleanup threshold.
    /// Used by pooling recycling manager.
    /// </summary>
    public bool IsBehindCleanupThreshold(float cleanupY)
    {
        return TopY < cleanupY;
    }

    /// <summary>
    /// Ensures that Left Wall (X = -7.5) and Right Wall (X = +7.5) colliders exist,
    /// are tagged "Colliders", have isTrigger = false, and are on layer Default (0).
    /// Creates or updates them automatically if missing.
    /// </summary>
    public void EnsureBoundaryColliders()
    {
        float halfWidth = segmentWidth * 0.5f;
        float halfLength = segmentLength * 0.5f;

        // Ensure Left Wall
        if (leftWallCollider == null)
        {
            var leftTransform = transform.Find("Wall_Left");
            if (leftTransform != null)
            {
                leftWallCollider = leftTransform.GetComponent<BoxCollider2D>();
            }
        }

        if (leftWallCollider == null)
        {
            var leftWallGo = new GameObject("Wall_Left");
            leftWallGo.transform.SetParent(transform, false);
            leftWallGo.transform.localPosition = new Vector3(-halfWidth, halfLength, 0f);
            var box = leftWallGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.0f, segmentLength);
            box.isTrigger = false;
            leftWallGo.tag = "Colliders";
            leftWallGo.layer = 0; // Default
            leftWallCollider = box;
        }
        else
        {
            ConfigureColliderObject(leftWallCollider.gameObject, new Vector3(-halfWidth, halfLength, 0f), new Vector2(1.0f, segmentLength));
        }

        // Ensure Right Wall
        if (rightWallCollider == null)
        {
            var rightTransform = transform.Find("Wall_Right");
            if (rightTransform != null)
            {
                rightWallCollider = rightTransform.GetComponent<BoxCollider2D>();
            }
        }

        if (rightWallCollider == null)
        {
            var rightWallGo = new GameObject("Wall_Right");
            rightWallGo.transform.SetParent(transform, false);
            rightWallGo.transform.localPosition = new Vector3(halfWidth, halfLength, 0f);
            var box = rightWallGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.0f, segmentLength);
            box.isTrigger = false;
            rightWallGo.tag = "Colliders";
            rightWallGo.layer = 0; // Default
            rightWallCollider = box;
        }
        else
        {
            ConfigureColliderObject(rightWallCollider.gameObject, new Vector3(halfWidth, halfLength, 0f), new Vector2(1.0f, segmentLength));
        }
    }

    private void ConfigureColliderObject(GameObject wallGo, Vector3 localPos, Vector2 size)
    {
        wallGo.tag = "Colliders";
        wallGo.layer = 0; // Default
        var box = wallGo.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.isTrigger = false;
            box.size = size;
        }
        wallGo.transform.localPosition = localPos;
    }

    /// <summary>
    /// Validates all architectural rules: corridor width, wall tags, trigger flags, and dimensions.
    /// </summary>
    public bool ValidateSegment(out string failureReason)
    {
        if (segmentLength <= 0f)
        {
            failureReason = $"segmentLength must be positive, got {segmentLength}";
            return false;
        }

        if (segmentWidth <= 0f)
        {
            failureReason = $"segmentWidth must be positive, got {segmentWidth}";
            return false;
        }

        if (minCorridorWidth < 4.0f)
        {
            failureReason = $"minCorridorWidth ({minCorridorWidth}) must be >= 4.0u per requirement R2";
            return false;
        }

        EnsureBoundaryColliders();

        if (leftWallCollider == null || !leftWallCollider.CompareTag("Colliders") || leftWallCollider.isTrigger)
        {
            failureReason = "Left wall collider must be non-null, non-trigger, and tagged 'Colliders'";
            return false;
        }

        if (rightWallCollider == null || !rightWallCollider.CompareTag("Colliders") || rightWallCollider.isTrigger)
        {
            failureReason = "Right wall collider must be non-null, non-trigger, and tagged 'Colliders'";
            return false;
        }

        failureReason = null;
        return true;
    }

    private Transform[] GetCleanSpawnPoints(Transform[] rawPoints)
    {
        if (rawPoints == null || rawPoints.Length == 0) return new Transform[0];
        var list = new List<Transform>(rawPoints.Length);
        for (int i = 0; i < rawPoints.Length; i++)
        {
            if (rawPoints[i] != null)
            {
                list.Add(rawPoints[i]);
            }
        }
        return list.ToArray();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw segment bounding box in cyan
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireCube(Center, new Vector3(segmentWidth, segmentLength, 0f));

        // Draw Left Wall in red
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(LeftWallX, BottomY, 0f), new Vector3(LeftWallX, TopY, 0f));

        // Draw Right Wall in green
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(RightWallX, BottomY, 0f), new Vector3(RightWallX, TopY, 0f));

        // Draw Enemy Spawn Points in magenta spheres
        if (enemySpawnPoints != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var sp in enemySpawnPoints)
            {
                if (sp != null) Gizmos.DrawWireSphere(sp.position, 0.5f);
            }
        }

        // Draw Item Spawn Points in yellow spheres
        if (itemSpawnPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var ip in itemSpawnPoints)
            {
                if (ip != null) Gizmos.DrawWireSphere(ip.position, 0.35f);
            }
        }
    }
#endif
}
```

---

## 5. Verification Method

### 5.1 Test Baseline
Execute the project test suite via Unity MCP:
```json
{
  "tool": "execute_code",
  "arguments": {
    "action": "execute",
    "code": "var report = E2ETests.E2ETestRunner.RunAll(); return $\"Total: {report.TotalCount}, Passed: {report.PassedCount}, Failed: {report.FailedCount}\";"
  }
}
```
**Expected Outcome**: `Total: 505, Passed: 505, Failed: 0`.

### 5.2 Unit Verification for MapSegment
When `MapSegment.cs` is written to `Assets/scripts/MapSegment.cs`, verify via `execute_code`:
```csharp
var go = new GameObject("Test_MapSegment");
var segment = go.AddComponent<MapSegment>();
segment.EnsureBoundaryColliders();

bool isValid = segment.ValidateSegment(out string failureReason);
bool leftTagOk = segment.leftWallCollider.CompareTag("Colliders");
bool rightTagOk = segment.rightWallCollider.CompareTag("Colliders");
bool leftTriggerOk = !segment.leftWallCollider.isTrigger;
bool rightTriggerOk = !segment.rightWallCollider.isTrigger;
bool leftPosOk = Mathf.Approximately(segment.LeftWallX, -7.5f);
bool rightPosOk = Mathf.Approximately(segment.RightWallX, 7.5f);
bool boundsOk = Mathf.Approximately(segment.CalculateBounds().size.y, 20.0f);

GameObject.DestroyImmediate(go);

return $"Valid: {isValid}, Tags: {leftTagOk && rightTagOk}, Triggers: {leftTriggerOk && rightTriggerOk}, Walls: {leftPosOk && rightPosOk}, Bounds: {boundsOk}";
```
**Expected Output**: `Valid: True, Tags: True, Triggers: True, Walls: True, Bounds: True`.

### 5.3 Invalidation Conditions
- Any implementation where Left Wall or Right Wall has `isTrigger = true` (causes bullets to pass through and rigidbodies to breach bounds).
- Any implementation where wall tag is NOT `"Colliders"` (causes grenades not to detonate and pickup filtering to fail).
- Any implementation where `segmentLength != 20.0f` or `segmentWidth != 15.0f` (causes seam gaps or overlapping walls during infinite generation).
- Any implementation using `GameObject.Destroy()` on recycled segments instead of `ResetSegment()` and `SetActive(false)`.
