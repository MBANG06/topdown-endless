using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Boss encounter controller inheriting from EnemyBase and IDamageable.
/// Stats: 60 HP, 1.8 move speed, 500 score value, guaranteed 2 grenade pickups.
/// Features a periodic 360-degree radial projectile barrage (16 bullets at 5.0 u/s),
/// static and instance events for spawning, health updates, and defeat,
/// and notifies EnemySpawner upon defeat to resume endless scaling.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : EnemyBase
{
    [Header("Boss Specific Combat")]
    public float burstInterval = 3.5f;
    public float telegraphDuration = 0.5f;
    public int radialBulletCount = 16;
    public float projectileSpeed = 5.0f;
    public float projectileLifetime = 5.0f;
    public int bulletDamage = 1;
    public int contactDamage = 1;
    public int guaranteedGrenadeDrops = 2;
    public GameObject bossBulletPrefab;
    public Color telegraphColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Visual Telegraph Settings")]
    public float telegraphFlashFrequency = 8.0f;

    [Header("Boss State")]
    public bool isAttacking = false;

    // Static Events
    public static event Action<BossController> OnBossSpawned;
    public static event Action<int, int> OnBossHealthChanged;
    public static event Action OnBossDefeatedEvent;
    public static event Action OnBossKilled;

    // Instance Events
    public event Action<int, int> OnHealthChanged;
    public event Action OnDefeated;

    private Coroutine _attackRoutine;

    public BossController()
    {
        maxHealth = 60;
        currentHealth = 60;
        moveSpeed = 1.8f;
        scoreValue = 500;
        grenadeDropChance = 1.0f;
    }

    protected override void Awake()
    {
        maxHealth = 60;
        if (currentHealth <= 0 || currentHealth == 3)
        {
            currentHealth = maxHealth;
        }
        if (moveSpeed == 2.8f || moveSpeed <= 0f)
        {
            moveSpeed = 1.8f;
        }
        scoreValue = 500;
        grenadeDropChance = 1.0f;

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Broadcast spawn event
        OnBossSpawned?.Invoke(this);
        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (Application.isPlaying)
        {
            _attackRoutine = StartCoroutine(RadialBarrageLoop());
        }
    }

    private void FixedUpdate()
    {
        if (isDead || !IsAlive) return;

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Pause movement during attack telegraph/burst
        if (isAttacking)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        if (playerTransform == null)
        {
            LocatePlayer();
            if (playerTransform == null) return;
        }

        if (!IsPlayerAlive())
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 targetPos = playerTransform.position;
        Vector2 diff = targetPos - currentPos;

        if (diff.sqrMagnitude <= 0.0001f)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = diff.normalized;
        Vector2 targetMove = currentPos + moveDir * moveSpeed * Time.fixedDeltaTime;

        if (rb != null)
        {
            rb.MovePosition(targetMove);
        }
        else
        {
            transform.position = targetMove;
        }

        if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.05f)
        {
            spriteRenderer.flipX = moveDir.x < 0;
        }
    }

    /// <summary>
    /// Continuous periodic attack loop during combat.
    /// </summary>
    private IEnumerator RadialBarrageLoop()
    {
        while (!isDead && IsAlive)
        {
            yield return new WaitForSeconds(burstInterval);

            if (isDead || !IsAlive) yield break;

            yield return StartCoroutine(ExecuteRadialBarrage());
        }
    }

    /// <summary>
    /// Executes telegraph warning and then radial projectile emission.
    /// Enhanced with a pulsating warning flash between originalColor and telegraphColor over telegraphDuration (0.5s).
    /// </summary>
    public IEnumerator ExecuteRadialBarrage()
    {
        isAttacking = true;
        if (rb != null) rb.velocity = Vector2.zero;

        // Enhanced telegraph: flashing warning / charge-up color pulse
        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            if (isDead || !IsAlive) yield break;

            if (spriteRenderer != null && flashCoroutine == null)
            {
                float pulse = Mathf.PingPong(elapsed * telegraphFlashFrequency, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, telegraphColor, pulse);
            }

            float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.05f;
            elapsed += dt;
            yield return null;
        }

        // Restore base color before projectile emission
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (!isDead && IsAlive)
        {
            FireRadialBurst();
        }

        isAttacking = false;
    }

    /// <summary>
    /// Emits 16 projectiles spaced evenly by 22.5 degrees in a 360-degree circle.
    /// </summary>
    public void FireRadialBurst()
    {
        float angleStep = 360f / radialBulletCount; // 22.5 degrees
        Vector2 origin = transform.position;

        for (int i = 0; i < radialBulletCount; i++)
        {
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            SpawnBossProjectile(origin, dir, angleDeg);
        }
    }

    /// <summary>
    /// Instantiates and initializes a single radial projectile.
    /// </summary>
    public GameObject SpawnBossProjectile(Vector2 origin, Vector2 dir, float angleDeg)
    {
        GameObject bulletObj;
        Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg - 90f);

        if (bossBulletPrefab != null)
        {
            bulletObj = Instantiate(bossBulletPrefab, origin, rot);
        }
        else
        {
            bulletObj = new GameObject("BossBullet");
            bulletObj.transform.position = origin;
            bulletObj.transform.rotation = rot;

            var sr = bulletObj.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.4f, 0.1f, 1f);
            sr.sortingOrder = 2;

            var col = bulletObj.AddComponent<CircleCollider2D>();
            col.radius = 0.25f;
            col.isTrigger = true;

            var bulletRb = bulletObj.AddComponent<Rigidbody2D>();
            bulletRb.gravityScale = 0f;
            bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var eb = bulletObj.AddComponent<EnemyBullet>();
            eb.speed = projectileSpeed;
            eb.damage = bulletDamage;
            eb.lifetime = projectileLifetime;
        }

        var enemyBullet = bulletObj.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.speed = projectileSpeed;
            enemyBullet.damage = bulletDamage;
            enemyBullet.lifetime = projectileLifetime;
        }

        var rb2d = bulletObj.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.velocity = dir * projectileSpeed;
        }

        return bulletObj;
    }

    public override void TakeDamage(int damage)
    {
        if (!IsAlive || isDead || damage <= 0) return;

        base.TakeDamage(damage);

        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected override IEnumerator FlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = isAttacking ? telegraphColor : originalColor;
        }

        flashCoroutine = null;
    }

    public override void Die()
    {
        if (isDead) return;

        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
        StopAllCoroutines();
        isAttacking = false;

        // Report 0 HP to listeners
        OnBossHealthChanged?.Invoke(0, maxHealth);
        OnHealthChanged?.Invoke(0, maxHealth);

        // Notify EnemySpawner to clear boss status and resume endless mode
        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.OnBossDefeated();
        }

        // Trigger GameManager victory panel if GameManager exists
        TriggerGameManagerVictory();

        // Broadcast death events
        OnBossDefeatedEvent?.Invoke();
        OnBossKilled?.Invoke();
        OnDefeated?.Invoke();

        base.Die();
    }

    /// <summary>
    /// Guaranteed drop of 2 grenade pickups upon Boss defeat.
    /// </summary>
    protected override void RollGrenadeDrop()
    {
        if (grenadePickupPrefab == null) return;

        Instantiate(grenadePickupPrefab, (Vector2)transform.position + new Vector2(-0.6f, 0f), Quaternion.identity);
        Instantiate(grenadePickupPrefab, (Vector2)transform.position + new Vector2(0.6f, 0f), Quaternion.identity);
    }

    private void TriggerGameManagerVictory()
    {
        try
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var gmType = assembly.GetType("GameManager");
                if (gmType != null)
                {
                    var instanceProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var victoryMethod = gmType.GetMethod("TriggerVictory", BindingFlags.Public | BindingFlags.Instance);
                        victoryMethod?.Invoke(instance, null);
                    }
                    break;
                }
            }
        }
        catch
        {
            // GameManager may not be present until M5
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryInflictContactDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryInflictContactDamage(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryInflictContactDamage(collider.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        TryInflictContactDamage(collider.gameObject);
    }

    private void TryInflictContactDamage(GameObject target)
    {
        if (isDead || !IsAlive || target == null) return;

        if (target.CompareTag("Player") || target.GetComponent<PlayerHealth>() != null)
        {
            var ph = target.GetComponent<PlayerHealth>() ?? target.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.IsAlive)
            {
                ph.TakeDamage(contactDamage);
            }
        }
    }
}
