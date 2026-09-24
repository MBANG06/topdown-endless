using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Area of Effect (AoE) explosion created upon grenade detonation.
/// Queries all colliders in a 3.5 unit radius and inflicts 50 damage
/// to all IDamageable targets (instantly eliminating regular enemies).
/// Player has friendly fire immunity.
/// </summary>
public class ExplosionAoE : MonoBehaviour
{
    [Header("Explosion Parameters")]
    [Tooltip("Blast radius in world units.")]
    public float explosionRadius = 3.5f;

    [Tooltip("Damage inflicted to all hostile entities in blast radius.")]
    public int damage = 50;

    [Tooltip("Duration before explosion object auto-destroys.")]
    public float lifetime = 0.6f;

    [Header("Visual Feedback & VFX")]
    [Tooltip("Visual effect prefab (such as Fire Effect) instantiated at blast center.")]
    public GameObject visualEffectPrefab;

    [Tooltip("Scale multiplier for the instantiated visual effect.")]
    public float visualScale = 3.5f;

    private bool _hasExploded = false;

    private void Awake()
    {
        Explode();
    }

    /// <summary>
    /// Executes the radial blast query, damages hostile targets, spawns VFX, and schedules destruction.
    /// </summary>
    public void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // Ensure 2D physics world is synchronized with transforms
        Physics2D.SyncTransforms();

        Vector2 blastCenter = transform.position;

        // Query all colliders in explosion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(blastCenter, explosionRadius);

        HashSet<IDamageable> damagedEntities = new HashSet<IDamageable>();

        if (hitColliders != null)
        {
            foreach (var col in hitColliders)
            {
                if (col == null) continue;

                // Friendly fire immunity: Player never takes grenade damage
                if (col.CompareTag("Player") ||
                    col.GetComponent<PlayerHealth>() != null ||
                    col.GetComponentInParent<PlayerHealth>() != null)
                {
                    continue;
                }

                IDamageable target = col.GetComponent<IDamageable>() ??
                                     col.GetComponentInParent<IDamageable>();

                if (target != null && !(target is PlayerHealth))
                {
                    if (damagedEntities.Add(target))
                    {
                        target.TakeDamage(damage);
                    }
                }
            }
        }

        // Spawn visual explosion effect
        SpawnVisualEffect(blastCenter);

        // Auto-destroy object in play mode
        if (Application.isPlaying)
        {
            Destroy(gameObject, lifetime);
        }
    }

    /// <summary>
    /// Spawns and scales the explosion particle or animated sprite VFX.
    /// </summary>
    private void SpawnVisualEffect(Vector2 position)
    {
        if (visualEffectPrefab != null)
        {
            GameObject vfx = Instantiate(visualEffectPrefab, position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * visualScale;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(vfx);
            }
            else
            {
                Destroy(vfx, lifetime);
            }
#else
            Destroy(vfx, lifetime);
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
