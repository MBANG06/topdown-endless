using System;
using UnityEngine;

/// <summary>
/// Collectible grenade item dropped by defeated enemies or placed in the arena.
/// When touched by the Player, increases the player's grenade inventory
/// (if below max capacity) and cleanly destroys itself.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class GrenadePickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Number of grenade charges granted upon collection.")]
    public int grenadeAmount = 1;

    [Header("Arena Boundaries")]
    public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
    public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
    public bool useDynamicBounds = true;

    [Header("Visual Floating Effect")]
    public bool enableFloatingAnimation = true;
    public float floatAmplitude = 0.08f;
    public float floatFrequency = 2.5f;

    private Vector3 _startPosition;
    private bool _isCollected = false;

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    private void Start()
    {
        // Clamp position inside arena bounds to ensure drops near perimeter stay inside playable arena
        Vector3 pos = transform.position;

        var scrollCam = ScrollingCameraController.Instance;
        if (scrollCam == null && Application.isPlaying)
        {
            scrollCam = FindObjectOfType<ScrollingCameraController>();
        }
        if (useDynamicBounds && scrollCam != null)
        {
            pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
        }
        else
        {
            pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
            pos.y = Mathf.Clamp(pos.y, arenaMin.y, arenaMax.y);
        }

        transform.position = pos;
        _startPosition = transform.position;
    }

    private void Update()
    {
        if (enableFloatingAnimation && !_isCollected)
        {
            float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = _startPosition + new Vector3(0f, yOffset, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null)
        {
            TryCollect(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject != null)
        {
            TryCollect(collision.gameObject);
        }
    }

    /// <summary>
    /// Attempts to collect this pickup by the collector GameObject.
    /// Only the Player can collect it, and only if player inventory is not at max capacity.
    /// </summary>
    public bool TryCollect(GameObject collector)
    {
        if (_isCollected || collector == null) return false;

        // Verify collector is the Player
        bool isPlayer = collector.CompareTag("Player") ||
                        collector.GetComponent<PlayerHealth>() != null ||
                        collector.GetComponent<GrenadeThrower>() != null ||
                        collector.GetComponentInParent<GrenadeThrower>() != null;

        if (!isPlayer)
        {
            return false;
        }

        // Locate GrenadeThrower component on player
        GrenadeThrower thrower = collector.GetComponent<GrenadeThrower>() ??
                                collector.GetComponentInParent<GrenadeThrower>();

        if (thrower == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                thrower = playerObj.GetComponent<GrenadeThrower>();
            }
        }

        if (thrower != null)
        {
            // Capacity guard: if player inventory is full, do not consume pickup
            if (thrower.grenadeCount >= thrower.maxGrenades)
            {
                return false;
            }

            bool added = thrower.AddGrenades(grenadeAmount);
            if (!added) return false;
        }

        _isCollected = true;
        PlayPickupFeedback();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
#else
        Destroy(gameObject);
#endif
        return true;
    }

    /// <summary>
    /// Plays pickup sound or triggers feedback if SoundManager is present.
    /// </summary>
    private void PlayPickupFeedback()
    {
        try
        {
            var soundMgrType = Type.GetType("SoundManager");
            if (soundMgrType != null)
            {
                var instProp = soundMgrType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var inst = instProp?.GetValue(null);
                if (inst != null)
                {
                    var method = soundMgrType.GetMethod("PlayPickupSFX", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(inst, null);
                }
            }
        }
        catch
        {
            // Optional feedback fallback
        }
    }
}
