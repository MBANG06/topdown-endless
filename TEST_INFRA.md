# E2E Test Infra: Continuous Upward (+Y) Endless Scrolling System

## Test Philosophy
- Opaque-box, requirement-driven testing. Derived strictly from `ORIGINAL_REQUEST.md` and user-facing acceptance criteria.
- Methodology: Category-Partition + Boundary Value Analysis (BVA) + Pairwise Combinatorial Testing + Real-World Workload Testing.

## Feature Inventory
| # | Feature | Source | Tier 1 (Coverage) | Tier 2 (Boundaries) | Tier 3 (Interactions) |
|---|---------|--------|:-----------------:|:-------------------:|:---------------------:|
| 1 | Camera +Y Scrolling (2.0 - 3.5 u/s) | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| 2 | Player Viewport Clamping (X: 0.05-0.95, Y: 0.08-0.92) | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| 3 | Bottom Edge Push / Kill Plane | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| 4 | MapSegment Modular Spawning (20u length, >=4u corridor) | ORIGINAL_REQUEST R2 | 5 | 5 | ✓ |
| 5 | Segment Object Pooling (Zero GC, cleanup at camY - 25u) | ORIGINAL_REQUEST R2 | 5 | 5 | ✓ |
| 6 | 500-Point Boss Encounter & Center Lock | ORIGINAL_REQUEST R3 | 5 | 5 | ✓ |
| 7 | Boss 16-Bullet 360° Radial Barrage | ORIGINAL_REQUEST R3 | 5 | 5 | ✓ |
| 8 | Boss Defeat Rewards (2 Grenades, 500 pts, Resume) | ORIGINAL_REQUEST R3 | 5 | 5 | ✓ |
| 9 | HUD Indicators (Distance, Warning, Boss Arena Banner) | ORIGINAL_REQUEST R4 | 5 | 5 | ✓ |
| 10 | 10+ Minute Stability & Memory Zero-GC | ORIGINAL_REQUEST R5 | 5 | 5 | ✓ |

## Test Architecture
- **Execution Channels**:
  1. `E2E Tests/Run All Tests` via unityMCP `execute_menu_item` or `E2ETestRunner.RunAllFormatted()`.
  2. NUnit Unity Test Runner via unityMCP `run_tests(mode='EditMode')` and `run_tests(mode='PlayMode')`.
- **Pass/Fail Semantics**: 100% assertion pass with 0 errors and 0 exceptions.
- **Test File Location**: `Assets/scripts/Tests/ScrollingMapTests.cs` (and integration with `E2ETestRunner.cs`).

## Real-World Application Scenarios (Tier 4)
| # | Scenario | Features Exercised | Complexity |
|---|----------|--------------------|------------|
| 1 | Full Endless Run: Player traverses 1000m through 50 segments with continuous speed ramp | F1, F2, F3, F4, F5, F9, F10 | High |
| 2 | Boss Arena Transition & Victory: Score reaches 500, arena spawns, camera locks, boss defeated, scrolling resumes | F1, F4, F6, F7, F8, F9 | High |
| 3 | Heavy Combat Obstacle Navigation: Player navigates narrow corridor while dodging enemies and throwing grenades | F2, F4, F5, F9 | Medium |
| 4 | Bottom Edge Trap Recovery: Player pinned by obstacle collider, pushed forward without falling off screen | F2, F3, F4 | Medium |
| 5 | Rapid Boss Defeat with Grenades: Throwing 2 grenades deals 100 damage to boss, triggering immediate rewards and resume | F6, F7, F8, F9 | Medium |

## Coverage Thresholds
- **Tier 1 (Feature Coverage)**: ≥ 50 test cases (≥5 per feature)
- **Tier 2 (Boundary & Corner Cases)**: ≥ 50 test cases (min/max speed, viewport corners, zero/negative distance, pool limits)
- **Tier 3 (Cross-Feature Combinations)**: ≥ 15 pairwise interaction tests
- **Tier 4 (Real-World Application Scenarios)**: ≥ 5 realistic end-to-end integration tests
- **Total Minimum Target**: ≥ 120 test cases dedicated to R1-R5, run alongside the existing 421 baseline tests.
