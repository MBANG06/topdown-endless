## 2026-09-21T16:56:12Z
You are the Specification Investigator for the Unity 2D top-down endless shooter game.
Working Directory: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\spec_miner_survey
Original Request: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\ORIGINAL_REQUEST.md

Your task:
1. Read ORIGINAL_REQUEST.md thoroughly.
2. Extract and formalize all requirements (R1 to R6 and Acceptance Criteria) into precise technical specifications:
   - Player Combat & Health: movement speed/feel, boundaries, aiming, firing mechanics, HP count (5), damage rules (1 HP/hit), i-frames duration and visual flash effect, Game Over condition.
   - Endless Enemy Spawning & Enemy Types: spawn boundary/logic, progressive scaling curves (spawn interval reduction, max concurrent count increase vs time/score), 3 distinct enemy archetypes (Chaser, Shooter, Rusher) with specific speeds, HP, attack patterns, scoring values.
   - Grenade Mechanic: drop probability on enemy death, pickup detection, inventory limit/HUD, throw trajectory/target (mouse pos / E / RMB), fuse/timer or impact detonation, AoE radius, damage calculation.
   - Boss Encounter: spawn trigger (500 pts), single-instance guarantee, HP scale, dedicated Boss health bar, 360-degree radial projectile attack pattern/frequency, death sequence, reward score, endless continuation.
   - UI / HUD & Game Flow: In-game HUD components (5 HP icons/hearts, Score, High Score via PlayerPrefs, Grenade count, Boss HP slider), screens (Main Menu, Pause Menu ESC/P with timeScale=0, Game Over screen, Victory/Continue screen), transitions.
   - Visuals & Audio: damage flash, particles, Tiny RPG Forest assets integration.
   - Acceptance Criteria & Constraints: 0 compiler errors, 0 runtime exceptions.
3. Write your comprehensive analysis to `c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\spec_miner_survey\survey_spec.md` and your handoff to `c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\spec_miner_survey\handoff.md`.
4. Maintain `progress.md` in your working directory with timestamped updates.
5. When complete, send a message back to the orchestrator.
