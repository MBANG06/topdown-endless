using System;
using UnityEngine;

/// <summary>
/// Chaser enemy archetype (Melee).
/// Directly tracks player position and deals 1 contact damage upon collision.
/// Default stats: 3 HP, 2.8 speed, 10 score, 20% grenade drop chance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : EnemyBase
{
    [Header("Chaser Combat")]
    public int contactDamage = 1;

    public ChaserEnemy()
    {
        maxHealth = 3;
        currentHealth = 3;
        moveSpeed = 2.8f;
        scoreValue = 10;
        grenadeDropChance = 0.20f;
    }

    protected override void Awake()
    {
        maxHealth = 3;
        moveSpeed = 2.8f;
        scoreValue = 10;
        grenadeDropChance = 0.20f;

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

        // Zero distance check to avoid NaN
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
