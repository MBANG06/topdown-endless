using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Abstract base class for all enemy archetypes.
/// Implements IDamageable, provides health management, damage flashing,
/// score notification, death cleanup, and grenade item drop probability rolls.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Enemy Stats")]
    public int maxHealth = 3;
    public int currentHealth { get; protected set; } = 3;
    public float moveSpeed = 2.8f;
    public int scoreValue = 10;
    public float grenadeDropChance = 0.2f;

    public bool IsAlive => currentHealth > 0;

    [Header("Visual Feedback & VFX")]
    public SpriteRenderer spriteRenderer;
    public Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashDuration = 0.1f;
    public GameObject hitEffect;
    public GameObject deathEffect;
    public GameObject grenadePickupPrefab;

    public bool isDead { get; protected set; } = false;

    // Events
    public static event Action<int> OnEnemyKilledScore;
    public static event Action<EnemyBase> OnEnemyDied;

    protected Transform playerTransform;
    protected Rigidbody2D rb;
    protected Color originalColor = Color.white;
    protected Coroutine flashCoroutine;

    protected virtual void Awake()
    {
        if (currentHealth <= 0 || currentHealth != maxHealth)
        {
            currentHealth = maxHealth;
        }

        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    protected virtual void Start()
    {
        LocatePlayer();
    }

    /// <summary>
    /// Finds the player transform in the scene if not explicitly assigned.
    /// </summary>
    public virtual void LocatePlayer()
    {
        if (playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null) playerObj = ph.gameObject;
        }

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    /// <summary>
    /// Explicitly injects target player transform.
    /// </summary>
    public virtual void SetPlayer(Transform target)
    {
        playerTransform = target;
    }

    /// <summary>
    /// Evaluates whether target player is valid and alive.
    /// </summary>
    public virtual bool IsPlayerAlive()
    {
        if (playerTransform == null) return false;
        var ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null) return ph.IsAlive;
        return true;
    }

    /// <summary>
    /// Ingests damage from bullets or explosions.
    /// Triggers damage color flash and invokes Die() on 0 HP.
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (!IsAlive || isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Spawn hit visual effect if present
        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(fx, 0.5f);
        }

        // Trigger damage flash
        TriggerDamageFlash();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Triggers visual damage flash safely handling disabled renderers and re-entrant hits.
    /// </summary>
    protected virtual void TriggerDamageFlash()
    {
        if (spriteRenderer == null || !spriteRenderer.enabled) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        if (gameObject.activeInHierarchy)
        {
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    protected virtual IEnumerator FlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        flashCoroutine = null;
    }

    /// <summary>
    /// Handles death sequence idempotently: disables collision, notifies score systems,
    /// rolls grenade item drop, spawns death VFX, and cleans up GameObject.
    /// </summary>
    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        // 1. Immediately disable all colliders so dead enemy cannot interact further
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = false;
        }

        // 2. Stop ongoing flash routine and restore color
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // 3. Award score to GameManager via reflection or static events
        AwardScore();

        // 4. Notify listeners (spawner, test hooks)
        OnEnemyDied?.Invoke(this);

        // 5. Roll grenade drop probability
        RollGrenadeDrop();

        // 6. Spawn death VFX
        if (deathEffect != null)
        {
            GameObject vfx = Instantiate(deathEffect, transform.position, Quaternion.identity);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(vfx);
            else Destroy(vfx, 0.6f);
#else
            Destroy(vfx, 0.6f);
#endif
        }

        // 7. Clean destruction
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
    }

    /// <summary>
    /// Safely dispatches score addition to GameManager if present,
    /// and broadcasts static event for score listeners.
    /// </summary>
    protected virtual void AwardScore()
    {
        OnEnemyKilledScore?.Invoke(scoreValue);

        try
        {
            // Reflection check for GameManager.Instance.AddScore(scoreValue)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var gmType = assembly.GetType("GameManager");
                if (gmType != null)
                {
                    var instanceProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var addScoreMethod = gmType.GetMethod("AddScore", BindingFlags.Public | BindingFlags.Instance);
                        addScoreMethod?.Invoke(instance, new object[] { scoreValue });
                    }
                    break;
                }
            }
        }
        catch
        {
            // Ignored if GameManager is not yet loaded or initialized
        }
    }

    /// <summary>
    /// Rolls probability for dropping a Grenade item pickup upon death.
    /// </summary>
    protected virtual void RollGrenadeDrop()
    {
        if (grenadePickupPrefab == null) return;

        float roll = UnityEngine.Random.value;
        if (roll <= grenadeDropChance)
        {
            Instantiate(grenadePickupPrefab, transform.position, Quaternion.identity);
        }
    }

    protected virtual void OnDestroy()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }
}
