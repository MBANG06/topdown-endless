using System;
using UnityEngine;

/// <summary>
/// Rusher enemy archetype (Speed Melee).
/// Rapidly intercepts the player at high velocity (6.2 u/s),
/// dealing 1 contact damage upon collision.
/// Fragile glass-cannon with exactly 1 HP, 15 score, 15% grenade drop chance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RusherEnemy : EnemyBase
{
    [Header("Rusher Combat")]
    public int contactDamage = 1;

    public RusherEnemy()
    {
        maxHealth = 1;
        currentHealth = 1;
        moveSpeed = 6.2f;
        scoreValue = 15;
        grenadeDropChance = 0.15f;
    }

    protected override void Awake()
    {
        maxHealth = 1;
        moveSpeed = 6.2f;
        scoreValue = 15;
        grenadeDropChance = 0.15f;

        base.Awake();
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

        // Flip sprite facing direction
        if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.05f)
        {
            spriteRenderer.flipX = moveDir.x < 0;
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
