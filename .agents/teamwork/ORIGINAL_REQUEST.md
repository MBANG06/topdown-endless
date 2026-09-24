# Original User Request

## 2026-09-22T14:17:51Z

Implement a continuous upward (+Y) endless scrolling map system for the existing Unity 2D top-down shooter, maintaining 8-directional WASD movement, mouse aiming, grenade drops, 5 HP survival, and a 500-point boss encounter with radial projectile barrage.

Working directory: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity
Integrity mode: development

## Requirements

### R1. Forward Camera Scrolling & Player Viewport Clamping
The main camera must continuously scroll along the +Y axis at a baseline speed (default 2.0 u/s) that scales with progression up to a safety ceiling (3.5 u/s). The player's WASD movement must be restricted inside the visible camera viewport; if pushed behind the camera's bottom edge threshold, the player receives damage or triggers game over.

### R2. Modular Map Segment Spawning & Object Pooling
The map must be generated procedurally ahead of the camera using interchangeable MapSegment prefabs (length 20 units) chosen with controlled randomness. Segments moving behind the camera's cleanup distance must be deactivated and recycled into an object pool to guarantee zero runtime GC allocations and memory stability over long play sessions. Each segment must guarantee at least one passable corridor (minimum width 4.0 units) free of impassable obstacles.

### R3. Seamless Boss Arena Encounter & Resume Loop
Upon reaching the configurable threshold (500 points), standard segment generation temporarily halts and a dedicated Boss Arena segment spawns ahead. When the camera aligns with the boss arena, camera scrolling locks in place. The boss spawns and executes its 360-degree radial projectile barrage. When defeated, the boss drops 2 guaranteed grenades, awards 500 points, unlocks the camera, and resumes normal endless scrolling.

### R4. HUD Indicators & Telegraphed Warnings
The HUD must display current Score, High Score, 5 HP Heart icons, Grenade count, travelled Distance (in meters), an early telegraph warning ("BOSS APPROACHING!"), and a "BOSS ARENA" status banner while in the arena.

### R5. unity-MCP Automation & Automated Verification
The system must be inspectable and verifiable using unity-MCP tools and must pass automated unit/integration tests covering segment connection, pooling recycling, player viewport bounds clamping, and boss encounter state transitions.

## Acceptance Criteria

### Camera & Player Movement
- [ ] Camera auto-scrolls along +Y smoothly without jitter in FixedUpdate/LateUpdate.
- [ ] Player WASD movement remains responsive within camera boundaries (viewport X: 0.05 - 0.95, Y: 0.08 - 0.92).
- [ ] Bottom kill/push plane prevents player from getting lost behind the moving screen.

### Segment Generation & Pooling
- [ ] At least 3 distinct interchangeable MapSegment prefabs with varied obstacle layouts exist and spawn seamlessly without collision overlap.
- [ ] Passing corridors are guaranteed (width >= 4 units) so no impossible dead-ends occur.
- [ ] Off-screen segments are cleanly recycled to object pool without GameObject.Destroy or memory leaks.

### Combat & Spawning Mechanics
- [ ] Enemies spawn exclusively from valid SpawnPoints inside upcoming segments or dynamic perimeter relative to moving camera.
- [ ] Grenade pickups spawn or drop from killed enemies; grenade throw AoE functions accurately during scrolling.
- [ ] Player health behaves correctly: 5 HP maximum, losing 1 HP per hit, triggering Game Over at 0 HP.

### Boss Arena Flow
- [ ] At 500 points, boss arena segment spawns ahead and camera stops scrolling when reaching arena center.
- [ ] Boss executes telegraphed radial barrage (16 bullets, 360 degrees).
- [ ] Defeating boss awards 500 points, drops 2 guaranteed grenades, triggers victory fanfare, and resumes endless scrolling upon continuation.

### Verification & Stability
- [ ] Game runs continuously for 10+ minutes without FPS drop or unbounded memory usage.
- [ ] NUnit tests in Unity Test Runner pass via unity-MCP `run_tests`.
