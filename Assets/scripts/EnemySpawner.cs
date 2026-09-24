using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Controls progressive wave spawning of Chaser, Shooter, and Rusher enemies.
/// Calculates off-screen perimeter spawn coordinates outside the arena,
/// implements progressive difficulty scaling curves for spawn interval and concurrency caps,
/// and handles Boss encounter triggering and suppression.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Perimeter Spawn Bounds")]
    public float minX = -10.5f;
    public float maxX = 16.0f;
    public float minY = -6.0f;
    public float maxY = 7.0f;
    public float minPlayerDistance = 6.0f;

    [Header("Scaling Curve Parameters")]
    public float baseSpawnInterval = 3.0f;
    public float minSpawnInterval = 0.6f;
    public float timeIntervalFactor = 0.015f;
    public float scoreIntervalFactor = 0.002f;

    public int initialConcurrencyCap = 5;
    public int maxConcurrencyCeiling = 25;

    [Header("Enemy Prefab Catalog")]
    public GameObject chaserPrefab;
    public GameObject shooterPrefab;
    public GameObject rusherPrefab;
    public GameObject bossPrefab;

    [Header("Boss Encounter State")]
    public bool bossSpawned = false;
    public bool isBossActive = false;
    public Vector3 bossSpawnPosition = new Vector3(2.69f, 3.5f, 0.0f);

    [Header("Spawner Runtime Status")]
    public bool isSpawning = true;
    public float survivalTime = 0f;

    [Header("Dynamic Segment Spawning (Milestone 2)")]
    [Tooltip("If true, enemies spawn at active segment spawn points instead of static perimeter.")]
    public bool useSegmentSpawnPoints = true;

    [Tooltip("Whether to automatically populate each newly spawned segment with enemies upon activation.")]
    public bool autoPopulateSegments = true;

    [Tooltip("Maximum enemies to spawn when a segment is activated.")]
    public int enemiesPerSegment = 2;

    private readonly List<Transform> _activeSegmentSpawnPoints = new List<Transform>();

    public int ActiveSegmentSpawnPointCount => _activeSegmentSpawnPoints.Count;
    public IReadOnlyList<Transform> ActiveSegmentSpawnPoints => _activeSegmentSpawnPoints;

    private float _spawnTimer = 0f;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>();

    public int ActiveEnemyCount
    {
        get
        {
            CleanDeadEnemies();
            return _activeEnemies.Count;
        }
    }

    private void Awake()
    {
        LocatePlayer();
    }

    private void Start()
    {
        LocatePlayer();
    }

    private void OnEnable()
    {
        LocatePlayer();
        if (_playerHealth != null)
        {
            _playerHealth.OnPlayerDeath += OnPlayerDied;
        }
        EnemyBase.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnPlayerDeath -= OnPlayerDied;
        }
        EnemyBase.OnEnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        CleanDeadEnemies();

        if (!isSpawning) return;

        // Freeze spawning (and difficulty clock) outside active gameplay, e.g. on
        // Main Menu boot. EditMode tests bypass to stay hermetic.
        if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        if (_playerTransform == null)
        {
            LocatePlayer();
            if (_playerTransform == null) return;
        }

        if (_playerHealth != null && !_playerHealth.IsAlive)
        {
            StopSpawning();
            return;
        }

        survivalTime += Time.deltaTime;
        int currentScore = GetCurrentScore();

        // Check boss spawn trigger at 500 points (legacy fallback when MapManager is absent)
        if (currentScore >= 500 && !bossSpawned)
        {
            if (MapManager.Instance == null)
            {
                SpawnBoss();
            }
        }

        int maxAllowed = CalculateMaxConcurrentEnemies(survivalTime, currentScore);
        if (_activeEnemies.Count >= maxAllowed)
        {
            return; // Concurrency cap reached; throttle spawning
        }

        float currentInterval = CalculateSpawnInterval(survivalTime, currentScore);
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= currentInterval)
        {
            _spawnTimer = 0f;
            SpawnEnemy(currentScore);
        }
    }

    /// <summary>
    /// Starts or resumes enemy spawning.
    /// </summary>
    public void StartSpawning()
    {
        isSpawning = true;
    }

    /// <summary>
    /// Halts enemy spawning.
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    /// <summary>
    /// Triggers single-instance boss spawn and activates 50% spawner suppression.
    /// </summary>
    public void SpawnBoss()
    {
        if (bossSpawned) return;
        bossSpawned = true;
        isBossActive = true;

        if (bossPrefab != null)
        {
            Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
        }
    }

    /// <summary>
    /// Resumes normal endless spawning rates after Boss defeat.
    /// </summary>
    public void OnBossDefeated()
    {
        isBossActive = false;
        StartSpawning();
    }

    /// <summary>
    /// Calculates dynamic spawn interval according to I(t, S) formula.
    /// </summary>
    public float CalculateSpawnInterval(float t, int s)
    {
        if (t < 0f || s < 0) return baseSpawnInterval;

        float raw = baseSpawnInterval - (t * timeIntervalFactor) - (s * scoreIntervalFactor);
        float interval = Mathf.Max(minSpawnInterval, raw);

        // 50% spawn rate suppression during Boss encounter
        if (isBossActive)
        {
            interval *= 2.0f;
        }

        return interval;
    }

    /// <summary>
    /// Calculates dynamic maximum concurrent enemy ceiling N(t, S).
    /// </summary>
    public int CalculateMaxConcurrentEnemies(float t, int s)
    {
        if (t < 0f || s < 0) return initialConcurrencyCap;

        int raw = initialConcurrencyCap + Mathf.FloorToInt(t / 20f) + Mathf.FloorToInt(s / 60f);
        return Mathf.Min(maxConcurrencyCeiling, raw);
    }

    /// <summary>
    /// Selects an enemy archetype prefab based on current score tiers.
    /// </summary>
    public GameObject SelectEnemyArchetype(int score)
    {
        float roll = UnityEngine.Random.value;

        if (score < 100)
        {
            // Tier 1: 75% Chaser, 15% Rusher, 10% Shooter
            if (roll < 0.75f) return chaserPrefab ?? rusherPrefab ?? shooterPrefab;
            if (roll < 0.90f) return rusherPrefab ?? chaserPrefab ?? shooterPrefab;
            return shooterPrefab ?? chaserPrefab ?? rusherPrefab;
        }
        else if (score < 300)
        {
            // Tier 2: 50% Chaser, 25% Rusher, 25% Shooter
            if (roll < 0.50f) return chaserPrefab ?? rusherPrefab ?? shooterPrefab;
            if (roll < 0.75f) return rusherPrefab ?? chaserPrefab ?? shooterPrefab;
            return shooterPrefab ?? rusherPrefab ?? chaserPrefab;
        }
        else
        {
            // Tier 3: 35% Chaser, 30% Rusher, 35% Shooter
            if (roll < 0.35f) return chaserPrefab ?? rusherPrefab ?? shooterPrefab;
            if (roll < 0.65f) return rusherPrefab ?? chaserPrefab ?? shooterPrefab;
            return shooterPrefab ?? chaserPrefab ?? rusherPrefab;
        }
    }

    /// <summary>
    /// Generates perimeter spawn point coordinates strictly on the outer perimeter box,
    /// rejecting any candidates within minPlayerDistance (6.0u) from player.
    /// </summary>
    public Vector2 GeneratePerimeterPosition(Vector2 playerPos)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int edge = UnityEngine.Random.Range(0, 4);
            Vector2 candidate = Vector2.zero;

            switch (edge)
            {
                case 0: // Top edge
                    candidate = new Vector2(UnityEngine.Random.Range(minX, maxX), maxY);
                    break;
                case 1: // Bottom edge
                    candidate = new Vector2(UnityEngine.Random.Range(minX, maxX), minY);
                    break;
                case 2: // Left edge
                    candidate = new Vector2(minX, UnityEngine.Random.Range(minY, maxY));
                    break;
                case 3: // Right edge
                    candidate = new Vector2(maxX, UnityEngine.Random.Range(minY, maxY));
                    break;
            }

            if (Vector2.Distance(candidate, playerPos) >= minPlayerDistance)
            {
                return candidate;
            }
        }

        // Fallback: spawn on opposite edge from player
        float oppositeX = playerPos.x >= 0 ? minX : maxX;
        float oppositeY = playerPos.y >= 0 ? minY : maxY;
        return new Vector2(oppositeX, oppositeY);
    }

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

        segment.hasSpawnedEnemies = true;
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

    /// <summary>
    /// Spawns a single enemy archetype instance at the perimeter edge or segment spawn point.
    /// </summary>
    public GameObject SpawnEnemy(int score)
    {
        GameObject prefab = SelectEnemyArchetype(score);
        if (prefab == null) return null; // Safe empty pool handling

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

    private void CleanDeadEnemies()
    {
        _activeEnemies.RemoveAll(e => e == null || !e.IsAlive);
    }

    private void HandleEnemyDied(EnemyBase enemy)
    {
        if (enemy != null)
        {
            _activeEnemies.Remove(enemy);
        }
    }

    private void OnPlayerDied()
    {
        StopSpawning();
    }

    private void LocatePlayer()
    {
        if (_playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null) playerObj = ph.gameObject;
        }

        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    private int GetCurrentScore()
    {
        try
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var gmType = assembly.GetType("GameManager");
                if (gmType != null)
                {
                    var instProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var inst = instProp?.GetValue(null);
                    if (inst != null)
                    {
                        var scoreProp = gmType.GetProperty("CurrentScore", BindingFlags.Public | BindingFlags.Instance);
                        if (scoreProp != null)
                        {
                            return Convert.ToInt32(scoreProp.GetValue(inst));
                        }
                    }
                    break;
                }
            }
        }
        catch
        {
            // Fallback to 0 if GameManager is not active
        }
        return 0;
    }
}
