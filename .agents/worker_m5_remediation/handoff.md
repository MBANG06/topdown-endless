# Milestone 5 Remediation Handoff Report

**Worker**: Remediation Worker (`worker_m5_remediation`)  
**Target Milestone**: Milestone 5 (UI / HUD, Game Loop & Audio)  
**Files Modified**:
- `Assets/scripts/UIManager.cs`
- `Assets/scripts/GameManager.cs`
- `Assets/scripts/SoundManager.cs`
- `Assets/Scenes/shooting.unity`
- `Assets/scripts/Tests/Milestone5Tests.cs`

---

## 1. Observation

Direct empirical observations before and after remediation:

### Defect 1: Leaked Transient Objects in `shooting.unity`
- **Before**:
  `unityMCP.execute_code` identified 181 transient test objects serialized into `Assets/Scenes/shooting.unity`:
  128 `BossBullet`, 20 `DummyBoss(Clone)`, 16 `GrenadeProjectile_Fallback`, 8 `DummyPickup(Clone)`, 6 `ExplosionAoE_Fallback`, plus test-spawned `Score`, `High`, and `Gren` root entities.
- **Remediation**:
  Purged all 181 transient test objects, leaving only the 12 canonical game roots (`Main Camera`, `Player`, `floor`, `arvores`, `Colliders`, `Fire Effect`, `MapBounds`, `EnemySpawner`, `GameManager`, `SoundManager`, `EventSystem`, `Canvas`). Re-saved scene to disk.
- **After**:
  `unityMCP.execute_code` returns: `Roots: 12, Leaked objects in scene: 0`. Disk greps for `BossBullet`, `DummyBoss`, `DummyPickup`, `GrenadeProjectile_Fallback`, `ExplosionAoE_Fallback` return 0 results.

### Defect 2: Controls Modal Double-Wiring
- **Before**:
  `controlsButton` and `closeControlsButton` were persistently wired in the scene and dynamically wired via `onClick.AddListener(OnControlsButtonClicked)`. Both invoked `ToggleControlsModal()`, causing two toggles per click (`false -> true -> false`), leaving the modal closed (`Modal activeSelf: False`).
- **Remediation**:
  In `UIManager.cs`:
  - Added explicit decoupled methods:
    `public void OpenControlsModal() => SetControlsModal(true);`
    `public void CloseControlsModal() => SetControlsModal(false);`
  - Added `OnCloseControlsButtonClicked()` and rewired `closeControlsButton` persistent event to it.
  - Implemented `SafeAddButtonListener()` in `WireButtons()`: checks `btn.onClick.GetPersistentMethodName()` to avoid adding duplicate runtime listeners when inspector persistent events exist.
  - Configured persistent button listeners to `UnityEventCallState.EditorAndRuntime`.
- **After**:
  `ui.controlsButton.onClick.Invoke()` -> `ui.controlsModal.activeSelf == True`.
  `ui.closeControlsButton.onClick.Invoke()` -> `ui.controlsModal.activeSelf == False`.

### Defect 3: Player Dead / Paralyzed State on Menu -> Play Transition
- **Before**:
  Lethal damage disabled `PlayerMovement` and `Shooting` on `PlayerHealth.Die()`. Clicking `Menu` and then `Play` executed `StartGame()`, which only set `CurrentState = GameState.Playing` without resetting player health or components (`HP: 0, Movement: False, Shooting: False`).
- **Remediation**:
  In `GameManager.StartGame()`:
  - Added check: `if (playerHealth != null && (playerHealth.currentHealth <= 0 || !playerHealth.IsAlive))`
  - Invokes `playerHealth.ResetHealth()` (restores 5 HP, enables `PlayerMovement`, enables `Shooting`), resets `thrower.ResetGrenades(2)`, clears active enemies, resets `EnemySpawner`, and resets `CurrentScore = 0`.
- **After**:
  Lethal damage -> `ui.OnMenuButtonClicked()` -> `ui.OnPlayButtonClicked()`:
  Returns: `HP: 5, Movement: True, Shooting: True, State: Playing`.

### Defect 4: Unbounded Event Delegate Leak on Restarts
- **Before**:
  `UIManager.HookSceneEntities()` appended delegates to `playerHealth.OnHealthChanged` and `thrower.OnGrenadeCountChanged` on each `RestartGame()` without unsubscribing, leaking hundreds of duplicate invocations.
- **Remediation**:
  In `UIManager.cs`:
  - In `HookSceneEntities()`: tracked references `_subscribedPlayerHealth` and `_subscribedThrower`, unsubscribing prior delegates before subscribing:
    `playerHealth.OnHealthChanged -= UpdateHearts; playerHealth.OnHealthChanged += UpdateHearts;`
    `thrower.OnGrenadeCountChanged -= UpdateGrenades; thrower.OnGrenadeCountChanged += UpdateGrenades;`
  - In `OnDestroy()`: cleanly unsubscribes from `_subscribedPlayerHealth` and `_subscribedThrower`.
- **After**:
  After 20 consecutive `GameManager.Instance.RestartGame()` calls:
  `Thrower delegate count: 1, PlayerHealth delegate count: 1`.

### Minor Defect: Runtime Audio Clamping (Finding 6)
- **Remediation**:
  In `SoundManager.PlayClip()`: clamped `masterVolume` and `sfxVolume` via `Mathf.Clamp01(masterVolume) * Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(volumeScale)`.

---

## 2. Logic Chain

1. **Defect 1**: Persistent test objects left in `Assets/Scenes/shooting.unity` polluted the scene hierarchy and could interfere with physics colliders. Deleting these non-canonical roots and saving the scene directly via `EditorSceneManager` restores the clean production scene state.
2. **Defect 2**: The button failure was caused by a combination of non-idempotent toggle logic (`ToggleControlsModal`) and double listener invocation. Decoupling into explicit `OpenControlsModal` and `CloseControlsModal` makes the actions idempotent. Adding `SafeAddButtonListener` guarantees that listeners are not double-registered when persistent listeners exist. Setting persistent listeners to `EditorAndRuntime` ensures consistent execution across both Edit Mode test harnesses and runtime gameplay.
3. **Defect 3**: A fresh game session from the Main Menu must always guarantee an active, controllable player with full health. Checking if the player is dead in `StartGame()` and triggering `ResetHealth()`, resetting grenades, enemies, and score ensures the state machine cleanly reinitializes without requiring a full scene reload.
4. **Defect 4**: Delegate leaks occur when event subscriptions are added during repeated lifecycle calls without corresponding removal. Using `-= delegate` immediately prior to `+= delegate`, combined with cached reference unsubscription in `OnDestroy()`, guarantees that delegate invocation lists remain strictly singular (size 1).

---

## 3. Caveats

- `SoundManager.PlayHitSFX()`, `PlayExplosionSFX()`, and `PlayHurtSFX()` were not wired into `Bullet.cs`, `ExplosionAoE.cs`, or `PlayerHealth.cs` because those files are outside the Milestone 5 file ownership boundary.
- All modifications were strictly limited to the 5 authorized files: `GameManager.cs`, `UIManager.cs`, `SoundManager.cs`, `shooting.unity`, and `Milestone5Tests.cs`.

---

## 4. Conclusion

All 4 defects reported by Reviewer 2 (plus volume clamping) have been successfully remediated and empirically verified.
- Leaked test objects in scene: **0**
- Compiler console errors: **0**
- `Milestone5Tests`: **19 / 19 passed** (including 4 new regression tests M5-16 to M5-19)
- `Challenger1M5Tests`: **31 / 31 passed**
- `Challenger2M5Tests`: **30 / 30 passed**
- `E2ETestRunner.RunAll()`: **385 / 385 passed (0 failed, 0 pending, 0 skipped)**

Milestone 5 is fully stable, clean, and ready for approval.

---

## 5. Verification Method

To independently verify the remediation:

1. **Check Compiler Output**:
   Execute via `unityMCP.read_console`:
   - Expected: 0 errors.

2. **Verify Leaked Objects in Scene (Defect 1)**:
   ```csharp
   var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   var roots = scene.GetRootGameObjects();
   int leaked = 0;
   foreach (var go in GameObject.FindObjectsOfType<GameObject>()) {
       if (go.name.Contains("Dummy") || go.name.Contains("Fallback") || go.name.Contains("BossBullet")) leaked++;
   }
   return $"Roots: {roots.Length}, Leaked: {leaked}";
   // Expected: Roots: 12, Leaked: 0
   ```

3. **Verify Controls Modal Open / Close (Defect 2)**:
   ```csharp
   var ui = GameObject.Find("Canvas").GetComponent<UIManager>();
   ui.controlsModal.SetActive(false);
   ui.controlsButton.onClick.Invoke();
   bool opened = ui.controlsModal.activeSelf;
   ui.closeControlsButton.onClick.Invoke();
   bool closed = !ui.controlsModal.activeSelf;
   return $"Opened: {opened}, Closed: {closed}";
   // Expected: Opened: True, Closed: True
   ```

4. **Verify Death -> Menu -> Play State Restoration (Defect 3)**:
   ```csharp
   var player = GameObject.FindObjectOfType<PlayerHealth>();
   var ui = GameObject.Find("Canvas").GetComponent<UIManager>();
   var isInvulProp = typeof(PlayerHealth).GetProperty("isInvulnerable");
   for (int i = 0; i < 5; i++) {
       if (isInvulProp != null) isInvulProp.SetValue(player, false, null);
       player.TakeDamage(1);
   }
   ui.OnMenuButtonClicked();
   ui.OnPlayButtonClicked();
   return $"HP: {player.currentHealth}, Movement: {player.GetComponent<PlayerMovement>().enabled}, Shooting: {player.GetComponent<Shooting>().enabled}, State: {GameManager.Instance.CurrentState}";
   // Expected: HP: 5, Movement: True, Shooting: True, State: Playing
   ```

5. **Verify Delegate Idempotence (Defect 4)**:
   ```csharp
   for (int i = 0; i < 20; i++) GameManager.Instance.RestartGame();
   var thrower = GameObject.FindObjectOfType<GrenadeThrower>();
   var f = typeof(GrenadeThrower).GetField("OnGrenadeCountChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
   var lenThrower = ((System.MulticastDelegate)f.GetValue(thrower))?.GetInvocationList().Length ?? 0;
   var player = GameObject.FindObjectOfType<PlayerHealth>();
   var fHealth = typeof(PlayerHealth).GetField("OnHealthChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
   var lenHealth = ((System.MulticastDelegate)fHealth.GetValue(player))?.GetInvocationList().Length ?? 0;
   return $"Thrower: {lenThrower}, Health: {lenHealth}";
   // Expected: Thrower: 1, Health: 1
   ```

6. **Run Test Suites**:
   - `Tests.Milestone5Tests.RunAllTests()` -> Total: 19, Passed: 19, Failed: 0
   - `Tests.Challenger1M5Tests.RunAllTests()` -> Total: 31, Passed: 31, Failed: 0
   - `Tests.Challenger2M5Tests.RunAllTests()` -> Total: 30, Passed: 30, Failed: 0
   - `E2ETests.E2ETestRunner.RunAll()` -> Total: 385, Passed: 385, Failed: 0
