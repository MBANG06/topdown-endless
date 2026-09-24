# Progress Log - Milestone 2: Enemy Archetypes & Spawner System

Last visited: 2026-09-22T00:23:00Z
Status: Completed all Milestone 2 deliverables and test suites with 100% pass rate.

## Completed Tasks
- [x] Initial setup: DISPATCH.md and BRIEFING.md created.
- [x] Researched specifications in survey_spec.md, survey_assets.md, PROJECT.md, and E2ETestRunner.
- [x] Implemented `Assets/scripts/EnemyBase.cs`: Abstract base class implementing IDamageable, damage flash, score dispatch, death VFX, and grenade drop roll.
- [x] Implemented `Assets/scripts/ChaserEnemy.cs`: Melee pursuit archetype with direct tracking and contact damage (3 HP, 2.8 speed, 10 score, 20% drop).
- [x] Implemented `Assets/scripts/EnemyBullet.cs`: Hostile projectile dealing damage to player, ignoring friendly enemies, and auto-destructing on walls/lifetime.
- [x] Implemented `Assets/scripts/ShooterEnemy.cs`: Ranged archetype kiting at 3.8-5.5u, firing EnemyBullet at player (2 HP, 2.0 speed, 20 score, 25% drop).
- [x] Implemented `Assets/scripts/RusherEnemy.cs`: High-speed glass cannon interceptor with 1 HP, 6.2 speed, 15 score, 15% drop.
- [x] Implemented `Assets/scripts/EnemySpawner.cs`: Off-screen perimeter spawner with progressive scaling curves for interval I(t, S) and concurrency cap N(t, S), archetype selection weights, and Boss suppression hook.
- [x] Created prefabs in `Assets/Prefabs/`:
  - `ChaserEnemy.prefab` (Treant sprite, CircleCollider2D, Rigidbody2D, ChaserEnemy component)
  - `ShooterEnemy.prefab` (Treant sprite with violet tint, CircleCollider2D, Rigidbody2D, ShooterEnemy component)
  - `RusherEnemy.prefab` (Mole sprite with amber tint, CircleCollider2D, Rigidbody2D, RusherEnemy component)
  - `EnemyBullet.prefab` (Arrow sprite, CircleCollider2D trigger, Rigidbody2D, EnemyBullet component)
  - `GrenadePickup.prefab` (Gem sprite, CircleCollider2D trigger)
- [x] Scene updates in `Assets/Scenes/shooting.unity`:
  - Tagged Player GameObject with "Player" tag.
  - Added "EnemySpawner" GameObject, attached `EnemySpawner` component, and wired prefab references.
  - Saved scene cleanly.
- [x] Verified 0 compiler errors via Unity MCP read_console.
- [x] Implemented and executed `Milestone2Tests.cs`: 16/16 Passed (100%).
- [x] Executed full test regression suite:
  - `Milestone1Tests`: 12/12 Passed (100%)
  - `ChallengerM1Tests`: 14/14 Passed (100%)
  - `Milestone2Tests`: 16/16 Passed (100%)
  - `E2ETestRunner`: 352/385 Passed, 0 Failed, 33 Pending (only M3-M5 pending)
