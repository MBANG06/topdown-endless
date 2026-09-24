using System;
using UnityEngine;

/// <summary>
/// Camera update execution timing options.
/// </summary>
public enum CameraUpdateMode
{
    FixedUpdate,
    LateUpdate
}

/// <summary>
/// Controls continuous upward (+Y) auto-scrolling for the Main Camera in the 2D top-down shooter.
/// Scales scrolling speed with distance travelled, synchronizes with physics to prevent jitter,
/// and provides lock/unlock capabilities for boss arena encounters.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class ScrollingCameraController : MonoBehaviour
{
    public static ScrollingCameraController Instance { get; set; }

    [Header("Speed Settings")]
    [Tooltip("Baseline camera scrolling speed along +Y in world units per second.")]
    public float baselineSpeed = 2.0f;

    [Tooltip("Maximum camera scrolling speed ceiling.")]
    public float maxSpeed = 3.5f;

    [Tooltip("Scale factor determining speed acceleration per 100 units travelled.")]
    public float speedScaleFactor = 0.5f;

    [Header("Execution Timing")]
    [Tooltip("Update loop for camera translation. FixedUpdate eliminates physics jitter with Rigidbody2D.")]
    public CameraUpdateMode updateMode = CameraUpdateMode.FixedUpdate;

    [Header("Aspect Lock")]
    [Tooltip("Lock the view to 16:9 via letterbox/pillarbox so segment walls frame consistently on any screen.")]
    public bool lockAspect16x9 = true;
    [Tooltip("Target aspect ratio (width / height).")]
    public float targetAspect = 16f / 9f;
    private int _lastScreenW = 0;
    private int _lastScreenH = 0;

    [Header("Runtime State")]
    [SerializeField] private bool _isScrollLocked = false;
    [SerializeField] private float _distanceTravelled = 0f;
    [SerializeField] private float _initialY = 0f;
    [SerializeField] private float? _targetLockY = null;
    [SerializeField] private bool _isAlignedToLock = false;

    /// <summary>
    /// Whether camera scrolling is currently locked in place.
    /// </summary>
    public bool isScrollLocked
    {
        get => _isScrollLocked;
        set => _isScrollLocked = value;
    }

    /// <summary>
    /// PascalCase alias for isScrollLocked.
    /// </summary>
    public bool IsScrollLocked
    {
        get => _isScrollLocked;
        set => _isScrollLocked = value;
    }

    /// <summary>
    /// Total distance travelled along the +Y axis from initial starting position.
    /// </summary>
    public float DistanceTravelled => _distanceTravelled;

    /// <summary>
    /// lowercase alias for DistanceTravelled.
    /// </summary>
    public float distanceTravelled => DistanceTravelled;

    /// <summary>
    /// Initial starting world Y position.
    /// </summary>
    public float InitialY => _initialY;

    /// <summary>
    /// The target Y position when locking to an arena, if specified.
    /// </summary>
    public float? TargetLockY => _targetLockY;

    /// <summary>
    /// True when the camera has arrived and aligned at the target lock coordinate.
    /// </summary>
    public bool IsAlignedToLock => _isAlignedToLock;

    /// <summary>
    /// Calculated cruising speed based on progression formula:
    /// currentSpeed = Mathf.Min(maxSpeed, baselineSpeed + (distanceTravelled / 100f) * speedScaleFactor)
    /// </summary>
    public float CruisingSpeed => Mathf.Min(maxSpeed, baselineSpeed + (_distanceTravelled / 100f) * speedScaleFactor);

    /// <summary>
    /// Current scrolling speed. Returns 0f when scroll is locked, otherwise CruisingSpeed.
    /// </summary>
    public float CurrentSpeed => _isScrollLocked ? 0f : CruisingSpeed;

    /// <summary>
    /// lowercase alias for CurrentSpeed.
    /// </summary>
    public float currentSpeed => CurrentSpeed;

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

        _initialY = transform.position.y;
        EnforceAspect();
    }

    /// <summary>
    /// Locks the camera to the target aspect ratio with letterbox/pillarbox bars.
    /// Cheap no-op until the screen resolution changes. Zero GC.
    /// </summary>
    public void EnforceAspect()
    {
        if (!lockAspect16x9) return;
        var cam = GetComponent<Camera>();
        if (cam == null) return;
        int w = Screen.width;
        int h = Screen.height;
        if (w == _lastScreenW && h == _lastScreenH) return;
        _lastScreenW = w;
        _lastScreenH = h;
        if (w <= 0 || h <= 0 || targetAspect <= 0f) return;
        float window = (float)w / (float)h;
        if (Mathf.Approximately(window, targetAspect))
        {
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }
        if (window > targetAspect)
        {
            float rw = targetAspect / window;
            cam.rect = new Rect((1f - rw) * 0.5f, 0f, rw, 1f);
        }
        else
        {
            float rh = window / targetAspect;
            cam.rect = new Rect(0f, (1f - rh) * 0.5f, 1f, rh);
        }
    }

    private void Start()
    {
        if (_distanceTravelled == 0f)
        {
            _initialY = transform.position.y;
        }

        if (Application.isPlaying)
        {
            OpenStartingArenaTopWall();
        }
    }

    /// <summary>
    /// Disables BoxCollider2D on MapBounds/Wall_Top at runtime in PlayMode so entities can scroll upward freely,
    /// while preserving Wall_Top GameObject and component in EditMode tests.
    /// </summary>
    public void OpenStartingArenaTopWall()
    {
        var mapBounds = GameObject.Find("MapBounds");
        if (mapBounds != null)
        {
            var topWall = mapBounds.transform.Find("Wall_Top");
            if (topWall != null)
            {
                var col = topWall.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void FixedUpdate()
    {
        if (updateMode == CameraUpdateMode.FixedUpdate)
        {
            StepScroll(Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        if (updateMode == CameraUpdateMode.LateUpdate)
        {
            StepScroll(Time.deltaTime);
        }
    }

    /// <summary>
    /// Advances camera translation along +Y by deltaTime.
    /// Can be invoked directly by unit and integration tests without running play mode.
    /// </summary>
    /// <param name="dt">Time step in seconds.</param>
    public void StepScroll(float dt)
    {
        EnforceAspect();
        if (dt <= 0f) return;

        // Pause guard: freeze scrolling if game is paused or game over
        if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        if (_isScrollLocked)
        {
            // If locked with a target lock position and not yet aligned, smoothly advance to it
            if (_targetLockY.HasValue && !_isAlignedToLock)
            {
                float targetY = _targetLockY.Value;
                float step = CruisingSpeed * dt;
                float newY = Mathf.MoveTowards(transform.position.y, targetY, step);
                SetCameraY(newY);

                if (Mathf.Approximately(transform.position.y, targetY))
                {
                    _isAlignedToLock = true;
                }
            }
            return;
        }

        // Standard auto-scrolling
        float speed = CurrentSpeed;
        float deltaY = speed * dt;
        float nextY = transform.position.y + deltaY;

        // If a target lock point is set ahead, align and lock upon arrival
        if (_targetLockY.HasValue && nextY >= _targetLockY.Value)
        {
            nextY = _targetLockY.Value;
            _isScrollLocked = true;
            _isAlignedToLock = true;
        }

        SetCameraY(nextY);
    }

    /// <summary>
    /// Alias for StepScroll to support common test naming conventions.
    /// </summary>
    public void UpdateScroll(float dt) => StepScroll(dt);

    /// <summary>
    /// Directly sets the camera's Y position while preserving X and Z coordinates,
    /// and updates DistanceTravelled.
    /// </summary>
    /// <param name="newY">World Y coordinate.</param>
    public void SetCameraY(float newY)
    {
        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;
        _distanceTravelled = Mathf.Max(0f, transform.position.y - _initialY);
    }

    /// <summary>
    /// Locks camera scrolling at the specified world Y coordinate.
    /// If snapImmediate is true, the camera teleports immediately to worldY.
    /// Otherwise, the camera smoothly completes travel to worldY and stops.
    /// </summary>
    /// <param name="worldY">Target world Y coordinate (e.g. boss arena center).</param>
    /// <param name="snapImmediate">If true, snaps camera position immediately.</param>
    public void LockAt(float worldY, bool snapImmediate = false)
    {
        _targetLockY = worldY;
        _isScrollLocked = true;

        if (snapImmediate || Mathf.Approximately(transform.position.y, worldY) || transform.position.y >= worldY)
        {
            SetCameraY(worldY);
            _isAlignedToLock = true;
        }
        else
        {
            _isAlignedToLock = false;
        }
    }

    /// <summary>
    /// Unlocks camera scrolling and resumes endless upward movement.
    /// </summary>
    public void UnlockAndResume()
    {
        _isScrollLocked = false;
        _isAlignedToLock = false;
        _targetLockY = null;
    }

    /// <summary>
    /// Sets initial reference Y for distance calculation. Useful for tests or scene transitions.
    /// </summary>
    public void SetInitialY(float y)
    {
        _initialY = y;
        _distanceTravelled = Mathf.Max(0f, transform.position.y - _initialY);
    }

    /// <summary>
    /// Directly overrides DistanceTravelled (primarily for testing speed progression).
    /// </summary>
    public void SetDistanceTravelled(float distance)
    {
        _distanceTravelled = Mathf.Max(0f, distance);
    }

    /// <summary>
    /// Resets camera position, distance, and lock state to baseline for game restart.
    /// </summary>
    public void ResetProgress()
    {
        _isScrollLocked = false;
        _targetLockY = null;
        _isAlignedToLock = false;
        _distanceTravelled = 0f;
        SetCameraY(_initialY);
    }
}
