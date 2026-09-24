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
    public float aheadTriggerDistance = 35.0f;

    [Tooltip("Distance behind camera below which off-screen segments are recycled (camY - 25.0f).")]
    public float cleanupDistance = 25.0f;

    [Header("Boss Arena Settings")]
    [Tooltip("Dedicated boss arena prefab (24u x 18u).")]
    public GameObject bossArenaPrefab;

    [Tooltip("Vertical length of the boss arena.")]
    public float bossArenaLength = 24.0f;

    [Tooltip("How far ahead of the camera the boss arena entrance is placed when triggered. Short lead keeps the boss encounter prompt (camera arrives in ~5s) instead of after a long trek during which score drifts far past the threshold.")]
    public float bossArenaLeadDistance = 12.0f;

    [Header("Runtime Status")]
    [SerializeField] private float _nextSpawnY = 0f;
    [SerializeField] private int _lastPrefabIndex = -1;
    [SerializeField] private int _totalSegmentsSpawned = 0;
    [SerializeField] private int _totalSegmentsRecycled = 0;
    [SerializeField] private bool _isBossArenaQueued = false;
    [SerializeField] private bool _isBossArenaSpawned = false;
    [SerializeField] private float _bossArenaCenterY = 0f;
    [SerializeField] private MapSegment _currentBossArenaSegment;
    [SerializeField] private bool _isBossEncounterActive = false;

    private readonly List<MapSegment> _activeSegments = new List<MapSegment>();

    // Public Properties
    public float NextSpawnY
    {
        get => _nextSpawnY;
        set => _nextSpawnY = value;
    }

    public int LastPrefabIndex => _lastPrefabIndex;
    public int TotalSegmentsSpawned => _totalSegmentsSpawned;
    public int TotalSegmentsRecycled => _totalSegmentsRecycled;
    public int ActiveSegmentCount => _activeSegments.Count;
    public IReadOnlyList<MapSegment> ActiveSegments => _activeSegments;
    public bool IsBossArenaQueued => _isBossArenaQueued;
    public bool IsBossArenaSpawned => _isBossArenaSpawned;
    public float BossArenaCenterY => _bossArenaCenterY;
    public MapSegment CurrentBossArenaSegment => _currentBossArenaSegment;
    public bool IsBossEncounterActive => _isBossEncounterActive;

    // Events
    public event Action<MapSegment> OnSegmentSpawned;
    public event Action<MapSegment> OnSegmentRecycled;
    public event Action OnBossArenaSpawnedEvent;
    public event Action OnBossArenaEnteredEvent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(this);
            else
                Destroy(this);
#else
            Destroy(this);
#endif
            return;
        }

        ResolveDependencies();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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

#if UNITY_EDITOR
        if (bossArenaPrefab == null)
        {
            bossArenaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapSegments/MapSegment_BossArena.prefab");
        }
#endif
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
        _isBossEncounterActive = false;
        _currentBossArenaSegment = null;

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

        // 1. Trailing cleanup
        CheckCleanupTrailing(currentCamY);

        // 2. Spawning ahead
        CheckSpawnAhead(currentCamY);

        // 3. Boss arena encounter check
        CheckBossArenaEncounter(currentCamY);
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
            var spawned = SpawnNextSegment();
            if (spawned == null) break;
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
    /// Pulls the arena close to the camera (bossArenaLeadDistance): unseen
    /// ahead-segments beyond the lead are recycled so the encounter starts
    /// promptly after the score threshold instead of after a long camera trek.
    /// </summary>
    public void QueueBossArena()
    {
        _isBossArenaQueued = true;

        float leadY = GetCameraY() + Mathf.Max(8f, bossArenaLeadDistance);
        if (_nextSpawnY > leadY)
        {
            // Drop unseen segments overlapping the pulled-in arena slot.
            // Everything recycled here is above the viewport top (player is
            // clamped near/below the camera), so the cut is invisible.
            for (int i = _activeSegments.Count - 1; i >= 0; i--)
            {
                MapSegment seg = _activeSegments[i];
                if (seg != null && seg.TopY > leadY + 0.01f)
                {
                    RecycleSegmentAt(i);
                }
            }
            _nextSpawnY = leadY;
        }
    }

    private void SpawnBossArenaInternal()
    {
        _isBossArenaSpawned = true;
        _isBossArenaQueued = false;
        _isBossEncounterActive = false;

        float arenaStartY = _nextSpawnY;
        _bossArenaCenterY = arenaStartY + (bossArenaLength * 0.5f);

        if (bossArenaPrefab == null)
        {
#if UNITY_EDITOR
            bossArenaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapSegments/MapSegment_BossArena.prefab");
#endif
        }

        GameObject arenaObj = null;
        if (bossArenaPrefab != null)
        {
            arenaObj = Instantiate(bossArenaPrefab, new Vector3(0f, arenaStartY, 0f), Quaternion.identity);
            arenaObj.SetActive(true);
            var seg = arenaObj.GetComponent<MapSegment>();
            if (seg != null)
            {
                _currentBossArenaSegment = seg;
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
    /// Checks whether the camera has reached the entry of the active boss arena.
    /// </summary>
    public void CheckBossArenaEncounter(float camY)
    {
        if (_isBossArenaSpawned && !_isBossEncounterActive)
        {
            if (camY >= _bossArenaCenterY - 12.0f)
            {
                TriggerBossArenaEntry();
            }
        }
    }

    /// <summary>
    /// Triggers boss encounter: locks camera at arena center and spawns boss inside arena.
    /// </summary>
    public void TriggerBossArenaEntry()
    {
        if (!_isBossArenaSpawned || _isBossEncounterActive) return;
        _isBossEncounterActive = true;

        if (cameraController != null)
        {
            cameraController.LockAt(_bossArenaCenterY);
        }

        SpawnBossInsideArena();
        OnBossArenaEnteredEvent?.Invoke();
    }

    /// <summary>
    /// Spawns the boss inside the active boss arena segment at bossSpawnPoint.
    /// </summary>
    public BossController SpawnBossInsideArena()
    {
        GameObject prefab = (enemySpawner != null && enemySpawner.bossPrefab != null)
            ? enemySpawner.bossPrefab
            : null;

#if UNITY_EDITOR
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossEnemy.prefab");
        }
#endif

        if (prefab == null) return null;

        Vector3 spawnPos = (_currentBossArenaSegment != null && _currentBossArenaSegment.bossSpawnPoint != null)
            ? _currentBossArenaSegment.bossSpawnPoint.position
            : new Vector3(0f, _bossArenaCenterY + 6.0f, 0f);

        GameObject bossObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        var boss = bossObj.GetComponent<BossController>();

        if (enemySpawner != null)
        {
            enemySpawner.bossSpawned = true;
            enemySpawner.isBossActive = true;
        }

        return boss;
    }

    /// <summary>
    /// Opens the top boundary wall of the active boss arena.
    /// </summary>
    public void OpenBossArenaTopWall()
    {
        if (_currentBossArenaSegment != null)
        {
            _currentBossArenaSegment.OpenTopWall();
        }
        else
        {
            for (int i = 0; i < _activeSegments.Count; i++)
            {
                if (_activeSegments[i] != null && _activeSegments[i].isBossArena)
                {
                    _activeSegments[i].OpenTopWall();
                }
            }
        }
    }

    /// <summary>
    /// Resumes normal procedural segment generation after boss defeat.
    /// Unlocks camera, opens boss arena top wall, and clears boss arena state.
    /// </summary>
    public void ResumeStandardSpawning()
    {
        _isBossArenaSpawned = false;
        _isBossArenaQueued = false;
        _isBossEncounterActive = false;
        _currentBossArenaSegment = null;

        OpenBossArenaTopWall();

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
