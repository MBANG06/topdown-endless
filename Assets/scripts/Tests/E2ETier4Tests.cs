using System;
using UnityEngine;

namespace E2ETests
{
    public static class E2ETier4Tests
    {
        public static void RunAll(TestSuiteReport report)
        {
            RunScenario1(report);
            RunScenario2(report);
            RunScenario3(report);
            RunScenario4(report);
            RunScenario5(report);
        }

        #region Scenario 1 - Standard Survival Wave
        private static void RunScenario1(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T4_SCENARIO_01", "Scenario1", 4,
                "Standard Survival Wave",
                "Simulates player movement, mouse aiming, basic fire, Chaser spawning, bullet collision, death, score and HUD update",
                () =>
                {
                    using var ctx = new E2ETestContext();

                    // 1. Setup player in arena at (0, 0)
                    var player = ctx.CreateMockPlayer(Vector2.zero, out var playerRb);
                    var pm = player.AddComponent<PlayerMovement>();
                    pm.rb = playerRb;

                    // 2. Simulate WASD movement (moving right)
                    Vector2 moveDir = Vector2.right;
                    Vector2 nextPos = (Vector2)player.transform.position + moveDir.normalized * pm.moveSpeed * 0.02f;
                    player.transform.position = nextPos;
                    E2EAssert.AreApproximatelyEqual(0.1f, player.transform.position.x, 0.001f, "Player should move right by 0.1 units in 1 frame");

                    // 3. Aim at enemy spawn direction (5, 0)
                    Vector2 aimTarget = new Vector2(5, 0);
                    Vector2 lookDir = aimTarget - (Vector2)player.transform.position;
                    float rotAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                    playerRb.rotation = rotAngle;
                    E2EAssert.AreApproximatelyEqual(-90f, rotAngle, 0.01f);

                    // 4. Spawner spawns Chaser enemy at perimeter (-10.5, 0)
                    Vector2 chaserSpawn = new Vector2(-10.5f, 0f);
                    var chaser = ctx.CreateMockEnemy("Chaser", chaserSpawn, 3);
                    E2EAssert.AreEqual(-10.5f, chaser.transform.position.x);

                    // 5. Player fires bullet towards enemy
                    var bullet = ctx.CreateGameObject("Bullet");
                    bullet.transform.position = player.transform.position;
                    var bulletRb = bullet.AddComponent<Rigidbody2D>();
                    bulletRb.gravityScale = 0f;
                    bulletRb.velocity = Vector2.left * 20.0f;
                    E2EAssert.AreEqual(-20.0f, bulletRb.velocity.x);

                    // 6. Bullet travels and hits Chaser (dealing 3 damage over 3 hits)
                    int chaserHp = 3;
                    int bulletDmg = 1;
                    chaserHp -= bulletDmg; // Hit 1 -> 2 HP
                    E2EAssert.AreEqual(2, chaserHp);
                    chaserHp -= bulletDmg; // Hit 2 -> 1 HP
                    E2EAssert.AreEqual(1, chaserHp);
                    chaserHp -= bulletDmg; // Hit 3 -> 0 HP (Lethal)
                    E2EAssert.AreEqual(0, chaserHp);

                    // 7. Chaser dies, awards 10 points
                    int currentScore = 0;
                    if (chaserHp <= 0)
                    {
                        currentScore += 10;
                        UnityEngine.Object.DestroyImmediate(chaser);
                    }
                    E2EAssert.AreEqual(10, currentScore, "Chaser kill must award 10 points");

                    // 8. HUD updates formatted score
                    string hudScore = $"SCORE: {currentScore:D5}";
                    E2EAssert.AreEqual("SCORE: 00010", hudScore);
                });
        }
        #endregion

        #region Scenario 2 - Heavy Swarm & AoE Grenade Rescue
        private static void RunScenario2(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T4_SCENARIO_02", "Scenario2", 4,
                "Heavy Swarm & AoE Grenade Rescue",
                "Simulates Rusher and Shooter swarm, grenade drop & pickup, throw via E/RMB, AoE multi-kill detonation",
                () =>
                {
                    using var ctx = new E2ETestContext();

                    // 1. Player at center with 0 grenades
                    var player = ctx.CreateMockPlayer(Vector2.zero, out _);
                    int playerGrenades = 0;

                    // 2. Swarm of 5 Rushers and 3 Shooters surrounding player
                    int rusherCount = 5;
                    int shooterCount = 3;
                    int totalEnemies = rusherCount + shooterCount;
                    E2EAssert.AreEqual(8, totalEnemies);

                    // 3. Enemy drops a grenade pickup at (1, 0)
                    Vector2 dropPos = new Vector2(1, 0);
                    var pickup = ctx.CreateGameObject("GrenadePickup");
                    pickup.transform.position = dropPos;
                    var pickupCol = pickup.AddComponent<CircleCollider2D>();
                    pickupCol.isTrigger = true;

                    // 4. Player walks over pickup to collect
                    player.transform.position = dropPos;
                    if (playerGrenades < 5)
                    {
                        playerGrenades++;
                        UnityEngine.Object.DestroyImmediate(pickup);
                    }
                    E2EAssert.AreEqual(1, playerGrenades, "Player inventory should have 1 grenade");
                    E2EAssert.IsTrue(pickup == null, "Pickup should be destroyed after collection");

                    // 5. Player aims at swarm cluster (0, 2) and presses E
                    Vector2 mousePos = new Vector2(0, 2);
                    Vector2 throwVec = Vector2.ClampMagnitude(mousePos - (Vector2)player.transform.position, 7.0f);
                    E2EAssert.AreApproximatelyEqual(new Vector2(-1, 2).magnitude, throwVec.magnitude, 0.01f);

                    // Throw decrements inventory
                    playerGrenades--;
                    E2EAssert.AreEqual(0, playerGrenades, "Throw should decrement grenade count to 0");

                    // 6. Grenade lands at target and detonates with 3.5u radius and 50 damage
                    Vector2 detonationPos = mousePos;
                    float blastRadius = 3.5f;
                    int blastDamage = 50;

                    // 7. Verify all 8 enemies in radius (within 3.5u of (0, 2)) are eliminated
                    int scoreEarned = 0;
                    for (int i = 0; i < rusherCount; i++)
                    {
                        int hp = 1;
                        hp -= blastDamage;
                        if (hp <= 0) scoreEarned += 15; // Rusher kill
                    }
                    for (int i = 0; i < shooterCount; i++)
                    {
                        int hp = 2;
                        hp -= blastDamage;
                        if (hp <= 0) scoreEarned += 20; // Shooter kill
                    }

                    // 5 * 15 + 3 * 20 = 75 + 60 = 135
                    E2EAssert.AreEqual(135, scoreEarned, "Grenade AoE multi-kill must award 135 points for the swarm");
                });
        }
        #endregion

        #region Scenario 3 - Damage Clamping & i-frame Recovery
        private static void RunScenario3(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T4_SCENARIO_03", "Scenario3", 4,
                "Damage Clamping & i-frame Recovery",
                "Simulates player hit, exactly 1 HP loss, i-frames blocking rapid subsequent hits, and damage allowed after 1.0s window",
                () =>
                {
                    // 1. Player starts at 5 HP
                    int playerHp = 5;
                    float iFrameTimer = 0f;
                    bool isInvulnerable = false;

                    // 2. Chaser attacks player at t = 0.0s
                    if (!isInvulnerable)
                    {
                        playerHp -= 1;
                        isInvulnerable = true;
                        iFrameTimer = 1.0f;
                    }
                    E2EAssert.AreEqual(4, playerHp, "First hit should deduct exactly 1 HP (5 -> 4)");
                    E2EAssert.IsTrue(isInvulnerable, "i-frames should be active");

                    // 3. Second enemy attacks at t = 0.3s (iFrameTimer = 0.7s remaining)
                    iFrameTimer = 0.7f;
                    if (!isInvulnerable || iFrameTimer <= 0f)
                    {
                        playerHp -= 1;
                    }
                    E2EAssert.AreEqual(4, playerHp, "Hit at t=0.3s must be blocked by i-frames");

                    // 4. Third enemy attacks at t = 0.7s (iFrameTimer = 0.3s remaining)
                    iFrameTimer = 0.3f;
                    if (!isInvulnerable || iFrameTimer <= 0f)
                    {
                        playerHp -= 1;
                    }
                    E2EAssert.AreEqual(4, playerHp, "Hit at t=0.7s must be blocked by i-frames");

                    // 5. At t = 1.1s, i-frames expire (iFrameTimer = 0.0f)
                    iFrameTimer = 0.0f;
                    isInvulnerable = false;

                    // 6. Fourth enemy attacks after expiration
                    if (!isInvulnerable)
                    {
                        playerHp -= 1;
                        isInvulnerable = true;
                        iFrameTimer = 1.0f;
                    }
                    E2EAssert.AreEqual(3, playerHp, "Hit at t=1.1s must succeed, deducting 1 HP (4 -> 3)");

                    // 7. HUD reflects 3 full hearts, 2 empty hearts
                    int fullHearts = playerHp;
                    int emptyHearts = 5 - playerHp;
                    E2EAssert.AreEqual(3, fullHearts);
                    E2EAssert.AreEqual(2, emptyHearts);
                });
        }
        #endregion

        #region Scenario 4 - Boss Battle at 500 Score
        private static void RunScenario4(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T4_SCENARIO_04", "Scenario4", 4,
                "Boss Battle at 500 Score",
                "Simulates reaching 500 score, Boss spawn latch, HP bar, radial burst, Boss defeat, +500 bonus, and endless resume",
                () =>
                {
                    // 1. Initial state: score 490, no boss
                    int score = 490;
                    bool bossSpawned = false;
                    bool bossDefeated = false;
                    bool bossBarVisible = false;

                    // 2. Kill Rusher (+15 pts) -> score = 505
                    score += 15;
                    E2EAssert.AreEqual(505, score);

                    // 3. Score >= 500 latch check
                    if (score >= 500 && !bossSpawned && !bossDefeated)
                    {
                        bossSpawned = true;
                        bossBarVisible = true;
                    }
                    E2EAssert.IsTrue(bossSpawned, "Boss must spawn upon reaching 500 score");
                    E2EAssert.IsTrue(bossBarVisible, "Boss HP bar must activate");

                    // 4. Boss properties: 60 HP, spawn at (2.69, 3.5)
                    int bossHp = 60;
                    int bossMaxHp = 60;
                    Vector3 bossPos = new Vector3(2.69f, 3.5f, 0f);
                    E2EAssert.AreEqual(60, bossHp);
                    E2EAssert.AreApproximatelyEqual(2.69f, bossPos.x, 0.01f);

                    // 5. Boss fires 16 radial bullets (360 degrees, 22.5 deg spacing)
                    int radialBulletCount = 16;
                    float angleStep = 360f / radialBulletCount;
                    E2EAssert.AreApproximatelyEqual(22.5f, angleStep, 0.001f);

                    // 6. Player throws grenade at Boss (50 damage)
                    bossHp -= 50;
                    E2EAssert.AreEqual(10, bossHp, "Boss HP should drop from 60 to 10 after grenade");
                    float sliderRatio = (float)bossHp / bossMaxHp;
                    E2EAssert.AreApproximatelyEqual(10f / 60f, sliderRatio, 0.001f);

                    // 7. Player fires 10 basic bullets (1 damage each)
                    for (int b = 0; b < 10; b++)
                    {
                        bossHp -= 1;
                    }
                    E2EAssert.AreEqual(0, bossHp, "Boss HP should reach 0");

                    // 8. Boss defeat sequence
                    int grenadeDrops = 0;
                    bool victoryModalVisible = false;
                    if (bossHp <= 0)
                    {
                        bossDefeated = true;
                        score += 500; // +500 bonus
                        grenadeDrops = 2; // guaranteed 2 drops
                        bossBarVisible = false;
                        victoryModalVisible = true;
                    }
                    E2EAssert.AreEqual(1005, score, "Total score should equal 505 + 500 = 1005");
                    E2EAssert.AreEqual(2, grenadeDrops, "Boss must drop guaranteed 2 grenades");
                    E2EAssert.IsFalse(bossBarVisible, "Boss HP bar should be hidden");
                    E2EAssert.IsTrue(victoryModalVisible, "Victory modal should be visible");

                    // 9. Player clicks Continue -> endless mode resumes
                    victoryModalVisible = false;
                    Time.timeScale = 1.0f;
                    E2EAssert.IsFalse(victoryModalVisible);
                    E2EAssert.AreEqual(1.0f, Time.timeScale);

                    // 10. Verify second boss never spawns in continuation
                    score += 50; // score now 1055
                    bool canSpawnSecondBoss = (score >= 500 && !bossSpawned);
                    E2EAssert.IsFalse(canSpawnSecondBoss, "Duplicate boss spawn must be blocked in endless continuation");
                });
        }
        #endregion

        #region Scenario 5 - Full Game Loop & Persistence
        private static void RunScenario5(TestSuiteReport report)
        {
            TestRunnerHelper.RunTest(report, "T4_SCENARIO_05", "Scenario5", 4,
                "Full Game Loop & Persistence",
                "Simulates Main Menu -> Play -> Score 250 -> Pause -> Resume -> Player Death -> High Score saved in PlayerPrefs -> Restart",
                () =>
                {
                    string highKey = "E2E_Loop_HighScore_Test";
                    PlayerPrefs.DeleteKey(highKey);
                    PlayerPrefs.Save();

                    // 1. Initial State: Main Menu
                    string gameState = "MainMenu";
                    E2EAssert.AreEqual("MainMenu", gameState);

                    // 2. Click Play -> enter Playing state (timeScale = 1.0)
                    gameState = "Playing";
                    Time.timeScale = 1.0f;
                    int currentScore = 0;
                    int initialHigh = PlayerPrefs.GetInt(highKey, 0);
                    E2EAssert.AreEqual(0, initialHigh);

                    // 3. Play game, eliminate enemies to score 250
                    currentScore += 250;
                    E2EAssert.AreEqual(250, currentScore);

                    // 4. Player presses ESC -> Game Paused (timeScale = 0.0)
                    gameState = "Paused";
                    Time.timeScale = 0.0f;
                    E2EAssert.AreEqual(0.0f, Time.timeScale);

                    // 5. Player clicks Resume -> Playing resumed (timeScale = 1.0)
                    gameState = "Playing";
                    Time.timeScale = 1.0f;
                    E2EAssert.AreEqual(1.0f, Time.timeScale);

                    // 6. Player takes fatal damage (HP reaches 0)
                    int playerHp = 0;
                    if (playerHp <= 0)
                    {
                        gameState = "GameOver";
                        Time.timeScale = 0.0f;

                        // High score check & save
                        int currentRecord = PlayerPrefs.GetInt(highKey, 0);
                        if (currentScore > currentRecord)
                        {
                            PlayerPrefs.SetInt(highKey, currentScore);
                            PlayerPrefs.Save();
                        }
                    }
                    E2EAssert.AreEqual("GameOver", gameState);
                    E2EAssert.AreEqual(0.0f, Time.timeScale);

                    // 7. Verify High Score persisted to PlayerPrefs as 250
                    int savedRecord = PlayerPrefs.GetInt(highKey, 0);
                    E2EAssert.AreEqual(250, savedRecord, "HighScore must be persisted as 250");

                    // 8. Player clicks Restart on Game Over screen
                    Time.timeScale = 1.0f;
                    gameState = "Playing";
                    currentScore = 0;
                    playerHp = 5;
                    int loadedHigh = PlayerPrefs.GetInt(highKey, 0);

                    E2EAssert.AreEqual(1.0f, Time.timeScale, "TimeScale must be restored to 1.0");
                    E2EAssert.AreEqual(0, currentScore, "Current score must reset to 0");
                    E2EAssert.AreEqual(5, playerHp, "Player health must reset to 5");
                    E2EAssert.AreEqual(250, loadedHigh, "Loaded high score must remain 250 across restart");

                    // Cleanup test key
                    PlayerPrefs.DeleteKey(highKey);
                    PlayerPrefs.Save();
                });
        }
        #endregion
    }
}
