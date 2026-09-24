using UnityEngine;

/// <summary>
/// Thrown grenade projectile that travels along a simulated parabolic trajectory
/// toward target coordinates. Detonates upon fuse expiration (1.2s) or upon
/// direct impact with enemies or obstacles, spawning ExplosionAoE.
/// </summary>
public class GrenadeProjectile : MonoBehaviour
{
    [Header("Trajectory & Timing")]
    [Tooltip("Travel time to target coordinate in seconds.")]
    public float flightDuration = 0.7f;

    [Tooltip("Total fuse duration until forced detonation in seconds.")]
    public float fuseTime = 1.2f;

    [Tooltip("Peak height scale multiplier for parabolic height illusion.")]
    public float maxArcHeight = 0.5f;

    [Header("Explosion References")]
    [Tooltip("Explosion prefab containing ExplosionAoE component.")]
    public GameObject explosionPrefab;

    private Vector2 _startPosition;
    private Vector2 _targetPosition;
    private float _elapsedTime = 0f;
    private bool _hasDetonated = false;
    private bool _hasArrived = false;
    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        _baseScale = transform.localScale;
        if (_baseScale == Vector3.zero)
        {
            _baseScale = Vector3.one;
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    /// <summary>
    /// Initializes projectile start and target destination coordinates.
    /// </summary>
    public void Initialize(Vector2 start, Vector2 target)
    {
        _startPosition = start;
        _targetPosition = target;
        transform.position = start;
        _elapsedTime = 0f;
        _hasArrived = false;
        _hasDetonated = false;
    }

    private void Update()
    {
        if (_hasDetonated) return;

        _elapsedTime += Time.deltaTime;

        // Trajectory flight motion
        if (!_hasArrived)
        {
            float t = Mathf.Clamp01(_elapsedTime / flightDuration);
            transform.position = Vector2.Lerp(_startPosition, _targetPosition, t);

            // Parabolic height scale oscillation: sin(t * pi) reaches peak 1.0 at t=0.5
            float heightCurve = Mathf.Sin(t * Mathf.PI);
            transform.localScale = _baseScale * (1.0f + heightCurve * maxArcHeight);

            if (t >= 1.0f)
            {
                _hasArrived = true;
                transform.position = _targetPosition;
                transform.localScale = _baseScale;
            }
        }

        // Fuse expiration check
        if (_elapsedTime >= fuseTime)
        {
            Detonate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject != null)
        {
            HandleImpact(collision.gameObject);
        }
    }

    /// <summary>
    /// Evaluates impact target: ignores friendly player, detonates on hostile entities or walls.
    /// </summary>
    private void HandleImpact(GameObject target)
    {
        if (_hasDetonated || target == null) return;

        // Friendly player collision ignored (no detonation on player)
        if (target.CompareTag("Player") ||
            target.GetComponent<PlayerHealth>() != null ||
            target.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        // Hostile enemy or obstacle collision causes immediate detonation
        bool isEnemy = target.CompareTag("Enemy") ||
                       target.GetComponent<EnemyBase>() != null ||
                       target.GetComponentInParent<EnemyBase>() != null ||
                       target.GetComponent<IDamageable>() != null;

        bool isWall = target.CompareTag("Colliders") || target.name.Contains("Collider") || target.name.Contains("Wall");

        if (isEnemy || isWall)
        {
            Detonate();
        }
    }

    /// <summary>
    /// Detonates the grenade, instantiating ExplosionAoE and destroying this projectile.
    /// </summary>
    public void Detonate()
    {
        if (_hasDetonated) return;
        _hasDetonated = true;

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // Runtime fallback if explosionPrefab was not assigned in inspector
            var fallback = new GameObject("ExplosionAoE_Fallback");
            fallback.transform.position = transform.position;
            fallback.AddComponent<ExplosionAoE>();
        }

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
}
