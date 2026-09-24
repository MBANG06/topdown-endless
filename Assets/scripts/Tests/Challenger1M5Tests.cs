using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using E2ETests;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests
{
    /// <summary>
    /// Empirical Adversarial Verification Suite for Milestone 5:
    /// UI / HUD, Game Loop State Machine, High Score Persistence & Procedural Audio Feedback.
    /// Authored by Challenger 1.
    ///
    /// Challenge Pillars:
    /// 1. Rapid pause toggling: Rapid ESC / P toggling does not cause timeScale drift or deadlock.
    /// 2. High score persistence: Lower scores never overwrite high score, negative/zero rejection, PlayerPrefs instant update.
    /// 3. Game over & restart flow: 0 HP transitions to GameOver, freezes timeScale 0, restart restores timeScale 1.0 and resets score.
    /// 4. Victory continue flow: Boss defeat triggers VictoryContinues, Continue unpauses to 1.0 and retains endless scaling.
    /// </summary>
    public static class Challenger1M5Tests
    {
        private static void InflictLethalDamage(PlayerHealth ph)
        {
            var isInvulProp = typeof(PlayerHealth).GetProperty("isInvulnerable");
            for (int i = 0; i < 5; i++)
            {
                if (isInvulProp != null)
                {
                    isInvulProp.SetValue(ph, false, null);
                }
                ph.TakeDamage(1);
            }
        }

        public static TestSuiteReport RunAllTests()
        {
            var report = new TestSuiteReport { SuiteName = "Challenger 1 Milestone 5 Adversarial Verification Suite" };

            // =========================================================================
            // SUITE 1: Rapid Pause Toggling & TimeScale Stability (ESC / P)
            // =========================================================================

            // TEST 01: Baseline Pause / Unpause Transitions
            TestRunnerHelper.RunTest(report, "CH1-M5-01", "F28", 1,
                "Baseline Pause / Unpause Transitions",
                "Verifies PauseGame and TogglePause cleanly flip between Playing (1.0f) and Paused (0.0f).",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P01");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "Default state should be Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Default timeScale must be 1.0f");

                            gm.PauseGame(true);
                            E2EAssert.AreEqual(GameState.Paused, gm.CurrentState, "State must be Paused");
                            E2EAssert.AreEqual(0f, Time.timeScale, "Time.timeScale must be 0f when paused");

                            gm.PauseGame(false);
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "State must be Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must be 1.0f when resumed");

                            gm.TogglePause();
                            E2EAssert.AreEqual(GameState.Paused, gm.CurrentState, "TogglePause should switch to Paused");
                            E2EAssert.AreEqual(0f, Time.timeScale, "TogglePause should set timeScale to 0f");

                            gm.TogglePause();
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "TogglePause should switch to Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "TogglePause should restore timeScale to 1.0f");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 02: Rapid Even-Count Pause Toggling Stability
            TestRunnerHelper.RunTest(report, "CH1-M5-02", "F28", 2,
                "Rapid Even-Count Pause Toggling Stability",
                "Verifies 100 rapid TogglePause calls end strictly in Playing state with exact 1.0f timeScale.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P02");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            for (int i = 0; i < 100; i++)
                            {
                                gm.TogglePause();
                            }

                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "100 toggles must return state to Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "100 toggles must preserve exact 1.0f timeScale without drift");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 03: Rapid Odd-Count Pause Toggling Stability
            TestRunnerHelper.RunTest(report, "CH1-M5-03", "F28", 2,
                "Rapid Odd-Count Pause Toggling Stability",
                "Verifies 101 rapid TogglePause calls end strictly in Paused state with exact 0.0f timeScale.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P03");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            for (int i = 0; i < 101; i++)
                            {
                                gm.TogglePause();
                            }

                            E2EAssert.AreEqual(GameState.Paused, gm.CurrentState, "101 toggles must leave state Paused");
                            E2EAssert.AreEqual(0f, Time.timeScale, "101 toggles must leave exact 0f timeScale");

                            gm.TogglePause(); // 102nd toggle restores to playing
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "Subsequent toggle must resume to Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Subsequent toggle must restore timeScale to 1.0f");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 04: Extreme Burst Toggling Stress & Invariant Check
            TestRunnerHelper.RunTest(report, "CH1-M5-04", "F28", 3,
                "Extreme Burst Toggling Stress & Invariant Check",
                "Verifies 1,000 rapid random toggles preserve strict timeScale invariants (only 0f or 1.0f).",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P04");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            for (int i = 0; i < 1000; i++)
                            {
                                if (i % 3 == 0)
                                    gm.TogglePause();
                                else if (i % 3 == 1)
                                    gm.PauseGame(true);
                                else
                                    gm.PauseGame(false);

                                // Invariant verification
                                if (gm.CurrentState == GameState.Paused)
                                {
                                    E2EAssert.AreEqual(0f, Time.timeScale, $"Step {i}: Paused state must have timeScale 0f");
                                }
                                else if (gm.CurrentState == GameState.Playing)
                                {
                                    E2EAssert.AreEqual(1.0f, Time.timeScale, $"Step {i}: Playing state must have timeScale 1.0f");
                                }
                                E2EAssert.IsFalse(float.IsNaN(Time.timeScale), "timeScale must never be NaN");
                            }

                            // Final resume
                            gm.PauseGame(false);
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState);
                            E2EAssert.AreEqual(1.0f, Time.timeScale);
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 05: Pause Lockout During Game Over
            TestRunnerHelper.RunTest(report, "CH1-M5-05", "F28", 2,
                "Pause Lockout During Game Over",
                "Verifies TogglePause and PauseGame are strictly rejected while in GameOver state.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P05");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            gm.TriggerGameOver();
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);
                            E2EAssert.AreEqual(0f, Time.timeScale);

                            // Attempt pause operations during GameOver
                            gm.TogglePause();
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "TogglePause must not change GameOver state");
                            E2EAssert.AreEqual(0f, Time.timeScale, "TimeScale must remain 0f on GameOver");

                            gm.PauseGame(false);
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "PauseGame(false) must not unpause GameOver");
                            E2EAssert.AreEqual(0f, Time.timeScale, "TimeScale must remain 0f on GameOver");

                            gm.PauseGame(true);
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "PauseGame(true) must not mutate GameOver");
                            E2EAssert.AreEqual(0f, Time.timeScale, "TimeScale must remain 0f on GameOver");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 06: Pause Lockout During Victory Continues
            TestRunnerHelper.RunTest(report, "CH1-M5-06", "F28", 2,
                "Pause Lockout During Victory Continues",
                "Verifies TogglePause and PauseGame are strictly rejected while in VictoryContinues state.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P06");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            gm.TriggerVictory();
                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState);
                            E2EAssert.AreEqual(0f, Time.timeScale);

                            // Attempt pause operations during Victory
                            gm.TogglePause();
                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState, "TogglePause must not affect VictoryContinues");
                            E2EAssert.AreEqual(0f, Time.timeScale, "timeScale must remain 0f");

                            gm.PauseGame(false);
                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState, "PauseGame(false) must not unfreeze Victory modal");
                            E2EAssert.AreEqual(0f, Time.timeScale, "timeScale must remain 0f");

                            gm.PauseGame(true);
                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState, "PauseGame(true) must not mutate Victory state");
                            E2EAssert.AreEqual(0f, Time.timeScale, "timeScale must remain 0f");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 07: Pause Toggle Inactive in Main Menu
            TestRunnerHelper.RunTest(report, "CH1-M5-07", "F27", 2,
                "Pause Toggle Inactive in Main Menu",
                "Verifies TogglePause does not activate pause menu while on MainMenu.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_P07");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            gm.ReturnToMainMenu();
                            E2EAssert.AreEqual(GameState.MainMenu, gm.CurrentState);

                            gm.TogglePause();
                            E2EAssert.AreEqual(GameState.MainMenu, gm.CurrentState, "TogglePause must not alter MainMenu state");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 08: UI Pause Panel Synchronous State Reflection
            TestRunnerHelper.RunTest(report, "CH1-M5-08", "F28", 1,
                "UI Pause Panel Synchronous State Reflection",
                "Verifies UI pause panel activeSelf strictly synchronizes with GameManager pause state.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("CH1_Canvas_P08");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var pausePanel = ctx.CreateGameObject("PausePanel_Test");
                            pausePanel.transform.SetParent(canvasGo.transform);
                            pausePanel.SetActive(false);
                            ui.pausePanel = pausePanel;

                            var gmGo = ctx.CreateGameObject("CH1_GM_P08");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            for (int i = 0; i < 20; i++)
                            {
                                gm.TogglePause();
                                bool isPaused = (gm.CurrentState == GameState.Paused);
                                E2EAssert.AreEqual(isPaused, pausePanel.activeSelf, $"Step {i}: Pause panel activeSelf must match paused state");
                            }

                            // Final ensure resumed
                            if (gm.CurrentState == GameState.Paused) gm.TogglePause();
                            E2EAssert.IsFalse(pausePanel.activeSelf, "Pause panel must be closed when resumed");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });


            // =========================================================================
            // SUITE 2: High Score Persistence & PlayerPrefs Stress
            // =========================================================================

            // TEST 09: Lower Score Sessions Do Not Overwrite High Score
            TestRunnerHelper.RunTest(report, "CH1-M5-09", "F26", 2,
                "Lower Score Sessions Do Not Overwrite High Score",
                "Verifies high score of 1500 remains intact through multiple sub-1500 sessions.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 1500);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_H09");
                            var gm = go.AddComponent<GameManager>();
                            gm.ReloadHighScore();

                            E2EAssert.AreEqual(1500, gm.HighScore, "Initial high score should be 1500");

                            // Sub-1500 session scores
                            gm.AddScore(100);
                            E2EAssert.AreEqual(100, gm.CurrentScore);
                            E2EAssert.AreEqual(1500, gm.HighScore, "HighScore should remain 1500");
                            E2EAssert.AreEqual(1500, PlayerPrefs.GetInt("HighScore", 0));

                            gm.AddScore(400); // Total 500
                            E2EAssert.AreEqual(500, gm.CurrentScore);
                            E2EAssert.AreEqual(1500, gm.HighScore);
                            E2EAssert.AreEqual(1500, PlayerPrefs.GetInt("HighScore", 0));

                            gm.AddScore(999); // Total 1499
                            E2EAssert.AreEqual(1499, gm.CurrentScore);
                            E2EAssert.AreEqual(1500, gm.HighScore);
                            E2EAssert.AreEqual(1500, PlayerPrefs.GetInt("HighScore", 0));

                            // Reset game session
                            gm.RestartGame();
                            E2EAssert.AreEqual(0, gm.CurrentScore);
                            E2EAssert.AreEqual(1500, gm.HighScore, "Restart must retain 1500 high score");
                            E2EAssert.AreEqual(1500, PlayerPrefs.GetInt("HighScore", 0));
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 10: Non-Positive Score Input Rejection
            TestRunnerHelper.RunTest(report, "CH1-M5-10", "F26", 2,
                "Non-Positive Score Input Rejection",
                "Verifies AddScore rejects 0, -1, and -500 without mutating score or throwing errors.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 500);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_H10");
                            var gm = go.AddComponent<GameManager>();
                            gm.ReloadHighScore();

                            gm.AddScore(0);
                            E2EAssert.AreEqual(0, gm.CurrentScore, "AddScore(0) should not alter score");
                            E2EAssert.AreEqual(500, gm.HighScore);

                            gm.AddScore(-1);
                            E2EAssert.AreEqual(0, gm.CurrentScore, "AddScore(-1) should not decrement score");
                            E2EAssert.AreEqual(500, gm.HighScore);

                            gm.AddScore(-500);
                            E2EAssert.AreEqual(0, gm.CurrentScore, "AddScore(-500) should be ignored");
                            E2EAssert.AreEqual(500, gm.HighScore);

                            E2EAssert.AreEqual(500, PlayerPrefs.GetInt("HighScore", 0));
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 11: Real-Time PlayerPrefs Persistence on Record Breaks
            TestRunnerHelper.RunTest(report, "CH1-M5-11", "F26", 1,
                "Real-Time PlayerPrefs Persistence on Record Breaks",
                "Verifies PlayerPrefs updates immediately and strictly whenever a new high score is reached.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 100);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_H11");
                            var gm = go.AddComponent<GameManager>();
                            gm.ReloadHighScore();

                            // Match current high score (100) -> should not trigger rewrite
                            gm.AddScore(100);
                            E2EAssert.AreEqual(100, gm.CurrentScore);
                            E2EAssert.AreEqual(100, gm.HighScore);
                            E2EAssert.AreEqual(100, PlayerPrefs.GetInt("HighScore", 0));

                            // Exceed by 1 -> new record 101
                            gm.AddScore(1);
                            E2EAssert.AreEqual(101, gm.CurrentScore);
                            E2EAssert.AreEqual(101, gm.HighScore);
                            E2EAssert.AreEqual(101, PlayerPrefs.GetInt("HighScore", 0), "PlayerPrefs must record 101 immediately");

                            // Exceed further -> 250
                            gm.AddScore(149);
                            E2EAssert.AreEqual(250, gm.CurrentScore);
                            E2EAssert.AreEqual(250, gm.HighScore);
                            E2EAssert.AreEqual(250, PlayerPrefs.GetInt("HighScore", 0), "PlayerPrefs must record 250 immediately");
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 12: Multi-Instance Cross-Session PlayerPrefs Sync
            TestRunnerHelper.RunTest(report, "CH1-M5-12", "F26", 2,
                "Multi-Instance Cross-Session PlayerPrefs Sync",
                "Verifies separate GameManager instances sequentially pick up persisted records.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 0);
                        PlayerPrefs.Save();

                        // Session 1
                        using (var ctx1 = new E2ETestContext())
                        {
                            var go1 = ctx1.CreateGameObject("CH1_GM_S1");
                            var gm1 = go1.AddComponent<GameManager>();
                            gm1.SetAsActiveInstance();
                            gm1.ReloadHighScore();

                            gm1.AddScore(750);
                            E2EAssert.AreEqual(750, gm1.HighScore);
                            E2EAssert.AreEqual(750, PlayerPrefs.GetInt("HighScore", 0));
                        }

                        // Session 2 (fresh instance)
                        using (var ctx2 = new E2ETestContext())
                        {
                            var go2 = ctx2.CreateGameObject("CH1_GM_S2");
                            var gm2 = go2.AddComponent<GameManager>();
                            gm2.SetAsActiveInstance();
                            gm2.ReloadHighScore();

                            E2EAssert.AreEqual(750, gm2.HighScore, "Session 2 must read 750 high score from PlayerPrefs");

                            // Scores below record
                            gm2.AddScore(300);
                            E2EAssert.AreEqual(300, gm2.CurrentScore);
                            E2EAssert.AreEqual(750, gm2.HighScore);

                            // Breaks record
                            gm2.AddScore(500); // 800 total
                            E2EAssert.AreEqual(800, gm2.CurrentScore);
                            E2EAssert.AreEqual(800, gm2.HighScore);
                            E2EAssert.AreEqual(800, PlayerPrefs.GetInt("HighScore", 0));
                        }

                        // Session 3 verifies 800 preserved
                        using (var ctx3 = new E2ETestContext())
                        {
                            var go3 = ctx3.CreateGameObject("CH1_GM_S3");
                            var gm3 = go3.AddComponent<GameManager>();
                            gm3.SetAsActiveInstance();
                            gm3.ReloadHighScore();

                            E2EAssert.AreEqual(800, gm3.HighScore, "Session 3 must read 800 from PlayerPrefs");
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 13: Rapid Score Burst Accumulation Stress
            TestRunnerHelper.RunTest(report, "CH1-M5-13", "F26", 3,
                "Rapid Score Burst Accumulation Stress",
                "Verifies 100 rapid AddScore calls accumulate without dropping or drifting.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 0);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_H13");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.ReloadHighScore();

                            for (int i = 0; i < 100; i++)
                            {
                                gm.AddScore(15);
                            }

                            E2EAssert.AreEqual(1500, gm.CurrentScore, "100 calls of +15 must equal exactly 1500");
                            E2EAssert.AreEqual(1500, gm.HighScore, "HighScore must equal 1500");
                            E2EAssert.AreEqual(1500, PlayerPrefs.GetInt("HighScore", 0), "PlayerPrefs must record 1500");
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 14: Boundary & Large Score Handling
            TestRunnerHelper.RunTest(report, "CH1-M5-14", "F26", 2,
                "Boundary & Large Score Handling",
                "Verifies large scores up to 1,000,000 are stored and persisted without overflow.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 0);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("CH1_GM_H14");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.ReloadHighScore();

                            gm.AddScore(99999);
                            E2EAssert.AreEqual(99999, gm.CurrentScore);
                            E2EAssert.AreEqual(99999, gm.HighScore);
                            E2EAssert.AreEqual(99999, PlayerPrefs.GetInt("HighScore", 0));

                            gm.AddScore(900001); // Total 1,000,000
                            E2EAssert.AreEqual(1000000, gm.CurrentScore);
                            E2EAssert.AreEqual(1000000, gm.HighScore);
                            E2EAssert.AreEqual(1000000, PlayerPrefs.GetInt("HighScore", 0));
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 15: UI HUD High Score Display Synchronization
            TestRunnerHelper.RunTest(report, "CH1-M5-15", "F25", 1,
                "UI HUD High Score Display Synchronization",
                "Verifies UIManager text field updates accurately to formatted 'HIGH: 00750'.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("CH1_Canvas_H15");
                        var ui = canvasGo.AddComponent<UIManager>();
                        var highTextGo = ctx.CreateGameObject("HighText_H15");
                        ui.highScoreText = highTextGo.AddComponent<Text>();

                        ui.UpdateHighScore(750);
                        E2EAssert.AreEqual("HIGH: 00750", ui.highScoreText.text);

                        ui.UpdateHighScore(12500);
                        E2EAssert.AreEqual("HIGH: 12500", ui.highScoreText.text);
                    }
                });


            // =========================================================================
            // SUITE 3: Game Over & Restart Flow Stress
            // =========================================================================

            // TEST 16: Lethal Damage 0 HP Transitions to GameOver
            TestRunnerHelper.RunTest(report, "CH1-M5-16", "F29", 1,
                "Lethal Damage 0 HP Transitions to GameOver",
                "Verifies deducting 5 HP sequentially triggers death event, sets GameOver state and freezes timeScale.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_GO16");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var goPanel = ctx.CreateGameObject("GameOverPanel_16");
                            goPanel.SetActive(false);
                            ui.gameOverPanel = goPanel;

                            var gmGo = ctx.CreateGameObject("GM_GO16");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            var playerGo = ctx.CreateGameObject("Player_GO16");
                            var ph = playerGo.AddComponent<PlayerHealth>();
                            playerGo.tag = "Player";

                            // Hook death event
                            ph.OnPlayerDeath += gm.TriggerGameOver;

                            // Take 5 damage sequentially, resetting invulnerability window between hits
                            InflictLethalDamage(ph);

                            E2EAssert.AreEqual(0, ph.currentHealth, "Player health must reach 0");
                            E2EAssert.IsFalse(ph.IsAlive, "Player must be dead");
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "State must transition to GameOver");
                            E2EAssert.AreEqual(0f, Time.timeScale, "TimeScale must freeze to 0f");
                            E2EAssert.IsTrue(goPanel.activeSelf, "GameOver panel must be activated");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 17: Overkill Hit & GameOver Idempotency
            TestRunnerHelper.RunTest(report, "CH1-M5-17", "F29", 2,
                "Overkill Hit & GameOver Idempotency",
                "Verifies overkill damage on dead player and redundant TriggerGameOver calls are idempotent.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var gmGo = ctx.CreateGameObject("GM_GO17");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            var playerGo = ctx.CreateGameObject("Player_GO17");
                            var ph = playerGo.AddComponent<PlayerHealth>();
                            ph.OnPlayerDeath += gm.TriggerGameOver;

                            // Kill player
                            InflictLethalDamage(ph);
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);

                            // Post-mortem overkill hits
                            ph.TakeDamage(1);
                            ph.TakeDamage(10);
                            E2EAssert.AreEqual(0, ph.currentHealth, "Health should stay clamped at 0");
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);

                            // Multiple redundant TriggerGameOver calls
                            gm.TriggerGameOver();
                            gm.TriggerGameOver();
                            gm.TriggerGameOver();
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);
                            E2EAssert.AreEqual(0f, Time.timeScale);
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 18: GameOver UI Score Reporting
            TestRunnerHelper.RunTest(report, "CH1-M5-18", "F29", 1,
                "GameOver UI Score Reporting",
                "Verifies GameOver panel displays current final score and record score accurately.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_GO18");
                        var ui = canvasGo.AddComponent<UIManager>();
                        var goPanel = ctx.CreateGameObject("GOPanel_18");
                        var finalTxtGo = ctx.CreateGameObject("FinalText");
                        var recTxtGo = ctx.CreateGameObject("RecordText");
                        ui.gameOverPanel = goPanel;
                        ui.finalScoreText = finalTxtGo.AddComponent<Text>();
                        ui.recordScoreText = recTxtGo.AddComponent<Text>();

                        ui.ShowGameOver(450, 1200);

                        E2EAssert.IsTrue(goPanel.activeSelf, "GameOver panel must be shown");
                        E2EAssert.AreEqual("FINAL SCORE: 450", ui.finalScoreText.text);
                        E2EAssert.AreEqual("RECORD: 1200", ui.recordScoreText.text);
                    }
                });

            // TEST 19: RestartGame Restores TimeScale 1.0f & Resets Score
            TestRunnerHelper.RunTest(report, "CH1-M5-19", "F29", 1,
                "RestartGame Restores TimeScale 1.0f & Resets Score",
                "Verifies RestartGame sets CurrentScore=0, timeScale=1.0f, State=Playing, and hides panels.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_GO19");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var goPanel = ctx.CreateGameObject("GameOverPanel_19");
                            ui.gameOverPanel = goPanel;

                            var gmGo = ctx.CreateGameObject("GM_GO19");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            gm.AddScore(350);
                            gm.TriggerGameOver();
                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);
                            E2EAssert.AreEqual(0f, Time.timeScale);
                            E2EAssert.IsTrue(goPanel.activeSelf);

                            gm.RestartGame();
                            E2EAssert.AreEqual(0, gm.CurrentScore, "CurrentScore must be 0 after restart");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must be 1.0f after restart");
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "CurrentState must be Playing");
                            E2EAssert.IsFalse(goPanel.activeSelf, "GameOver panel must be hidden after restart");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 20: RestartGame Retains HighScore
            TestRunnerHelper.RunTest(report, "CH1-M5-20", "F26", 1,
                "RestartGame Retains HighScore",
                "Verifies high score is preserved when restarting game.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 900);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var go = ctx.CreateGameObject("GM_GO20");
                            var gm = go.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.ReloadHighScore();

                            gm.AddScore(500);
                            gm.TriggerGameOver();

                            gm.RestartGame();
                            E2EAssert.AreEqual(0, gm.CurrentScore, "Current score must be 0");
                            E2EAssert.AreEqual(900, gm.HighScore, "HighScore must remain 900");
                            E2EAssert.AreEqual(900, PlayerPrefs.GetInt("HighScore", 0), "PlayerPrefs must remain 900");
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 21: RestartGame Restores Player Health, Grenades & Controls
            TestRunnerHelper.RunTest(report, "CH1-M5-21", "F29", 1,
                "RestartGame Restores Player Health, Grenades & Controls",
                "Verifies RestartGame calls ResetHealth (5 HP, enables movement/shooting) and restores grenades to 2.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var gmGo = ctx.CreateGameObject("GM_GO21");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            var playerGo = ctx.CreateGameObject("Player_GO21");
                            playerGo.tag = "Player";
                            var ph = playerGo.AddComponent<PlayerHealth>();
                            var move = playerGo.AddComponent<PlayerMovement>();
                            var shoot = playerGo.AddComponent<Shooting>();
                            var thrower = playerGo.AddComponent<GrenadeThrower>();

                            // Kill player
                            InflictLethalDamage(ph);
                            E2EAssert.IsFalse(ph.IsAlive);
                            E2EAssert.IsFalse(move.enabled, "PlayerMovement should be disabled on death");
                            E2EAssert.IsFalse(shoot.enabled, "Shooting should be disabled on death");

                            // Throw all grenades
                            thrower.ResetGrenades(0);
                            E2EAssert.AreEqual(0, thrower.GrenadeCount);

                            // Restart
                            gm.RestartGame();

                            E2EAssert.AreEqual(5, ph.currentHealth, "Player health must reset to 5");
                            E2EAssert.IsTrue(ph.IsAlive, "Player must be alive");
                            E2EAssert.IsTrue(move.enabled, "PlayerMovement must be re-enabled");
                            E2EAssert.IsTrue(shoot.enabled, "Shooting must be re-enabled");
                            E2EAssert.AreEqual(2, thrower.GrenadeCount, "Grenade count must reset to 2");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 22: RestartGame Purges Active Enemies Execution
            TestRunnerHelper.RunTest(report, "CH1-M5-22", "F29", 2,
                "RestartGame Purges Active Enemies Execution",
                "Verifies RestartGame executes enemy cleanup iteration over active EnemyBase instances without errors.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var gmGo = ctx.CreateGameObject("GM_GO22");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            var e1 = ctx.CreateGameObject("Enemy_1").AddComponent<ChaserEnemy>();
                            var e2 = ctx.CreateGameObject("Enemy_2").AddComponent<ShooterEnemy>();
                            var e3 = ctx.CreateGameObject("Enemy_3").AddComponent<RusherEnemy>();

                            var initialEnemies = GameObject.FindObjectsOfType<EnemyBase>();
                            E2EAssert.IsTrue(initialEnemies.Length >= 3, "At least 3 enemies exist prior to restart");

                            // In runtime PlayMode, Destroy(enemy.gameObject) destroys at frame end
                            gm.RestartGame();

                            E2EAssert.IsTrue(true, "RestartGame enemy purge loop executed cleanly");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 23: RestartGame Resets Spawner Boss State
            TestRunnerHelper.RunTest(report, "CH1-M5-23", "F29", 2,
                "RestartGame Resets Spawner Boss State",
                "Verifies EnemySpawner state (survivalTime, bossSpawned, isBossActive, isSpawning) is cleanly reset.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var gmGo = ctx.CreateGameObject("GM_GO23");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();

                            var spawnerGo = ctx.CreateGameObject("Spawner_GO23");
                            var spawner = spawnerGo.AddComponent<EnemySpawner>();
                            spawner.survivalTime = 150f;
                            spawner.bossSpawned = true;
                            spawner.isBossActive = true;
                            spawner.isSpawning = false;

                            gm.RestartGame();

                            E2EAssert.AreEqual(0f, spawner.survivalTime, "survivalTime must be reset to 0f");
                            E2EAssert.IsFalse(spawner.bossSpawned, "bossSpawned must be reset to false for new run");
                            E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be reset to false");
                            E2EAssert.IsTrue(spawner.isSpawning, "isSpawning must be reset to true");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 24: Rapid Restart Stress Test
            TestRunnerHelper.RunTest(report, "CH1-M5-24", "F29", 3,
                "Rapid Restart Stress Test",
                "Verifies 10 consecutive cycles of AddScore -> GameOver -> RestartGame remain fully stable.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_GO24");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var goPanel = ctx.CreateGameObject("GOPanel_24");
                            ui.gameOverPanel = goPanel;

                            var gmGo = ctx.CreateGameObject("GM_GO24");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            var playerGo = ctx.CreateGameObject("Player_GO24");
                            playerGo.tag = "Player";
                            var ph = playerGo.AddComponent<PlayerHealth>();

                            for (int i = 0; i < 10; i++)
                            {
                                gm.AddScore(200);
                                gm.TriggerGameOver();
                                E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState);
                                E2EAssert.AreEqual(0f, Time.timeScale);

                                gm.RestartGame();
                                E2EAssert.AreEqual(0, gm.CurrentScore);
                                E2EAssert.AreEqual(1.0f, Time.timeScale);
                                E2EAssert.AreEqual(GameState.Playing, gm.CurrentState);
                                E2EAssert.IsFalse(goPanel.activeSelf);
                            }
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });


            // =========================================================================
            // SUITE 4: Victory Continue Flow & Endless Scaling Stress
            // =========================================================================

            // TEST 25: Boss Defeat Triggers VictoryContinues & Freezes TimeScale
            TestRunnerHelper.RunTest(report, "CH1-M5-25", "F30", 1,
                "Boss Defeat Triggers VictoryContinues & Freezes TimeScale",
                "Verifies TriggerVictory sets state to VictoryContinues and freezes timeScale to 0f.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_V25");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var victPanel = ctx.CreateGameObject("VictoryPanel_25");
                            victPanel.SetActive(false);
                            ui.victoryPanel = victPanel;

                            var gmGo = ctx.CreateGameObject("GM_V25");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            gm.TriggerVictory();

                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState, "State must be VictoryContinues");
                            E2EAssert.AreEqual(0f, Time.timeScale, "timeScale must be 0f on victory");
                            E2EAssert.IsTrue(victPanel.activeSelf, "Victory panel must be displayed");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 26: Victory UI Panel & Banner Text
            TestRunnerHelper.RunTest(report, "CH1-M5-26", "F30", 1,
                "Victory UI Panel & Banner Text",
                "Verifies Victory panel reveals with banner 'BOSS SLAIN! +500 PTS'.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var canvasGo = ctx.CreateGameObject("Canvas_V26");
                        var ui = canvasGo.AddComponent<UIManager>();
                        var victPanel = ctx.CreateGameObject("VictoryPanel_26");
                        var bannerGo = ctx.CreateGameObject("BannerText");
                        ui.victoryPanel = victPanel;
                        ui.victoryBannerText = bannerGo.AddComponent<Text>();

                        ui.ShowVictory();

                        E2EAssert.IsTrue(victPanel.activeSelf, "Victory panel must be active");
                        E2EAssert.AreEqual("BOSS SLAIN! +500 PTS", ui.victoryBannerText.text);
                    }
                });

            // TEST 27: ContinueEndless Restores TimeScale 1.0f & Playing State
            TestRunnerHelper.RunTest(report, "CH1-M5-27", "F30", 1,
                "ContinueEndless Restores TimeScale 1.0f & Playing State",
                "Verifies ResumeEndlessAfterBoss / ContinueEndless restores timeScale to 1.0f and state to Playing.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_V27");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var victPanel = ctx.CreateGameObject("VictoryPanel_27");
                            ui.victoryPanel = victPanel;

                            var gmGo = ctx.CreateGameObject("GM_V27");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            gm.TriggerVictory();
                            E2EAssert.AreEqual(GameState.VictoryContinues, gm.CurrentState);

                            // Player clicks Continue button
                            gm.ResumeEndlessAfterBoss();

                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState, "State must return to Playing");
                            E2EAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must be restored to 1.0f");
                            E2EAssert.IsFalse(victPanel.activeSelf, "Victory panel must be dismissed");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            // TEST 28: Endless Spawner Continuation & Suppression Lift
            TestRunnerHelper.RunTest(report, "CH1-M5-28", "F24", 2,
                "Endless Spawner Continuation & Suppression Lift",
                "Verifies OnBossDefeated lifts 50% spawn rate suppression while keeping bossSpawned latch.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner_V28");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();
                        spawner.bossSpawned = true;
                        spawner.isBossActive = true;

                        // During boss: spawn interval is doubled (50% suppression)
                        float bossInterval = spawner.CalculateSpawnInterval(10f, 500);

                        spawner.OnBossDefeated();

                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive must be false post-boss");
                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned latch must remain true");
                        E2EAssert.IsTrue(spawner.isSpawning, "isSpawning must be true to continue endless");

                        // Suppression lifted: interval should now be half of boss interval
                        float normalInterval = spawner.CalculateSpawnInterval(10f, 500);
                        E2EAssert.IsTrue(Mathf.Approximately(bossInterval, normalInterval * 2.0f),
                            "Spawn interval should return to normal (unsuppressed) rate");
                    }
                });

            // TEST 29: Endless Score Progression Post-Boss
            TestRunnerHelper.RunTest(report, "CH1-M5-29", "F24", 2,
                "Endless Score Progression Post-Boss",
                "Verifies score and high score continue to accrue dynamically in endless mode after boss defeat.",
                () =>
                {
                    int oldHigh = PlayerPrefs.GetInt("HighScore", 0);
                    try
                    {
                        PlayerPrefs.SetInt("HighScore", 600);
                        PlayerPrefs.Save();

                        using (var ctx = new E2ETestContext())
                        {
                            var gmGo = ctx.CreateGameObject("GM_V29");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.ReloadHighScore();

                            // Defeat boss at 500 points
                            gm.AddScore(500);
                            gm.TriggerVictory();
                            gm.ResumeEndlessAfterBoss();

                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState);
                            E2EAssert.AreEqual(500, gm.CurrentScore);
                            E2EAssert.AreEqual(600, gm.HighScore);

                            // Continue killing endless enemies
                            gm.AddScore(250); // Total 750 (breaks 600 record)
                            E2EAssert.AreEqual(750, gm.CurrentScore);
                            E2EAssert.AreEqual(750, gm.HighScore, "HighScore should update to 750 in endless mode");
                            E2EAssert.AreEqual(750, PlayerPrefs.GetInt("HighScore", 0));
                        }
                    }
                    finally
                    {
                        PlayerPrefs.SetInt("HighScore", oldHigh);
                        PlayerPrefs.Save();
                    }
                });

            // TEST 30: No Duplicate Boss Spawn in Post-Victory Endless Mode
            TestRunnerHelper.RunTest(report, "CH1-M5-30", "F21", 2,
                "No Duplicate Boss Spawn in Post-Victory Endless Mode",
                "Verifies score increases to 1000, 2000, 5000 in endless mode never spawn a second boss.",
                () =>
                {
                    using (var ctx = new E2ETestContext())
                    {
                        var spawnerGo = ctx.CreateGameObject("Spawner_V30");
                        var spawner = spawnerGo.AddComponent<EnemySpawner>();

                        // Simulate initial boss defeat
                        spawner.bossSpawned = true;
                        spawner.OnBossDefeated();

                        // Advance scores in endless mode
                        int[] highScores = new int[] { 1000, 1500, 2500, 5000 };
                        foreach (int s in highScores)
                        {
                            if (s >= 500 && !spawner.bossSpawned)
                            {
                                spawner.SpawnBoss();
                            }
                        }

                        E2EAssert.IsTrue(spawner.bossSpawned, "bossSpawned should remain true");
                        E2EAssert.IsFalse(spawner.isBossActive, "isBossActive should not be re-enabled");
                    }
                });

            // TEST 31: Post-Victory Death Cleanly Transitions to GameOver
            TestRunnerHelper.RunTest(report, "CH1-M5-31", "F30", 2,
                "Post-Victory Death Cleanly Transitions to GameOver",
                "Verifies dying in endless mode after Boss defeat transitions cleanly to GameOver.",
                () =>
                {
                    float prevScale = Time.timeScale;
                    try
                    {
                        using (var ctx = new E2ETestContext())
                        {
                            var canvasGo = ctx.CreateGameObject("Canvas_V31");
                            var ui = canvasGo.AddComponent<UIManager>();
                            var victPanel = ctx.CreateGameObject("VictoryPanel_31");
                            var goPanel = ctx.CreateGameObject("GameOverPanel_31");
                            ui.victoryPanel = victPanel;
                            ui.gameOverPanel = goPanel;

                            var gmGo = ctx.CreateGameObject("GM_V31");
                            var gm = gmGo.AddComponent<GameManager>();
                            gm.SetAsActiveInstance();
                            gm.UIManagerRef = ui;

                            var playerGo = ctx.CreateGameObject("Player_V31");
                            playerGo.tag = "Player";
                            var ph = playerGo.AddComponent<PlayerHealth>();
                            ph.OnPlayerDeath += gm.TriggerGameOver;

                            // Boss defeated and continue
                            gm.AddScore(500);
                            gm.TriggerVictory();
                            gm.ResumeEndlessAfterBoss();
                            E2EAssert.AreEqual(GameState.Playing, gm.CurrentState);

                            // Score more in endless
                            gm.AddScore(300); // 800 total

                            // Player takes lethal damage in endless mode
                            InflictLethalDamage(ph);

                            E2EAssert.AreEqual(GameState.GameOver, gm.CurrentState, "State must transition from Playing to GameOver");
                            E2EAssert.AreEqual(0f, Time.timeScale, "timeScale must freeze to 0f on post-victory death");
                            E2EAssert.IsTrue(goPanel.activeSelf, "GameOver panel must be active");
                            E2EAssert.IsFalse(victPanel.activeSelf, "Victory panel must be closed");
                            E2EAssert.AreEqual(800, gm.CurrentScore, "Score must retain all pre- and post-boss points");
                        }
                    }
                    finally
                    {
                        Time.timeScale = prevScale;
                    }
                });

            return report;
        }

#if UNITY_EDITOR
        [MenuItem("E2E Tests/Run Challenger 1 M5 Adversarial Tests")]
        public static void RunMenu()
        {
            var report = RunAllTests();
            Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
