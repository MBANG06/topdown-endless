using System;
using UnityEngine;

/// <summary>
/// Shooter enemy archetype (Ranged).
/// Maintains a tactical distance (kiting at 3.8u - 5.5u) from the player
/// and periodically fires aimed EnemyBullet projectiles.
/// Default stats: 2 HP, 2.0 speed, 20 score, 25% grenade drop chance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShooterEnemy : EnemyBase
{
    [Header("Shooter Distance & Kiting")]
    public float retreatDistance = 3.8f;
    public float advanceDistance = 5.5f;
    public Vector2 arenaMin = new Vector2(-8.5f, -4.2f);
    public Vector2 arenaMax = new Vector2(13.8f, 5.2f);
    public bool useDynamicBounds = true;

    [Header("Ranged Attack")]
    public float shootInterval = 2.5f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    private float _shootTimer = 0f;

    public ShooterEnemy()
    {
        maxHealth = 2;
        currentHealth = 2;
        moveSpeed = 2.0f;
        scoreValue = 20;
        grenadeDropChance = 0.25f;
    }

    protected override void Awake()
    {
        maxHealth = 2;
        moveSpeed = 2.0f;
        scoreValue = 20;
        grenadeDropChance = 0.25f;

        base.Awake();
        _shootTimer = shootInterval * 0.5f; // Initial pacing offset
    }

    private void Update()
    {
        if (isDead || !IsAlive) return;

        if (playerTransform == null)
        {
            LocatePlayer();
            if (playerTransform == null) return;
        }

        if (!IsPlayerAlive()) return;

        _shootTimer += Time.deltaTime;
        if (_shootTimer >= shootInterval)
        {
            _shootTimer = 0f;
            Shoot();
        }
    }

    private void FixedUpdate()
    {
        if (isDead || !IsAlive) return;

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
        float distance = Vector2.Distance(targetPos, currentPos);
        Vector2 moveDir = Vector2.zero;

        if (distance < retreatDistance)
        {
            // Player is too close: kite away
            Vector2 away = currentPos - targetPos;
            if (away.sqrMagnitude > 0.0001f)
            {
                moveDir = away.normalized;
            }
        }
        else if (distance > advanceDistance)
        {
            // Player is too far: advance closer
            Vector2 toward = targetPos - currentPos;
            if (toward.sqrMagnitude > 0.0001f)
            {
                moveDir = toward.normalized;
            }
        }
        // In sweet spot [retreatDistance, advanceDistance]: hold position

        if (moveDir != Vector2.zero)
        {
            Vector2 nextPos = currentPos + moveDir * moveSpeed * Time.fixedDeltaTime;

            // Clamp position inside arena boundaries (adapts to scrolling camera if present)
            var scrollCam = ScrollingCameraController.Instance;
            if (scrollCam == null && Application.isPlaying)
            {
                scrollCam = FindObjectOfType<ScrollingCameraController>();
            }
            if (useDynamicBounds && scrollCam != null)
            {
                nextPos.x = Mathf.Clamp(nextPos.x, arenaMin.x, arenaMax.x);
                float camY = scrollCam.transform.position.y;
                nextPos.y = Mathf.Max(camY - 12f, nextPos.y);
            }
            else
            {
                nextPos.x = Mathf.Clamp(nextPos.x, arenaMin.x, arenaMax.x);
                nextPos.y = Mathf.Clamp(nextPos.y, arenaMin.y, arenaMax.y);
            }

            if (rb != null)
            {
                rb.MovePosition(nextPos);
            }
            else
            {
                transform.position = nextPos;
            }

            // Flip sprite facing player
            if (spriteRenderer != null)
            {
                float dx = targetPos.x - currentPos.x;
                if (Mathf.Abs(dx) > 0.05f)
                {
                    spriteRenderer.flipX = dx < 0;
                }
            }
        }
        else
        {
            if (rb != null) rb.velocity = Vector2.zero;

            // Still face player while standing
            if (spriteRenderer != null)
            {
                float dx = targetPos.x - currentPos.x;
                if (Mathf.Abs(dx) > 0.05f)
                {
                    spriteRenderer.flipX = dx < 0;
                }
            }
        }
    }

    /// <summary>
    /// Fires an EnemyBullet aimed directly at player position.
    /// </summary>
    public void Shoot()
    {
        if (bulletPrefab == null || isDead || !IsAlive || !IsPlayerAlive()) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = (Vector2)playerTransform.position - (Vector2)spawnPos;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            dir = Vector2.up;
        }
        else
        {
            dir = dir.normalized;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        Instantiate(bulletPrefab, spawnPos, rot);
    }
}
