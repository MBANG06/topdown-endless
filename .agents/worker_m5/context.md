# Milestone 5 Worker Context: UI / HUD, Game Loop & Audio

## Target Files:
- Assets/scripts/GameManager.cs
- Assets/scripts/UIManager.cs
- Assets/scripts/SoundManager.cs
- Assets/Scenes/shooting.unity (Canvas, Panels, HUD elements, EventSystem, Component wiring)
- Assets/scripts/Tests/Milestone5Tests.cs

## Requirements:
- R5: UI, HUD & Game Flow State Machine
  - F25: HUD 5 Hearts (empty/full sprites), formatted score ("SCORE: 00120"), high score ("HIGH: 00500"), grenade counter ("x 3"), Boss Health slider (toggled during boss fight).
  - F26: High score persistence in PlayerPrefs ("HighScore").
  - F27: Main Menu (Play button, Controls modal, Quit).
  - F28: Pause Menu (toggled via ESC or P, Time.timeScale = 0, Resume, Restart, Menu buttons).
  - F29: Game Over Screen (triggered on Player death, displays final score and high score, Restart button unpausing timeScale, Menu button).
  - F30: Victory / Continue modal (triggered on Boss defeat, "BOSS SLAIN! +500 PTS", Continue button resumes endless wave scaling).
- R6: Audio Feedback SFX
  - F34: SoundManager procedural 8-bit audio generation using AudioClip.Create for Shoot, Hit, Explosion, Hurt, Pickup, GameOver, Victory (0 missing audio asset files).
- F35: 0 compiler errors via read_console, 0 runtime exceptions.
- 100% pass on all 385 automated E2E tests via E2ETestRunner.RunAll().
