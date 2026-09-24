## 2026-09-21T20:17:22Z
You are the Implementation Worker for Milestone 3: Grenade Mechanic (AoE Pickup & Throw).
Working Directory: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\worker_m3
Original Request: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\ORIGINAL_REQUEST.md
Project Index: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\PROJECT.md
Specification Report: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\spec_miner_survey\survey_spec.md
Test Runner: Assets/scripts/Tests/E2ETestRunner.cs

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Ownership:
You exclusively own and may create/edit:
- Assets/scripts/GrenadePickup.cs
- Assets/scripts/GrenadeThrower.cs
- Assets/scripts/GrenadeProjectile.cs
- Assets/scripts/ExplosionAoE.cs
- Assets/Prefabs/ (GrenadePickup, GrenadeProjectile, ExplosionAoE prefabs)
- Assets/Scenes/shooting.unity (add GrenadeThrower component to Player GameObject)
- Assets/scripts/Tests/Milestone3Tests.cs

Your Mission:
1. Read ORIGINAL_REQUEST.md, PROJECT.md, and survey reports.
2. Implement Milestone 3 according to specifications:
   - GrenadePickup.cs: Trigger collider item. When Player touches it, increases player's grenade count via GrenadeThrower, plays pickup feedback (event or sound), and destroys itself cleanly.
   - GrenadeThrower.cs: Component attached to Player GameObject.
     - Tracks grenadeCount (starting e.g. at 1 or 2, max inventory e.g. 5).
     - Dispatches event OnGrenadeCountChanged(int count).
     - Listens for KeyCode.E and RMB (Input.GetButtonDown("Fire2") or Input.GetMouseButtonDown(1)).
     - When thrown: if grenadeCount > 0, decrements count, computes target world position from mouse cursor, clamps throw distance to max 7.0 units from player, and instantiates GrenadeProjectile directed toward target.
   - GrenadeProjectile.cs: Moves toward target coordinate (or physics arc), decelerates/arrives, detonates upon fuse expiration (e.g. 1.0s) or upon direct collision with enemies/walls, instantiating ExplosionAoE.
   - ExplosionAoE.cs: Detonates in a 3.5 unit radius using Physics2D.OverlapCircleAll. Inflicts 50 damage on all IDamageable targets in radius (destroying regular enemies). Spawns Fire Effect VFX scaled up (e.g. 3.5x scale) or particle effect, auto-destroys.
   - Wire Player in shooting.unity with GrenadeThrower referencing grenadePrefab.
3. Validate compilation with Unity MCP read_console (0 errors).
4. Run automated tests in Unity Editor:
   - Run your own Milestone3Tests.
   - Run E2ETestRunner.RunAll() and verify features F16-F20 pass cleanly.
5. Write your handoff report to c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity\.agents\worker_m3\handoff.md.
6. Maintain progress.md in your working directory and notify the orchestrator when complete.
