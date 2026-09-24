# Milestone 5 Remediation Context: Reviewer 2 Defect Fixes

## Target Files:
- Assets/scripts/GameManager.cs
- Assets/scripts/UIManager.cs
- Assets/scripts/SoundManager.cs
- Assets/Scenes/shooting.unity
- Assets/scripts/Tests/Milestone5Tests.cs

## Defects to Remediate (from Reviewer 2 Handoff):
1. **[Critical] 94 Leaked Test GameObjects in `shooting.unity`**:
   - The scene file contains 64 active `BossBullet`, 10 `DummyBoss(Clone)`, 12 `GrenadeProjectile_Fallback`, 4 `DummyPickup(Clone)`, and 4 `ExplosionAoE_Fallback` objects accidentally serialized to disk from past test runs.
   - Clean `Assets/Scenes/shooting.unity` by destroying all these leaked test GameObjects and saving the clean scene asset.
2. **[Critical] Double Button Wiring Breaks Controls Modal**:
   - Canvas buttons have persistent serialized `m_OnClick` listeners AND dynamic `onClick.AddListener()` in `UIManager.Start()`.
   - This causes `controlsButton` to fire twice on a single click (false -> true -> false), keeping the modal closed.
   - Fix: Decouple into explicit methods `OpenControlsModal() => SetControlsModal(true);` and `CloseControlsModal() => SetControlsModal(false);` and prevent duplicate listeners in `WireButtons()`.
3. **[Major] Player Remains Dead & Uncontrollable on Menu -> Play Transition**:
   - If player dies, returns to Main Menu, and clicks Play, `StartGame()` fails to reset player state.
   - Fix: In `StartGame()`, ensure player health is reset via `playerHealth.ResetHealth()` if player was dead, restoring 5 HP, enabling PlayerMovement and Shooting, and resetting score.
4. **[Major] Unbounded Event Delegate Leak on Game Restarts**:
   - In `UIManager.HookSceneEntities()`, unsubscribe before subscribing (`playerHealth.OnHealthChanged -= UpdateHearts; playerHealth.OnHealthChanged += UpdateHearts;`, etc.) to prevent duplicate delegate accumulation on repeated restarts, and unsubscribe in `OnDestroy()`.
5. **[Verification]**:
   - Verify 0 compiler errors via read_console.
   - Run Milestone 5 tests, Challenger tests, and E2ETestRunner.RunAll() (all 385 tests must pass).
   - Write handoff.md with verified evidence.
