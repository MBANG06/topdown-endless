using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Singleton UI manager controlling in-game HUD (5 heart health display, score, high score,
/// grenade inventory, boss health bar) and screen panels (MainMenu, Pause, GameOver, Victory).
/// Responds to entity events and provides callbacks for UI button interaction.
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("HUD Hearts")]
    public Image[] heartIcons = new Image[5];
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("HUD Text Indicators")]
    public Text scoreText;
    public Text highScoreText;
    public Text grenadeCountText;

    [Header("HUD Boss Health Bar")]
    public Slider bossHealthSlider;
    public GameObject bossBarContainer;

    [Header("HUD Distance & Boss Banners (M4)")]
    public Text distanceText;
    public GameObject bossWarningBanner;
    public Text bossWarningText;
    public GameObject bossArenaBanner;
    public Text bossArenaBannerText;
    public float bossWarningThresholdOffset = 50f;
    public float bossWarningFlashFrequency = 4f;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject controlsModal;

    [Header("Panel Text Fields")]
    public Text finalScoreText;
    public Text recordScoreText;
    public Text victoryBannerText;

    [Header("Buttons")]
    public Button playButton;
    public Button controlsButton;
    public Button quitButton;
    public Button closeControlsButton;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;
    public Button gameOverRestartButton;
    public Button gameOverMenuButton;
    public Button victoryContinueButton;

    private PlayerHealth _subscribedPlayerHealth;
    private GrenadeThrower _subscribedThrower;

    private void Awake()
    {
        if (_instance == null || !Application.isPlaying)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadHeartSpritesIfMissing();
    }

    private void Start()
    {
        // Initial setup for HUD
        int initialHigh = PlayerPrefs.GetInt("HighScore", 0);
        UpdateHighScore(initialHigh);
        UpdateScore(0);
        UpdateHearts(5);
        UpdateGrenades(2);

        // Hide Boss Bar initially
        SetBossBarVisible(false);

        // M4: ensure Distance + Boss banners exist and init hidden states
        EnsureM4HUD();
        UpdateDistance(0f);
        ShowBossWarning(false);
        ShowBossArenaStatus(false);

        // Self-healing: rebuild battle-result panels if missing (scene surgery,
        // bad merges, or stale memory must never silently kill end screens).
        EnsureBattlePanels();

        // Ensure panels are hidden unless starting in Main Menu
        HideAllPanels();
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
        {
            ShowMainMenu(true);
        }

        // Subscribe to PlayerHealth and GrenadeThrower if present in scene
        HookSceneEntities();

        // Subscribe to Boss events
        BossController.OnBossSpawned += HandleBossSpawned;
        BossController.OnBossHealthChanged += HandleBossHealthChanged;
        BossController.OnBossDefeatedEvent += HandleBossDefeated;

        WireButtons();
    }

    private void WireButtons()
    {
        SafeAddButtonListener(playButton, OnPlayButtonClicked, nameof(OnPlayButtonClicked));
        SafeAddButtonListener(controlsButton, OnControlsButtonClicked, nameof(OnControlsButtonClicked));
        SafeAddButtonListener(quitButton, OnQuitButtonClicked, nameof(OnQuitButtonClicked));
        SafeAddButtonListener(closeControlsButton, OnCloseControlsButtonClicked, nameof(OnCloseControlsButtonClicked));
        SafeAddButtonListener(resumeButton, OnResumeButtonClicked, nameof(OnResumeButtonClicked));
        SafeAddButtonListener(pauseRestartButton, OnRestartButtonClicked, nameof(OnRestartButtonClicked));
        SafeAddButtonListener(pauseMenuButton, OnMenuButtonClicked, nameof(OnMenuButtonClicked));
        SafeAddButtonListener(gameOverRestartButton, OnRestartButtonClicked, nameof(OnRestartButtonClicked));
        SafeAddButtonListener(gameOverMenuButton, OnMenuButtonClicked, nameof(OnMenuButtonClicked));
        SafeAddButtonListener(victoryContinueButton, OnContinueButtonClicked, nameof(OnContinueButtonClicked));
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        // M4 live HUD: distance follows camera, warning/arena follow game state.
        // Zero-GC: no allocations, straightforward polling with null guards.
        var cam = ScrollingCameraController.Instance;
        if (cam != null)
        {
            UpdateDistance(cam.DistanceTravelled);
        }
        var gm = GameManager.Instance;
        if (gm != null)
        {
            ShowBossWarning(ShouldShowBossWarning(gm.CurrentScore, gm.NextBossScoreThreshold));
        }
        // Arena banner follows camera lock; MapManager encounter is authoritative when present.
        var map = MapManager.Instance;
        if (map != null)
        {
            ShowBossArenaStatus(map.IsBossEncounterActive);
        }
        else if (cam != null)
        {
            ShowBossArenaStatus(cam.isScrollLocked);
        }
        // Flashing warning telegraph (unscaled time so it pulses even if timeScale changes).
        if (bossWarningBanner != null && bossWarningBanner.activeSelf && bossWarningText != null)
        {
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * bossWarningFlashFrequency * Mathf.PI);
            var c = bossWarningText.color;
            c.a = pulse;
            bossWarningText.color = c;
        }
    }

    private void SafeAddButtonListener(Button btn, UnityEngine.Events.UnityAction action, string methodName)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentMethodName(i) == methodName)
            {
                return;
            }
        }
        btn.onClick.AddListener(action);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        BossController.OnBossSpawned -= HandleBossSpawned;
        BossController.OnBossHealthChanged -= HandleBossHealthChanged;
        BossController.OnBossDefeatedEvent -= HandleBossDefeated;

        if (_subscribedPlayerHealth != null)
        {
            _subscribedPlayerHealth.OnHealthChanged -= UpdateHearts;
            _subscribedPlayerHealth = null;
        }

        if (_subscribedThrower != null)
        {
            _subscribedThrower.OnGrenadeCountChanged -= UpdateGrenades;
            _subscribedThrower = null;
        }
    }

    private void LoadHeartSpritesIfMissing()
    {
#if UNITY_EDITOR
        if (fullHeartSprite == null)
        {
            fullHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny RPG Forest/Artwork/sprites/misc/hearts/hearts-1.png");
        }
        if (emptyHeartSprite == null)
        {
            emptyHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny RPG Forest/Artwork/sprites/misc/hearts/hearts-2.png");
        }
#endif
    }

    /// <summary>
    /// Searches active scene for player components and hooks health/grenade events.
    /// </summary>
    public void HookSceneEntities()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            if (_subscribedPlayerHealth != null && _subscribedPlayerHealth != playerHealth)
            {
                _subscribedPlayerHealth.OnHealthChanged -= UpdateHearts;
            }
            playerHealth.OnHealthChanged -= UpdateHearts;
            playerHealth.OnHealthChanged += UpdateHearts;
            _subscribedPlayerHealth = playerHealth;
            UpdateHearts(playerHealth.currentHealth);
        }

        var thrower = FindObjectOfType<GrenadeThrower>();
        if (thrower != null)
        {
            if (_subscribedThrower != null && _subscribedThrower != thrower)
            {
                _subscribedThrower.OnGrenadeCountChanged -= UpdateGrenades;
            }
            thrower.OnGrenadeCountChanged -= UpdateGrenades;
            thrower.OnGrenadeCountChanged += UpdateGrenades;
            _subscribedThrower = thrower;
            UpdateGrenades(thrower.GrenadeCount);
        }
    }

    #region HUD Updates

    /// <summary>
    /// Updates the 5 heart icons to show full vs empty hearts based on current player health.
    /// </summary>
    public void UpdateHearts(int currentHealth)
    {
        if (heartIcons == null) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] == null) continue;

            if (i < currentHealth)
            {
                if (fullHeartSprite != null) heartIcons[i].sprite = fullHeartSprite;
                heartIcons[i].enabled = true;
            }
            else
            {
                if (emptyHeartSprite != null)
                {
                    heartIcons[i].sprite = emptyHeartSprite;
                    heartIcons[i].enabled = true;
                }
                else
                {
                    heartIcons[i].enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Formats score text with standard label and 5-digit padding: 'SCORE: 00120'.
    /// </summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {Mathf.Max(0, score):D5}";
        }
    }

    /// <summary>
    /// Formats high score text with standard label and 5-digit padding: 'HIGH: 00500'.
    /// </summary>
    public void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"HIGH: {Mathf.Max(0, highScore):D5}";
        }
    }

    /// <summary>
    /// Formats grenade inventory indicator text: 'x N'.
    /// </summary>
    public void UpdateGrenades(int count)
    {
        if (grenadeCountText != null)
        {
            grenadeCountText.text = $"x {Mathf.Max(0, count)}";
        }
    }

    /// <summary>
    /// Toggles the Boss Health Bar visibility.
    /// </summary>
    public void SetBossBarVisible(bool visible)
    {
        if (bossBarContainer != null)
        {
            bossBarContainer.SetActive(visible);
        }
        else if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Updates the boss health bar slider value between 0.0 and 1.0.
    /// </summary>
    public void UpdateBossHealth(int current, int max)
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = max > 0 ? max : 60;
            bossHealthSlider.value = Mathf.Clamp(current, 0, max);
        }
    }

    public void UpdateBossHealth(float ratio)
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.minValue = 0f;
            bossHealthSlider.maxValue = 1f;
            bossHealthSlider.value = Mathf.Clamp01(ratio);
        }
    }

    /// <summary>
    /// Formats travelled distance as DIST: 0000m (4-digit zero-padded, no truncation above 9999).
    /// </summary>
    public static string FormatDistance(float meters)
    {
        return $"DIST: {(int)Mathf.Max(0f, meters):D4}m";
    }

    /// <summary>
    /// Updates distance HUD indicator from travelled meters.
    /// </summary>
    public void UpdateDistance(float meters)
    {
        if (distanceText != null)
        {
            distanceText.text = FormatDistance(meters);
        }
    }

    /// <summary>
    /// Early telegraph predicate: score in [threshold-50, threshold).
    /// Defaults match acceptance: 450 &lt;= score &lt; 500.
    /// </summary>
    public bool ShouldShowBossWarning(int score, int nextThreshold)
    {
        int warnAt = nextThreshold - (int)bossWarningThresholdOffset;
        return score >= warnAt && score < nextThreshold;
    }

    /// <summary>
    /// Shows/hides BOSS APPROACHING! early warning banner (flashing handled in Update).
    /// </summary>
    public void ShowBossWarning(bool show)
    {
        if (bossWarningText != null && bossWarningText.text != "BOSS APPROACHING!")
        {
            bossWarningText.text = "BOSS APPROACHING!";
        }
        if (bossWarningBanner != null)
        {
            bossWarningBanner.SetActive(show);
        }
        else if (bossWarningText != null)
        {
            bossWarningText.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// Shows/hides BOSS ARENA status banner while camera is locked in encounter.
    /// </summary>
    public void ShowBossArenaStatus(bool show)
    {
        if (bossArenaBannerText != null && bossArenaBannerText.text != "BOSS ARENA")
        {
            bossArenaBannerText.text = "BOSS ARENA";
        }
        if (bossArenaBanner != null)
        {
            bossArenaBanner.SetActive(show);
        }
        else if (bossArenaBannerText != null)
        {
            bossArenaBannerText.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// Runtime self-heal: finds scene references or creates minimal HUD objects if missing.
    /// Guarantees zero missing-reference errors across environments.
    /// </summary>
    public void EnsureM4HUD()
    {
        if (distanceText == null)
        {
            var found = GameObject.Find("Canvas/HUD/DistanceText");
            if (found != null) distanceText = found.GetComponent<Text>();
        }
        if (bossWarningText == null)
        {
            var found = GameObject.Find("Canvas/HUD/BossWarningBanner");
            if (found != null) bossWarningText = found.GetComponent<Text>();
        }
        if (bossWarningBanner == null && bossWarningText != null)
        {
            bossWarningBanner = bossWarningText.gameObject;
        }
        if (bossArenaBannerText == null)
        {
            var found = GameObject.Find("Canvas/HUD/BossArenaBanner");
            if (found != null) bossArenaBannerText = found.GetComponent<Text>();
        }
        if (bossArenaBanner == null && bossArenaBannerText != null)
        {
            bossArenaBanner = bossArenaBannerText.gameObject;
        }
    }

    /// <summary>
    /// Self-healing: rebuilds the GameOver/Victory end screens if their objects
    /// are missing (destroyed Unity nulls fail the == null check). Rebuilt panels
    /// copy PausePanel styling; clicks are wired at runtime by WireButtons().
    /// Build-safe: fonts come from existing labels, no editor-only APIs.
    /// </summary>
    public void EnsureBattlePanels()
    {
        if (gameOverPanel == null)
        {
            BuildGameOverPanel();
        }
        if (victoryPanel == null)
        {
            BuildVictoryPanel();
        }
    }

    private Font PanelFont()
    {
        var t = GetComponentInChildren<Text>(true);
        if (t != null && t.font != null) return t.font;
        var pauseTitle = transform.Find("PausePanel/PauseTitle")?.GetComponent<Text>();
        if (pauseTitle != null && pauseTitle.font != null) return pauseTitle.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private GameObject MakeFullscreenPanel(string name)
    {
        var p = new GameObject(name);
        p.transform.SetParent(transform, false);
        var rt = p.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var im = p.AddComponent<Image>();
        im.color = new Color(0f, 0f, 0f, 0.6f);
        p.SetActive(false);
        return p;
    }

    private Text MakeBannerText(Transform parent, string name, string text, int fontSize, Color color, Vector2 anchored, Vector2 size)
    {
        var t = new GameObject(name);
        t.transform.SetParent(parent, false);
        var rt = t.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchored;
        var tx = t.AddComponent<Text>();
        tx.font = PanelFont();
        tx.fontSize = fontSize;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = color;
        tx.text = text;
        var ol = t.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.8f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return tx;
    }

    private Button MakePanelButton(Transform parent, string name, string label, Color color, Vector2 anchored)
    {
        // Clone the PausePanel button template for identical styling, then wipe
        // its listeners: WireButtons() adds the correct runtime ones at Start.
        var template = transform.Find("PausePanel/PauseRestartButton")?.gameObject;
        GameObject t = template != null
            ? Instantiate(template, parent, false)
            : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        if (t.transform.parent != parent) t.transform.SetParent(parent, false);
        t.name = name;
        var rt = t.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = anchored;
        var img = t.GetComponent<Image>();
        if (img != null) img.color = color;
        var lt = t.GetComponentInChildren<Text>();
        if (lt != null) lt.text = label;
        var btn = t.GetComponent<Button>();
        if (btn != null) btn.onClick = new Button.ButtonClickedEvent();
        return btn;
    }

    private void BuildGameOverPanel()
    {
        var p = MakeFullscreenPanel("GameOverPanel");
        MakeBannerText(p.transform, "GameOverTitle", "GAME OVER", 50, Color.red, new Vector2(0f, 150f), new Vector2(500f, 70f));
        finalScoreText = MakeBannerText(p.transform, "FinalScoreText", "FINAL SCORE: 0", 30, Color.white, new Vector2(0f, 60f), new Vector2(500f, 45f));
        recordScoreText = MakeBannerText(p.transform, "RecordScoreText", "RECORD: 0", 28, Color.yellow, new Vector2(0f, 10f), new Vector2(500f, 40f));
        gameOverRestartButton = MakePanelButton(p.transform, "GameOverRestartButton", "PLAY AGAIN", new Color(0.2f, 0.7f, 0.3f), new Vector2(0f, -70f));
        gameOverMenuButton = MakePanelButton(p.transform, "GameOverMenuButton", "MAIN MENU", new Color(0.3f, 0.45f, 0.7f), new Vector2(0f, -140f));
        gameOverPanel = p;
        WireButtons();
    }

    private void BuildVictoryPanel()
    {
        var p = MakeFullscreenPanel("VictoryPanel");
        victoryBannerText = MakeBannerText(p.transform, "VictoryBannerText", "BOSS SLAIN! +500 PTS", 44, Color.yellow, new Vector2(0f, 80f), new Vector2(700f, 60f));
        victoryContinueButton = MakePanelButton(p.transform, "VictoryContinueButton", "CONTINUE", new Color(0.2f, 0.7f, 0.3f), new Vector2(0f, -40f));
        victoryPanel = p;
        WireButtons();
    }

    #endregion

    #region Boss Event Callbacks

    private void HandleBossSpawned(BossController boss)
    {
        SetBossBarVisible(true);
        ShowBossWarning(false);
        ShowBossArenaStatus(true);
        if (boss != null)
        {
            UpdateBossHealth(boss.currentHealth, boss.maxHealth);
        }
    }

    private void HandleBossHealthChanged(int current, int max)
    {
        UpdateBossHealth(current, max);
    }

    private void HandleBossDefeated()
    {
        SetBossBarVisible(false);
        ShowBossWarning(false);
        ShowBossArenaStatus(false);
    }

    #endregion

    #region Panel Display & Navigation

    public void ShowMainMenu(bool show)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(show);
        if (show && controlsModal != null) controlsModal.SetActive(false);
    }

    public void ShowPause(bool show)
    {
        if (pausePanel != null) pausePanel.SetActive(show);
    }

    public void ShowGameOver(int finalScore, int recordScore)
    {
        EnsureBattlePanels();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (finalScoreText != null)
        {
            finalScoreText.text = $"FINAL SCORE: {finalScore}";
        }
        if (recordScoreText != null)
        {
            recordScoreText.text = $"RECORD: {recordScore}";
        }
    }

    public void ShowVictory()
    {
        EnsureBattlePanels();
        if (victoryPanel != null) victoryPanel.SetActive(true);

        if (victoryBannerText != null)
        {
            victoryBannerText.text = "BOSS SLAIN! +500 PTS";
        }
    }

    public void HideVictory()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (controlsModal != null) controlsModal.SetActive(false);
    }

    public void ToggleControlsModal()
    {
        if (controlsModal != null)
        {
            controlsModal.SetActive(!controlsModal.activeSelf);
        }
    }

    public void OpenControlsModal() => SetControlsModal(true);
    public void CloseControlsModal() => SetControlsModal(false);

    public void SetControlsModal(bool show)
    {
        if (controlsModal != null)
        {
            controlsModal.SetActive(show);
        }
    }

    #endregion

    #region Button Wire Handlers

    public void OnPlayButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Time.timeScale = 1.0f;
            ShowMainMenu(false);
        }
    }

    public void OnControlsButtonClicked()
    {
        OpenControlsModal();
    }

    public void OnCloseControlsButtonClicked()
    {
        CloseControlsModal();
    }

    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        Debug.Log("[UIManager] Application.Quit() requested.");
#else
        Application.Quit();
#endif
    }

    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame(false);
        }
        else
        {
            Time.timeScale = 1.0f;
            ShowPause(false);
        }
    }

    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Time.timeScale = 1.0f;
            HideAllPanels();
        }
    }

    public void OnMenuButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            Time.timeScale = 1.0f;
            HideAllPanels();
            ShowMainMenu(true);
        }
    }

    public void OnContinueButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeEndlessAfterBoss();
        }
        else
        {
            Time.timeScale = 1.0f;
            HideVictory();
        }
    }

    #endregion
}
