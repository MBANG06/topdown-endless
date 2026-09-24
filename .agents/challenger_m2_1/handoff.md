# Challenger Handoff Report — Milestone 2: Enemy Archetypes & Spawner System

**Milestone**: Milestone 2 (Enemy Archetypes & Spawner System)  
**Agent**: Challenger 1 (EMPIRICAL CHALLENGER / critic, specialist)  
**Verdict**: **APPROVE**

---

## 1. Observation

All empirical tests were executed directly in the Unity Editor environment via Unity MCP (`execute_code`, `manage_editor`, `read_console`).

### 1.1 Baseline Test Suite Execution
- `Milestone2Tests.RunAllTests()`: 16 tests executed, 16 passed, 0 failed.
- Full E2E Test Suite (`E2ETestRunner.RunAll()`):
  ```
  Total Tests: 385 | Passed: 352 | Failed: 0 | Pending: 33 | Skipped: 0 in 10.00ms
  Tier 1 (Happy Path): 142/175 Passed, 0 Failed, 33 Pending
  Tier 2 (Boundary): 175/175 Passed, 0 Failed, 0 Pending
  Tier 3 (Pairwise): 30/30 Passed, 0 Failed, 0 Pending
  Tier 4 (Scenarios): 5/5 Passed, 0 Failed, 0 Pending
  ```
  *(33 pending tests strictly belong to M3 Grenade AoE, M4 Boss Encounter, and M5 UI/HUD & SoundManager).*
- Unity Editor Console: 0 compiler errors, 0 runtime exceptions via `read_console`.

---

### 1.2 Adversarial Challenge Suite 1: Shooter Kiting Behavior (`ShooterEnemy.cs`)

Direct empirical test via `execute_code`:
1. **Retreat Behavior ($d < 3.8\text{u}$)**:
   Tested at distances $d \in \{1.0\text{u}, 2.0\text{u}, 3.0\text{u}, 3.79\text{u}\}$:
   - $d = 1.00\text{u}$: displacement = $-0.0400$, pass = `True`
   - $d = 2.00\text{u}$: displacement = $-0.0400$, pass = `True`
   - $d = 3.00\text{u}$: displacement = $-0.0400$, pass = `True`
   - $d = 3.79\text{u}$: displacement = $-0.0400$, pass = `True`
   In all cases, Shooter strictly retreated away from the player with velocity $-2.0 \times 0.02 = -0.0400\text{u/frame}$.
2. **Advance Behavior ($d > 5.5\text{u}$)**:
   Tested at distances $d \in \{5.51\text{u}, 7.0\text{u}, 10.0\text{u}, 15.0\text{u}\}$:
   - $d = 5.51\text{u}$: displacement = $+0.0400$, pass = `True`
   - $d = 7.00\text{u}$: displacement = $+0.0400$, pass = `True`
   - $d = 10.00\text{u}$: displacement = $+0.0400$, pass = `True`
   - $d = 15.00\text{u}$: displacement = $+0.0400$, pass = `True`
   Shooter strictly advanced toward the player with velocity $+0.0400\text{u/frame}$.
3. **Sweet Spot Hold ($3.8\text{u} \le d \le 5.5\text{u}$)**:
   Tested at distances $d \in \{3.80\text{u}, 4.00\text{u}, 4.65\text{u}, 5.00\text{u}, 5.50\text{u}\}$:
   - At $d = 3.80\text{u}$: displacement = $0.00000$, pass = `True`
   - At $d = 4.00\text{u}$: displacement = $0.00000$, pass = `True`
   - At $d = 4.65\text{u}$: displacement = $0.00000$, pass = `True`
   - At $d = 5.00\text{u}$: displacement = $0.00000$, pass = `True`
   - At $d = 5.50\text{u}$: displacement = $0.00000$, pass = `True`
   `rb.velocity` is zero and position remains invariant.
4. **Directional Invariance (8 Cardinal & Diagonal Vectors)**:
   Tested retreat and advance vectors across angles $0^\circ, 45^\circ, 90^\circ, 135^\circ, 180^\circ, 225^\circ, 270^\circ, 315^\circ$:
   - Dot product for retreat: $\text{dot} = 1.000$ in all 8 directions.
   - Dot product for advance: $\text{dot} = 1.000$ in all 8 directions.
5. **Arena Boundary Clamping**:
   - Shooter pinned at $X = 13.8$ with player advancing at $X = 12.0$: final $X = 13.8000$, strictly clamped at `arenaMax.x`.
   - Shooter pinned at $Y = 5.2$ with player at $Y = 4.0$: final $Y \le 5.2000$, strictly clamped at `arenaMax.y`.
6. **Zero Distance Guard**:
   At $d = 0.00\text{u}$ (player exactly overlapping shooter): displacement = $(0.0000, 0.0000)$, `noNaN = True`. Zero-magnitude check (`away.sqrMagnitude > 0.0001f`) prevents division by zero.
7. **Player Death Response**:
   When `ph.IsAlive == false`: Shooter position remained $(0.0000, 0.0000)$, `halted = True`.
8. **Aimed Firing Geometry**:
   `Shooter.Shoot()` spawned `EnemyBullet` at player. Firing angles tested:
   - Up $(0, 5) \to z\text{Angle} = 0.0^\circ$
   - Right $(5, 0) \to z\text{Angle} = 270.0^\circ$
   - Down $(0, -5) \to z\text{Angle} = 180.0^\circ$
   - Left $(-5, 0) \to z\text{Angle} = 90.0^\circ$
   All projectile angles match target vectors.

---

### 1.3 Adversarial Challenge Suite 2: Rusher Velocity & Elimination (`RusherEnemy.cs`)

Direct empirical test via `execute_code`:
1. **Speed Benchmark**:
   - `rusher.moveSpeed = 6.2f`
   - `player.moveSpeed = 5.0f`
   - Condition `rusherSpeed > playerSpeed`: `True` ($6.2 > 5.0$).
2. **Kinematic Step Displacement**:
   - In 1 fixed physics step ($\Delta t = 0.02\text{s}$): displacement was $0.1240\text{u}$.
   - Expected step: $6.2 \times 0.02 = 0.1240\text{u}$. Match = `True`.
3. **1-HP Glass-Cannon Elimination**:
   - Initial state: `maxHealth = 1`, `currentHealth = 1`, `IsAlive = True`.
   - Non-positive damage stress:
     - `TakeDamage(0)`: `currentHealth = 1`, `IsAlive = True`.
     - `TakeDamage(-5)`: `currentHealth = 1`, `IsAlive = True`.
   - Single valid hit: `TakeDamage(1)`:
     - `currentHealth = 0`
     - `IsAlive = False`
     - `isDead = True`
     - Event `OnEnemyKilledScore` dispatched exactly 15 points.
4. **Contact Damage & i-Frames Immunity**:
   - Initial player health: $5\text{ HP}$.
   - Contact hit: player health dropped $5 \to 4\text{ HP}$, `ph.isInvulnerable = True`.
   - 10 rapid contact damage calls while in i-frames: player health remained $4\text{ HP}$ (0 additional damage taken).

---

### 1.4 Adversarial Challenge Suite 3: Chaser Pursuit & Contact Damage (`ChaserEnemy.cs`)

Direct empirical test via `execute_code`:
1. **Smooth Trajectory & Velocity**:
   - Stats: `maxHealth = 3`, `moveSpeed = 2.8f`, `scoreValue = 10`, `grenadeDropChance = 0.20f`.
   - Over 50 physics steps in open corridor:
     - Monotonic approach toward player: `Monotonic = True` (distance decreased every single step).
     - Total displacement: $2.8000\text{u}$.
     - Expected displacement: $2.8 \times 0.02 \times 50 = 2.8000\text{u}$. Match = `True`.
2. **Physical Collision Integrity**:
   - When positioned against scene obstacles (`Wall_Bottom` at $Y=-5.42$ or Player collider at $X=3.25$), Box2D physics solver properly halted movement on collision.
3. **3-Hit Health Decay**:
   - Hit 1: $3 \to 2\text{ HP}$, `IsAlive = True`.
   - Hit 2: $2 \to 1\text{ HP}$, `IsAlive = True`.
   - Hit 3: $1 \to 0\text{ HP}$, `IsAlive = False`, `isDead = True`, awarded 10 points.
4. **Contact Damage to Player**:
   - Contact hit reduced player HP from $5 \to 4\text{ HP}$. 10 consecutive contact calls during i-frames were rejected without damage.

---

### 1.5 Adversarial Challenge Suite 4: Spawner Scaling Formulas & Bounds (`EnemySpawner.cs`)

Empirical formula evaluations via `execute_code`:
$$I(t, S) = \max(0.6, 3.0 - 0.015t - 0.002S)$$
$$N_{\max}(t, S) = \min(25, 5 + \lfloor t/20 \rfloor + \lfloor S/60 \rfloor)$$

1. **Four Core Bounds**:
   - **$t = 0\text{s}, S = 0\text{ pts}$**:
     $I = 3.000\text{s}$ (expected $3.0$), $N_{\max} = 5$ (expected $5$). Pass = `True`.
   - **$t = 100\text{s}, S = 0\text{ pts}$**:
     $I = 1.500\text{s}$ (expected $3.0 - 1.5 = 1.5$), $N_{\max} = 10$ (expected $5 + 5 = 10$). Pass = `True`.
   - **$t = 0\text{s}, S = 1000\text{ pts}$**:
     $I = 1.000\text{s}$ (expected $3.0 - 2.0 = 1.0$), $N_{\max} = 21$ (expected $5 + 16 = 21$). Pass = `True`.
   - **$t = 100\text{s}, S = 1000\text{ pts}$**:
     $I = 0.600\text{s}$ (clamped at $0.6\text{s}$ floor), $N_{\max} = 25$ (clamped at $25$ ceiling). Pass = `True`.
2. **Boundary & Transition Testing**:
   - Negative inputs: $t = -50, S = -100 \implies I = 3.000\text{s}, N_{\max} = 5$ (safe fallback). Pass = `True`.
   - Time transition: $t = 19.99\text{s} \implies N_{\max} = 5$; $t = 20.00\text{s} \implies N_{\max} = 6$. Pass = `True`.
   - Score transition: $S = 59 \implies N_{\max} = 5$; $S = 60 \implies N_{\max} = 6$. Pass = `True`.
3. **Boss Encounter Suppression**:
   - At $t=0, S=0$ with `isBossActive = true`: $I = 6.000\text{s}$ (doubled from $3.0\text{s}$). Pass = `True`.
   - At $t=100, S=1000$ with `isBossActive = true`: $I = 1.200\text{s}$ (doubled from $0.6\text{s}$). Pass = `True`.
4. **Perimeter Edge Generation Stress Test (1,000 Random Points)**:
   - 1,000 samples generated around player at $(2.5, 0.5)$:
   - Exactly on 1 of 4 perimeter edges ($X \in \{-10.5, 16.0\} \lor Y \in \{-6.0, 7.0\}$): $1,000 / 1,000$ ($100\%$).
   - Violations of $\text{minPlayerDistance} \ge 6.0\text{u}$: $0 / 1,000$ ($0\%$).
5. **Archetype Selection Distribution (30,000 Monte Carlo Iterations)**:
   - Tier 1 ($S = 50 < 100$): Chaser = $75.2\%$ (exp $75\%$), Rusher = $14.5\%$ (exp $15\%$), Shooter = $10.3\%$ (exp $10\%$).
   - Tier 2 ($S = 200$, $100 \le S < 300$): Chaser = $49.1\%$ (exp $50\%$), Rusher = $25.7\%$ (exp $25\%$), Shooter = $25.2\%$ (exp $25\%$).
   - Tier 3 ($S = 400 \ge 300$): Chaser = $35.5\%$ (exp $35\%$), Rusher = $29.3\%$ (exp $30\%$), Shooter = $35.3\%$ (exp $35\%$).

---

### 1.6 Additional Stress Testing & Live Play Mode

1. **Rapid-Fire Damage Spike (100 Hits in 1 Frame)**:
   - 100 calls to `chaser.TakeDamage(1)` in a single frame:
   - Final HP = 0 (clamped).
   - Death events fired = 1 (strictly idempotent).
   - Score dispatched = 10 (0 duplicate score exploits).
2. **Concurrency Saturation & External Object Destruction**:
   - Spawner capped at 5 active enemies.
   - Destroying 2 active enemies externally without calling `Die()`: `CleanDeadEnemies()` purged dead references immediately, resetting active count to 3 without exceptions.
3. **EnemyBullet Collision Matrix**:
   - Bullet $\to$ Player: Damaged = `True` ($5 \to 4\text{ HP}$), Bullet Destroyed = `True`.
   - Bullet $\to$ Friendly Enemy: Enemy Untouched = `True`, Bullet Survived = `True`.
   - Bullet $\to$ EnemyBullet: Bullet Survived = `True`.
   - Bullet $\to$ Trigger Pickup: Bullet Survived = `True`.
   - Bullet $\to$ Wall Collider: Bullet Destroyed = `True`.
4. **Live Play Mode Verification**:
   - Switched editor to Play Mode via `manage_editor` `play`.
   - Observed live spawner creating Chaser and Shooter entities on perimeter edges.
   - Verified 0 compiler errors and 0 runtime exceptions via `read_console`.
   - Exited Play Mode cleanly via `manage_editor` `stop`.

---

## 2. Logic Chain

1. **R2 Requirements Adherence**:
   - User specification R2 requires 3 distinct enemy archetypes:
     1. Chaser (melee pursuit, standard speed, contact damage).
     2. Shooter (kiting distance keeper, periodic aimed projectile firing).
     3. Rusher (glass cannon low HP, high movement speed).
   - Observations 1.2, 1.3, and 1.4 empirically demonstrate that all three archetypes execute their exact specified behaviors:
     - Shooter advances when $d > 5.5\text{u}$, retreats when $d < 3.8\text{u}$, holds inside $[3.8, 5.5]\text{u}$, clamps to arena bounds, and fires aimed projectiles.
     - Rusher moves at $6.2\text{ u/s} > 5.0\text{ u/s}$, eliminates on exactly 1 hit, and inflicts 1 contact damage.
     - Chaser pursues player smoothly at $2.8\text{ u/s}$, withstands 2 hits, dies on 3rd hit, and inflicts 1 contact damage.

2. **Mathematical Precision of Scaling Curves**:
   - `ORIGINAL_REQUEST.md` and `PROJECT.md` require progressive spawn scaling based on survival time $t$ and score $S$.
   - Observation 1.5 confirms that the mathematical formulas for $I(t, S)$ and $N_{\max}(t, S)$ match the exact values at all 4 boundary conditions ($t=0, S=0$; $t=100, S=0$; $t=0, S=1000$; $t=100, S=1000$), clamp correctly at the $0.6\text{s}$ floor and $25$ enemy ceiling, double the interval during Boss encounters, and generate 100% of spawn positions on perimeter edges $\ge 6.0\text{u}$ away from the player.

3. **Stability, Idempotence & Zero Exploits**:
   - Observation 1.6 shows that 100 hits in 1 frame trigger exactly 1 death event and 1 score award, eliminating double-death and score farming bugs.
   - EnemyBullet strictly damages the player and ignores friendly enemies and pickups.
   - Live Play Mode runs with 0 console errors and 0 runtime exceptions.

---

## 3. Caveats

1. **Boss Encounter Mechanics (Milestone 4)**: While `EnemySpawner` includes `SpawnBoss()` triggering at 500 points and 50% rate suppression, `BossController.cs` and the radial burst projectile logic belong to Milestone 4 and will be tested then.
2. **Grenade AoE Mechanics (Milestone 3)**: Enemy death rolls the 15–25% grenade drop probability and instantiates `GrenadePickup.prefab`. Collection and AoE explosion logic belong to Milestone 3.

---

## 4. Conclusion

**Verdict: APPROVE**

Milestone 2 (Enemy Archetypes & Spawner System) has been thoroughly and adversarially tested. All components (`EnemyBase`, `ChaserEnemy`, `ShooterEnemy`, `RusherEnemy`, `EnemyBullet`, `EnemySpawner`) operate with mathematical precision, adhere strictly to all behavioral specifications, handle edge cases gracefully, and produce 0 errors in the Unity Editor.

---

## 5. Verification Method

To independently reproduce the empirical findings:

1. **Run Milestone 2 Tests**:
   Execute via Unity MCP `execute_code`:
   ```csharp
   var report = Tests.Milestone2Tests.RunAllTests();
   return report.GenerateMarkdownSummary();
   ```
   *Expected*: 16/16 Passed, 0 Failed.

2. **Run Full E2E Test Suite**:
   Execute via Unity MCP `execute_code`:
   ```csharp
   var report = E2ETests.E2ETestRunner.RunAll();
   return report.GenerateMarkdownSummary();
   ```
   *Expected*: 352 Passed, 0 Failed, 33 Pending.

3. **Verify Console Purity**:
   Call Unity MCP `read_console` with `action: "get"`, `types: ["error"]`.
   *Expected*: 0 log entries.
