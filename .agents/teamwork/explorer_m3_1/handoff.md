# Handoff Report: MapSegment_BossArena Prefab & Builder Architecture (Milestone 3 / R3)

**Author**: `explorer_m3_1` (teamwork_preview_explorer)  
**Date**: 2026-09-22T20:40:00Z  
**Target Milestone**: Milestone 3 (R3 Boss Arena Encounter & Resume Loop)  
**Deliverable**: Architectural analysis, prefab specification, and programmatic builder extension for `MapSegment_BossArena.prefab`

---

## 1. Observation

### 1.1 Existing MapSegment Architecture (`Assets/scripts/MapSegment.cs`)
- **Dimensions & Bounds**:
  - Lines 25–32: Standardized segments have `segmentLength = 20.0f` and `segmentWidth = 15.0f` with `minCorridorWidth = 4.0f`.
  - Line 46: `public bool isBossArena = false;` already exists as an architectural flag on `MapSegment`.
  - Lines 48–54: Only `leftWallCollider` and `rightWallCollider` (`Collider2D`) exist; no `topWallCollider` or `topWall` field is currently declared.
  - Lines 107–110: `TopY => transform.position.y + segmentLength;` and `Center => transform.position + new Vector3(0f, segmentLength * 0.5f, 0f);`.
  - Lines 272–341: `EnsureBoundaryColliders()` calculates `halfWidth = segmentWidth * 0.5f` and `halfLength = segmentLength * 0.5f`. It positions the Left Wall at `(-halfWidth, halfLength, 0)` and the Right Wall at `(halfWidth, halfLength, 0)` with size `(1.0f, segmentLength)`. It enforces `tag = "Colliders"`, `isTrigger = false`, and `layer = 0` (Default).
  - Lines 360–396: `ValidateSegment(out string failureReason)` checks that `segmentLength > 0`, `segmentWidth > 0`, `minCorridorWidth >= 4.0f`, and both left and right wall colliders are non-null, non-trigger, and tagged `"Colliders"`. It does not yet validate `topWallCollider` when `isBossArena == true`.

### 1.2 Existing Prefab Generation Utility (`Assets/scripts/Editor/MapSegmentPrefabBuilder.cs`)
- Lines 11–39: `MapSegmentPrefabBuilder` is an Editor utility with `[MenuItem("Tools/Build Map Segment Prefabs")]` (`BuildAllPrefabs()`) that creates prefabs in `Assets/Prefabs/MapSegments/`:
  - `MapSegment_Corridor.prefab` (Line 41)
  - `MapSegment_ChokePoint.prefab` (Line 91)
  - `MapSegment_Slalom.prefab` (Line 152)
- Lines 216–239: `CreateBoundaryWalls(Transform parent, MapSegment seg)` creates `Wall_Left` at `(-7.5, 10.0, 0)` and `Wall_Right` at `(7.5, 10.0, 0)` with size `(1.0, 20.0)`, tag `"Colliders"`, and layer `0`.
- Lines 305–312: `SavePrefab(GameObject root, string path)` utilizes `PrefabUtility.SaveAsPrefabAsset(root, path, out bool success)`.
- Currently, **`BuildBossArenaPrefab()` does not exist** in `MapSegmentPrefabBuilder.cs`, and `MapSegment_BossArena.prefab` is missing from `Assets/Prefabs/MapSegments/`.

### 1.3 MapManager & Camera Scrolling Integration (`Assets/scripts/MapManager.cs` & `ScrollingCameraController.cs`)
- `MapManager.cs`:
  - Lines 45–50: `public GameObject bossArenaPrefab;` and `public float bossArenaLength = 24.0f;`.
  - Lines 349–377: `SpawnBossArenaInternal()` computes:
    ```csharp
    float arenaStartY = _nextSpawnY;
    _bossArenaCenterY = arenaStartY + (bossArenaLength * 0.5f);
    arenaObj = Instantiate(bossArenaPrefab, new Vector3(0f, arenaStartY, 0f), Quaternion.identity);
    _nextSpawnY += bossArenaLength;
    cameraController.LockAt(_bossArenaCenterY);
    ```
    At `length = 24.0f`, `_bossArenaCenterY = arenaStartY + 12.0f`.
  - Lines 382–391: `ResumeStandardSpawning()` sets `_isBossArenaSpawned = false` and calls `cameraController.UnlockAndResume()`, but does **not yet open the active boss arena's top wall**.
- `ScrollingCameraController.cs`:
  - Lines 136–151: `OpenStartingArenaTopWall()` disables `BoxCollider2D.enabled` on starting arena `MapBounds/Wall_Top` at runtime to permit upward scrolling.
  - Lines 251–265: `LockAt(float worldY, bool snapImmediate = false)` locks camera translation.
  - Lines 268–275: `UnlockAndResume()` unlocks camera translation along +Y.

### 1.4 BossController & Combat Integration (`Assets/scripts/BossController.cs`)
- Lines 32–36: Static events `OnBossSpawned`, `OnBossHealthChanged`, `OnBossDefeatedEvent`, `OnBossKilled`.
- Lines 262–294: `Die()` broadcasts `OnBossDefeatedEvent?.Invoke()`, drops guaranteed 2 grenades, and triggers victory.

---

## 2. Logic Chain

1. **Arena Dimension & Boundary Positioning**:
   - The user request and `PROJECT.md` specify Boss Arena dimensions: `length = 24.0f`, `width = 18.0f` (spanning X from `-9.0f` to `+9.0f`).
   - In `MapSegment.cs` local space:
     - Arena bottom entrance is at local `Y = 0`.
     - Arena center is at local `Y = 24.0 * 0.5 = 12.0f`, `X = 0f`.
     - Arena top exit is at local `Y = 24.0f`, `X = 0f`.
     - Left Wall is centered at local `X = -9.0f`, `Y = 12.0f`, with size `Vector2(1.0f, 24.0f)`.
     - Right Wall is centered at local `X = +9.0f`, `Y = 12.0f`, with size `Vector2(1.0f, 24.0f)`.
     - Top Wall is centered at local `X = 0f`, `Y = 24.0f`, spanning X from `-9.0f` to `+9.0f`, with size `Vector2(18.0f, 1.0f)`.
   - All three boundary walls must be non-trigger (`isTrigger = false`), tagged `"Colliders"`, on layer `0` (`Default`).

2. **Anchor Transforms (Boss Spawn & Camera Lock)**:
   - **Camera Lock Point**: Needs to align camera exactly at arena center (`local X = 0, Y = 12.0`). This matches `MapManager._bossArenaCenterY = arenaStartY + 12.0f`. Providing a dedicated Transform child `ArenaCenter` (or `CameraLockPoint`) allows visual gizmo inspection in Editor and deterministic runtime coordinate resolution.
   - **Boss Spawn Point**: Placed at top center: `local X = 0, Y = 18.0f` (`segmentLength * 0.75f`). This provides ample separation from the bottom entrance (`18.0u` distance from player entry at `Y = 0`), giving the player clear visibility for the boss spawn telegraph and 16-bullet radial barrage avoidance.

3. **Dynamic Top Wall Architecture**:
   - During the boss fight, the top wall collider must be enabled (`isTrigger = false`, `enabled = true`) to prevent player escape.
   - Upon boss defeat (and when continuing endless scrolling), the top wall must be opened so the player and camera can advance upward into newly spawned segments.
   - Mechanism:
     - `MapSegment` exposes `public Collider2D topWallCollider;` and `public GameObject topWall;`.
     - `MapSegment.OpenTopWall()` sets `topWallCollider.enabled = false` and `topWall.SetActive(false)`.
     - `MapSegment.CloseTopWall()` sets `topWallCollider.enabled = true` and `topWall.SetActive(true)`.
     - `MapManager.ResumeStandardSpawning()` finds active segments where `isBossArena == true` and invokes `seg.OpenTopWall()`.
     - An optional `BossArenaController` component on the prefab can also listen to `BossController.OnBossDefeatedEvent` for direct autonomous opening.

4. **Programmatic Builder Pipeline (`MapSegmentPrefabBuilder.cs`)**:
   - `MapSegmentPrefabBuilder.cs` already builds the other 3 prefabs using standard Unity Editor APIs (`GameObject`, `AddComponent<MapSegment>()`, `PrefabUtility.SaveAsPrefabAsset`).
   - Extending `MapSegmentPrefabBuilder` with `BuildBossArenaPrefab()` ensures that the prefab can be generated deterministically on any machine or CI pipeline without relying on manual Inspector GUI manipulation.

---

## 3. Caveats

- **Bottom Entrance Wall**: The arena intentionally lacks a physical bottom wall. Entrance containment is enforced by the locked camera viewport (`ScrollingCameraController.isScrollLocked = true`) and `PlayerMovement` viewport clamping (`minViewportY = 0.08f` and `bottomKillThreshold = 0.04f`). A physical bottom wall would collide with the player upon entering.
- **Top Wall Corner Overlap**: At `X = ±9.0f` and `Y = 24.0f`, a top wall of width `18.0f` meets the side walls centered at `X = ±9.0f`. If side walls have thickness `1.0f` (extending from `8.5` to `9.5`), an `18.0f` wide top wall (extending from `-9.0` to `+9.0`) overlaps by `0.5u` on each side, perfectly sealing the corner against bullet or player slipping.
- **Corridor Width Contract**: In standard segments, obstacles reduce corridor width down to `5.5u`–`8.0u`. In `MapSegment_BossArena`, the combat interior should remain completely unobstructed (`minCorridorWidth = 16.0f` or `18.0f`) to allow full 360° radial barrage dodging. Boundary decorations (monuments, rocks) should be purely decorative or strictly placed along outer margins (`|X| >= 7.5u`).

---

## 4. Conclusion & Recommended Specifications

### 4.1 Prefab Specification (`MapSegment_BossArena.prefab`)

```
MapSegment_BossArena (Root, Position: 0, 0, 0)
├── Component: MapSegment
│   ├── segmentLength: 24.0
│   ├── segmentWidth: 18.0
│   ├── minCorridorWidth: 16.0
│   ├── isBossArena: true
│   ├── leftWallCollider: -> Boundaries/Wall_Left (BoxCollider2D)
│   ├── rightWallCollider: -> Boundaries/Wall_Right (BoxCollider2D)
│   ├── topWallCollider: -> Boundaries/Wall_Top (BoxCollider2D)
│   ├── topWall: -> Boundaries/Wall_Top (GameObject)
│   ├── bossSpawnPoint: -> SpawnPoints/BossSpawnPoint (Transform)
│   ├── cameraLockPoint: -> ArenaCenter (Transform)
│   ├── enemySpawnPoints: [ -> SpawnPoints/BossSpawnPoint ]
│   └── itemSpawnPoints: [ -> SpawnPoints/ItemSpawn_1 ]
├── Component: BossArenaController (Optional/Recommended companion)
├── Boundaries (GameObject, LocalPos: 0, 0, 0)
│   ├── Wall_Left (LocalPos: -9.0, 12.0, 0 | BoxCollider2D: size 1.0 x 24.0, isTrigger: false | Tag: Colliders | Layer: 0)
│   ├── Wall_Right (LocalPos: 9.0, 12.0, 0 | BoxCollider2D: size 1.0 x 24.0, isTrigger: false | Tag: Colliders | Layer: 0)
│   └── Wall_Top (LocalPos: 0.0, 24.0, 0 | BoxCollider2D: size 18.0 x 1.0, isTrigger: false | Tag: Colliders | Layer: 0)
├── ArenaCenter (LocalPos: 0.0, 12.0, 0)
├── SpawnPoints (GameObject, LocalPos: 0, 0, 0)
│   ├── BossSpawnPoint (LocalPos: 0.0, 18.0, 0)
│   └── ItemSpawn_1 (LocalPos: 0.0, 6.0, 0)
└── Decorations (GameObject, LocalPos: 0, 0, 0)
    ├── Monument_TopLeft (LocalPos: -7.5, 22.0, 0 | SpriteRenderer: rock-monument.png)
    ├── Monument_TopRight (LocalPos: 7.5, 22.0, 0 | SpriteRenderer: rock-monument.png)
    ├── Monument_BotLeft (LocalPos: -7.5, 2.0, 0 | SpriteRenderer: rock-monument.png)
    ├── Monument_BotRight (LocalPos: 7.5, 2.0, 0 | SpriteRenderer: rock-monument.png)
    ├── Rock_L1 (LocalPos: -8.2, 8.0, 0 | SpriteRenderer: rock.png)
    ├── Rock_L2 (LocalPos: -8.2, 16.0, 0 | SpriteRenderer: rock.png)
    ├── Rock_R1 (LocalPos: 8.2, 8.0, 0 | SpriteRenderer: rock.png)
    └── Rock_R2 (LocalPos: 8.2, 16.0, 0 | SpriteRenderer: rock.png)
```

---

### 4.2 Exact C# Modifications Required

#### 4.2.1 Extension to `Assets/scripts/MapSegment.cs`

Add the following fields, properties, and methods to `MapSegment.cs`:

```csharp
    [Header("Boss Arena References")]
    [Tooltip("Top boundary wall Collider2D (dynamic wall for Boss Arena).")]
    public Collider2D topWallCollider;

    [Tooltip("Top boundary wall GameObject reference (opened/deactivated upon boss defeat).")]
    public GameObject topWall;

    [Tooltip("Dedicated boss spawn point anchor (top center, e.g. local X=0, Y=18).")]
    public Transform bossSpawnPoint;

    [Tooltip("Arena center / camera lock point anchor (e.g. local X=0, Y=12).")]
    public Transform cameraLockPoint;

    /// <summary>
    /// World space position of the boss spawn point.
    /// </summary>
    public Vector3 BossSpawnPosition => bossSpawnPoint != null 
        ? bossSpawnPoint.position 
        : transform.position + new Vector3(0f, segmentLength * 0.75f, 0f);

    /// <summary>
    /// World space position of the camera lock point.
    /// </summary>
    public Vector3 CameraLockPosition => cameraLockPoint != null 
        ? cameraLockPoint.position 
        : Center;

    /// <summary>
    /// Deactivates / opens the top boundary wall so player can progress upward after boss defeat.
    /// </summary>
    public void OpenTopWall()
    {
        if (topWallCollider != null)
        {
            topWallCollider.enabled = false;
        }
        if (topWall != null)
        {
            topWall.SetActive(false);
        }
    }

    /// <summary>
    /// Re-enables / closes the top boundary wall.
    /// </summary>
    public void CloseTopWall()
    {
        if (topWall != null)
        {
            topWall.SetActive(true);
        }
        if (topWallCollider != null)
        {
            topWallCollider.enabled = true;
        }
    }
```

Update `EnsureBoundaryColliders()` in `MapSegment.cs` to handle `Wall_Top`:

```csharp
        // Ensure Top Wall if this segment is a Boss Arena
        if (isBossArena)
        {
            if (topWallCollider == null)
            {
                var topTransform = transform.Find("Wall_Top");
                if (topTransform == null)
                {
                    var boundaries = transform.Find("Boundaries");
                    if (boundaries != null) topTransform = boundaries.Find("Wall_Top");
                }

                if (topTransform != null)
                {
                    topWallCollider = topTransform.GetComponent<BoxCollider2D>();
                    topWall = topTransform.gameObject;
                }
            }

            if (topWallCollider == null)
            {
                var topWallGo = new GameObject("Wall_Top");
                topWallGo.transform.SetParent(transform, false);
                topWallGo.transform.localPosition = new Vector3(0f, segmentLength, 0f);
                var box = topWallGo.AddComponent<BoxCollider2D>();
                box.size = new Vector2(segmentWidth, 1.0f);
                box.isTrigger = false;
                topWallGo.tag = "Colliders";
                topWallGo.layer = 0; // Default
                topWallCollider = box;
                topWall = topWallGo;
            }
            else
            {
                ConfigureColliderObject(topWallCollider.gameObject, new Vector3(0f, segmentLength, 0f), new Vector2(segmentWidth, 1.0f));
                if (topWall == null) topWall = topWallCollider.gameObject;
            }
        }
```

Update `ValidateSegment(out string failureReason)` in `MapSegment.cs`:

```csharp
        if (isBossArena)
        {
            if (topWallCollider == null || !topWallCollider.CompareTag("Colliders") || topWallCollider.isTrigger)
            {
                failureReason = "Boss arena segment must have a non-null, non-trigger top wall collider tagged 'Colliders'";
                return false;
            }
        }
```

Update `ResetSegment()` in `MapSegment.cs`:

```csharp
        if (isBossArena)
        {
            CloseTopWall();
        }
```

---

#### 4.2.2 Extension to `Assets/scripts/Editor/MapSegmentPrefabBuilder.cs`

Add `BuildBossArenaPrefab()` and menu item to `MapSegmentPrefabBuilder.cs`:

```csharp
    [MenuItem("Tools/Build Boss Arena Prefab")]
    public static void BuildBossArenaPrefabMenu()
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        Sprite rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/rock.png");
        Sprite monumentSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/rock-monument.png");
        Sprite treeOrangeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/tree-orange.png");
        Sprite treePinkSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/tree-pink.png");

        BuildBossArenaPrefab(monumentSprite, rockSprite, treeOrangeSprite, treePinkSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MapSegmentPrefabBuilder] Successfully created MapSegment_BossArena.prefab in " + PrefabDir);
    }

    public static void BuildBossArenaPrefab(Sprite monument, Sprite rock, Sprite treeOrange, Sprite treePink)
    {
        var root = new GameObject("MapSegment_BossArena");
        try
        {
            var seg = root.AddComponent<MapSegment>();
            seg.segmentLength = 24.0f;
            seg.segmentWidth = 18.0f;
            seg.minCorridorWidth = 16.0f;
            seg.isBossArena = true;

            // Boundaries
            var boundaries = new GameObject("Boundaries");
            boundaries.transform.SetParent(root.transform, false);

            // Left Wall (X = -9.0, Y = 12.0, size 1.0 x 24.0)
            var leftWall = new GameObject("Wall_Left");
            leftWall.transform.SetParent(boundaries.transform, false);
            leftWall.transform.localPosition = new Vector3(-9.0f, 12.0f, 0f);
            var colL = leftWall.AddComponent<BoxCollider2D>();
            colL.size = new Vector2(1.0f, 24.0f);
            colL.isTrigger = false;
            leftWall.tag = "Colliders";
            leftWall.layer = 0;
            seg.leftWallCollider = colL;

            // Right Wall (X = +9.0, Y = 12.0, size 1.0 x 24.0)
            var rightWall = new GameObject("Wall_Right");
            rightWall.transform.SetParent(boundaries.transform, false);
            rightWall.transform.localPosition = new Vector3(9.0f, 12.0f, 0f);
            var colR = rightWall.AddComponent<BoxCollider2D>();
            colR.size = new Vector2(1.0f, 24.0f);
            colR.isTrigger = false;
            rightWall.tag = "Colliders";
            rightWall.layer = 0;
            seg.rightWallCollider = colR;

            // Top Wall (X = 0, Y = 24.0, size 18.0 x 1.0)
            var topWall = new GameObject("Wall_Top");
            topWall.transform.SetParent(boundaries.transform, false);
            topWall.transform.localPosition = new Vector3(0.0f, 24.0f, 0f);
            var colTop = topWall.AddComponent<BoxCollider2D>();
            colTop.size = new Vector2(18.0f, 1.0f);
            colTop.isTrigger = false;
            topWall.tag = "Colliders";
            topWall.layer = 0;
            seg.topWallCollider = colTop;
            seg.topWall = topWall;

            // Camera Lock Anchor / Arena Center (X = 0, Y = 12.0)
            var arenaCenter = new GameObject("ArenaCenter");
            arenaCenter.transform.SetParent(root.transform, false);
            arenaCenter.transform.localPosition = new Vector3(0.0f, 12.0f, 0f);
            seg.cameraLockPoint = arenaCenter.transform;

            // Spawn Points
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform, false);

            var bossSpawn = CreateSpawnPoint(spawnPoints.transform, "BossSpawnPoint", new Vector3(0.0f, 18.0f, 0f));
            seg.bossSpawnPoint = bossSpawn;
            seg.enemySpawnPoints = new Transform[] { bossSpawn };

            var itemSpawn = CreateSpawnPoint(spawnPoints.transform, "ItemSpawn_1", new Vector3(0.0f, 6.0f, 0f));
            seg.itemSpawnPoints = new Transform[] { itemSpawn };

            // Optional Companion Controller
            var arenaCtrl = root.AddComponent<BossArenaController>();
            arenaCtrl.mapSegment = seg;
            arenaCtrl.topWallCollider = colTop;
            arenaCtrl.topWall = topWall;
            arenaCtrl.bossSpawnPoint = bossSpawn;
            arenaCtrl.cameraLockPoint = arenaCenter.transform;

            // Perimeter Visual Decorations (non-blocking outside combat corridor)
            var decorations = new GameObject("Decorations");
            decorations.transform.SetParent(root.transform, false);

            if (monument != null)
            {
                CreateVisual(decorations.transform, "Monument_TopLeft", new Vector3(-7.5f, 22.0f, 0f), monument);
                CreateVisual(decorations.transform, "Monument_TopRight", new Vector3(7.5f, 22.0f, 0f), monument);
                CreateVisual(decorations.transform, "Monument_BotLeft", new Vector3(-7.5f, 2.0f, 0f), monument);
                CreateVisual(decorations.transform, "Monument_BotRight", new Vector3(7.5f, 2.0f, 0f), monument);
            }

            if (rock != null)
            {
                CreateVisual(decorations.transform, "Rock_L1", new Vector3(-8.2f, 8.0f, 0f), rock);
                CreateVisual(decorations.transform, "Rock_L2", new Vector3(-8.2f, 16.0f, 0f), rock);
                CreateVisual(decorations.transform, "Rock_R1", new Vector3(8.2f, 8.0f, 0f), rock);
                CreateVisual(decorations.transform, "Rock_R2", new Vector3(8.2f, 16.0f, 0f), rock);
            }

            SavePrefab(root, $"{PrefabDir}/MapSegment_BossArena.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
```

And in `MapSegmentPrefabBuilder.BuildAllPrefabs()`:
```csharp
        BuildCorridorPrefab(rockSprite, treeOrangeSprite, treePinkSprite, bushSprite);
        BuildChokePointPrefab(monumentSprite, rockSprite, bushSprite);
        BuildSlalomPrefab(treeOrangeSprite, treePinkSprite, rockSprite);
        BuildBossArenaPrefab(monumentSprite, rockSprite, treeOrangeSprite, treePinkSprite);
```

---

#### 4.2.3 New Script: `Assets/scripts/BossArenaController.cs` (Recommended Companion)

```csharp
using UnityEngine;

/// <summary>
/// Controls Boss Arena dynamic boundary state, responds to BossController defeat events,
/// and manages top wall opening so player and camera can transition seamlessly back to endless scrolling.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MapSegment))]
public class BossArenaController : MonoBehaviour
{
    [Header("References")]
    public MapSegment mapSegment;
    public Collider2D topWallCollider;
    public GameObject topWall;
    public Transform bossSpawnPoint;
    public Transform cameraLockPoint;

    [Header("Configuration")]
    [Tooltip("If true, automatically opens the top wall when BossController broadcasts OnBossDefeatedEvent.")]
    public bool autoOpenOnDefeat = true;

    [Header("State")]
    [SerializeField] private bool _isTopWallOpen = false;
    public bool IsTopWallOpen => _isTopWallOpen;

    private void Awake()
    {
        if (mapSegment == null) mapSegment = GetComponent<MapSegment>();
        if (topWallCollider == null && mapSegment != null) topWallCollider = mapSegment.topWallCollider;
        if (topWall == null && mapSegment != null) topWall = mapSegment.topWall;
        if (bossSpawnPoint == null && mapSegment != null) bossSpawnPoint = mapSegment.bossSpawnPoint;
        if (cameraLockPoint == null && mapSegment != null) cameraLockPoint = mapSegment.cameraLockPoint;
    }

    private void OnEnable()
    {
        if (autoOpenOnDefeat)
        {
            BossController.OnBossDefeatedEvent += HandleBossDefeated;
        }
    }

    private void OnDisable()
    {
        BossController.OnBossDefeatedEvent -= HandleBossDefeated;
    }

    private void HandleBossDefeated()
    {
        OpenTopWall();
    }

    /// <summary>
    /// Opens the top boundary wall to allow progress along +Y.
    /// </summary>
    public void OpenTopWall()
    {
        _isTopWallOpen = true;

        if (mapSegment != null)
        {
            mapSegment.OpenTopWall();
        }
        else
        {
            if (topWallCollider != null) topWallCollider.enabled = false;
            if (topWall != null) topWall.SetActive(false);
        }
    }

    /// <summary>
    /// Closes the top boundary wall.
    /// </summary>
    public void CloseTopWall()
    {
        _isTopWallOpen = false;

        if (mapSegment != null)
        {
            mapSegment.CloseTopWall();
        }
        else
        {
            if (topWall != null) topWall.SetActive(true);
            if (topWallCollider != null) topWallCollider.enabled = true;
        }
    }
}
```

---

#### 4.2.4 `MapManager.cs` Integration

In `MapManager.ResumeStandardSpawning()`:
```csharp
    public void ResumeStandardSpawning()
    {
        _isBossArenaSpawned = false;
        _isBossArenaQueued = false;

        // Open top wall of active boss arena segment(s)
        for (int i = 0; i < _activeSegments.Count; i++)
        {
            if (_activeSegments[i] != null && _activeSegments[i].isBossArena)
            {
                _activeSegments[i].OpenTopWall();
            }
        }

        if (cameraController != null)
        {
            cameraController.UnlockAndResume();
        }
    }
```

In `MapManager.ResolveDependencies()`:
```csharp
#if UNITY_EDITOR
        if (bossArenaPrefab == null)
        {
            bossArenaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapSegments/MapSegment_BossArena.prefab");
        }
#endif
```

---

## 5. Verification Method

### 5.1 Programmatic Builder Execution
1. In Unity Editor, execute Menu Item: `Tools -> Build Boss Arena Prefab` (or run via MCP `execute_menu_item`).
2. Verify asset creation at: `Assets/Prefabs/MapSegments/MapSegment_BossArena.prefab`.

### 5.2 Unit & Integration Test Specifications
The following test suite can be implemented by Test Writer to verify all contract requirements:

```csharp
[Test]
public void BossArenaPrefab_DimensionsAndColliders_Valid()
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapSegments/MapSegment_BossArena.prefab");
    Assert.IsNotNull(prefab, "MapSegment_BossArena.prefab must exist in Assets/Prefabs/MapSegments/");

    var seg = prefab.GetComponent<MapSegment>();
    Assert.IsNotNull(seg, "MapSegment component must be attached");
    Assert.AreEqual(24.0f, seg.segmentLength);
    Assert.AreEqual(18.0f, seg.segmentWidth);
    Assert.IsTrue(seg.minCorridorWidth >= 4.0f);
    Assert.IsTrue(seg.isBossArena);

    // Verify Boundary Walls
    Assert.IsNotNull(seg.leftWallCollider);
    Assert.IsNotNull(seg.rightWallCollider);
    Assert.IsNotNull(seg.topWallCollider);

    Assert.AreEqual("Colliders", seg.leftWallCollider.tag);
    Assert.AreEqual("Colliders", seg.rightWallCollider.tag);
    Assert.AreEqual("Colliders", seg.topWallCollider.tag);

    Assert.IsFalse(seg.leftWallCollider.isTrigger);
    Assert.IsFalse(seg.rightWallCollider.isTrigger);
    Assert.IsFalse(seg.topWallCollider.isTrigger);

    var boxL = (BoxCollider2D)seg.leftWallCollider;
    var boxR = (BoxCollider2D)seg.rightWallCollider;
    var boxTop = (BoxCollider2D)seg.topWallCollider;

    Assert.AreEqual(new Vector2(1.0f, 24.0f), boxL.size);
    Assert.AreEqual(new Vector2(1.0f, 24.0f), boxR.size);
    Assert.AreEqual(new Vector2(18.0f, 1.0f), boxTop.size);

    Assert.AreEqual(-9.0f, boxL.transform.localPosition.x);
    Assert.AreEqual(12.0f, boxL.transform.localPosition.y);
    Assert.AreEqual(9.0f, boxR.transform.localPosition.x);
    Assert.AreEqual(12.0f, boxR.transform.localPosition.y);
    Assert.AreEqual(0.0f, boxTop.transform.localPosition.x);
    Assert.AreEqual(24.0f, boxTop.transform.localPosition.y);

    // Verify Anchors
    Assert.IsNotNull(seg.bossSpawnPoint);
    Assert.AreEqual(new Vector3(0f, 18.0f, 0f), seg.bossSpawnPoint.localPosition);

    Assert.IsNotNull(seg.cameraLockPoint);
    Assert.AreEqual(new Vector3(0f, 12.0f, 0f), seg.cameraLockPoint.localPosition);
}

[Test]
public void BossArena_DynamicTopWall_OpenAndClose()
{
    var go = new GameObject("TestBossArena");
    var seg = go.AddComponent<MapSegment>();
    seg.segmentLength = 24.0f;
    seg.segmentWidth = 18.0f;
    seg.isBossArena = true;
    seg.EnsureBoundaryColliders();

    Assert.IsTrue(seg.topWallCollider.enabled);

    seg.OpenTopWall();
    Assert.IsFalse(seg.topWallCollider.enabled);

    seg.CloseTopWall();
    Assert.IsTrue(seg.topWallCollider.enabled);

    UnityEngine.Object.DestroyImmediate(go);
}
```

### 5.3 Invalidation Conditions
- If `MapSegment_BossArena.prefab` is not generated with exact dimensions `(18.0 x 24.0)`.
- If `Wall_Top` is missing, is a trigger, or lacks tag `"Colliders"`.
- If `bossSpawnPoint` is not at local `Y = 18.0f` or `cameraLockPoint` is not at local `Y = 12.0f`.
- If `OpenTopWall()` fails to disable the top collider or restore it upon `CloseTopWall()` / `ResetSegment()`.
