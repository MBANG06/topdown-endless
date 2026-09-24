# Milestone 5 Independent Quality & Adversarial Review Report

**Reviewer**: Reviewer 2 (Archetype: `reviewer_critic`)  
**Target Milestone**: Milestone 5 (UI / HUD, Game Loop & Audio)  
**Target Files Reviewed**:
- `Assets/scripts/GameManager.cs`
- `Assets/scripts/UIManager.cs`
- `Assets/scripts/SoundManager.cs`
- `Assets/Scenes/shooting.unity`
- `Assets/scripts/Tests/Milestone5Tests.cs`
- `Assets/scripts/Tests/Challenger1M5Tests.cs`
- `Assets/scripts/Tests/E2ETestRunner.cs`

---

## Review Summary

**Verdict**: **REQUEST_CHANGES**

Milestone 5 has delivered functional UI rendering (5 heart health display, score, high score, grenade counter, and boss bar) and procedural 8-bit sound generation passing all 385 automated E2E tests and 15 M5 tests. However, deep adversarial investigation and live scene inspection revealed **2 Critical** and **2 Major** defects that prevent approval:

1. **[CRITICAL] 94 Leaked Test GameObjects Baked into `shooting.unity`**:
   The scene file contains 64 active `BossBullet`, 10 `DummyBoss(Clone)`, 12 `GrenadeProjectile_Fallback`, 4 `DummyPickup(Clone)`, and 4 `ExplosionAoE_Fallback` objects accidentally serialized to disk from past test runs.
2. **[CRITICAL] Double Button Wiring Breaks Controls Modal**:
   Canvas buttons contain persistent serialized `onClick` events in YAML AND duplicate dynamic listeners added in `UIManager.Start()`. When the player clicks `Controls`, `ToggleControlsModal()` fires twice in the same click (`false -> true -> false`), leaving the dialog permanently closed.
3. **[MAJOR] Player Remains Dead & Uncontrollable on Menu -> Play Transition**:
   If a player dies, returns to Main Menu, and clicks Play, `StartGame()` fails to reset player health, movement, or shooting. The player enters gameplay with 0 HP and disabled controls.
4. **[MAJOR] Unbounded Event Delegate Leak on Game Restarts**:
   `UIManager.HookSceneEntities()` adds listeners to `PlayerHealth.OnHealthChanged` and `GrenadeThrower.OnGrenadeCountChanged` on each `RestartGame()` without ever unsubscribing, leaking over 1,000 duplicate delegate invocations.

---

## Findings

### [Critical] Finding 1: 94 Leaked Test GameObjects Serialized in Production Scene
- **What**: The production scene YAML file `Assets/Scenes/shooting.unity` contains 94 active test artifacts and spawned combat entities from previous test sessions.
- **Where**: `Assets/Scenes/shooting.unity` (lines 306, 357, 672, 929, 977, 1028, 1189, 1220, 1252, etc.).
- **Evidence**:
  Inspecting active objects in the loaded scene via `execute_code`:
  ```csharp
  // Output:
  EnemyBullets in scene: 64, Leaked test objects in scene: 94
  // Breakdown:
  BossBullet: 64, GrenadeProjectile_Fallback: 12, DummyBoss(Clone): 10,
  ExplosionAoE_Fallback: 4, DummyPickup(Clone): 4
  ```
  In `shooting.unity`, each `BossBullet` has `m_IsActive: 1` and contains `CircleCollider2D`, `Rigidbody2D`, and `EnemyBullet` components positioned throughout the arena (e.g., at `(0.00, 0.00, 0.00)`, `(-4.24, -4.24, 0.00)`).
- **Why this is a problem**: Serializing transient test objects directly into the production scene bloats the scene file to 22,000+ lines, pollutes the hierarchy, and introduces stray active colliders and enemy bullets into the play arena.
- **Suggestion**: Clean `Assets/Scenes/shooting.unity` by destroying all `BossBullet`, `DummyBoss*`, `DummyPickup*`, and `*Fallback` GameObjects, then re-saving the clean scene asset.

---

### [Critical] Finding 2: Double Button Wiring Breaks Controls Modal
- **What**: Every button in `shooting.unity` has duplicate event listeners. When clicking `controlsButton`, the modal toggles ON and then immediately OFF in the exact same click event, making it impossible for the player to open the Controls screen.
- **Where**:
  - `Assets/Scenes/shooting.unity` (Canvas button serialized `m_OnClick` persistent listeners)
  - `Assets/scripts/UIManager.cs` lines 112-124 (`WireButtons()`) and lines 384-387 (`OnControlsButtonClicked()`)
- **Evidence**:
  Inspecting button persistent events:
  ```csharp
  controlsButton: count=1 -> Canvas.OnControlsButtonClicked();
  closeControlsButton: count=1 -> Canvas.OnControlsButtonClicked();
  ```
  `UIManager.Start()` additionally executes:
  ```csharp
  if (controlsButton != null) controlsButton.onClick.AddListener(OnControlsButtonClicked);
  ```
  Empirical verification via `execute_code`:
  ```csharp
  ui.controlsModal.SetActive(false);
  ui.controlsButton.onClick.Invoke();
  // Result: ui.controlsModal.activeSelf == false (toggled twice: false -> true -> false)
  ```
- **Why this is a problem**: The Controls instruction screen required by ORIGINAL_REQUEST §R5 cannot be opened by the player during gameplay. Unit test `M5-14` failed to catch this because it tested `ui.ToggleControlsModal()` directly on a mock object instead of invoking the actual button click on the scene canvas.
- **Suggestion**:
  1. In `UIManager.cs`, decouple Open and Close actions into explicit methods:
     ```csharp
     public void OpenControlsModal() => SetControlsModal(true);
     public void CloseControlsModal() => SetControlsModal(false);
     ```
  2. Prevent duplicate listeners in `WireButtons()` by removing prior listeners (`btn.onClick.RemoveListener(...)`) or relying solely on serialized inspector hooks.

---

### [Major] Finding 3: Dead Player & Disabled Controls on Menu -> Play Transition
- **What**: When a player dies and navigates to the Main Menu before clicking Play, they enter gameplay as a dead entity with 0 HP, disabled movement, and disabled shooting.
- **Where**:
  - `Assets/scripts/GameManager.cs` lines 256-265 (`StartGame()`) and lines 441-451 (`ReturnToMainMenu()`)
  - `Assets/scripts/PlayerHealth.cs` lines 122-133 (`Die()`)
- **Evidence**:
  In `PlayerHealth.Die()`, movement and shooting are disabled:
  ```csharp
  PlayerMovement movement = GetComponent<PlayerMovement>();
  if (movement != null) movement.enabled = false;
  Shooting shooting = GetComponent<Shooting>();
  if (shooting != null) shooting.enabled = false;
  ```
  When the player clicks Menu from GameOver, `ReturnToMainMenu()` sets state to `MainMenu`.
  When the player then clicks Play, `StartGame()` only does:
  ```csharp
  CurrentState = GameState.Playing;
  Time.timeScale = 1.0f;
  if (UIManagerRef != null) UIManagerRef.ShowMainMenu(false);
  ```
  Empirical verification via `execute_code`:
  ```csharp
  // 1. Inflict lethal damage
  // 2. ui.OnMenuButtonClicked();
  // 3. ui.OnPlayButtonClicked();
  // Result:
  "Dead before: True, State: Playing, Player HP: 0, Movement enabled: False, Shooting enabled: False"
  ```
- **Why this is a problem**: The core gameplay loop breaks if a player returns to the main menu after dying and starts a new session.
- **Suggestion**: Update `StartGame()` (or `ReturnToMainMenu()`) to ensure a fresh session is initialized when coming from a dead state by invoking `RestartGame()` or resetting player health, controls, score, and spawner.

---

### [Major] Finding 4: Unbounded Event Delegate Leak on Game Restarts
- **What**: Every time `RestartGame()` is called, `UIManager.HookSceneEntities()` appends new delegates to `PlayerHealth.OnHealthChanged` and `GrenadeThrower.OnGrenadeCountChanged` without unsubscribing.
- **Where**: `Assets/scripts/UIManager.cs` lines 155-170 (`HookSceneEntities()`) and `Assets/scripts/GameManager.cs` line 435.
- **Evidence**:
  Inspecting invocation list length after repeated restarts:
  ```csharp
  // After test cycles in editor:
  OnHealthChanged delegate count: 354
  OnGrenadeCountChanged delegate count: 1044
  ```
  In `UIManager.cs`:
  ```csharp
  public void HookSceneEntities()
  {
      var playerHealth = FindObjectOfType<PlayerHealth>();
      if (playerHealth != null)
      {
          playerHealth.OnHealthChanged += UpdateHearts; // Never unsubscribed!
          UpdateHearts(playerHealth.currentHealth);
      }
      var thrower = FindObjectOfType<GrenadeThrower>();
      if (thrower != null)
      {
          thrower.OnGrenadeCountChanged += UpdateGrenades; // Never unsubscribed!
          UpdateGrenades(thrower.GrenadeCount);
      }
  }
  ```
- **Why this is a problem**: In prolonged play sessions, each player hit triggers hundreds or thousands of redundant `UpdateHearts` invocations, causing GC pressure and performance stutter.
- **Suggestion**: Ensure idempotent subscription in `HookSceneEntities()`:
  ```csharp
  playerHealth.OnHealthChanged -= UpdateHearts;
  playerHealth.OnHealthChanged += UpdateHearts;
  thrower.OnGrenadeCountChanged -= UpdateGrenades;
  thrower.OnGrenadeCountChanged += UpdateGrenades;
  ```
  Also unsubscribe in `UIManager.OnDestroy()`.

---

### [Minor] Finding 5: Orphaned / Dead Audio Methods in `SoundManager.cs`
- **What**: `SoundManager` implements procedural 8-bit synthesizers for `PlayHitSFX()`, `PlayExplosionSFX()`, and `PlayHurtSFX()`, but none of the gameplay scripts call them.
- **Where**:
  - `Assets/scripts/SoundManager.cs` lines 99-115
  - Missing call sites in `Assets/scripts/Bullet.cs`, `Assets/scripts/ExplosionAoE.cs`, `Assets/scripts/PlayerHealth.cs`
- **Evidence**:
  Grep for `PlayHitSFX`, `PlayExplosionSFX`, and `PlayHurtSFX` reveals calls only exist in test files (`Milestone5Tests.cs`, `E2ETier1Tests.cs`). In actual gameplay, bullet impacts, grenade detonations, and player damage produce no audio.
- **Suggestion**: Hook `PlayExplosionSFX()` into `ExplosionAoE.Explode()`, `PlayHitSFX()` into `Bullet.HandleHit()`, and `PlayHurtSFX()` into `PlayerHealth.TakeDamage()`.

---

### [Minor] Finding 6: Missing Runtime Clamping for `masterVolume` and `sfxVolume`
- **What**: In `SoundManager.PlayClip()`, volume scaling relies on `masterVolume * sfxVolume * Mathf.Clamp01(volumeScale)`.
- **Where**: `Assets/scripts/SoundManager.cs` line 140.
- **Evidence**:
  `[Range(0f, 1f)]` only bounds values in the Inspector. Setting `masterVolume` or `sfxVolume` via script to values > 1.0f or < 0f results in unclamped audio levels.
- **Suggestion**: Apply `Mathf.Clamp01` to `masterVolume` and `sfxVolume` in `PlayClip()`:
  ```csharp
  float effectiveVolume = Mathf.Clamp01(masterVolume) * Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(volumeScale);
  ```

---

## Verified Claims

- **Unity Console Compiler Output**: Verified via `unityMCP.read_console` -> 0 compiler errors.
- **Milestone 5 Test Suite (`Milestone5Tests.RunAllTests`)**: Verified via `unityMCP.execute_code` -> Total: 15, Passed: 15, Failed: 0, Pending: 0.
- **Full E2E Test Suite (`E2ETestRunner.RunAll`)**: Verified via `unityMCP.execute_code` -> Total: 385, Passed: 385, Failed: 0, Pending: 0, Skipped: 0.
- **Challenger 1 M5 Suite (`Challenger1M5Tests.RunAllTests`)**: Verified via `unityMCP.execute_code` -> Total: 31, Passed: 31, Failed: 0, Pending: 0.
- **HUD Heart Sprites**: Verified `fullHeartSprite` maps to `hearts-1` and `emptyHeartSprite` maps to `hearts-2`.
- **PlayerPrefs High Score**: Verified persistence across runs with lower-score rejection.
- **Procedural 8-bit Audio Generation**: Verified all 7 clips synthesize in-memory without missing asset dependencies.

---

## 1. Observation
- `unityMCP.read_console`:
  ```json
  {"success":true,"message":"Retrieved 10 log entries.","data":["... 0 compiler errors"]}
  ```
- `unityMCP.execute_code` (`Tests.Milestone5Tests.RunAllTests()`):
  ```json
  {"result":"Total: 15, Passed: 15, Failed: 0, Pending: 0","compiler":"roslyn"}
  ```
- `unityMCP.execute_code` (`E2ETests.E2ETestRunner.RunAll()`):
  ```json
  {"result":"Total: 385, Passed: 385, Failed: 0, Pending: 0, Skipped: 0","compiler":"roslyn"}
  ```
- `unityMCP.execute_code` (Inspection of leaked objects in `shooting.unity`):
  ```json
  {"result":"EnemyBullets in scene: 64, Leaked test objects in scene: 94","compiler":"roslyn"}
  ```
- `unityMCP.execute_code` (Controls button double toggle test):
  ```json
  {"result":"Modal activeSelf after 1 click on controlsButton: False","compiler":"roslyn"}
  ```
- `unityMCP.execute_code` (Player death -> Menu -> Play flow test):
  ```json
  {"result":"Dead before: True, State: Playing, Player HP: 0, Movement enabled: False, Shooting enabled: False","compiler":"roslyn"}
  ```
- `unityMCP.execute_code` (Event delegate leak test):
  ```json
  {"result":"OnHealthChanged delegate count on PlayerHealth: 354, OnGrenadeCountChanged: 1044","compiler":"roslyn"}
  ```

---

## 2. Logic Chain
1. *From inspection of `Assets/Scenes/shooting.unity`*: 94 leaked test objects (including 64 active `BossBullet` entities) were saved directly into the scene YAML.
2. *From inspection of Canvas buttons and `UIManager.WireButtons()`*: Buttons have both persistent serialized `onClick` events in YAML and dynamic listeners added in `Start()`. This causes two invocations per click. Because `controlsButton` executes `ToggleControlsModal()`, it toggles `false -> true -> false`, preventing the Controls screen from opening.
3. *From tracing player state across the FSM*: `PlayerHealth.Die()` disables `PlayerMovement` and `Shooting`. When returning to Main Menu and clicking Play, `StartGame()` activates `GameState.Playing` without resetting player health or components, leaving the player paralyzed with 0 HP.
4. *From tracing `UIManager.HookSceneEntities()`*: Each restart subscribes `UpdateHearts` and `UpdateGrenades` without removing prior handlers, accumulating hundreds of duplicate delegate calls.
5. *From adversarial code audit*: These issues compromise gameplay stability and violate acceptance criteria (intact game loop and clean scene), necessitating changes before milestone approval.

---

## 3. Caveats
- No caveats. All findings were verified empirically using live Unity Editor execution (`execute_code`) on the target project.

---

## 4. Conclusion
**Verdict: REQUEST_CHANGES**

Milestone 5 cannot be approved in its current state due to 2 Critical and 2 Major defects:
1. Production scene `Assets/Scenes/shooting.unity` is polluted with 94 active leaked test objects (including 64 Boss bullets).
2. Canvas button double-wiring prevents the Controls screen from ever displaying.
3. Returning to Main Menu after dying results in a paralyzed, dead player upon starting a new game.
4. `UIManager.HookSceneEntities()` leaks event delegates without bound on game restarts.

---

## 5. Verification Method

To independently reproduce and verify all findings:

1. **Verify Scene Object Pollution**:
   Execute via `unityMCP.execute_code`:
   ```csharp
   var dummys = GameObject.FindObjectsOfType<GameObject>();
   int count = 0;
   foreach (var go in dummys) {
       if (go.name.Contains("Dummy") || go.name.Contains("Fallback") || go.name.Contains("BossBullet")) count++;
   }
   return $"Leaked test objects in scene: {count}";
   ```
   *Expected defect confirmation*: returns `Leaked test objects in scene: 94`.

2. **Verify Controls Button Failure**:
   Execute via `unityMCP.execute_code`:
   ```csharp
   var ui = GameObject.Find("Canvas").GetComponent<UIManager>();
   ui.controlsModal.SetActive(false);
   ui.controlsButton.onClick.Invoke();
   return $"Modal activeSelf: {ui.controlsModal.activeSelf}";
   ```
   *Expected defect confirmation*: returns `Modal activeSelf: False`.

3. **Verify Die -> Menu -> Play State Paralyzation**:
   Execute via `unityMCP.execute_code`:
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
   return $"HP: {player.currentHealth}, Movement: {player.GetComponent<PlayerMovement>().enabled}";
   ```
   *Expected defect confirmation*: returns `HP: 0, Movement: False`.

4. **Verify Delegate Leak**:
   Execute via `unityMCP.execute_code`:
   ```csharp
   for (int i = 0; i < 5; i++) GameManager.Instance.RestartGame();
   var thrower = GameObject.FindObjectOfType<GrenadeThrower>();
   var f = typeof(GrenadeThrower).GetField("OnGrenadeCountChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
   var len = ((System.MulticastDelegate)f.GetValue(thrower))?.GetInvocationList().Length ?? 0;
   return $"Delegate count: {len}";
   ```
   *Expected defect confirmation*: delegate count increases on each restart.
