# Adversarial Challenge Report: Milestone 3 (Grenade Mechanic: AoE Pickup & Throw)

**Agent**: Challenger 2 (Empirical Challenger M3)  
**Milestone**: Milestone 3 (AoE Pickup & Throw, F16–F20, F33)  
**Date**: 2026-09-22T03:32:00+07:00  

---

## Challenge Summary

**Overall risk assessment**: **LOW**  
All core Milestone 3 grenade mechanics were empirically verified through custom test suites, direct reflection probes, physics simulation, and real PlayMode execution in the Unity Editor. No game-breaking bugs, exploit paths, or runtime memory leaks were found during live gameplay.

---

## Challenges

### [Low] Challenge 1: EditMode Fallback Instantiation in Test Runners
- **Assumption challenged**: When running automated tests without assigning a `grenadePrefab` to `GrenadeThrower`, `ThrowGrenade` creates a fallback GameObject (`"GrenadeProjectile_Fallback"`).
- **Attack scenario**: In EditMode, time does not advance continuously. If an automated test invokes `thrower.ThrowGrenade(target)` without assigning a prefab, `GrenadeThrower` instantiates a fallback GameObject via `new GameObject("GrenadeProjectile_Fallback")`. Because this fallback is not tracked by the test context's disposable tracker and its `Update()` loop is not stepped, the fallback object lingers in the scene hierarchy.
- **Blast radius**: Low. In real gameplay (PlayMode), `Player` has `grenadePrefab` assigned to `GrenadeProjectile.prefab`. Even if fallback is spawned in PlayMode, `Update()` ticks, the 1.2s fuse expires, and `Detonate()` cleanly destroys the projectile. The leak only manifested in EditMode test suites that omitted cleanup.
- **Mitigation**: Automated test harnesses must assign a tracked dummy projectile or clean up spawned clones immediately after calling `ThrowGrenade()`.

### [Low] Challenge 2: Single-Precision Floating Point Epsilon on Parabolic Arc
- **Assumption challenged**: Mathematical evaluation of $\sin(\pi)$ equals $0.0$ exactly.
- **Attack scenario**: In IEEE 754 single-precision float math, `Mathf.Sin(Mathf.PI)` evaluates to approximately `-8.742278E-08f`. If trajectory scaling relied solely on the raw sinusoidal evaluation, scale at $t=1.0$ could dip imperceptibly below $1.0$ ($0.9999999$).
- **Blast radius**: Negligible.
- **Mitigation verified in implementation**: `GrenadeProjectile.cs` (lines 76–80) explicitly enforces `if (t >= 1.0f) { _hasArrived = true; transform.position = _targetPosition; transform.localScale = _baseScale; }`. This completely eliminates floating-point drift upon landing.

### [Low] Challenge 3: Delayed Destruction in EditMode Lifecycle
- **Assumption challenged**: `ExplosionAoE` auto-destruction via `Destroy(gameObject, lifetime)` does not execute in EditMode.
- **Attack scenario**: In EditMode, Unity disallows delayed destruction calls (`Destroy(obj, delay)`). `ExplosionAoE.cs` guards this with `if (Application.isPlaying) Destroy(gameObject, lifetime);`. In EditMode, `ExplosionAoE` remains alive unless cleaned up by the test context.
- **Blast radius**: Low. In PlayMode, Unity handles delayed destruction natively, destroying the explosion and VFX after 0.6 seconds. Verified in live PlayMode: 0 lingering objects after detonation.
- **Mitigation**: Maintain the existing `if (Application.isPlaying)` guard; ensure test runners destroy created explosion instances.

---

## Stress Test Results

| Test ID | Category | Scenario | Expected Behavior | Actual Behavior | Verdict |
|---|---|---|---|---|---|
| **CH-M3-01** | Friendly Fire | Player at explosion epicenter ($d=0$) | HP remains 5; IsAlive=true | HP=5, IsAlive=true | **PASS** |
| **CH-M3-02** | Friendly Fire | Player at offsets $d \in \{0.5, 1.0, 2.0, 3.0, 3.49\}$ | HP remains 5 across all distances | HP=5 at all distances | **PASS** |
| **CH-M3-03** | Friendly Fire | Compound Player (untagged child hurtbox collider) | 0 damage received; HP remains 5 | HP=5 | **PASS** |
| **CH-M3-04** | Friendly Fire | 20 simultaneous overlapping explosions at epicenter | HP remains 5; no accumulated damage | HP=5 | **PASS** |
| **CH-M3-05** | AoE Discrimination | Co-located Player + Chaser, Shooter, Rusher | Enemies eliminated (HP=0); Player HP=5 | Enemies eliminated; Player HP=5 | **PASS** |
| **CH-M3-06** | Projectile Impact | Projectile passes through or hits Player | Collision ignored; no early detonation | `_hasDetonated=false` | **PASS** |
| **CH-M3-07** | Max Capacity | Walking over pickup at 5 grenades | Rejects collection; count=5; pickup alive | `TryCollect=false`, count=5, pickup alive | **PASS** |
| **CH-M3-08** | Max Capacity | Player at 5 grenades walking over 3 pickups | All 3 pickups reject collection; count=5 | 3/3 rejected; all 3 alive | **PASS** |
| **CH-M3-09** | Max Capacity | Throw 1 grenade (5 -> 4), walk over rejected pickup | Pickup collected; count restored to 5 | `TryCollect=true`, count=5, pickup destroyed | **PASS** |
| **CH-M3-10** | Max Capacity | `AddGrenades(10)` when inventory is at 3 | Inventory clamped strictly at 5 | Count=5; further adds return false | **PASS** |
| **CH-M3-11** | Non-Player Immunity | Enemies, bullets, and walls touching pickup | Rejects collection; pickup untouched | All rejected; pickup alive | **PASS** |
| **CH-M3-12** | Boundary Clamp | Drop pickups at extreme coordinates ($\pm 100$) | Clamped inside $[-8.5, 13.8] \times [-4.2, 5.2]$ | All clamped inside arena bounds | **PASS** |
| **CH-M3-13** | Trajectory Arc | Parabolic scale sampling at $t \in \{0, 0.25, 0.5, 0.75, 1.0\}$ | Peak scale 1.5 at apex; landing scale 1.0 | Apex=1.50, Landing=1.00 | **PASS** |
| **CH-M3-14** | Trajectory Flight | Trajectory destination interpolation over 0.7s | Interpolates smoothly to target | Arrives at target in 0.7s | **PASS** |
| **CH-M3-15** | Fuse Detonation | Fuse timeout check at 1.15s vs 1.20s | Not detonated at 1.15s; detonates at 1.20s | Detonates strictly at $t \ge 1.2$s | **PASS** |
| **CH-M3-16** | Impact Detonation | Direct projectile impact on Enemy | Immediate detonation before fuse expires | `_hasDetonated=true` immediately | **PASS** |
| **CH-M3-17** | Impact Detonation | Direct projectile impact on Arena Wall | Immediate detonation before fuse expires | `_hasDetonated=true` immediately | **PASS** |
| **CH-M3-18** | Distance Clamp | Throw target requested at 18 units away | Clamped to magnitude 7.0u from player | Vector distance=7.000u | **PASS** |
| **CH-M3-19** | Arena Clamp | Throw target requested outside arena boundary | Clamped within arena perimeter | Clamped inside arena boundary | **PASS** |
| **CH-M3-20** | Zero Vector | Throw target requested at exact player pos $(0, 0)$ | Safe handling; no NaN or crash | Decrements count; pos valid (no NaN) | **PASS** |
| **CH-M3-21** | Zero Inventory | Throw grenade when inventory is 0 | Blocked; count remains 0; nothing spawned | Count=0; no projectile | **PASS** |
| **CH-M3-22** | State Guards | Throw grenade when paused or player dead | Blocked; inventory unchanged | Count unchanged in both cases | **PASS** |
| **CH-M3-23** | Event Dispatch | `OnGrenadeCountChanged` dispatch values | Exact count dispatched on add, throw, reset | Correct count dispatched | **PASS** |
| **CH-M3-24** | Prefab Wiring | Projectile -> Explosion -> VFX references | All asset references valid and linked | All valid and non-null | **PASS** |
| **CH-M3-25** | Crowd Clearing | 30 packed Chaser enemies in blast radius | All 30 enemies eliminated simultaneously | 30/30 eliminated (0 survived) | **PASS** |
| **CH-M3-26** | Memory Cleanliness | 20 consecutive projectile spawns & detonations | 100% projectiles destroyed; 0 net leak | 0/20 alive; net leak=0 | **PASS** |

---

## Live PlayMode Verification

In addition to programmatic tests, live execution was validated in real Unity PlayMode:
1. **Grenade Throw**: Decremented count from 2 to 1, spawned `GrenadeProjectile(Clone)`, flew along parabolic arc, landed, detonated at 1.2s fuse, spawned `ExplosionAoE(Clone)` and `Fire Effect(Clone)`.
2. **Auto-Destruction**: After 1.8s (1.2s fuse + 0.6s lifetime), all projectile, explosion, and VFX clones were cleanly destroyed by Unity runtime.
3. **Pickup Collection**: Player touched pickup item; count incremented from 1 to 2; pickup was cleanly destroyed at end of frame.
4. **Max Capacity Rejection**: Player at 5 grenades walked over pickup; pickup was rejected and remained in the scene hierarchy.
5. **Friendly Fire Immunity**: Explosion detonated directly at Player coordinates; Player HP remained 5.
6. **Console Status**: 0 errors, 0 warnings.

---

## Unchallenged Areas

- **Sound Feedback (`SoundManager.PlayPickupSFX`)**: Audio implementation is scheduled for Milestone 5; pickup script includes a reflection hook that gracefully falls back without throwing exceptions.
- **Boss Encounter AoE Interaction**: Boss mechanics and Boss HP slider are scheduled for Milestone 4; however, `TakeDamage(50)` on high-HP `IDamageable` targets was verified.
