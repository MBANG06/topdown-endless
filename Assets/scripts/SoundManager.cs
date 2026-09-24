using System;
using UnityEngine;

/// <summary>
/// Singleton audio manager providing procedural 8-bit sound synthesizers
/// using AudioClip.Create. Generates authentic retro sound waveforms in-memory
/// ensuring 0 missing audio asset files and deterministic playback.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Audio Configuration")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    public bool isMuted = false;

    private AudioSource _audioSource;

    // Cached procedural AudioClips
    private AudioClip _shootClip;
    private AudioClip _hitClip;
    private AudioClip _explosionClip;
    private AudioClip _hurtClip;
    private AudioClip _pickupClip;
    private AudioClip _gameOverClip;
    private AudioClip _victoryClip;

    private void Awake()
    {
        if (_instance == null || !Application.isPlaying)
        {
            _instance = this;
            if (Application.isPlaying && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D Sound

        GenerateAllProceduralClips();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Synthesizes all procedural 8-bit sound effects into memory.
    /// </summary>
    public void GenerateAllProceduralClips()
    {
        _shootClip = CreateShootSFXClip();
        _hitClip = CreateHitSFXClip();
        _explosionClip = CreateExplosionSFXClip();
        _hurtClip = CreateHurtSFXClip();
        _pickupClip = CreatePickupSFXClip();
        _gameOverClip = CreateGameOverSFXClip();
        _victoryClip = CreateVictorySFXClip();
    }

    #region Public Playback API

    public void PlayShootSFX()
    {
        if (_shootClip == null) _shootClip = CreateShootSFXClip();
        PlayClip(_shootClip, 0.6f);
    }

    public void PlayHitSFX()
    {
        if (_hitClip == null) _hitClip = CreateHitSFXClip();
        PlayClip(_hitClip, 0.7f);
    }

    public void PlayExplosionSFX()
    {
        if (_explosionClip == null) _explosionClip = CreateExplosionSFXClip();
        PlayClip(_explosionClip, 1.0f);
    }

    public void PlayHurtSFX()
    {
        if (_hurtClip == null) _hurtClip = CreateHurtSFXClip();
        PlayClip(_hurtClip, 0.8f);
    }

    public void PlayPickupSFX()
    {
        if (_pickupClip == null) _pickupClip = CreatePickupSFXClip();
        PlayClip(_pickupClip, 0.75f);
    }

    public void PlayGameOverSFX()
    {
        if (_gameOverClip == null) _gameOverClip = CreateGameOverSFXClip();
        PlayClip(_gameOverClip, 0.9f);
    }

    public void PlayVictorySFX()
    {
        if (_victoryClip == null) _victoryClip = CreateVictorySFXClip();
        PlayClip(_victoryClip, 0.9f);
    }

    public void PlayClip(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null || isMuted) return;

        float effectiveVolume = Mathf.Clamp01(masterVolume) * Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(volumeScale);
        if (effectiveVolume <= 0.001f) return;

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(clip, effectiveVolume);
        }
    }

    #endregion

    #region Procedural Waveform Generators

    /// <summary>
    /// Generic helper to create an in-memory procedural AudioClip given a synthesis function.
    /// </summary>
    public AudioClip CreateProceduralClip(string clipName, float duration, Func<float, float> sampleGenerator, int sampleRate = 44100)
    {
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Clamp(sampleGenerator(t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Shoot SFX: 8-bit fast downward pitch sweep (laser / blaster chirp).
    /// </summary>
    public AudioClip CreateShootSFXClip()
    {
        float duration = 0.12f;
        int sampleRate = 44100;
        float startFreq = 880f;
        float endFreq = 220f;

        return CreateProceduralClip("SFX_Shoot", duration, t =>
        {
            float progress = t / duration;
            float freq = Mathf.Lerp(startFreq, endFreq, progress * progress);
            float phase = 2f * Mathf.PI * freq * t;
            // Square wave with pulse duty cycle 0.5
            float sample = Mathf.Sin(phase) >= 0f ? 1f : -1f;
            float envelope = 1f - progress;
            return sample * envelope * 0.5f;
        }, sampleRate);
    }

    /// <summary>
    /// Hit SFX: Short punchy click with frequency dive and subtle noise.
    /// </summary>
    public AudioClip CreateHitSFXClip()
    {
        float duration = 0.08f;
        int sampleRate = 44100;

        return CreateProceduralClip("SFX_Hit", duration, t =>
        {
            float progress = t / duration;
            float freq = Mathf.Lerp(450f, 120f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float tone = Mathf.Sin(phase);
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
            float envelope = Mathf.Exp(-progress * 8f);
            return (tone * 0.7f + noise) * envelope * 0.6f;
        }, sampleRate);
    }

    /// <summary>
    /// Explosion SFX: Low frequency rumble combined with decaying white noise burst.
    /// </summary>
    public AudioClip CreateExplosionSFXClip()
    {
        float duration = 0.45f;
        int sampleRate = 44100;

        return CreateProceduralClip("SFX_Explosion", duration, t =>
        {
            float progress = t / duration;
            float noise = UnityEngine.Random.value * 2f - 1f;
            float rumble = Mathf.Sin(2f * Mathf.PI * 65f * (1f - progress * 0.5f) * t);
            float envelope = Mathf.Exp(-progress * 4.5f);
            return (noise * 0.8f + rumble * 0.4f) * envelope * 0.7f;
        }, sampleRate);
    }

    /// <summary>
    /// Hurt SFX: Low harsh buzzing drop.
    /// </summary>
    public AudioClip CreateHurtSFXClip()
    {
        float duration = 0.2f;
        int sampleRate = 44100;

        return CreateProceduralClip("SFX_Hurt", duration, t =>
        {
            float progress = t / duration;
            float freq = Mathf.Lerp(240f, 70f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            // Sawtooth tone
            float saw = (2f * (t * freq - Mathf.Floor(t * freq + 0.5f)));
            float envelope = 1f - progress;
            return saw * envelope * 0.6f;
        }, sampleRate);
    }

    /// <summary>
    /// Pickup SFX: Melodic ascending 3-note chime (C5 -> E5 -> G5).
    /// </summary>
    public AudioClip CreatePickupSFXClip()
    {
        float duration = 0.24f;
        int sampleRate = 44100;
        float[] notes = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
        float noteDur = duration / notes.Length;

        return CreateProceduralClip("SFX_Pickup", duration, t =>
        {
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notes.Length - 1);
            float freq = notes[noteIndex];
            float noteT = t - (noteIndex * noteDur);
            float noteProgress = noteT / noteDur;
            float phase = 2f * Mathf.PI * freq * t;
            // Mixed sine + triangle wave
            float tone = Mathf.Sin(phase);
            float env = 1f - noteProgress;
            return tone * env * 0.5f;
        }, sampleRate);
    }

    /// <summary>
    /// GameOver SFX: Sad descending four-tone sequence (Eb4 -> D4 -> Db4 -> C4).
    /// </summary>
    public AudioClip CreateGameOverSFXClip()
    {
        float duration = 0.8f;
        int sampleRate = 44100;
        float[] notes = { 311.13f, 293.66f, 277.18f, 261.63f };
        float noteDur = duration / notes.Length;

        return CreateProceduralClip("SFX_GameOver", duration, t =>
        {
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notes.Length - 1);
            float freq = notes[noteIndex];
            float noteT = t - (noteIndex * noteDur);
            float noteProgress = noteT / noteDur;
            float phase = 2f * Mathf.PI * freq * t;
            // Pulse wave
            float pulse = Mathf.Sin(phase) > 0.2f ? 0.8f : -0.8f;
            float env = Mathf.Exp(-noteProgress * 3f);
            return pulse * env * 0.5f;
        }, sampleRate);
    }

    /// <summary>
    /// Victory SFX: Triumphant ascending arpeggio fanfare (C4 -> E4 -> G4 -> C5).
    /// </summary>
    public AudioClip CreateVictorySFXClip()
    {
        float duration = 0.7f;
        int sampleRate = 44100;
        float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f };
        float noteDur = duration / notes.Length;

        return CreateProceduralClip("SFX_Victory", duration, t =>
        {
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notes.Length - 1);
            float freq = notes[noteIndex];
            float noteT = t - (noteIndex * noteDur);
            float noteProgress = noteT / noteDur;
            float phase = 2f * Mathf.PI * freq * t;
            float tone = (Mathf.Sin(phase) >= 0f ? 0.7f : -0.7f);
            float env = 1f - noteProgress * 0.5f;
            return tone * env * 0.55f;
        }, sampleRate);
    }

    #endregion
}
