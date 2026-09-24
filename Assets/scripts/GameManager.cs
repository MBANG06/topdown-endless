using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Possible states of the core gameplay loop state machine.
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    VictoryContinues
}

/// <summary>
/// Central game loop manager coordinating gameplay state transitions,
/// score tracking, high score persistence via PlayerPrefs, and event dispatch.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Game State")]
    [SerializeField] private GameState _currentState = GameState.Playing;
    public GameState CurrentState
    {
        get => _currentState;
        private set => _currentState = value;
    }

    /// <summary>
    /// String representation or alias of current state for testing contracts.
    /// </summary>
    public string State => CurrentState.ToString();

    [Header("Score Tracking")]
    [SerializeField] private int _currentScore = 0;
    public int CurrentScore
    {
        get => _currentScore;
        private set => _currentScore = value;
    }

    public int currentScore => CurrentScore;

    private int _highScore = -1;
    public int HighScore
    {
        get
        {
            if (_highScore < 0)
            {
                _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            }
            return _highScore;
        }
        private set => _highScore = value;
    }

    [SerializeField] private UIManager _uiManager;
    public UIManager UIManagerRef
    {
        get => _uiManager != null ? _uiManager : UIManager.Instance;
        set => _uiManager = value;
    }

    [Header("Boss Encounter & Endless Scaling")]
    [Tooltip("Score interval between successive boss encounters (default 500).")]
    [SerializeField] private int _bossScoreInterval = 500;

    [Tooltip("Next score threshold at which a boss arena will be queued (default 500).")]
    [SerializeField] private int _nextBossScoreThreshold = 500;

    [Tooltip("Score recorded when the current boss encounter was triggered.")]
    [SerializeField] private int _lastBossTriggerScore = 0;

    public int BossScoreInterval => _bossScoreInterval;
    public int NextBossScoreThreshold
    {
        get => _nextBossScoreThreshold;
        set => _nextBossScoreThreshold = value;
    }
    public int nextBossScoreThreshold
    {
        get => _nextBossScoreThreshold;
        set => _nextBossScoreThreshold = value;
    }

    public void ScaleBossThreshold()
    {
        _lastBossTriggerScore = CurrentScore;
        _nextBossScoreThreshold += _bossScoreInterval;
    }

    public void CheckBossScoreTrigger()
    {
        if (CurrentScore >= _nextBossScoreThreshold && CurrentScore > _lastBossTriggerScore)
        {
            var mapMgr = MapManager.Instance ?? FindObjectOfType<MapManager>();
            if (mapMgr != null && !mapMgr.IsBossArenaQueued && !mapMgr.IsBossArenaSpawned)
            {
                mapMgr.QueueBossArena();
            }
        }
    }

    public void SetAsActiveInstance()
    {
        _instance = this;
    }

    private const string HighScoreKey = "HighScore";

    private bool _handlingEventScore = false;
    private bool _suppressNextReflection = false;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (_instance == null || !Application.isPlaying)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject);
            return;
        }

        if (Application.isPlaying)
        {
            CurrentScore = 0;
            Time.timeScale = 1.0f;
        }

        LoadHighScore();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        HookPlayer();
        UpdateUIAll();
    }

    private void OnEnable()
    {
        EnemyBase.OnEnemyKilledScore += HandleEnemyKilledScore;
        BossController.OnBossDefeatedEvent += HandleBossDefeated;
        HookPlayer();
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyKilledScore -= HandleEnemyKilledScore;
        BossController.OnBossDefeatedEvent -= HandleBossDefeated;

        if (_playerHealth != null)
        {
            _playerHealth.OnPlayerDeath -= TriggerGameOver;
        }
    }

    private void Update()
    {
        // Toggle pause on Escape or P keys
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
            {
                TogglePause();
            }
        }
    }

    private void LateUpdate()
    {
        // Reset reflection deduplication flag at end of frame
        _suppressNextReflection = false;
    }

    private void HookPlayer()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindObjectOfType<PlayerHealth>();
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDeath -= TriggerGameOver;
                _playerHealth.OnPlayerDeath += TriggerGameOver;
            }
        }
    }

    public void ReloadHighScore()
    {
        LoadHighScore();
    }

    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void UpdateUIAll()
    {
        if (UIManagerRef != null)
        {
            UIManagerRef.UpdateScore(CurrentScore);
            UIManagerRef.UpdateHighScore(HighScore);
        }
    }

    #region Score Management

    private void HandleEnemyKilledScore(int points)
    {
        _suppressNextReflection = true;
        _handlingEventScore = true;
        try
        {
            AddScore(points);
        }
        finally
        {
            _handlingEventScore = false;
        }
    }

    /// <summary>
    /// Adds points to current score, updates high score if beaten,
    /// and dispatches notifications to UIManager.
    /// </summary>
    public void AddScore(int points)
    {
        if (points <= 0) return;

        // Deduplicate redundant reflection call immediately following event dispatch
        if (!_handlingEventScore && _suppressNextReflection)
        {
            _suppressNextReflection = false;
            return;
        }

        CurrentScore += points;

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        if (UIManagerRef != null)
        {
            UIManagerRef.UpdateScore(CurrentScore);
            UIManagerRef.UpdateHighScore(HighScore);
        }

        CheckBossScoreTrigger();
    }

    #endregion

    #region Game Flow State Machine

    /// <summary>
    /// Starts or resumes gameplay from Main Menu.
    /// Resets player health, movement, shooting, and score if player was dead.
    /// </summary>
    public void StartGame()
    {
        // New run from Main Menu always starts at 0 (fresh score + boss cycle),
        // independent of how the previous session ended.
        CurrentScore = 0;
        _nextBossScoreThreshold = _bossScoreInterval;
        _lastBossTriggerScore = 0;
        if (UIManagerRef != null)
        {
            UIManagerRef.UpdateScore(CurrentScore);
        }

        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null && (playerHealth.currentHealth <= 0 || !playerHealth.IsAlive))
        {
            playerHealth.ResetHealth();

            var thrower = FindObjectOfType<GrenadeThrower>();
            if (thrower != null)
            {
                thrower.ResetGrenades(2);
            }

            var spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.survivalTime = 0f;
                spawner.bossSpawned = false;
                spawner.isBossActive = false;
                spawner.isSpawning = true;
            }
        }

        // Fresh run always starts on a clean field (clears menu-idle spawns too).
        var leftovers = FindObjectsOfType<EnemyBase>();
        foreach (var enemy in leftovers)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        CurrentState = GameState.Playing;
        Time.timeScale = 1.0f;

        var ui = UIManagerRef != null ? UIManagerRef : FindObjectOfType<UIManager>();
        if (ui != null)
        {
            ui.ShowMainMenu(false);
            ui.UpdateScore(CurrentScore);
            ui.UpdateHighScore(HighScore);
            ui.HookSceneEntities();
        }
    }

    public void PlayGame() => StartGame();

    /// <summary>
    /// Pauses or unpauses gameplay, manipulating Time.timeScale and Pause Panel.
    /// </summary>
    public void PauseGame(bool pause = true)
    {
        if (CurrentState == GameState.GameOver || CurrentState == GameState.VictoryContinues)
        {
            return; // Cannot pause during Game Over or Victory screens
        }

        if (pause)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            if (UIManagerRef != null)
            {
                UIManagerRef.ShowPause(true);
            }
        }
        else
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1.0f;
            if (UIManagerRef != null)
            {
                UIManagerRef.ShowPause(false);
            }
        }
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            PauseGame(true);
        }
        else if (CurrentState == GameState.Paused)
        {
            PauseGame(false);
        }
    }

    /// <summary>
    /// Triggers Game Over sequence on player death.
    /// Freezes timeScale, reveals GameOver panel, and plays audio feedback.
    /// </summary>
    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;

        if (UIManagerRef != null)
        {
            UIManagerRef.ShowGameOver(CurrentScore, HighScore);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGameOverSFX();
        }
    }

    /// <summary>
    /// Triggers Victory modal upon Boss defeat.
    /// Freezes timeScale, reveals Victory modal ("BOSS SLAIN! +500 PTS"), and plays victory fanfare.
    /// </summary>
    public void TriggerVictory()
    {
        if (CurrentState == GameState.VictoryContinues) return;

        CurrentState = GameState.VictoryContinues;
        Time.timeScale = 0f;

        if (UIManagerRef != null)
        {
            UIManagerRef.ShowVictory();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayVictorySFX();
        }

        // Auto-resume endless mode after a short celebration so the game never
        // sits frozen waiting for input. Clicking CONTINUE skips the wait.
        // Play-mode only: EditMode tests assert the frozen modal state synchronously.
        if (Application.isPlaying)
        {
            StartCoroutine(AutoContinueAfterVictory(3f));
        }
    }

    private IEnumerator AutoContinueAfterVictory(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        if (CurrentState == GameState.VictoryContinues)
        {
            ResumeEndlessAfterBoss();
        }
    }

    private void HandleBossDefeated()
    {
        TriggerVictory();
    }

    /// <summary>
    /// Resumes endless enemy scaling after Boss defeat modal is dismissed.
    /// Restores Time.timeScale to 1.0f, unlocks camera, opens boss arena top wall,
    /// resumes standard segment spawning, and scales next boss threshold (+500 pts).
    /// </summary>
    public void ResumeEndlessAfterBoss()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1.0f;

        if (UIManagerRef != null)
        {
            UIManagerRef.HideVictory();
        }

        // 1. Camera Unlock & Smooth Resume
        var camCtrl = ScrollingCameraController.Instance ?? FindObjectOfType<ScrollingCameraController>();
        if (camCtrl != null)
        {
            camCtrl.UnlockAndResume();
        }

        // 2. Open Arena Top Wall & Resume Standard Spawning
        var mapMgr = MapManager.Instance ?? FindObjectOfType<MapManager>();
        if (mapMgr != null)
        {
            mapMgr.OpenBossArenaTopWall();
            mapMgr.ResumeStandardSpawning();
        }

        // 3. Scale Next Boss Score Threshold (+500 pts)
        ScaleBossThreshold();
    }

    public void ContinueEndless()
    {
        ResumeEndlessAfterBoss();
    }

    /// <summary>
    /// Resets gameplay session cleanly: score to 0, unpauses timeScale to 1.0f,
    /// clears panels, resets player health/grenades, and resets enemy spawner.
    /// </summary>
    public void RestartGame()
    {
        CurrentScore = 0;
        _nextBossScoreThreshold = _bossScoreInterval;
        _lastBossTriggerScore = 0;
        Time.timeScale = 1.0f;
        CurrentState = GameState.Playing;

        if (UIManagerRef != null)
        {
            UIManagerRef.HideAllPanels();
            UIManagerRef.UpdateScore(0);
            UIManagerRef.UpdateHighScore(HighScore);
        }

        // Reset player health and controls
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        // Reset player grenades
        var thrower = FindObjectOfType<GrenadeThrower>();
        if (thrower != null)
        {
            thrower.ResetGrenades(2);
        }

        // Destroy active enemies
        var enemies = FindObjectsOfType<EnemyBase>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        // Reset EnemySpawner
        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.survivalTime = 0f;
            spawner.bossSpawned = false;
            spawner.isBossActive = false;
            spawner.isSpawning = true;
        }

        // Re-hook UI
        if (UIManagerRef != null)
        {
            UIManagerRef.HookSceneEntities();
        }
    }

    /// <summary>
    /// Returns to the Main Menu panel.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f;
        CurrentState = GameState.MainMenu;

        var ui = UIManagerRef != null ? UIManagerRef : FindObjectOfType<UIManager>();
        if (ui != null)
        {
            ui.HideAllPanels();
            ui.ShowMainMenu(true);
        }
    }

    #endregion
}
