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

        if (_pools == null || prefabIndex < 0 || prefabIndex >= _pools.Length)
        {
            Debug.LogError($"[MapSegmentPool] Invalid prefab index {prefabIndex}. Catalog size: {(_pools != null ? _pools.Length : 0)}");
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

        if (_pools == null || _pools.Length == 0) return;

        int index = segment.PrefabIndex;
        if (index >= 0 && index < _pools.Length)
        {
            _pools[index].Enqueue(segment);
        }
        else
        {
            // Fallback: search which queue matches
            bool enqueued = false;
            if (segmentPrefabs != null)
            {
                for (int i = 0; i < segmentPrefabs.Length; i++)
                {
                    if (segmentPrefabs[i] != null && segment.name.StartsWith(segmentPrefabs[i].name))
                    {
                        segment.PrefabIndex = i;
                        _pools[i].Enqueue(segment);
                        enqueued = true;
                        break;
                    }
                }
            }

            if (!enqueued && _pools.Length > 0)
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
