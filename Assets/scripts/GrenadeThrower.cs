using System;
using UnityEngine;

/// <summary>
/// Player component responsible for managing grenade inventory and throwing mechanics.
/// Listens for KeyCode.E and Right Mouse Button (Fire2), computes clamped trajectory targets,
/// and instantiates GrenadeProjectile directed toward target world coordinates.
/// </summary>
public class GrenadeThrower : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("Current number of grenades available in inventory.")]
    public int grenadeCount = 2;

    [Tooltip("Maximum capacity for grenade inventory.")]
    public int maxGrenades = 5;

    [Header("Throw Parameters")]
    [Tooltip("Prefab instantiated when a grenade is thrown.")]
    public GameObject grenadePrefab;

    [Tooltip("Maximum throw distance in world units from player position.")]
    public float maxThrowDistance = 7.0f;

    [Tooltip("Cooldown between consecutive grenade throws in seconds.")]
    public float throwCooldown = 0.3f;

    [Header("Arena Boundaries")]
    public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
    public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
    public bool useDynamicBounds = true;

    public int GrenadeCount => grenadeCount;
    public int MaxGrenades => maxGrenades;

    // Events
    public event Action<int> OnGrenadeCountChanged;
    public event Action<int> OnCountChanged; // Compatibility alias

    private PlayerHealth _playerHealth;
    private Camera _mainCamera;
    private float _cooldownTimer = 0f;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
        if (_playerHealth == null)
        {
            _playerHealth = GetComponentInParent<PlayerHealth>();
        }

        _mainCamera = Camera.main;
    }

    private void Start()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        // Notify initial inventory count
        OnGrenadeCountChanged?.Invoke(grenadeCount);
        OnCountChanged?.Invoke(grenadeCount);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        if (_playerHealth == null)
        {
            _playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        }
        if (_playerHealth != null && !_playerHealth.IsAlive) return;

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        // Check throw input: Key E or RMB (Fire2 / MouseButton 1)
        bool inputThrow = Input.GetKeyDown(KeyCode.E) ||
                          Input.GetButtonDown("Fire2") ||
                          Input.GetMouseButtonDown(1);

        if (inputThrow && _cooldownTimer <= 0f)
        {
            if (grenadeCount > 0)
            {
                Vector2 targetPos = GetMouseWorldPosition();
                ThrowGrenade(targetPos);
                _cooldownTimer = throwCooldown;
            }
        }
    }

    /// <summary>
    /// Throws a grenade directed towards the specified world coordinate.
    /// Clamps maximum travel distance to maxThrowDistance (7.0u) and within arena bounds.
    /// Decrements inventory and invokes inventory events.
    /// </summary>
    public void ThrowGrenade(Vector2 targetPos)
    {
        // Guard against paused game or dead player
        if (Time.timeScale <= 0f) return;

        if (_playerHealth == null)
        {
            _playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        }
        if (_playerHealth != null && !_playerHealth.IsAlive) return;
        if (grenadeCount <= 0) return;

        // Decrement inventory
        grenadeCount--;
        OnGrenadeCountChanged?.Invoke(grenadeCount);
        OnCountChanged?.Invoke(grenadeCount);

        Vector2 playerPos = transform.position;
        Vector2 throwDirection = targetPos - playerPos;
        Vector2 clampedOffset = Vector2.ClampMagnitude(throwDirection, maxThrowDistance);
        Vector2 finalTarget = playerPos + clampedOffset;

        // Ensure target is clamped within arena bounds (adapts to scrolling camera if present)
        var scrollCam = ScrollingCameraController.Instance;
        if (scrollCam == null && Application.isPlaying)
        {
            scrollCam = FindObjectOfType<ScrollingCameraController>();
        }
        if (useDynamicBounds && scrollCam != null)
        {
            finalTarget.x = Mathf.Clamp(finalTarget.x, arenaMin.x, arenaMax.x);
            float camY = scrollCam.transform.position.y;
            finalTarget.y = Mathf.Max(camY - 12f, finalTarget.y);
        }
        else
        {
            finalTarget.x = Mathf.Clamp(finalTarget.x, arenaMin.x, arenaMax.x);
            finalTarget.y = Mathf.Clamp(finalTarget.y, arenaMin.y, arenaMax.y);
        }

        // Instantiate grenade projectile
        if (grenadePrefab != null)
        {
            GameObject projObj = Instantiate(grenadePrefab, playerPos, Quaternion.identity);
            var proj = projObj.GetComponent<GrenadeProjectile>();
            if (proj != null)
            {
                proj.Initialize(playerPos, finalTarget);
            }
        }
        else
        {
            // Fallback: spawn runtime projectile if prefab wasn't assigned in inspector
            var fallback = new GameObject("GrenadeProjectile_Fallback");
            fallback.transform.position = playerPos;
            var proj = fallback.AddComponent<GrenadeProjectile>();
            proj.Initialize(playerPos, finalTarget);
        }
    }

    /// <summary>
    /// Adds grenades to player inventory up to maxGrenades capacity.
    /// Returns true if at least one grenade was successfully added.
    /// </summary>
    public bool AddGrenades(int count = 1)
    {
        if (count <= 0) return false;
        if (grenadeCount >= maxGrenades) return false;

        int newCount = Mathf.Min(maxGrenades, grenadeCount + count);
        if (newCount == grenadeCount) return false;

        grenadeCount = newCount;
        OnGrenadeCountChanged?.Invoke(grenadeCount);
        OnCountChanged?.Invoke(grenadeCount);
        return true;
    }

    /// <summary>
    /// Resets the grenade count to the specified amount (default 2).
    /// </summary>
    public void ResetGrenades(int count = 2)
    {
        grenadeCount = Mathf.Clamp(count, 0, maxGrenades);
        OnGrenadeCountChanged?.Invoke(grenadeCount);
        OnCountChanged?.Invoke(grenadeCount);
    }

    /// <summary>
    /// Computes current mouse cursor position in 2D world space.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(mouseScreen);
            return new Vector2(worldPos.x, worldPos.y);
        }

        return (Vector2)transform.position;
    }
}
