using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls player bullet trajectory, damage application via IDamageable,
/// collision/trigger impact effects, and lifetime expiration.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public int damage = 1;
    public float lifetime = 3f;
    public float speed = 20f;
    public GameObject hitEffect;

    private Rigidbody2D _rb;
    private bool _hasHit = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);

        // Ensure bullet has forward velocity if not propelled by impulse
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

        // Ignore hitting player or other player bullets
        if (hitObj.CompareTag("Player") ||
            hitObj.GetComponent<PlayerHealth>() != null ||
            hitObj.GetComponent<PlayerMovement>() != null ||
            hitObj.GetComponent<Bullet>() != null)
        {
            return;
        }

        // Check for damageable target
        IDamageable damageable = hitObj.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = hitObj.GetComponentInParent<IDamageable>();
        }

        // Pass through non-damageable triggers (such as item pickups)
        Collider2D hitCollider = hitObj.GetComponent<Collider2D>();
        if (hitCollider != null && hitCollider.isTrigger && damageable == null)
        {
            return;
        }

        _hasHit = true;

        if (damageable != null && damageable.IsAlive)
        {
            damageable.TakeDamage(damage);
        }

        // Spawn hit effect VFX
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }

        Destroy(gameObject);
    }
}
