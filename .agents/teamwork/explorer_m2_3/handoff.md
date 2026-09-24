# Handoff Report — Explorer M2.3: Modular Map Segment Spawning, Object Pooling & Dynamic Spawner Integration

**Date**: 2026-09-22  
**Author**: `explorer_m2_3` (teamwork_preview_explorer)  
**Recipient**: `orchestrator_1`  
**Target Milestone**: Milestone 2 (M2 - Modular Map Segment Spawning & Object Pooling)  
**Working Directory**: `.agents/teamwork/explorer_m2_3/`  
**Reference Documents**:
- `.agents/teamwork/ORIGINAL_REQUEST.md` (Requirement R2, Acceptance Criteria)
- `PROJECT.md` (Features 8, 9, 10, 11; Interface Contracts)
- `.agents/teamwork/explorer_1/handoff.md` & `explorer_2/handoff.md`
- `.agents/teamwork/explorer_m2_1/DISPATCH.md` & `explorer_m2_2/DISPATCH.md`

---

## 1. Observation

### 1.1 Project Test Suite Baseline & Existing Assertions
Inspection of the existing test suites (`Assets/scripts/Tests/`) revealed **541 automated tests** currently passing at 100%:
- `E2E Tests/Run All Tests` (Tiers 1–4): **385 / 385 passed**.
- `E2E Tests/Run Tier 5 (Adversarial Hardening)`: **36 / 36 passed**.
- `E2E Tests/Run Scrolling Map Tests` (`ScrollingMapTests.cs`): **120 / 120 passed**.
- Specific test assertions in `Milestone2Tests.cs` and `ChallengerM2Tests.cs`:
  - `M2-01` (`Milestone2Tests.cs:27-33`): Verifies `spawner.GeneratePerimeterPosition(playerPos)` produces coordinates on perimeter edges with `minX = -10.5f, maxX = 16.0f, minY = -6.0f, maxY = 7.0f`.
  - `CH-M2-07` (`ChallengerM2Tests.cs:200-268`): Executes 1,000 iterations verifying candidate points strictly lie on the perimeter box.
  - `CH-M2-15` (`ChallengerM2Tests.cs:556-602`): Calls `spawner.SpawnEnemy(0)` 30 times in an empty test scene without active map segments and asserts `spawner.ActiveEnemyCount == 30`.
  - **Invariance Rule**: Any modifications to `EnemySpawner.cs` must be strictly additive and keep `GeneratePerimeterPosition` and default field values identical, falling back to legacy perimeter spawning when no segment spawn points are registered.

### 1.2 Scrolling Map System Specifications (`ScrollingMapTests.cs` & `PROJECT.md`)
Direct code inspection of `ScrollingMapTests.cs` establishes exact mathematical constants and contracts:
1. **Segment Dimensions** (`ScrollingMapTests.cs:420-435`):
   - Standard length: `segmentLength = 20.0f` units along +Y.
   - Standard width: `segmentWidth = 15.0f` units with lateral walls at $X \in [-7.5, +7.5]$.
   - Guaranteed corridor width: strictly $\ge 4.0\text{f}$ units clearance.
2. **Alignment Formula** (`ScrollingMapTests.cs:440-449, 1200-1208`):
   $$\text{SpawnY}_k = \text{SpawnY}_{k-1} + 20.0\text{f}$$
   $$\text{SeamDelta} = |\text{SpawnY}_k - (\text{SpawnY}_{k-1} + 20.0\text{f})| < 0.001\text{f}$$
3. **Active Segment Count** (`ScrollingMapTests.cs:548-564, 1757-1767, 1976-1984`):
   - Active segment count must remain strictly bounded between **3 and 4 segments** at all times.
   - Initial active count = 3 or 4 covering $60-80$ units ahead of camera.
   - At 1000m traversal (Tier 4 Scenario 1, lines 1980–1984): `segmentsSpawned = 50`, `segmentsRecycled = 47`, `activeCount = 3`.
4. **Cleanup Threshold** (`ScrollingMapTests.cs:481-493, 1254-1269`):
   - Threshold formula: $\text{CleanupThreshold} = \text{cam.transform.position.y} - 25.0\text{f}$.
   - Recycling condition: when $\text{segment.TopY} < \text{CleanupThreshold}$, deactivate (`SetActive(false)`) and recycle into object pool queue without `GameObject.Destroy()`.
   - "Exact Despawn Distance: Verifies segment at camY - 24.99u is kept; segment at camY - 25.01u is recycled."
5. **Controlled Randomness**:
   - Immediate repetition of the identical segment prefab is forbidden ($P(\text{consecutive identical}) = 0$).

### 1.3 Active Scene & Component Context (`Scenes/shooting.unity` & `ScrollingCameraController.cs`)
- `ScrollingCameraController.cs`:
  - Attached to Main Camera. Translates camera along +Y at baseline speed 2.0 u/s scaling up to 3.5 u/s.
  - Automatically disables `Wall_Top` BoxCollider2D at runtime via `OpenStartingArenaTopWall()` (`ScrollingCameraController.cs:136-151`), allowing seamless progression into upcoming segments.
  - Exposes `isScrollLocked`, `LockAt(worldY)`, `UnlockAndResume()`, `DistanceTravelled`, `CurrentSpeed`.
- Starting Arena Geometry:
  - Starting arena extends from $Y = -8.79$ to $Y = 8.95$.
  - First procedural segment connects at $\text{initialSpawnY} = 8.95\text{f}$ (or $0.0\text{f}$ in standalone tests).

---

## 2. Logic Chain

```
[Observation 1.1: 541 Tests Passing & Baseline Invariants]
   │
   ├─► Must not break default fields or API of EnemySpawner.cs
   │   => Dynamic segment spawning must be additive (useSegmentSpawnPoints flag).
   │   => When _activeSegmentSpawnPoints is empty (e.g. in test suites), fall back to legacy perimeter!
   │
[Observation 1.2: Pool Sizing & Zero-GC Guarantee]
   │
   ├─► Max active segments at any time = 4 segments (80 units).
   │   There are 3 interchangeable prefabs (Corridor, ChokePoint, Slalom).
   │   Controlled randomness forbids immediate repetition of the same prefab.
   │   => At most 2 instances of any single prefab can be active simultaneously!
   │   => Prewarming 3 instances per prefab = 9 instances total.
   │   => Pool queue will NEVER be empty in steady-state gameplay!
   │   => Zero runtime Instantiate() or Destroy() calls => Zero GC allocation!
   │
[Observation 1.2: Alignment & Seam Math]
   │
   ├─► Each segment origin is at its bottom center: (0.0f, spawnY, 0.0f).
   │   Segment length = 20.0f.
   │   Next spawn Y is monotonically incremented: SpawnY_k = SpawnY_{k-1} + 20.0f.
   │   => Boundary colliders at X = -7.5 and X = +7.5 connect continuously across segments without gaps.
   │
[Observation 1.2: Cleanup Threshold at camY - 25.0f]
   │
   ├─► Camera vertical half-height is ~6.31 units (visible bottom edge is camY - 6.31).
   │   Cleanup distance of 25.0f is well behind camera bottom edge (18.69 units below viewport).
   │   => Zero visible pop-out or premature deactivation!
   │   => Recycled segments are deactivated via SetActive(false) and returned to pool queue.
   │
[Observation 1.2 & 1.3: Dynamic Enemy Spawner Integration]
   │
   └─► When MapManager activates a new segment ahead, it feeds segment.enemySpawnPoints
       to EnemySpawner via OnSegmentActivated(segment).
       When recycled, MapManager calls OnSegmentRecycled(segment).
       => Enemies spawn at tactical, passable segment locations ahead of player.
```

---

## 3. Caveats

1. **Unity Physics & Scene Transforms**:
   - `MapSegment` instances must be positioned with local scale `(1, 1, 1)` and zero rotation. Lateral boundary colliders inside prefabs must be local $X = -7.5$ and $+7.5$, keeping the segment centered at $X = 0.0$.
2. **Tagging Requirement**:
   - Boundary colliders and obstacle colliders inside all segment prefabs must be tagged `"Colliders"` with `isTrigger = false` to block projectiles (`Bullet.cs`, `EnemyBullet.cs`) and rigidbodies (`PlayerMovement.cs`, `EnemyBase.cs`).
3. **Boss Arena Handoff to M3**:
   - `MapManager` includes complete queue and trigger hooks for `bossArenaPrefab` and `cameraController.LockAt(bossArenaCenterY)` so Milestone 3 requires zero architectural refactoring.

---

## 4. Conclusion & Complete Implementation Design

### 4.1 Component 1: `Assets/scripts/MapSegment.cs` (Reference Specification)

This component sits on the root of every segment prefab:

```csharp
using UnityEngine;

/// <summary>
/// Component attached to the root of modular map segment prefabs.
/// Defines physical dimensions, lateral boundaries, passable corridor clearance,
/// and designated tactical enemy/item spawn points.
/// </summary>
[DisallowMultipleComponent]
public class MapSegment : MonoBehaviour
{
    [Header("Segment Dimensions")]
    [Tooltip("Standardized vertical length of this segment along +Y in world units.")]
    public float segmentLength = 20.0f;

    [Tooltip("Standardized lateral width between outer boundary walls in world units.")]
    public float segmentWidth = 15.0f;

    [Tooltip("Guaranteed minimum passable corridor width in world units (strictly >= 4.0u).")]
    public float minCorridorWidth = 4.0f;

    [Header("Spawn Points")]
    [Tooltip("Designated tactical spawn points for enemies inside this segment.")]
    public Transform[] enemySpawnPoints;

    [Tooltip("Designated spawn points for items/pickups inside this segment.")]
    public Transform[] itemSpawnPoints;

    [Header("Pool Metadata")]
    [Tooltip("Catalog index of the prefab this instance was pooled from.")]
    public int PrefabIndex { get; set; } = -1;

    [Tooltip("Sequential spawn index of this segment in the endless run.")]
    public int SegmentId { get; set; } = 0;

    /// <summary>
    /// World Y coordinate of the bottom edge of this segment.
    /// </summary>
    public float StartY => transform.position.y;

    /// <summary>
    /// World Y coordinate of the top edge of this segment.
    /// </summary>
    public float TopY => transform.position.y + segmentLength;

    /// <summary>
    /// Returns the enemy spawn points array, ensuring non-null return.
    /// </summary>
    public Transform[] GetSpawnPoints()
    {
        return enemySpawnPoints ?? new Transform[0];
    }

    /// <summary>
    /// Returns the world-space bounding box for this segment.
    /// </summary>
    public Bounds GetWorldBounds()
    {
        Vector3 center = new Vector3(transform.position.x, transform.position.y + segmentLength * 0.5f, transform.position.z);
        Vector3 size = new Vector3(segmentWidth, segmentLength, 1.0f);
        return new Bounds(center, size);
    }

    /// <summary>
    /// Resets runtime state when the segment is deactivated and returned to the object pool.
    /// </summary>
    public void ResetSegment()
    {
        // Deactivate any uncollected pickup instances attached to this segment
        if (itemSpawnPoints != null)
        {
            for (int i = 0; i < itemSpawnPoints.Length; i++)
            {
                if (itemSpawnPoints[i] != null)
                {
                    var pickup = itemSpawnPoints[i].GetComponentInChildren<GrenadePickup>();
                    if (pickup != null)
                    {
                        pickup.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
```

---

### 4.2 Component 2: `Assets/scripts/MapSegmentPool.cs` (Complete Code)

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prewarmed object pool for procedural MapSegment instances.
/// Maintains queues of inactive segments to guarantee zero runtime GC allocations
/// and memory stability over prolonged endless runs (Requirement R2).
/// </summary>
[DisallowMultipleComponent]
public class MapSegmentPool : MonoBehaviour
{
    [Header("Segment Prefab Catalog")]
    [Tooltip("Catalog of interchangeable MapSegment prefabs (e.g., Corridor, ChokePoint, Slalom).")]
    public GameObject[] segmentPrefabs;

    [Header("Pool Configuration")]
    [Tooltip("Number of prewarmed instances per prefab catalog entry (default 3).")]
    [Range(2, 5)]
    public int prewarmCountPerPrefab = 3;

    [Tooltip("Parent transform to hold inactive pooled segments in the hierarchy.")]
    public Transform poolContainer;

    private Queue<MapSegment>[] _pools;
    private bool _isInitialized = false;
    private int _totalPrewarmed = 0;

    /// <summary>
    /// Total number of segment instances instantiated across all prewarm queues.
    /// </summary>
    public int TotalPrewarmedCount => _totalPrewarmed;

    /// <summary>
    /// Number of prefab types in the catalog.
    /// </summary>
    public int PrefabCatalogCount => segmentPrefabs != null ? segmentPrefabs.Length : 0;

    /// <summary>
    /// Whether the pool has completed initial prewarming.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    private void Awake()
    {
        if (!_isInitialized && segmentPrefabs != null && segmentPrefabs.Length > 0)
        {
            Prewarm();
        }
    }

    /// <summary>
    /// Prewarms the pool queues with segment instances.
    /// Safe to call explicitly in EditMode or integration tests.
    /// </summary>
    public void Prewarm(Transform container = null)
    {
        if (_isInitialized) return;

        if (container != null)
        {
            poolContainer = container;
        }
        else if (poolContainer == null)
        {
            var containerGo = new GameObject("[SegmentPoolContainer]");
            containerGo.transform.SetParent(transform);
            poolContainer = containerGo.transform;
        }

        if (segmentPrefabs == null || segmentPrefabs.Length == 0)
        {
            _pools = new Queue<MapSegment>[0];
            _isInitialized = true;
            return;
        }

        _pools = new Queue<MapSegment>[segmentPrefabs.Length];
        _totalPrewarmed = 0;

        for (int i = 0; i < segmentPrefabs.Length; i++)
        {
            _pools[i] = new Queue<MapSegment>();
            GameObject prefab = segmentPrefabs[i];
            if (prefab == null) continue;

            for (int k = 0; k < prewarmCountPerPrefab; k++)
            {
                MapSegment instance = CreateNewInstance(prefab, i);
                _pools[i].Enqueue(instance);
                _totalPrewarmed++;
            }
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Retrieves an inactive segment instance of the specified prefab catalog index from the pool.
    /// If the queue is depleted, defensively instantiates an additional instance with a warning.
    /// </summary>
    /// <param name="prefabIndex">Index into segmentPrefabs array.</param>
    /// <returns>A pooled MapSegment instance (ready to be positioned and activated).</returns>
    public MapSegment GetSegment(int prefabIndex)
    {
        if (!_isInitialized)
        {
            Prewarm();
        }

        if (prefabIndex < 0 || prefabIndex >= _pools.Length)
        {
            Debug.LogError($"[MapSegmentPool] Invalid prefab index {prefabIndex}. Catalog size: {_pools.Length}");
            return null;
        }

        Queue<MapSegment> queue = _pools[prefabIndex];
        MapSegment segment;

        if (queue.Count > 0)
        {
            segment = queue.Dequeue();
        }
        else
        {
            // Defensive capacity expansion fallback
            Debug.LogWarning($"[MapSegmentPool] Pool queue for prefab index {prefabIndex} exhausted. Dynamically allocating fallback instance.");
            segment = CreateNewInstance(segmentPrefabs[prefabIndex], prefabIndex);
            _totalPrewarmed++;
        }

        return segment;
    }

    /// <summary>
    /// Recycles an active segment back into its appropriate pool queue.
    /// Deactivates the GameObject without any runtime GC allocation or Destroy call.
    /// </summary>
    /// <param name="segment">The MapSegment to recycle.</param>
    public void ReturnSegment(MapSegment segment)
    {
        if (segment == null) return;

        // Reset segment state
        segment.ResetSegment();

        // Deactivate and reparent to pool container
        segment.gameObject.SetActive(false);
        if (poolContainer != null)
        {
            segment.transform.SetParent(poolContainer);
        }

        int index = segment.PrefabIndex;
        if (index >= 0 && index < _pools.Length)
        {
            _pools[index].Enqueue(segment);
        }
        else
        {
            // Fallback: search which queue matches
            for (int i = 0; i < segmentPrefabs.Length; i++)
            {
                if (segmentPrefabs[i] != null && segment.name.StartsWith(segmentPrefabs[i].name))
                {
                    segment.PrefabIndex = i;
                    _pools[i].Enqueue(segment);
                    return;
                }
            }
            if (_pools.Length > 0)
            {
                _pools[0].Enqueue(segment);
            }
        }
    }

    /// <summary>
    /// Returns the number of available (inactive) segments in the queue for a given prefab index.
    /// </summary>
    public int AvailableCount(int prefabIndex)
    {
        if (_pools == null || prefabIndex < 0 || prefabIndex >= _pools.Length) return 0;
        return _pools[prefabIndex].Count;
    }

    private MapSegment CreateNewInstance(GameObject prefab, int prefabIndex)
    {
        GameObject obj = Instantiate(prefab, poolContainer);
        obj.name = $"{prefab.name}_Pooled";
        obj.SetActive(false);

        MapSegment seg = obj.GetComponent<MapSegment>();
        if (seg == null)
        {
            seg = obj.AddComponent<MapSegment>();
        }

        seg.PrefabIndex = prefabIndex;
        return seg;
    }
}
```

---

### 4.3 Component 3: `Assets/scripts/MapManager.cs` (Complete Code)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages procedural modular map segment spawning, sequential alignment, recycling,
/// camera distance tracking, and dynamic enemy spawn point feeding for endless scrolling.
/// </summary>
[DisallowMultipleComponent]
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Dependencies")]
    [Tooltip("Reference to the camera controller. Defaults to ScrollingCameraController.Instance.")]
    public ScrollingCameraController cameraController;

    [Tooltip("Reference to the main camera. Defaults to Camera.main.")]
    public Camera cam;

    [Tooltip("Reference to the MapSegmentPool component.")]
    public MapSegmentPool pool;

    [Tooltip("Reference to the EnemySpawner in the scene.")]
    public EnemySpawner enemySpawner;

    [Header("Spawning & Alignment Settings")]
    [Tooltip("Starting world Y coordinate where the first procedural segment connects.")]
    public float initialSpawnY = 8.95f;

    [Tooltip("Standardized vertical length per segment (SpawnY_k = SpawnY_{k-1} + 20.0f).")]
    public float segmentLength = 20.0f;

    [Tooltip("Number of segments to maintain ahead of camera initially.")]
    [Range(2, 5)]
    public int initialSegmentCount = 3;

    [Tooltip("Distance ahead of camera view that must remain covered by active segments.")]
    public float aheadTriggerDistance = 60.0f;

    [Tooltip("Distance behind camera below which off-screen segments are recycled (camY - 25.0f).")]
    public float cleanupDistance = 25.0f;

    [Header("Boss Arena Settings")]
    [Tooltip("Dedicated boss arena prefab (24u x 18u).")]
    public GameObject bossArenaPrefab;

    [Tooltip("Vertical length of the boss arena.")]
    public float bossArenaLength = 24.0f;

    [Header("Runtime Status")]
    [SerializeField] private float _nextSpawnY = 0f;
    [SerializeField] private int _lastPrefabIndex = -1;
    [SerializeField] private int _totalSegmentsSpawned = 0;
    [SerializeField] private int _totalSegmentsRecycled = 0;
    [SerializeField] private bool _isBossArenaQueued = false;
    [SerializeField] private bool _isBossArenaSpawned = false;
    [SerializeField] private float _bossArenaCenterY = 0f;

    private readonly List<MapSegment> _activeSegments = new List<MapSegment>();

    // Public Properties
    public float NextSpawnY => _nextSpawnY;
    public int LastPrefabIndex => _lastPrefabIndex;
    public int TotalSegmentsSpawned => _totalSegmentsSpawned;
    public int TotalSegmentsRecycled => _totalSegmentsRecycled;
    public int ActiveSegmentCount => _activeSegments.Count;
    public IReadOnlyList<MapSegment> ActiveSegments => _activeSegments;
    public bool IsBossArenaQueued => _isBossArenaQueued;
    public bool IsBossArenaSpawned => _isBossArenaSpawned;
    public float BossArenaCenterY => _bossArenaCenterY;

    // Events
    public event Action<MapSegment> OnSegmentSpawned;
    public event Action<MapSegment> OnSegmentRecycled;
    public event Action OnBossArenaSpawnedEvent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        ResolveDependencies();
    }

    private void Start()
    {
        ResolveDependencies();

        if (Application.isPlaying)
        {
            InitializeMap();
        }
    }

    public void ResolveDependencies()
    {
        if (cam == null) cam = Camera.main;
        if (cameraController == null) cameraController = ScrollingCameraController.Instance ?? FindObjectOfType<ScrollingCameraController>();
        if (pool == null) pool = GetComponent<MapSegmentPool>() ?? FindObjectOfType<MapSegmentPool>();
        if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();
    }

    /// <summary>
    /// Initializes or resets the procedural map.
    /// Prewarms the pool and spawns the initial active segments ahead of camera.
    /// </summary>
    public void InitializeMap(float? startY = null)
    {
        ResolveDependencies();

        RecycleAllActiveSegments();

        _nextSpawnY = startY ?? initialSpawnY;
        _lastPrefabIndex = -1;
        _totalSegmentsSpawned = 0;
        _totalSegmentsRecycled = 0;
        _isBossArenaQueued = false;
        _isBossArenaSpawned = false;

        if (pool != null && !pool.IsInitialized)
        {
            pool.Prewarm();
        }

        // Spawn initial active segments
        for (int i = 0; i < initialSegmentCount; i++)
        {
            SpawnNextSegment();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Pause guard: freeze map updates when game is paused or over
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        float currentCamY = GetCameraY();

        // 1. Spawning ahead
        CheckSpawnAhead(currentCamY);

        // 2. Trailing cleanup
        CheckCleanupTrailing(currentCamY);
    }

    /// <summary>
    /// Checks camera position and spawns segments ahead if coverage drops below aheadTriggerDistance.
    /// </summary>
    public void CheckSpawnAhead(float camY)
    {
        if (_isBossArenaSpawned) return;

        if (_isBossArenaQueued && !_isBossArenaSpawned)
        {
            SpawnBossArenaInternal();
            return;
        }

        while (_nextSpawnY - camY < aheadTriggerDistance)
        {
            SpawnNextSegment();
        }
    }

    /// <summary>
    /// Spawns the next procedural map segment aligned with the previous segment.
    /// Alignment formula: SpawnY_k = SpawnY_{k-1} + 20.0f
    /// </summary>
    /// <param name="specificPrefabIndex">Optional forced prefab index (for deterministic testing).</param>
    /// <returns>The spawned MapSegment.</returns>
    public MapSegment SpawnNextSegment(int specificPrefabIndex = -1)
    {
        if (pool == null)
        {
            Debug.LogError("[MapManager] MapSegmentPool is not assigned.");
            return null;
        }

        int prefabIndex = (specificPrefabIndex >= 0) ? specificPrefabIndex : PickNextPrefabIndex();
        MapSegment segment = pool.GetSegment(prefabIndex);
        if (segment == null) return null;

        // Position segment at sequential top Y alignment (X = 0, Y = _nextSpawnY)
        segment.transform.position = new Vector3(0f, _nextSpawnY, 0f);
        segment.SegmentId = _totalSegmentsSpawned;
        segment.gameObject.SetActive(true);

        _activeSegments.Add(segment);
        _totalSegmentsSpawned++;

        // Advance next spawn coordinate by segmentLength (20.0f)
        _nextSpawnY += segment.segmentLength;

        // Feed enemy spawn points to EnemySpawner
        if (enemySpawner != null)
        {
            enemySpawner.OnSegmentActivated(segment);
        }

        OnSegmentSpawned?.Invoke(segment);
        return segment;
    }

    /// <summary>
    /// Checks trailing segments and recycles any whose top edge has fallen behind cleanup threshold.
    /// Cleanup threshold: segment.TopY < camY - 25.0f
    /// </summary>
    public void CheckCleanupTrailing(float camY)
    {
        float cleanupThreshold = camY - cleanupDistance;

        while (_activeSegments.Count > 0)
        {
            MapSegment oldest = _activeSegments[0];
            if (oldest == null)
            {
                _activeSegments.RemoveAt(0);
                continue;
            }

            if (oldest.TopY < cleanupThreshold)
            {
                RecycleSegmentAt(0);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Recycles a specific active segment by index.
    /// </summary>
    public void RecycleSegmentAt(int index)
    {
        if (index < 0 || index >= _activeSegments.Count) return;

        MapSegment segment = _activeSegments[index];
        _activeSegments.RemoveAt(index);

        if (segment != null)
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnSegmentRecycled(segment);
            }

            OnSegmentRecycled?.Invoke(segment);

            if (pool != null)
            {
                pool.ReturnSegment(segment);
            }
            else
            {
                segment.gameObject.SetActive(false);
            }

            _totalSegmentsRecycled++;
        }
    }

    /// <summary>
    /// Recycles all currently active segments back to the pool.
    /// </summary>
    public void RecycleAllActiveSegments()
    {
        while (_activeSegments.Count > 0)
        {
            RecycleSegmentAt(0);
        }
    }

    /// <summary>
    /// Controlled randomness: selects a random prefab index from the catalog
    /// while strictly preventing immediate identical repetition.
    /// </summary>
    public int PickNextPrefabIndex()
    {
        if (pool == null || pool.PrefabCatalogCount <= 1)
        {
            _lastPrefabIndex = 0;
            return 0;
        }

        int catalogCount = pool.PrefabCatalogCount;
        int nextIndex;

        if (_lastPrefabIndex < 0)
        {
            nextIndex = UnityEngine.Random.Range(0, catalogCount);
        }
        else
        {
            // Pick uniformly from the (N-1) non-identical alternatives
            int offset = UnityEngine.Random.Range(1, catalogCount);
            nextIndex = (_lastPrefabIndex + offset) % catalogCount;
        }

        _lastPrefabIndex = nextIndex;
        return nextIndex;
    }

    /// <summary>
    /// Queues the boss arena segment to spawn at the next spawn boundary.
    /// Halts regular random segment generation.
    /// </summary>
    public void QueueBossArena()
    {
        _isBossArenaQueued = true;
    }

    private void SpawnBossArenaInternal()
    {
        _isBossArenaSpawned = true;
        _isBossArenaQueued = false;

        float arenaStartY = _nextSpawnY;
        _bossArenaCenterY = arenaStartY + (bossArenaLength * 0.5f);

        GameObject arenaObj = null;
        if (bossArenaPrefab != null)
        {
            arenaObj = Instantiate(bossArenaPrefab, new Vector3(0f, arenaStartY, 0f), Quaternion.identity);
            arenaObj.SetActive(true);
            var seg = arenaObj.GetComponent<MapSegment>();
            if (seg != null)
            {
                _activeSegments.Add(seg);
            }
        }

        _nextSpawnY += bossArenaLength;

        // Command camera controller to lock at arena center
        if (cameraController != null)
        {
            cameraController.LockAt(_bossArenaCenterY);
        }

        OnBossArenaSpawnedEvent?.Invoke();
    }

    /// <summary>
    /// Resumes normal procedural segment generation after boss defeat.
    /// </summary>
    public void ResumeStandardSpawning()
    {
        _isBossArenaSpawned = false;
        _isBossArenaQueued = false;

        if (cameraController != null)
        {
            cameraController.UnlockAndResume();
        }
    }

    /// <summary>
    /// Helper to get current camera Y coordinate safely.
    /// </summary>
    public float GetCameraY()
    {
        if (cam != null) return cam.transform.position.y;
        if (cameraController != null) return cameraController.transform.position.y;
        return 0f;
    }
}
```

---

### 4.4 Component 4: `Assets/scripts/EnemySpawner.cs` Extensions

Add the following fields and methods to `EnemySpawner.cs` without altering any existing public members or default values:

```csharp
// -------------------------------------------------------------
// ADDITIVE EXTENSIONS FOR MILESTONE 2: DYNAMIC SEGMENT SPAWNING
// -------------------------------------------------------------

[Header("Dynamic Segment Spawning (Milestone 2)")]
[Tooltip("If true, enemies spawn at active segment spawn points instead of static perimeter.")]
public bool useSegmentSpawnPoints = true;

[Tooltip("Whether to automatically populate each newly spawned segment with enemies upon activation.")]
public bool autoPopulateSegments = true;

[Tooltip("Maximum enemies to spawn when a segment is activated.")]
public int enemiesPerSegment = 2;

private readonly List<Transform> _activeSegmentSpawnPoints = new List<Transform>();

/// <summary>
/// Registers spawn points from an activated map segment.
/// Called by MapManager when a new segment spawns ahead.
/// </summary>
public void RegisterSegmentSpawnPoints(Transform[] spawnPoints)
{
    if (spawnPoints == null || spawnPoints.Length == 0) return;
    for (int i = 0; i < spawnPoints.Length; i++)
    {
        if (spawnPoints[i] != null && !_activeSegmentSpawnPoints.Contains(spawnPoints[i]))
        {
            _activeSegmentSpawnPoints.Add(spawnPoints[i]);
        }
    }
}

/// <summary>
/// Unregisters spawn points when a segment is recycled.
/// Called by MapManager before recycling a segment.
/// </summary>
public void UnregisterSegmentSpawnPoints(Transform[] spawnPoints)
{
    if (spawnPoints == null || spawnPoints.Length == 0) return;
    for (int i = 0; i < spawnPoints.Length; i++)
    {
        if (spawnPoints[i] != null)
        {
            _activeSegmentSpawnPoints.Remove(spawnPoints[i]);
        }
    }
}

/// <summary>
/// Clears all registered segment spawn points.
/// </summary>
public void ClearSegmentSpawnPoints()
{
    _activeSegmentSpawnPoints.Clear();
}

/// <summary>
/// When a new segment is activated, MapManager invokes this entry point.
/// Registers spawn points and populates the segment if autoPopulateSegments is true.
/// </summary>
public void OnSegmentActivated(MapSegment segment)
{
    if (segment == null) return;

    RegisterSegmentSpawnPoints(segment.enemySpawnPoints);

    if (autoPopulateSegments && isSpawning)
    {
        int score = GetCurrentScore();
        SpawnEnemiesInSegment(segment, score);
    }
}

/// <summary>
/// When a segment is deactivated and recycled, MapManager invokes this entry point.
/// </summary>
public void OnSegmentRecycled(MapSegment segment)
{
    if (segment == null) return;
    UnregisterSegmentSpawnPoints(segment.enemySpawnPoints);
}

/// <summary>
/// Populates an activated segment with enemies using its designated spawn points.
/// Respects max concurrency ceiling and current score tiers.
/// </summary>
public List<GameObject> SpawnEnemiesInSegment(MapSegment segment, int score)
{
    var spawnedList = new List<GameObject>();
    if (segment == null || segment.enemySpawnPoints == null) return spawnedList;

    int maxAllowed = CalculateMaxConcurrentEnemies(survivalTime, score);
    int spawnCount = Mathf.Min(enemiesPerSegment, segment.enemySpawnPoints.Length);

    for (int i = 0; i < spawnCount; i++)
    {
        if (ActiveEnemyCount >= maxAllowed) break;

        Transform pt = segment.enemySpawnPoints[i];
        if (pt == null) continue;

        GameObject prefab = SelectEnemyArchetype(score);
        if (prefab == null) continue;

        GameObject enemyObj = Instantiate(prefab, pt.position, Quaternion.identity);
        var enemyComp = enemyObj.GetComponent<EnemyBase>();
        if (enemyComp != null)
        {
            if (_playerTransform != null)
            {
                enemyComp.SetPlayer(_playerTransform);
            }
            _activeEnemies.Add(enemyComp);
            spawnedList.Add(enemyObj);
        }
    }

    return spawnedList;
}

/// <summary>
/// Selects the best candidate segment spawn point that is ahead of player and respects min distance.
/// </summary>
public Transform GetBestSegmentSpawnPoint(Vector2 playerPos)
{
    if (_activeSegmentSpawnPoints.Count == 0) return null;

    // Filter points ahead of player with distance >= minPlayerDistance
    var validCandidates = new List<Transform>();
    for (int i = 0; i < _activeSegmentSpawnPoints.Count; i++)
    {
        Transform pt = _activeSegmentSpawnPoints[i];
        if (pt == null) continue;

        float dist = Vector2.Distance(pt.position, playerPos);
        if (dist >= minPlayerDistance && pt.position.y >= playerPos.y - 2.0f)
        {
            validCandidates.Add(pt);
        }
    }

    if (validCandidates.Count > 0)
    {
        return validCandidates[UnityEngine.Random.Range(0, validCandidates.Count)];
    }

    return _activeSegmentSpawnPoints[UnityEngine.Random.Range(0, _activeSegmentSpawnPoints.Count)];
}
```

And in `EnemySpawner.SpawnEnemy(int score)`:
Update coordinate resolution to prefer `_activeSegmentSpawnPoints` when active:

```csharp
public GameObject SpawnEnemy(int score)
{
    GameObject prefab = SelectEnemyArchetype(score);
    if (prefab == null) return null;

    Vector2 playerPos = _playerTransform != null ? (Vector2)_playerTransform.position : Vector2.zero;
    Vector2 spawnPos;

    // Use segment spawn points if available, otherwise fall back to legacy perimeter box
    if (useSegmentSpawnPoints && _activeSegmentSpawnPoints.Count > 0)
    {
        Transform pt = GetBestSegmentSpawnPoint(playerPos);
        spawnPos = pt != null ? (Vector2)pt.position : GeneratePerimeterPosition(playerPos);
    }
    else
    {
        spawnPos = GeneratePerimeterPosition(playerPos);
    }

    GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
    var enemyComp = enemyObj.GetComponent<EnemyBase>();
    if (enemyComp != null)
    {
        if (_playerTransform != null)
        {
            enemyComp.SetPlayer(_playerTransform);
        }
        _activeEnemies.Add(enemyComp);
    }

    return enemyObj;
}
```

---

### 4.5 Scene Integration Architecture (`shooting.unity`)

To integrate into `shooting.unity`:
1. **Scene GameObject**:
   - Create empty GameObject named `[MapManager]` in `shooting.unity`.
   - Add `MapManager` and `MapSegmentPool` components.
   - Configure `segmentPrefabs` in `MapSegmentPool` with:
     - `Assets/Prefabs/MapSegments/MapSegment_Corridor.prefab`
     - `Assets/Prefabs/MapSegments/MapSegment_ChokePoint.prefab`
     - `Assets/Prefabs/MapSegments/MapSegment_Slalom.prefab`
   - Set `prewarmCountPerPrefab = 3`.
   - Assign `initialSpawnY = 8.95f` (immediately above starting arena).
2. **Player and Camera Alignment**:
   - Player starts inside the initial arena.
   - Main Camera auto-scrolls along +Y.
   - `ScrollingCameraController` disables `Wall_Top` BoxCollider2D.
   - Player moves smoothly through initial arena into Segment 0 at $Y = 8.95$.

---

## 5. Verification Method

To independently verify this design and all downstream code implementation:

### 5.1 Verification Commands via unityMCP

1. **Verify Baseline Test Invariance**:
   ```json
   {
     "tool": "execute_menu_item",
     "arguments": { "menu_path": "E2E Tests/Run All Tests" }
   }
   ```
   *Expected Output*: `Total: 385, Passed: 385, Failed: 0, Pending: 0, Skipped: 0`.

2. **Verify Milestone 2 Enemy & Spawner Invariance**:
   ```json
   {
     "tool": "execute_menu_item",
     "arguments": { "menu_path": "E2E Tests/Run Milestone 2 Tests" }
   }
   ```
   *Expected Output*: `Total: 16, Passed: 16, Failed: 0, Pending: 0, Skipped: 0`.

3. **Verify Challenger M2 Adversarial Suite**:
   ```json
   {
     "tool": "execute_menu_item",
     "arguments": { "menu_path": "E2E Tests/Run Challenger M2 Tests" }
   }
   ```
   *Expected Output*: `Total: 17, Passed: 17, Failed: 0, Pending: 0, Skipped: 0`.

4. **Verify Scrolling Map Test Suite**:
   ```json
   {
     "tool": "execute_menu_item",
     "arguments": { "menu_path": "E2E Tests/Run Scrolling Map Tests" }
   }
   ```
   *Expected Output*: `Total: 120, Passed: 120, Failed: 0, Pending: 0, Skipped: 0`.

### 5.2 Specific Assertions to Inspect in New M2 Unit Tests

1. **T1: Prewarm Capacity Test**:
   - Verify `pool.TotalPrewarmedCount == 9` (3 prefabs $\times$ 3 prewarmed).
   - Verify `pool.AvailableCount(0) == 3`, `AvailableCount(1) == 3`, `AvailableCount(2) == 3`.
   - Verify all prewarmed instances have `activeSelf == false`.
2. **T2: Zero-GC Cycling Test**:
   - Spawn and recycle 50 segments in a loop.
   - Verify `manager.TotalSegmentsSpawned == 50`, `manager.TotalSegmentsRecycled == 47`, `manager.ActiveSegmentCount == 3`.
   - Verify `pool.TotalPrewarmedCount == 9` (zero dynamic allocations).
3. **T3: Alignment Precision Test**:
   - Verify `seg[i].transform.position.y == initialSpawnY + (i * 20.0f)`.
   - Verify `Mathf.Abs(seg[i].TopY - seg[i+1].StartY) < 0.0001f`.
4. **T4: Despawn Threshold Test**:
   - At `camY = 100.0f`, `cleanupDistance = 25.0f` (threshold 75.0f).
   - Segment with `TopY = 75.01f` is NOT recycled.
   - Segment with `TopY = 74.99f` is recycled.
5. **T5: Controlled Randomness Uniformity Test**:
   - Roll 1,000 segment indexes via `PickNextPrefabIndex()`.
   - Assert $0$ instances where `index[k] == index[k-1]`.
6. **T6: Spawner Segment Feeding Test**:
   - When segment with 3 spawn points spawns, verify `_activeSegmentSpawnPoints.Count == 3`.
   - When segment recycles, verify `_activeSegmentSpawnPoints.Count == 0`.

### 5.3 Invalidation Conditions
- Any runtime invocation of `GameObject.Destroy()` during segment recycling.
- Any seam gap $> 0.001\text{u}$ between consecutive segments.
- Any segment where corridor clearance is $< 4.0\text{u}$.
- Any consecutive repetition of identical segment prefabs.
- Any failure in the 541 baseline automated tests.
