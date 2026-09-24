# Progress - Worker M3 (Milestone 3: Grenade Mechanic)

- **Status**: Completed
- **Last visited**: 2026-09-22T03:25:25+07:00
- **Current Step**: Implementation, verification, and regression tests complete (100% pass)

### Accomplishments:
1. Implemented `GrenadePickup.cs` with trigger collider, zero gravity, arena boundary clamping, player collection, and max capacity rejection.
2. Implemented `GrenadeThrower.cs` attached to Player, handling E and RMB throw inputs, 7.0u distance clamping, arena bounds clamping, inventory tracking (max 5), event dispatching, and pause/death guards.
3. Implemented `GrenadeProjectile.cs` with parabolic trajectory height curve, 0.7s flight duration, 1.2s fuse, collision detonation on hostile entities/colliders, friendly fire immunity, and ExplosionAoE instantiation.
4. Implemented `ExplosionAoE.cs` with 3.5u blast radius query, 50 damage to hostile IDamageable entities, friendly fire immunity for Player, scaled Fire Effect VFX, and clean lifetime management.
5. Configured `GrenadePickup.prefab` with GrenadePickup component and zero gravity.
6. Created `GrenadeProjectile.prefab` and `ExplosionAoE.prefab` with all required components, visual effects, and references.
7. Wired Player GameObject in `Assets/Scenes/shooting.unity` with `GrenadeThrower` referencing `GrenadeProjectile.prefab`.
8. Created `Milestone3Tests.cs` covering 20 comprehensive unit and integration tests (20/20 Passed).
9. Ran `E2ETestRunner.RunAll()`: Features F16-F20 are 50/50 Passed (0 Failed); Total test suite: 362 Passed, 0 Failed, 23 Pending (future milestones).
10. Ran all regression suites: Milestone 1 (12/12), Milestone 2 (16/16), Challenger M1 (14/14), Challenger M2 (17/17). Zero regressions.
11. Confirmed 0 compiler errors via `read_console`.
