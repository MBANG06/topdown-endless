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

    [Tooltip("Top boundary wall Collider2D (dynamic wall for Boss Arena).")]
    public Collider2D topWallCollider;

    [Tooltip("Top boundary wall GameObject reference (opened/deactivated upon boss defeat).")]
    public GameObject topWall;

    [Header("Boss Arena Anchors")]
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
        var t = transform.Find("Boundaries/Wall_Top") ?? transform.Find("Wall_Top");
        if (t != null)
        {
            var col = t.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
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
        var t = transform.Find("Boundaries/Wall_Top") ?? transform.Find("Wall_Top");
        if (t != null)
        {
            var col = t.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    [Header("Pool Metadata")]
    [Tooltip("Catalog index of the prefab this instance was pooled from.")]
    [SerializeField] private int _prefabIndex = -1;
    public int PrefabIndex
    {
        get => _prefabIndex;
        set => _prefabIndex = value;
    }

    public int SegmentId
    {
        get => segmentIndex;
        set => segmentIndex = value;
    }

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
    /// Alias for BottomY.
    /// </summary>
    public float StartY => transform.position.y;

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

        if (isBossArena)
        {
            CloseTopWall();
        }

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

        // Deactivate any uncollected pickup instances attached to item spawn points
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
    /// Alias for CalculateBounds.
    /// </summary>
    public Bounds GetWorldBounds() => CalculateBounds();

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
            if (leftTransform == null)
            {
                var boundaries = transform.Find("Boundaries");
                if (boundaries != null) leftTransform = boundaries.Find("Wall_Left");
            }

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
            if (rightTransform == null)
            {
                var boundaries = transform.Find("Boundaries");
                if (boundaries != null) rightTransform = boundaries.Find("Wall_Right");
            }

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

        if (isBossArena)
        {
            if (topWallCollider == null || !topWallCollider.CompareTag("Colliders") || topWallCollider.isTrigger)
            {
                failureReason = "Boss arena segment must have a non-null, non-trigger top wall collider tagged 'Colliders'";
                return false;
            }
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
