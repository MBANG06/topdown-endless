using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile fired by Shooter enemies and hostile entities.
/// Travels forward at constant velocity, damages the Player on collision,
/// ignores friendly enemies, and auto-destructs on walls or lifetime expiration.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 8.0f;
    public int damage = 1;
    public float lifetime = 4.0f;
    public GameObject hitEffect;

    private Rigidbody2D _rb;
    private bool _hasHit = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) Destroy(gameObject, lifetime);
#else
        Destroy(gameObject, lifetime);
#endif

        if (_rb != null && _rb.velocity.sqrMagnitude < 0.01f)
        {
            _rb.velocity = transform.up * speed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        HandleHit(collider.gameObject);
    }

    private void HandleHit(GameObject hitObj)
    {
        if (_hasHit || hitObj == null) return;

        // Friendly enemy immunity: ignore hits against other enemies or enemy bullets
        if (hitObj.GetComponent<EnemyBase>() != null ||
            hitObj.GetComponentInParent<EnemyBase>() != null ||
            hitObj.GetComponent<EnemyBullet>() != null ||
            hitObj.CompareTag("Enemy"))
        {
            return;
        }

        // Pass through pickups / trigger volumes that are not the player
        Collider2D hitCollider = hitObj.GetComponent<Collider2D>();
        bool isPlayer = hitObj.CompareTag("Player") || hitObj.GetComponent<PlayerHealth>() != null || hitObj.GetComponentInParent<PlayerHealth>() != null;

        if (hitCollider != null && hitCollider.isTrigger && !isPlayer)
        {
            return;
        }

        _hasHit = true;

        if (isPlayer)
        {
            var ph = hitObj.GetComponent<PlayerHealth>() ?? hitObj.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.IsAlive)
            {
                ph.TakeDamage(damage);
            }
        }

        // Spawn impact VFX if assigned
        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, transform.position, Quaternion.identity);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(fx);
            else Destroy(fx, 0.5f);
#else
            Destroy(fx, 0.5f);
#endif
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
