using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player basic weapon firing with rate-limiting cooldown and projectile instantiation.
/// </summary>
public class Shooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float bulletForce = 20f;
    public float fireRate = 0.2f; // Cooldown in seconds between shots (5 shots/sec)

    private float _nextFireTime = 0f;

    private void Update()
    {
        // Prevent firing when game is paused
        if (Time.timeScale <= 0f) return;

        // No firing outside active gameplay (e.g. Main Menu boot).
        // EditMode tests bypass to stay hermetic.
        if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    /// <summary>
    /// Instantiates bullet projectile at firePoint and applies forward impulse force.
    /// </summary>
    public void Shoot()
    {
        if (bulletPrefab == null) return;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(spawnPoint.up * bulletForce, ForceMode2D.Impulse);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShootSFX();
        }
    }
}
