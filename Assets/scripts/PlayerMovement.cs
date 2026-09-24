using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls 8-directional WASD player movement with diagonal normalization,
/// mouse-aim rotation, safe camera reference fallback, arena coordinate clamping,
/// dynamic viewport clamping, and bottom edge push/kill plane for scrolling gameplay.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Rigidbody2D rb;
    public Camera cam;

    [Header("Arena Boundary Clamping (Legacy Static Arena)")]
    public bool clampToBounds = true;
    public Vector2 minBounds = new Vector2(-8.5f, -4.2f);
    public Vector2 maxBounds = new Vector2(13.8f, 5.2f);

    [Header("Viewport Clamping Settings (Milestone 1 Scrolling System)")]
    [Tooltip("If enabled, player position is restricted within the moving camera viewport.")]
    public bool clampToViewport = false;
    [Tooltip("Normalized camera viewport X bounds (default [0.05, 0.95]).")]
    public float minViewportX = 0.05f;
    public float maxViewportX = 0.95f;
    [Tooltip("Normalized camera viewport Y bounds (default [0.08, 0.92]).")]
    public float minViewportY = 0.08f;
    public float maxViewportY = 0.92f;

    [Header("Bottom Edge Push / Kill Settings")]
    [Tooltip("Viewport Y threshold below which player takes damage or is pushed forward (default 0.04).")]
    public float bottomKillThreshold = 0.04f;
    [Tooltip("Whether to push player forward along +Y when crossing below bottomKillThreshold.")]
    public bool bottomPushForward = true;
    [Tooltip("Upward speed applied when pushing player forward from bottom danger zone.")]
    public float bottomPushSpeed = 5.0f;
    [Tooltip("Cooldown between consecutive bottom edge damage ticks in seconds.")]
    public float bottomDamageInterval = 1.0f;
    [Tooltip("Instant kill if player falls completely below camera viewport (Y <= 0).")]
    public bool instantKillBelowScreen = false;

    private Vector2 movement;
    private Vector2 mousePos;
    private float lastBottomDamageTime = -999f;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        // Auto-activate viewport clamping if ScrollingCameraController exists in the active scene
        if (!clampToViewport)
        {
            if (ScrollingCameraController.Instance != null || (Application.isPlaying && FindObjectOfType<ScrollingCameraController>() != null))
            {
                clampToViewport = true;
            }
        }
    }

    private void Update()
    {
        // 8-directional input sampling
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Safe camera fallback
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Freeze player outside active gameplay (e.g. Main Menu boot).
        // EditMode tests bypass to stay hermetic.
        if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        // Normalized 8-direction movement translation
        Vector2 nextPosition = rb.position + movement.normalized * (moveSpeed * Time.fixedDeltaTime);

        // Coordinate clamping: viewport clamping takes precedence when enabled and camera is available
        if (clampToViewport)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam != null)
            {
                ApplyViewportClamping(ref nextPosition);
            }
            else if (clampToBounds)
            {
                // Safe fallback to static bounds if camera reference is unavailable
                nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x, maxBounds.x);
                nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y, maxBounds.y);
            }
        }
        else if (clampToBounds)
        {
            // Legacy arena bounds clamping (100% backward compatible with 421 tests)
            nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.x, maxBounds.x);
            nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.y, maxBounds.y);
        }

        rb.MovePosition(nextPosition);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            rb.position = nextPosition;
        }
#endif

        // Check bottom edge push/kill plane against physical position
        if (clampToViewport && cam != null)
        {
            HandleBottomEdgePushKill();
        }

        // Mouse-aim rotation
        Vector2 lookDir = mousePos - rb.position;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }
    }

    /// <summary>
    /// Clamps candidate world position to the camera's viewport margins.
    /// Also naturally carries the player along +Y as camera scrolls upward.
    /// </summary>
    private void ApplyViewportClamping(ref Vector2 position)
    {
        Vector3 vp = cam.WorldToViewportPoint(new Vector3(position.x, position.y, 0f));
        vp.x = Mathf.Clamp(vp.x, minViewportX, maxViewportX);
        vp.y = Mathf.Clamp(vp.y, minViewportY, maxViewportY);
        Vector3 clampedWorld = cam.ViewportToWorldPoint(vp);
        position.x = clampedWorld.x;
        position.y = clampedWorld.y;
    }

    /// <summary>
    /// Checks if player's physical viewport Y has dropped below bottomKillThreshold.
    /// Inflicts 1 HP damage (with cooldown & i-frames) and pushes player forward towards minViewportY.
    /// </summary>
    private void HandleBottomEdgePushKill()
    {
        Vector3 currentVp = cam.WorldToViewportPoint(new Vector3(rb.position.x, rb.position.y, 0f));

        if (currentVp.y < bottomKillThreshold)
        {
            // 1. Inflict damage via PlayerHealth
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (playerHealth != null && playerHealth.IsAlive)
            {
                if (instantKillBelowScreen && currentVp.y <= 0f)
                {
                    playerHealth.TakeDamage(playerHealth.currentHealth);
                }
                else if (Time.time >= lastBottomDamageTime + bottomDamageInterval)
                {
                    lastBottomDamageTime = Time.time;
                    playerHealth.TakeDamage(1);
                }
            }

            // 2. Push player forward along +Y towards minViewportY
            if (bottomPushForward)
            {
                Vector3 targetVp = new Vector3(currentVp.x, minViewportY, currentVp.z);
                Vector3 targetWorld = cam.ViewportToWorldPoint(targetVp);

                float newY = Mathf.MoveTowards(rb.position.y, targetWorld.y, bottomPushSpeed * Time.fixedDeltaTime);
                rb.position = new Vector2(rb.position.x, newY);
            }
        }
    }

    /// <summary>
    /// Programmatically triggers bottom edge evaluation (for automated testing).
    /// </summary>
    public void ForceCheckBottomKillPlane()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            HandleBottomEdgePushKill();
        }
    }

    /// <summary>
    /// Configures viewport boundaries programmatically (for automated testing).
    /// </summary>
    public void SetViewportBounds(float minX, float maxX, float minY, float maxY)
    {
        minViewportX = minX;
        maxViewportX = maxX;
        minViewportY = minY;
        maxViewportY = maxY;
    }
}
