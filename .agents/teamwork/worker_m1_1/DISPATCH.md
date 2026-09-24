## 2026-09-22T14:39:10Z
You are worker_m1_1 (teamwork_preview_worker).
Your working directory is: c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity/.agents/teamwork/worker_m1_1/
Project root: c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

CRITICAL INSTRUCTIONS:
1. Read the original user request at:
   c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity/.agents/teamwork/ORIGINAL_REQUEST.md
2. Read PROJECT.md at project root and the M1 explorer handoff reports:
   - c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity/.agents/teamwork/explorer_m1_1/handoff.md
   - c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity/.agents/teamwork/explorer_m1_2/handoff.md
   - c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity/.agents/teamwork/explorer_m1_3/handoff.md
3. File Write Ownership:
   You exclusively own:
   - Assets/scripts/ScrollingCameraController.cs (create new)
   - Assets/scripts/PlayerMovement.cs (modify)
   - Assets/scripts/GrenadeThrower.cs (modify)
   - Assets/scripts/GrenadePickup.cs (modify)
   - Assets/scripts/ShooterEnemy.cs (modify)
4. Implement the components:
   - Implement `Assets/scripts/ScrollingCameraController.cs` following the exact C# code and specification in `explorer_m1_1/handoff.md §4.1`.
     * Ensure in `Start()`, if `MapBounds/Wall_Top` exists in the scene, disable its `BoxCollider2D` component at runtime so scrolling entities pass through freely during PlayMode, while preserving the GameObject for EditMode tests.
   - Update `Assets/scripts/PlayerMovement.cs` following `explorer_m1_2/handoff.md`:
     * Preserve exact default fields: `clampToBounds = true`, `minBounds = (-8.5f, -4.2f)`, `maxBounds = (13.8f, 5.2f)`!
     * Add `clampToViewport` mode with viewport bounds `[0.05, 0.95]` X and `[0.08, 0.92]` Y.
     * Implement bottom edge push/kill logic: if viewport Y < 0.04, push player forward and call `PlayerHealth.TakeDamage(1)`.
   - Update `GrenadeThrower.cs`, `GrenadePickup.cs`, and `ShooterEnemy.cs` per `explorer_m1_3/handoff.md` so they adapt when `ScrollingCameraController.Instance != null` without breaking legacy default bounds.
5. Compilation & Scene Integration:
   - Refresh and compile using unityMCP `refresh_unity`.
   - Confirm 0 compilation errors via `read_console`.
   - Attach `ScrollingCameraController` to `Main Camera` in `Assets/Scenes/shooting.unity` using `execute_code` with `Undo.AddComponent` and `EditorSceneManager.SaveScene` (follow `explorer_m1_1/handoff.md §4.2 Step 4`).
6. Verification:
   - Run `execute_menu_item(menu_path='E2E Tests/Run All Tests')` or run `E2ETestRunner.RunAllFormatted()` via `execute_code`.
   - Ensure all 541 tests pass (385 baseline + 36 Tier 5 + 120 scrolling map tests).
7. Write your handoff report to:
   c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity/.agents/teamwork/worker_m1_1/handoff.md
8. Notify orchestrator_1 when done using send_message.
