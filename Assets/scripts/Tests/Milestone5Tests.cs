using System;
using UnityEngine;
using UnityEngine.UI;
using E2ETests;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests
{
    /// <summary>
    /// Comprehensive test suite validating Milestone 5 specifications:
    /// - Core Game Loop State Machine & Transitions
    /// - Score Tracking & Persistent High Score via PlayerPrefs
    /// - HUD Formatting (5 Hearts, Score, High Score, Grenade Count, Boss Slider)
    /// - Panel Activation & Modal Navigation (MainMenu, Controls, Pause, GameOver, Victory)
    /// - Procedural 8-bit Audio Generation (Shoot, Explosion, Hit, Hurt, Pickup, GameOver, Victory)
    /// </summary>
    public static class Milestone5Tests
    {
        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Milestone 5 Test Suite - UI, Game Loop & Audio" };

            // TEST 1: GameManager Singleton & Initial State
            TestRunnerHelper.RunTest(report, "M5-01", "F25", 1,
                "GameManager Singleton & Default State",
                "Verifies GameManager instance exists and default state is Playing or MainMenu.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("GM_Test");
                        var gm = go.AddComponent<GameManager>();

                        E2EAssert.IsNotNull(GameManager.Instance, "GameManager.Instance singleton should be populated");
                        E2EAssert.IsTrue(gm.CurrentState == GameState.Playing || gm.CurrentState == GameState.MainMenu,
                            "Initial state should be Playing or MainMenu");
                        E2EAssert.AreEqual(gm.CurrentState.ToString(), gm.State, "State property should match CurrentState.ToString()");
                    }
                });

            // TEST 2: High Score PlayerPrefs Persistence
            TestRunnerHelper.RunTest(report, "M5-02", "F26", 1,
                "High Score Persistence via PlayerPrefs",
                "Verifies AddScore updates HighScore and writes to PlayerPrefs key 'HighScore'.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("GM_Score");
                            var gm = go.AddComponent<GameManager>();

                            gm.AddScore(250);
                            E2EAssert.AreEqual(250, gm.CurrentScore, "CurrentScore should be 250");
                            E2EAssert.IsTrue(gm.HighScore >= 250, "HighScore should reflect at least 250");

                            int stored = PlayerPrefs.GetInt("HighScore", 0);
                            E2EAssert.IsTrue(stored >= 250, "PlayerPrefs 'HighScore' key should be updated");
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 3: Lower Score Does Not Overwrite High Score
            TestRunnerHelper.RunTest(report, "M5-03", "F26", 2,
                "Lower Score Does Not Overwrite High Score",
                "Verifies lower score session does not downgrade an existing higher score record.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 800);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("GM_LowScore");
                            var gm = go.AddComponent<GameManager>();
                            gm.ReloadHighScore();

                            gm.AddScore(150);
                            E2EAssert.AreEqual(150, gm.CurrentScore);
                            E2EAssert.AreEqual(800, gm.HighScore, "HighScore should remain 800");
                            E2EAssert.AreEqual(800, PlayerPrefs.GetInt("HighScore", 0));
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 4: PauseGame State & TimeScale Manipulation
            TestRunnerHelper.RunTest(report, "M5-04", "F28", 1,
                "PauseGame State & TimeScale Freeze",
                "Verifies PauseGame(true) sets timeScale to 0f and PauseGame(false) restores to 1f.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("GM_Pause");
                            var gm = go.AddComponent<GameManager>();

                            gm.PauseGame(true);
                            E2EAssert.AreEqual(GameState.Paused, gm.CurrentState, "State should be Paused");
                            E2EAssert.AreEqual(0f, Time.timeScale, "Time.timeScale should be 0f when paused");

                            gm.PauseGame(false);
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "State should be Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale should be 1f when resumed");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 5: TriggerGameOver Sequence
            TestRunnerHelper.RunTest(report, "M5-05", "F29", 1,
                "TriggerGameOver Transition & Panel Activation",
                "Verifies TriggerGameOver sets State to GameOver, freezes timeScale, and activates GameOver panel.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_Test");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var goPanel = ctx.CreateGameObject("GameOverPanel");
                            goPanel.transform.SetParent(canvasGo.transform);
                            ui.gameOverPanel = goPanel;
                            goPanel.SetActive(false);

                            var gmGo = ctx.CreateGameObject("GM_GameOver");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.UIManagerRef = ui;

                            gm.TriggerGameOver();
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "State must be GameOver");
                            E2EAssert.AreEqual(0f, Time.timeScale, "Time.timeScale must be 0f on Game Over");
                            E2EAssert.IsTrue(goPanel.activeSelf, "GameOver panel must be activated");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 6: TriggerVictory Sequence
            TestRunnerHelper.RunTest(report, "M5-06", "F30", 1,
                "TriggerVictory Transition & Banner Display",
                "Verifies TriggerVictory sets State to VictoryContinues, freezes timeScale, and shows Victory panel.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_Victory");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var victPanel = ctx.CreateGameObject("VictoryPanel");
                            victPanel.transform.SetParent(canvasGo.transform);
                            ui.victoryPanel = victPanel;
                            victPanel.SetActive(false);

                            var gmGo = ctx.CreateGameObject("GM_Victory");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.UIManagerRef = ui;

                            gm.TriggerVictory();
                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState, "State must be VictoryContinues");
                            E2EAssert.AreEqual(0f, Time.timeScale, "Time.timeScale must be 0f on victory modal");
                            E2EAssert.IsTrue(victPanel.activeSelf, "Victory panel must be activated");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 7: ResumeEndlessAfterBoss Continuation
            TestRunnerHelper.RunTest(report, "M5-07", "F30", 1,
                "ResumeEndlessAfterBoss Continuation",
                "Verifies ResumeEndlessAfterBoss hides victory panel, restores timeScale=1f, and sets State=Playing.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_Cont");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var victPanel = ctx.CreateGameObject("VictoryPanel");
                            victPanel.transform.SetParent(canvasGo.transform);
                            ui.victoryPanel = victPanel;
                            victPanel.SetActive(true);

                            var gmGo = ctx.CreateGameObject("GM_Cont");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.UIManagerRef = ui;
                            gm.TriggerVictory();

                            gm.ResumeEndlessAfterBoss();
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "State should return to Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale should be restored to 1.0f");
                            E2EAssert.IsFalse(victPanel.activeSelf, "Victory panel should be hidden");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 8: RestartGame Full Reset
            TestRunnerHelper.RunTest(report, "M5-08", "F29", 1,
                "RestartGame Score & Panel Reset",
                "Verifies RestartGame resets score to 0, restores timeScale to 1.0f, and hides active panels.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_Reset");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var goPanel = ctx.CreateGameObject("GameOverPanel");
                            ui.gameOverPanel = goPanel;
                            goPanel.SetActive(true);

                            var gmGo = ctx.CreateGameObject("GM_Reset");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.UIManagerRef = ui;
                            gm.AddScore(450);
                            gm.TriggerGameOver();

                            gm.RestartGame();
                            E2EAssert.AreEqual(0, gm.CurrentScore, "CurrentScore must be reset to 0");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must be restored to 1.0f");
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "State must be Playing");
                            E2EAssert.IsFalse(goPanel.activeSelf, "GameOver panel must be closed");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 9: UIManager 5 Heart Icons Display
            TestRunnerHelper.RunTest(report, "M5-09", "F25", 1,
                "UIManager 5 Heart Icons Update",
                "Verifies UpdateHearts accurately switches full/empty sprites for 0-5 HP.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_Hearts");
                        var ui = canvasGo.AddComponent<UIManager>();
                        ui.heartIcons = new Image[5];

                        var fullSprite = Sprite.Create(new Texture2D(16, 16), new Rect(0, 0, 16, 16), Vector2.zero);
                        var emptySprite = Sprite.Create(new Texture2D(16, 16), new Rect(0, 0, 16, 16), Vector2.zero);
                        ui.fullHeartSprite = fullSprite;
                        ui.emptyHeartSprite = emptySprite;

                        for (int i = 0; i < 5; i++)
                        {
                            var hGo = ctx.CreateGameObject($"Heart_{i}");
                            ui.heartIcons[i] = hGo.AddComponent<Image>();
                        }

                        // Test with 3 HP: indices 0, 1, 2 should be full; 3, 4 should be empty
                        ui.UpdateHearts(3);
                        E2EAssert.AreEqual(fullSprite, ui.heartIcons[0].sprite);
                        E2EAssert.AreEqual(fullSprite, ui.heartIcons[1].sprite);
                        E2EAssert.AreEqual(fullSprite, ui.heartIcons[2].sprite);
                        E2EAssert.AreEqual(emptySprite, ui.heartIcons[3].sprite);
                        E2EAssert.AreEqual(emptySprite, ui.heartIcons[4].sprite);

                        // Test with 0 HP: all 5 empty
                        ui.UpdateHearts(0);
                        for (int i = 0; i < 5; i++)
                        {
                            E2EAssert.AreEqual(emptySprite, ui.heartIcons[i].sprite);
                        }
                    }
                });

            // TEST 10: Score, High Score & Grenade Text Formatting
            TestRunnerHelper.RunTest(report, "M5-10", "F25", 1,
                "HUD Text Formatting (Score, High, Grenades)",
                "Verifies exact text formatting strings: 'SCORE: 00120', 'HIGH: 00500', 'x 3'.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_Text");
                        var ui = canvasGo.AddComponent<UIManager>();

                        var scoreGo = ctx.CreateGameObject("ScoreText");
                        ui.scoreText = scoreGo.AddComponent<Text>();

                        var highGo = ctx.CreateGameObject("HighText");
                        ui.highScoreText = highGo.AddComponent<Text>();

                        var grenGo = ctx.CreateGameObject("GrenText");
                        ui.grenadeCountText = grenGo.AddComponent<Text>();

                        ui.UpdateScore(120);
                        E2EAssert.AreEqual("SCORE: 00120", ui.scoreText.text);

                        ui.UpdateHighScore(500);
                        E2EAssert.AreEqual("HIGH: 00500", ui.highScoreText.text);

                        ui.UpdateGrenades(3);
                        E2EAssert.AreEqual("x 3", ui.grenadeCountText.text);
                    }
                });

            // TEST 11: Boss Health Bar Visibility & Slider Update
            TestRunnerHelper.RunTest(report, "M5-11", "F21", 1,
                "Boss Health Bar Slider Controls",
                "Verifies SetBossBarVisible toggles slider container and UpdateBossHealth updates value.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_Boss");
                        var ui = canvasGo.AddComponent<UIManager>();

                        var container = ctx.CreateGameObject("BossContainer");
                        var sliderGo = ctx.CreateGameObject("Slider");
                        sliderGo.transform.SetParent(container.transform);
                        var slider = sliderGo.AddComponent<Slider>();

                        ui.bossBarContainer = container;
                        ui.bossHealthSlider = slider;

                        ui.SetBossBarVisible(false);
                        E2EAssert.IsFalse(container.activeSelf);

                        ui.SetBossBarVisible(true);
                        E2EAssert.IsTrue(container.activeSelf);

                        ui.UpdateBossHealth(45, 60);
                        E2EAssert.AreEqual(60f, slider.maxValue);
                        E2EAssert.AreEqual(45f, slider.value);
                    }
                });

            // TEST 12: SoundManager Singleton & Procedural 8-bit Audio Generation
            TestRunnerHelper.RunTest(report, "M5-12", "F34", 1,
                "SoundManager Procedural Audio Synthesis",
                "Verifies SoundManager synthesizes procedural clips without external asset dependencies.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("SoundManager_Test");
                        var sm = go.AddComponent<SoundManager>();

                        E2EAssert.IsNotNull(SoundManager.Instance, "SoundManager singleton should be accessible");

                        AudioClip shoot = sm.CreateShootSFXClip();
                        E2EAssert.IsNotNull(shoot, "Shoot clip should be generated");
                        E2EAssert.IsTrue(shoot.length > 0f, "Shoot clip duration must be > 0");

                        AudioClip explosion = sm.CreateExplosionSFXClip();
                        E2EAssert.IsNotNull(explosion, "Explosion clip should be generated");
                        E2EAssert.IsTrue(explosion.length > 0f, "Explosion clip duration must be > 0");

                        AudioClip hit = sm.CreateHitSFXClip();
                        E2EAssert.IsNotNull(hit, "Hit clip should be generated");

                        AudioClip hurt = sm.CreateHurtSFXClip();
                        E2EAssert.IsNotNull(hurt, "Hurt clip should be generated");

                        AudioClip pickup = sm.CreatePickupSFXClip();
                        E2EAssert.IsNotNull(pickup, "Pickup clip should be generated");

                        AudioClip victory = sm.CreateVictorySFXClip();
                        E2EAssert.IsNotNull(victory, "Victory clip should be generated");

                        AudioClip gameover = sm.CreateGameOverSFXClip();
                        E2EAssert.IsNotNull(gameover, "GameOver clip should be generated");
                    }
                });

            // TEST 13: SoundManager Safe Playback Execution
            TestRunnerHelper.RunTest(report, "M5-13", "F34", 2,
                "SoundManager Safe Playback Methods Execution",
                "Verifies calling all Play SFX methods succeeds without throwing exceptions.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var go = ctx.CreateGameObject("SM_Playback");
                        var sm = go.AddComponent<SoundManager>();

                        sm.PlayShootSFX();
                        sm.PlayHitSFX();
                        sm.PlayExplosionSFX();
                        sm.PlayHurtSFX();
                        sm.PlayPickupSFX();
                        sm.PlayGameOverSFX();
                        sm.PlayVictorySFX();

                        E2EAssert.IsTrue(true, "All procedural audio playback methods completed cleanly");
                    }
                });

            // TEST 14: Controls Modal Toggle
            TestRunnerHelper.RunTest(report, "M5-14", "F27", 1,
                "Controls Modal Toggle Navigation",
                "Verifies ToggleControlsModal toggles visibility state cleanly.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_Modal");
                        var ui = canvasGo.AddComponent<UIManager>();
                        var modal = ctx.CreateGameObject("ControlsModal");
                        modal.SetActive(false);
                        ui.controlsModal = modal;

                        ui.ToggleControlsModal();
                        E2EAssert.IsTrue(modal.activeSelf, "Modal should become active on toggle");

                        ui.ToggleControlsModal();
                        E2EAssert.IsFalse(modal.activeSelf, "Modal should become inactive on second toggle");
                    }
                });

            // TEST 15: Scene Setup Verification (Hierarchy, Canvas, EventSystem, Managers)
            TestRunnerHelper.RunTest(report, "M5-15", "F25", 1,
                "Scene Hierarchy & Manager Components",
                "Verifies active scene contains Canvas, EventSystem, GameManager, and SoundManager.",
                () =>
                {
                    var gmObj = GameObject.Find("GameManager");
                    var smObj = GameObject.Find("SoundManager");
                    var canvasObj = GameObject.Find("Canvas");
                    var esObj = GameObject.Find("EventSystem");

                    E2EAssert.IsNotNull(gmObj, "GameManager GameObject should exist in scene");
                    E2EAssert.IsNotNull(smObj, "SoundManager GameObject should exist in scene");
                    E2EAssert.IsNotNull(canvasObj, "Canvas GameObject should exist in scene");
                    E2EAssert.IsNotNull(esObj, "EventSystem GameObject should exist in scene");

                    var ui = canvasObj.GetComponent<UIManager>();
                    E2EAssert.IsNotNull(ui, "UIManager component must be attached to Canvas");
                    E2EAssert.AreEqual(5, ui.heartIcons.Length, "Canvas UIManager must reference 5 heart icons");
                });

            // TEST 16: Clean Scene Objects Verification (Defect 1 Remediation)
            TestRunnerHelper.RunTest(report, "M5-16", "F25", 1,
                "Clean Scene Objects Verification",
                "Verifies active scene contains zero leaked test GameObjects (BossBullet, DummyBoss, etc.).",
                () =>
                {
                    var allObjs = GameObject.FindObjectsOfType<GameObject>();
                    int leaked = 0;
                    foreach (var go in allObjs)
                    {
                        if (go.name.Contains("Dummy") || go.name.Contains("Fallback") || go.name.Contains("BossBullet"))
                        {
                            leaked++;
                        }
                    }
                    E2EAssert.AreEqual(0, leaked, $"Scene must not contain leaked test objects, found {leaked}");
                });

            // TEST 17: Controls Modal Decoupling & Button Wiring (Defect 2 Remediation)
            TestRunnerHelper.RunTest(report, "M5-17", "F27", 1,
                "Controls Modal Open/Close Decoupling & Button Wiring",
                "Verifies OpenControlsModal and CloseControlsModal explicitly set visibility, and button clicks function properly.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_Modal_Decoupled");
                        var ui = canvasGo.AddComponent<UIManager>();
                        var modal = ctx.CreateGameObject("ControlsModal_Decoupled");
                        modal.SetActive(false);
                        ui.controlsModal = modal;

                        ui.OpenControlsModal();
                        E2EAssert.IsTrue(modal.activeSelf, "Modal should be active after OpenControlsModal");
                        ui.OpenControlsModal();
                        E2EAssert.IsTrue(modal.activeSelf, "Modal should remain active after repeated OpenControlsModal");

                        ui.CloseControlsModal();
                        E2EAssert.IsFalse(modal.activeSelf, "Modal should be inactive after CloseControlsModal");
                        ui.CloseControlsModal();
                        E2EAssert.IsFalse(modal.activeSelf, "Modal should remain inactive after repeated CloseControlsModal");
                    }
                });

            // TEST 18: Player Death to Menu to Play State Restoration (Defect 3 Remediation)
            TestRunnerHelper.RunTest(report, "M5-18", "F29", 1,
                "Player Death to Menu to Play State Restoration",
                "Verifies StartGame restores health, movement, and shooting if player died prior to entering play.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var gmGo = ctx.CreateGameObject("GM_M5_18");
                        var gm = gmGo.AddComponent<GameManager>();

                        var playerGo = ctx.CreateGameObject("Player_M5_18");
                        var movement = playerGo.AddComponent<PlayerMovement>();
                        var shooting = playerGo.AddComponent<Shooting>();
                        var health = playerGo.AddComponent<PlayerHealth>();

                        // Inflict lethal damage
                        var isInvulProp = typeof(PlayerHealth).GetProperty("isInvulnerable");
                        for (int i = 0; i < 5; i++)
                        {
                            if (isInvulProp != null) isInvulProp.SetValue(health, false, null);
                            health.TakeDamage(1);
                        }

                        E2EAssert.AreEqual(0, health.currentHealth, "Player health should be 0 after lethal damage");
                        E2EAssert.IsFalse(movement.enabled, "PlayerMovement should be disabled on death");
                        E2EAssert.IsFalse(shooting.enabled, "Shooting should be disabled on death");

                        gm.ReturnToMainMenu();
                        E2EAssert.AreEqual(GameState.MainMenu, gm.CurrentState, "State should be MainMenu");

                        gm.StartGame();
                        E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "State should be Playing");
                        E2EAssert.AreEqual(5, health.currentHealth, "Player health must be restored to 5 on StartGame");
                        E2EAssert.IsTrue(movement.enabled, "PlayerMovement must be re-enabled on StartGame");
                        E2EAssert.IsTrue(shooting.enabled, "Shooting must be re-enabled on StartGame");
                        E2EAssert.AreEqual(0, gm.CurrentScore, "Score must be reset to 0 on new game session");
                    }
                });

            // TEST 19: Event Subscription Idempotence & Leak Prevention (Defect 4 Remediation)
            TestRunnerHelper.RunTest(report, "M5-19", "F25", 2,
                "Event Subscription Idempotence & Leak Prevention",
                "Verifies repeated HookSceneEntities calls do not accumulate duplicate delegate subscriptions.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_M5_19");
                        var ui = canvasGo.AddComponent<UIManager>();

                        var playerGo = ctx.CreateGameObject("Player_M5_19");
                        var health = playerGo.AddComponent<PlayerHealth>();
                        var thrower = playerGo.AddComponent<GrenadeThrower>();

                        for (int i = 0; i < 10; i++)
                        {
                            ui.HookSceneEntities();
                        }

                        var fHealth = typeof(PlayerHealth).GetField("OnHealthChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var healthDelegates = ((System.MulticastDelegate)fHealth.GetValue(health))?.GetInvocationList();
                        E2EAssert.AreEqual(1, healthDelegates?.Length ?? 0, "PlayerHealth.OnHealthChanged should have exactly 1 delegate subscription after repeated hooks");

                        var fThrower = typeof(GrenadeThrower).GetField("OnGrenadeCountChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var throwerDelegates = ((System.MulticastDelegate)fThrower.GetValue(thrower))?.GetInvocationList();
                        E2EAssert.AreEqual(1, throwerDelegates?.Length ?? 0, "GrenadeThrower.OnGrenadeCountChanged should have exactly 1 delegate subscription after repeated hooks");
                    }
                });

            return report;
        }

#if UNITY_EDITOR
        [MenuItem("E2E Tests/Run Milestone 5 Tests")]
        public static void RunMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
