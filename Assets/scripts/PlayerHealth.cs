using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player health, damage ingestion with i-frames, and death state.
/// Implements IDamageable.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int _maxHealth = 5;
    public int maxHealth
    {
        get => _maxHealth;
        set => _maxHealth = value;
    }

    public int currentHealth { get; private set; } = 5;

    public bool IsAlive => currentHealth > 0;

    [Header("Invulnerability & Flashing")]
    public float invulnerabilityDuration = 1.0f;
    public float flashInterval = 0.1f;
    public Color flashColor = new Color(1f, 0.2f, 0.2f, 0.4f);

    public bool isInvulnerable { get; private set; }

    [Header("References")]
    public SpriteRenderer spriteRenderer;

    // Events
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDeath;

    private Color _originalColor = Color.white;
    private Coroutine _invulnerabilityCoroutine;

    private void Awake()
    {
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
            _originalColor = spriteRenderer.color;
        }

        currentHealth = maxHealth;
    }

    private void OnDisable()
    {
        StopFlashRoutine();
    }

    /// <summary>
    /// Deducts exactly 1 HP per valid hit, as required by specification,
    /// and grants 1.0s invulnerability window with sprite flashing.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!IsAlive || isInvulnerable || damage <= 0)
        {
            return;
        }

        // Exactly 1 HP deducted per hit
        currentHealth = Mathf.Max(0, currentHealth - 1);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (_invulnerabilityCoroutine != null)
            {
                StopCoroutine(_invulnerabilityCoroutine);
            }
            _invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());
        }
    }

    /// <summary>
    /// Restores health up to maxHealth without exceeding it.
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Resets player health to maximum, clearing invulnerability and restoring controls.
    /// </summary>
    public void ResetHealth()
    {
        StopFlashRoutine();
        currentHealth = maxHealth;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;

        Shooting shooting = GetComponent<Shooting>();
        if (shooting != null) shooting.enabled = true;

        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        StopFlashRoutine();

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        Shooting shooting = GetComponent<Shooting>();
        if (shooting != null) shooting.enabled = false;

        OnPlayerDeath?.Invoke();
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        bool toggle = false;

        while (elapsed < invulnerabilityDuration)
        {
            toggle = !toggle;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = toggle ? flashColor : _originalColor;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        StopFlashRoutine();
    }

    private void StopFlashRoutine()
    {
        if (_invulnerabilityCoroutine != null)
        {
            StopCoroutine(_invulnerabilityCoroutine);
            _invulnerabilityCoroutine = null;
        }

        isInvulnerable = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _originalColor;
        }
    }
}
