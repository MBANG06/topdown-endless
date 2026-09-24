using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Endless floor tiler following the +Y scrolling camera.
/// Clones the source Tilemap into a pooled ring of chunks and recycles
/// them by repositioning (zero Instantiate/Destroy at runtime, zero GC).
/// Attach to EndlessMapManager. Mirrors MapManager coverage style:
/// behindCoverage (default 20u) + aheadCoverage (default 40u).
/// </summary>
[DisallowMultipleComponent]
public class EndlessFloor : MonoBehaviour
{
    public static EndlessFloor Instance { get; private set; }

    [Header("Source")]
    [Tooltip("Static source Tilemap to clone (floor Tilemap 24x10).")]
    public Tilemap sourceTilemap;

    [Header("Dependencies")]
    public ScrollingCameraController cameraController;
    public Camera cam;

    [Header("Coverage")]
    [Tooltip("Units kept floored behind the camera.")]
    public float behindCoverage = 20f;
    [Tooltip("Units kept floored ahead of the camera.")]
    public float aheadCoverage = 40f;
    [Tooltip("Number of pooled chunks (chunkHeight * poolSize must exceed behind+ahead+camera height + one chunk for grid-alignment slack).")]
    public int poolSize = 8;

    [Header("Runtime Status")]
    [SerializeField] private Tilemap[] _chunks = new Tilemap[0];
    [SerializeField] private float _chunkHeightUnits = 10f;
    [SerializeField] private float _chunkWidthUnits = 24f;
    [SerializeField] private float _floorWorldX = 2.69f;
    [SerializeField] private bool _initialized = false;

    // Chunks must live under the source Grid: a Tilemap outside a Grid does not render.
    private Transform _gridParent;

    public int ChunkCount => _chunks != null ? _chunks.Length : 0;
    public float ChunkHeight => _chunkHeightUnits;
    public bool IsInitialized => _initialized;

    public float CoverageBottom
    {
        get
        {
            if (_chunks == null || _chunks.Length == 0) return 0f;
            float min = float.MaxValue;
            for (int i = 0; i < _chunks.Length; i++)
                if (_chunks[i] != null) min = Mathf.Min(min, _chunks[i].transform.position.y);
            return min;
        }
    }

    public float CoverageTop
    {
        get
        {
            if (_chunks == null || _chunks.Length == 0) return 0f;
            float max = float.MinValue;
            for (int i = 0; i < _chunks.Length; i++)
                if (_chunks[i] != null) max = Mathf.Max(max, _chunks[i].transform.position.y + _chunkHeightUnits);
            return max;
        }
    }

    public bool IsCovering(float worldY) => worldY >= CoverageBottom && worldY <= CoverageTop;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this && Application.isPlaying) { Destroy(this); return; }
        ResolveDependencies();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ResolveDependencies();
        if (Application.isPlaying) InitializeFloor();
    }

    public void ResolveDependencies()
    {
        if (cam == null) cam = Camera.main;
        if (cameraController == null)
            cameraController = ScrollingCameraController.Instance ?? FindObjectOfType<ScrollingCameraController>();
        if (sourceTilemap == null)
        {
            var floorGrid = GameObject.Find("floor");
            if (floorGrid != null) sourceTilemap = floorGrid.GetComponentInChildren<Tilemap>();
        }
    }

    /// <summary>
    /// Clones source tiles once and lays chunks sequentially from the source bottom.
    /// </summary>
    public void InitializeFloor()
    {
        ResolveDependencies();
        if (sourceTilemap == null)
        {
            Debug.LogError("[EndlessFloor] sourceTilemap is not assigned and no floor Tilemap found.");
            return;
        }

        _chunkHeightUnits = Mathf.Max(1f, sourceTilemap.size.y * sourceTilemap.cellSize.y);
        _chunkWidthUnits = Mathf.Max(1f, sourceTilemap.size.x * sourceTilemap.cellSize.x);
        _floorWorldX = sourceTilemap.transform.position.x;

        // Chunks must be parented under the source Grid to render.
        _gridParent = sourceTilemap.transform.parent != null
            ? sourceTilemap.transform.parent
            : transform;

        // Cleanup previous chunks (editor re-init safety).
        for (int i = _gridParent.childCount - 1; i >= 0; i--)
        {
            var child = _gridParent.GetChild(i);
            if (child != null && child.name.StartsWith("FloorChunk_"))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        BoundsInt area = sourceTilemap.cellBounds;
        TileBase[] tiles = sourceTilemap.GetTilesBlock(area);

        // Endless chunks use 1 uniform pattern (the most common = base grass tile)
        // so vertical repetition is perfectly seamless: identical tiles cannot seam.
        // Chunk 0 keeps the full hand-designed artwork overlapping the source floor.
        TileBase baseTile = null;
        {
            var counts = new System.Collections.Generic.Dictionary<TileBase, int>();
            foreach (var t in tiles)
            {
                if (t == null) continue;
                counts.TryGetValue(t, out int n);
                counts[t] = n + 1;
                if (baseTile == null || counts[t] > counts[baseTile]) baseTile = t;
            }
        }
        TileBase[] grassBlock = new TileBase[tiles.Length];
        for (int k = 0; k < grassBlock.Length; k++) grassBlock[k] = baseTile;

        poolSize = Mathf.Max(2, poolSize);
        _chunks = new Tilemap[poolSize];

        float sourceBottomY = sourceTilemap.transform.position.y + sourceTilemap.origin.y * sourceTilemap.cellSize.y;
        // Chunk 0 aligns exactly over the source; chunks extend upward.
        // Single identical pattern for all chunks: the source artwork tiles seamlessly
        // in its original orientation, so cloning it verbatim keeps every seam invisible.
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"FloorChunk_{i}");
            go.transform.SetParent(_gridParent, false);
            // Use world-space position setter so grid offset is handled automatically.
            go.transform.position = new Vector3(_floorWorldX, sourceBottomY + i * _chunkHeightUnits, 0f);

            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sourceTilemap.GetComponent<TilemapRenderer>() != null
                ? sourceTilemap.GetComponent<TilemapRenderer>().sortingOrder : 0;

            tilemap.origin = sourceTilemap.origin;
            tilemap.size = sourceTilemap.size;
            tilemap.tileAnchor = sourceTilemap.tileAnchor;
            tilemap.orientation = sourceTilemap.orientation;
            tilemap.animationFrameRate = sourceTilemap.animationFrameRate;

            // Chunk 0 = verbatim design copy (invisible overlap over source floor).
            // Chunks 1+ = pure base grass for guaranteed-seamless endless ground.
            tilemap.SetTilesBlock(area, i == 0 ? tiles : grassBlock);
            tilemap.color = sourceTilemap.color;

            _chunks[i] = tilemap;
        }

        _initialized = true;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !_initialized) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        TileToCamera(GetCameraY());
    }

    /// <summary>
    /// Recycles chunks so [camY-behind, camY+ahead] stays floored. Zero allocation.
    /// </summary>
    public void TileToCamera(float camY)
    {
        if (_chunks == null || _chunks.Length == 0) return;
        float span = _chunks.Length * _chunkHeightUnits;
        for (int i = 0; i < _chunks.Length; i++)
        {
            var t = _chunks[i];
            if (t == null) continue;
            Vector3 p = t.transform.position;
            // Move chunk forward while it lies fully behind the coverage window.
            while (p.y + _chunkHeightUnits < camY - behindCoverage)
            {
                p.y += span;
            }
            // Move chunk back while it lies fully ahead of the coverage window.
            while (p.y > camY + aheadCoverage)
            {
                p.y -= span;
            }
            p.x = _floorWorldX;
            p.z = 0f;
            t.transform.position = p;
        }
    }

    public float GetCameraY()
    {
        if (cam != null) return cam.transform.position.y;
        if (cameraController != null) return cameraController.transform.position.y;
        return 0f;
    }
}
