# Technical Specification & Requirements Document: 2D Top-Down Endless Shooter

**Project Title**: Unity 2D Top-Down Endless Shooter (`top-down-shooting-unity`)  
**Specification Document Version**: 1.0.0  
**Author**: Specification Investigator (Spec Miner)  
**Date**: 2026-09-21  
**Target Engine / Environment**: Unity 2022.3.62f2 LTS, 2D Core, UGUI / TextMeshPro  
**Primary Specification Source**: `.agents/ORIGINAL_REQUEST.md`  
**Reference Codebase**: `Assets/scripts/PlayerMovement.cs`, `Assets/scripts/Shooting.cs`, `Assets/scripts/Bullet.cs`, `Assets/Scenes/shooting.unity`

---

## 1. Executive Summary & Architectural Scope

This specification formalizes the requirements for transforming the prototype repository into a complete, modular, polished 2D top-down endless shooter game. The gameplay loop centers on WASD 8-directional movement, cursor-oriented aiming, continuous primary shooting, tactical grenade collection and area-of-effect (AoE) deployment, surviving progressive endless enemy waves (Chaser, Shooter, Rusher), and overcoming a climactic Boss encounter triggered at 500 points featuring a 360-degree radial projectile barrage.

The design strictly enforces modular separation across components, zero runtime exceptions, zero compiler warnings/errors, deterministic physics layer separation, and persistent high score tracking via `PlayerPrefs`.

---

## 2. Specification Source Mapping

| Requirement Identifier | Source Document Section | Technical Subsystem | Primary Implementation Target |
|---|---|---|---|
| **R1** | `ORIGINAL_REQUEST.md § R1` | Player Combat & Health | `PlayerMovement.cs`, `PlayerHealth.cs`, `PlayerShooting.cs` |
| **R2** | `ORIGINAL_REQUEST.md § R2` | Endless Spawner & Enemy Types | `EnemySpawner.cs`, `EnemyBase.cs`, `ChaserEnemy.cs`, `ShooterEnemy.cs`, `RusherEnemy.cs` |
| **R3** | `ORIGINAL_REQUEST.md § R3` | Grenade System (AoE Pickup & Throw) | `GrenadePickup.cs`, `GrenadeThrower.cs`, `GrenadeProjectile.cs` |
| **R4** | `ORIGINAL_REQUEST.md § R4` | Boss Encounter & Radial Barrage | `BossController.cs`, `BossProjectile.cs`, `BossHealthBar.cs` |
| **R5** | `ORIGINAL_REQUEST.md § R5` | UI, HUD & Game Flow State Machine | `GameManager.cs`, `UIManager.cs`, In-Game Canvas Panels |
| **R6** | `ORIGINAL_REQUEST.md § R6` | Visuals, Spritesheet & FX Integration | `Tiny RPG Forest` assets, `DamageFlash.cs`, VFX Prefabs |
| **AC** | `ORIGINAL_REQUEST.md § Acceptance Criteria` | Acceptance & Quality Gates | Test validation, Compiler checks, Zero Exception gates |

---

## 3. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|---|---|---|---|---|---|---|
| 1 | Player | 8-Directional Movement | Responsive WASD / Arrow key movement with diagonal normalization | WASD / Axis Input (`Horizontal`, `Vertical`) | `rb.MovePosition` translation | Clamped to arena boundaries; no clipping outside | Codebase & Spec R1 |
| 2 | Player | Mouse-Facing Rotation | Character rotates smoothly to track mouse cursor in world space | Mouse position via `cam.ScreenToWorldPoint` | `rb.rotation = angle` (Euler Z) | Handles zero vector when mouse is at player center | Codebase & Spec R1 |
| 3 | Player | Arena Boundary Constraint | Prevents player from exiting the defined combat bounds | Player Rigidbody2D position | Clamped coordinates `[minX, maxX]`, `[minY, maxY]` | Rebounds/clamps safely without jitter | Spec R1 & Scene Survey |
| 4 | Player | Primary Weapon Firing | Left-click spawns bullet from `FirePoint` with velocity impulse | Left Mouse Button (`Fire1`) | `Bullet.prefab` spawned + Impulse force | Cooldown throttling prevents 1-frame spam | Codebase & Spec R1 |
| 5 | Player | Health & Damage Rule | Player has exactly 5 HP; each hostile hit subtracts exactly 1 HP | Collision with Enemy or Hostile Projectile | `currentHealth -= 1`, HUD heart update | Ignores multi-damage frames; ignores negative HP | Spec R1 |
| 6 | Player | Invulnerability Frames (i-frames) | 1.0s invulnerability window after hit with visual blinking | Damage event trigger | Temporary collision ignore, sprite opacity oscillation | Timer safely resets on game reset/restart | Spec R1 & AC |
| 7 | Player | Player Death & Defeat | HP reaches 0 triggering player death sequence and Game Over state | `currentHealth <= 0` | Death VFX, disable inputs, notify GameManager | Guards against duplicate Game Over invocations | Spec R1 & R5 |
| 8 | Spawner | Perimeter Spawn Placement | Spawns enemies outside visible camera viewport / arena edges | Spawner timer ticks | Instantiates enemy prefab at perimeter coords | Fallback to default edge if camera null | Spec R2 |
| 9 | Spawner | Progressive Spawn Interval Scaling | Spawn interval decreases asymptotically as time and score increase | `survivalTime`, `currentScore` | Dynamic `spawnInterval` (3.0s down to 0.6s) | Clamped to `minSpawnInterval` (prevents 0 or negative) | Spec R2 |
| 10 | Spawner | Concurrency Cap Scaling | Max simultaneous active enemies increases with score and time | `survivalTime`, `currentScore` | Dynamic `maxConcurrentEnemies` (5 up to 25) | Clamped to `maxEnemiesCap` (prevents performance drop) | Spec R2 |
| 11 | Enemy | Archetype: Chaser (Melee) | Medium-speed enemy that charges directly towards player | Player transform position | Continuous translation towards player; contact damage | Safe null check if player dies/destroyed | Spec R2 |
| 12 | Enemy | Archetype: Shooter (Ranged) | Kiting enemy maintaining distance, firing aimed bullets periodically | Player transform position, range threshold | Distance check, strafe/retreat, projectile spawn | Stops firing if player is dead | Spec R2 |
| 13 | Enemy | Archetype: Rusher (Speed Melee) | Fast, low-HP melee glass cannon that rapidly closes the gap | Player transform position | High-speed linear charge; contact damage | Safe collision handling with arena walls | Spec R2 |
| 14 | Enemy | Enemy Damage & Flashing | Enemies take damage from player bullets and flash white/red | `Bullet` or `Grenade` hit | `hp -= damage`, brief color tint, hit particle | Re-entrant damage handled without null refs | Spec R6 |
| 15 | Enemy | Enemy Death & Scoring | Grants score upon death, plays death animation, rolls item drop | `enemyHP <= 0` | Score added to `GameManager`, drops grenade, despawns | Ensures score awarded exactly once per enemy | Spec R2 & R6 |
| 16 | Items | Grenade Drop on Kill | Probability roll on enemy death to drop Grenade Pickup item | `Random.value <= dropRate` | Instantiates `GrenadePickup` at death position | Drop rate clamped between 0.0 and 1.0 | Spec R3 |
| 17 | Items | Grenade Collection | Player walks over pickup to collect grenade into inventory | Player trigger overlap | `grenadeCount++`, update HUD, destroy pickup | Ignores pickup if inventory at max capacity | Spec R3 |
| 18 | Items | Grenade Throwing | Pressing E or RMB launches grenade towards mouse cursor | Key `E` or `Input.GetMouseButtonDown(1)` | Spawns `GrenadeProjectile`, decrements inventory | Blocked if `grenadeCount <= 0` or game paused | Spec R3 |
| 19 | Items | Grenade Detonation & AoE | Grenade explodes after fuse (1.2s) or impact, dealing AoE damage | Fuse countdown or solid contact | `Physics2D.OverlapCircleAll`, deals 50 dmg, VFX | Non-allocating query avoids GC allocations | Spec R3 |
| 20 | Boss | Boss Spawn Trigger | Spawns single Boss instance once player score reaches 500 | `currentScore >= 500 && !bossSpawned` | Instantiates Boss, activates Boss HUD bar | Strict flag ensures exactly one Boss ever spawns | Spec R4 |
| 21 | Boss | Boss Health Bar | Dedicated HUD slider displaying real-time Boss HP | `bossCurrentHP / bossMaxHP` | Slider fill ratio updated | Bar hidden when Boss is inactive or defeated | Spec R4 & R5 |
| 22 | Boss | 360° Radial Projectile Burst | Boss periodically charges and emits a complete 360-degree ring of bullets | Periodic attack timer (every 4.5s) | Spawns 16-20 projectiles in evenly spaced circle | Projectiles auto-destruct on boundary/lifetime | Spec R4 |
| 23 | Boss | Boss Defeat & Reward | Huge score reward (+500), death explosion sequence, guaranteed drops | `bossHP <= 0` | Add +500 pts, trigger Victory/Continue panel | Guaranteed one-time trigger via `isDead` flag | Spec R4 |
| 24 | Boss | Endless Continuation | Defeating Boss unlocks continuation of endless mode without 2nd boss | "Continue" button clicked | Resumes `Time.timeScale = 1`, Spawner continues | Boss state retained so no duplicate spawns occur | Spec R4 |
| 25 | UI/HUD | 5 Heart Health Display | Renders 5 individual heart icons indicating current player HP | `PlayerHealth.OnHealthChanged` | Changes heart sprites from full to damaged/empty | Bounds-checked between 0 and 5 | Spec R5 & Tiny RPG Forest |
| 26 | UI/HUD | Current Score & High Score | Displays real-time score and all-time high score | `GameManager.OnScoreChanged` | TextMeshPro/Text string format updates | High score loaded from PlayerPrefs on start | Spec R5 |
| 27 | UI/HUD | Grenade Count Indicator | Shows available grenade charges | `GrenadeThrower.OnCountChanged` | Displays grenade icon + "xN" text | Updates immediately on pickup or throw | Spec R5 |
| 28 | UI | Main Menu Screen | Start menu with Play, Controls modal, and Quit | User UI interaction | Loads gameplay session, opens controls, or quits | WebGL/Editor safe quit fallback | Spec R5 |
| 29 | UI | Pause Menu System | ESC or P toggles pause state and displays Pause Panel | Key `ESC` or `P` | `Time.timeScale = 0`, shows Pause UI, toggles off | Ignores pause if game is already Game Over | Spec R5 |
| 30 | UI | Game Over Screen | Appears on death; shows final score, high score, Restart, Menu | Player death event | Shows Game Over UI, records high score | Restarts cleanly without state leakage | Spec R5 |
| 31 | UI | Victory / Continue Screen | Modal/panel shown after Boss kill allowing endless continuation | Boss death event | Shows victory banner, pausing until Continue | Safely unpauses upon Continue click | Spec R5 |
| 32 | Persistence | High Score Storage | Stores highest score persistently across game restarts | `PlayerPrefs.SetInt("HighScore", val)` | Saved to disk via `PlayerPrefs.Save()` | Fallback to 0 if key not found | Spec R5 |
| 33 | Visuals | Damage Flash Feedback | White/Red flash shader or color tint on receiving damage | Damage event on entity | Lerp SpriteRenderer color to red/white for 0.1s | Restores original sprite color accurately | Spec R6 |
| 34 | Visuals | Tiny RPG Forest Integration | Uses official tileset, treant, mole, hearts, death anim sprites | Unity Asset database | Renders matching 2D pixel art style | Correct PPU (pixels per unit) and filter mode | Spec R6 |

---

## 4. Detailed Module Specifications

### Module 1: Player Combat, Movement & Health System

#### 1.1 Movement & Kinematics
- **Input Sampling**: `Input.GetAxisRaw("Horizontal")` and `Input.GetAxisRaw("Vertical")` sampled in `Update()`.
- **Normalization**: Movement vector MUST be normalized (`movement.normalized`) to ensure diagonal speed equals orthogonal speed ($1.0 \times \text{moveSpeed}$).
- **Translation**: Executed in `FixedUpdate()` via `rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime)`.
- **Baseline Move Speed**: $5.0 \text{ units/sec}$.
- **Arena Bounds Clamping**:
  - Horizontal bounds: $X_{\min} = -8.5\text{f}, X_{\max} = 13.8\text{f}$.
  - Vertical bounds: $Y_{\min} = -4.2\text{f}, Y_{\max} = 5.2\text{f}$.
  - Applied via `Mathf.Clamp` inside `FixedUpdate()` immediately before or after `rb.MovePosition` to provide an absolute physical boundary.

#### 1.2 Aiming & Orientation
- **Screen to World Mapping**: Screen mouse coordinates sampled via `Input.mousePosition` and transformed via `cam.ScreenToWorldPoint()`.
- **Direction Calculation**: $\vec{v}_{\text{look}} = \vec{p}_{\text{mouse}} - \vec{p}_{\text{player}}$.
- **Angular Rotation**: $\theta = \text{atan2}(v_y, v_x) \times \frac{180}{\pi} - 90^\circ$.
- **RigidBody Integration**: Set `rb.rotation = angle` directly in `FixedUpdate()`.

#### 1.3 Primary Fire (Basic Bullet)
- **Fire Point**: Transform child `Fire Point` located at local offset $(0.35, 1.18, 0.0)$.
- **Trigger**: `Input.GetButton("Fire1")` or `Input.GetButtonDown("Fire1")`. To provide responsive combat feel, a rate-limited auto-fire or semi-auto mode with `fireCooldown = 0.2f` seconds ($5 \text{ rounds/sec}$) is specified.
- **Projectile Velocity**: $20.0 \text{ units/sec}$ applied via `Rigidbody2D.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse)`.
- **Damage**: $1 \text{ HP}$ per bullet.
- **Lifetime**: Projectile automatically destroys itself after $3.0\text{s}$ if no collision occurs to prevent resource leaks.

#### 1.4 Health Model & i-Frames
- **Max Health**: $5 \text{ HP}$.
- **Starting Health**: $5 \text{ HP}$.
- **Damage Ingestion Rule**: Each valid collision with an enemy or hostile projectile subtracts exactly $1 \text{ HP}$.
- **i-Frames Duration**: $1.0 \text{ second}$ of invulnerability triggered immediately upon taking damage.
- **i-Frames Visual Feedback**: Sprite alpha / color oscillates at $10\text{Hz}$ (e.g. alternating between $\alpha = 0.3$ and $\alpha = 1.0$) for the full $1.0\text{s}$.
- **Game Over Trigger**: When `currentHealth <= 0`:
  - Player GameObject input is disabled.
  - Death effect / particle is instantiated.
  - Event `OnPlayerDied` is broadcast to `GameManager`.

---

### Module 2: Endless Enemy Spawning & Enemy Archetypes

#### 2.1 Spawner Mechanics & Positioning
- **Off-Screen Spawning**: Spawner calculates positions on a perimeter rectangular bounding box beyond the camera frustum:
  - Perimeter margins: $X \in [-10.5, 16.0]$, $Y \in [-6.0, 7.0]$.
  - Picks a random side (Top, Bottom, Left, Right) and generates coordinates along that edge.
- **Minimum Player Distance**: If generated spawn point is within $6.0\text{ units}$ of player position, it is rejected and re-rolled to prevent cheap player hits.

#### 2.2 Progressive Scaling Curves
The spawner dynamically scales difficulty based on elapsed survival time $t$ (seconds) and player score $S$:

1. **Spawn Interval ($I_{\text{spawn}}$)**:
   $$I_{\text{spawn}}(t, S) = \max\left(0.6\text{s}, 3.0\text{s} - (t \times 0.015) - (S \times 0.002)\right)$$
   - Initial interval: $3.0\text{s}$.
   - Minimum interval floor: $0.6\text{s}$ (achieved around 100s or 300+ pts).

2. **Maximum Concurrent Enemies ($N_{\max}$)**:
   $$N_{\max}(t, S) = \min\left(25, 5 + \left\lfloor\frac{t}{20}\right\rfloor + \left\lfloor\frac{S}{60}\right\rfloor\right)$$
   - Initial concurrency cap: $5$ enemies.
   - Maximum concurrency ceiling: $25$ enemies (ensures optimal frame rate and playability).

3. **Archetype Probability Distribution**:
   - Tier 1 ($S < 100$): $75\%$ Chaser, $15\%$ Rusher, $10\%$ Shooter.
   - Tier 2 ($100 \le S < 300$): $50\%$ Chaser, $25\%$ Rusher, $25\%$ Shooter.
   - Tier 3 ($S \ge 300$): $35\%$ Chaser, $30\%$ Rusher, $35\%$ Shooter.

#### 2.3 Enemy Archetypes Detail Specification

```
+---------------------------------------------------------------------------------------+
|                                ENEMY ARCHETYPE MATRIX                                 |
+---------------+---------+------------+---------------+-------------+-------+----------+
| Archetype     | HP      | Move Speed | Attack Range  | Fire Rate   | Score | Drop %   |
+---------------+---------+------------+---------------+-------------+-------+----------+
| 1. Chaser     | 3 HP    | 2.8 u/s    | Melee contact | Continuous  | 10    | 20%      |
| 2. Shooter    | 2 HP    | 2.0 u/s    | Range 5.5 u   | 2.5s reload | 20    | 25%      |
| 3. Rusher     | 1 HP    | 6.2 u/s    | Melee contact | Instant     | 15    | 15%      |
+---------------+---------+------------+---------------+-------------+-------+----------+
```

1. **Chaser (Quái cận chiến)**:
   - Behavior: Direct path tracking towards player.
   - Physics: Rigidbody2D dynamic, moves via `MoveTowards` or velocity towards `player.position`.
   - Contact Damage: Deals 1 damage to player upon `OnCollisionEnter2D` or trigger touch.

2. **Shooter (Quái bắn xa)**:
   - Behavior: Evaluates distance to player $d = \|\vec{p}_{\text{player}} - \vec{p}_{\text{shooter}}\|$.
     - If $d > 5.5\text{u}$: Moves closer towards player.
     - If $d < 3.8\text{u}$: Retreats away from player (kites).
     - If $3.8\text{u} \le d \le 5.5\text{u}$: Holds position or circles laterally.
   - Attack: Fires projectile (`EnemyProjectile` using arrow or energy orb sprite) directly towards player position every $2.5\text{s}$. Projectile speed: $7.0\text{ u/s}$, damage: $1\text{ HP}$.

3. **Rusher (Quái tốc độ)**:
   - Behavior: High-velocity sprint directly at player. Movement speed $6.2\text{ u/s}$ exceeds player base speed ($5.0\text{ u/s}$).
   - Fragility: Exactly $1\text{ HP}$ (one shot from basic bullet destroys it). Requires rapid target prioritization.

---

### Module 3: Grenade Mechanic (AoE Pickup & Throw)

#### 3.1 Drop & Pickup Lifecycle
- **Drop Chance**: Rolled upon enemy death via `Random.value <= enemy.dropChance`.
- **Pickup Entity**: Spawns `GrenadePickup` at death position.
- **Pickup Visual**: Pulsing gem/grenade sprite with periodic floating animation.
- **Trigger Detection**: `CircleCollider2D` with `isTrigger = true`.
- **Inventory Addition**: When player collides:
  - If `playerGrenades < maxGrenades` (capacity: 5):
    - Increment `playerGrenades++`.
    - Trigger pickup sound/effect.
    - Update HUD grenade indicator.
    - Destroy pickup GameObject.

#### 3.2 Throwing Trajectory & Physics
- **Input Trigger**: Key `E` or Right Mouse Button (`Input.GetMouseButtonDown(1)`).
- **Target Selection**: Vector towards world mouse position: $\vec{v}_{\text{throw}} = \vec{p}_{\text{mouse}} - \vec{p}_{\text{player}}$.
- **Max Throw Distance**: $7.0 \text{ units}$. Clamped: $\vec{p}_{\text{target}} = \vec{p}_{\text{player}} + \text{ClampMagnitude}(\vec{v}_{\text{throw}}, 7.0\text{f})$.
- **Trajectory Motion**: Grenade travels towards target over $0.7\text{s}$ flight time with simulated parabolic elevation (scale pulsing to simulate height).

#### 3.3 Detonation & Area of Effect
- **Fuse Duration**: $1.2\text{ seconds}$ total fuse, or detonates upon reaching target destination / impacting solid enemy.
- **Explosion Radius**: $3.5\text{ units}$.
- **Damage Ingestion**: $50\text{ damage}$ in blast radius. Guarantees 1-shot elimination for all regular enemies (HP 1-3) and inflicts substantial damage on the Boss.
- **Physics Query**: Uses `Physics2D.OverlapCircleAll(explosionPosition, explosionRadius, enemyLayerMask)`.
- **VFX**: Spawns `Fire Effect` or explosion particle system + camera micro-shake (magnitude $0.25\text{f}$, duration $0.2\text{s}$).

---

### Module 4: Boss Encounter

#### 4.1 Activation & Single-Instance Guarantee
- **Trigger Threshold**: When `GameManager.currentScore >= 500`.
- **Idempotency Guard**:
  ```csharp
  if (currentScore >= 500 && !bossEncounterTriggered && !bossDefeated) {
      bossEncounterTriggered = true;
      SpawnBoss();
  }
  ```
- **Spawning Arena**: Spawns near top-center of the arena at $(2.69, 3.5, 0.0)$.
- **Mob Suppression**: During Boss encounter, normal spawner rate is reduced by $50\%$ to highlight the boss fight.

#### 4.2 Boss Characteristics & Health
- **Boss Max HP**: $60\text{ HP}$.
- **Visual Scale**: $2.2\times$ scale of Treant sprite, with unique dark red / corrupted tint.
- **Move Speed**: $1.8\text{ units/sec}$ (slow, deliberate advance towards player).
- **Dedicated HUD Bar**: Activates top-screen Boss Health Bar slider (`0.0` to `1.0`).

#### 4.3 360-Degree Radial Projectile Barrage
- **Cycle Frequency**: Fires radial burst every $4.5\text{ seconds}$.
- **Telegraphing Phase**: Boss stops moving and flashes yellow/white for $0.6\text{s}$ before release.
- **Radial Geometry**:
  - Emits $16$ projectiles simultaneously in full circle ($360^\circ$).
  - Angular spacing: $\Delta\theta = \frac{360^\circ}{16} = 22.5^\circ$.
  - Direction for projectile $k$ ($0 \le k < 16$):
    $$\vec{d}_k = \left(\cos(k \times 22.5^\circ), \sin(k \times 22.5^\circ)\right)$$
- **Projectile Properties**: Speed: $5.0\text{ units/sec}$, Damage: $1\text{ HP}$ to player. Despawns upon hitting boundary or after $5.0\text{s}$.

#### 4.4 Defeat Sequence & Endless Mode Transition
- **Death FX**: Chain of multiple explosion particle effects on boss body, sprite fades out.
- **Reward**: $+500\text{ bonus points}$ added directly to player score.
- **Drops**: Guaranteed drop of 2 Grenade pickups.
- **Boss HUD Bar**: Deactivated.
- **Victory / Continue Flow**:
  - Displays Victory modal / banner: "BOSS SLAIN! +500 PTS".
  - Offers "CONTINUE" button.
  - Clicking "CONTINUE" seamlessly resumes endless wave scaling.
  - Since `bossEncounterTriggered == true`, no second boss will ever spawn.

---

### Module 5: UI, HUD & Game Flow State Machine

#### 5.1 Game State Machine
The game flow is governed by a deterministic state machine:

```
                  +-------------------+
                  |     MAIN MENU     |
                  +---------+---------+
                            | [Play Button]
                            v
+-------------------------> PLAYING <------------------------+
|                           |   ^                            |
| [Restart]     [ESC / P]   |   | [Resume / Continue]        |
|                           v   |                            |
|                     +-----+---+-------+                    |
|                     |     PAUSED      |                    |
|                     +-----------------+                    |
|                               |                            |
|             +-----------------+----------------+           |
|             |                                  |           |
| [Player HP <= 0]                     [Boss Defeated]       |
|             v                                  v           |
|     +-------+-------+                  +-------+-------+   |
|     |   GAME OVER   |                  | VICTORY/CONT. |---+
|     +---------------+                  +---------------+
```

- **`MainMenu`**: Initial state. Shows Play, Controls modal, Quit.
- **`Playing`**: `Time.timeScale = 1.0f`. HUD active.
- **`Paused`**: `Time.timeScale = 0.0f`. Triggered by ESC or P. Shows Resume, Restart, Menu buttons.
- **`GameOver`**: Triggered on 0 HP. `Time.timeScale = 0.0f`. Shows Score, High Score, Restart, Menu buttons.
- **`VictoryContinue`**: Triggered on Boss death. Shows Boss Defeated banner, Continue button.

#### 5.2 In-Game HUD Elements
1. **Health Display (5 Hearts)**:
   - 5 Image slots arranged horizontally in top-left.
   - Sprites from `Assets/Tiny RPG Forest/Artwork/sprites/misc/hearts`:
     - Full heart: `hearts-1.png`
     - Empty heart: `hearts-2.png`
   - Dynamically updates as player takes damage or recovers.
2. **Current Score**:
   - Formatted label: `SCORE: 00480`.
3. **High Score**:
   - Formatted label: `HIGH: 01250`.
   - Loaded from `PlayerPrefs.GetInt("HighScore", 0)` on start; updated when current score exceeds it.
4. **Grenade Inventory**:
   - Icon of grenade + text: `x 3`.
5. **Boss Health Bar**:
   - Slider with red fill and "ANCIENT BOSS" header. Inactive by default; enabled only during boss fight.

---

### Module 6: Visuals, Particles & Audio Feedback

#### 6.1 Sprite Asset Mapping
- **Player**: Existing `soldier-no-bg.png` or `Tiny RPG Forest/Artwork/sprites/hero`.
- **Chaser**: `Tiny RPG Forest/Artwork/sprites/treant/walk` or `mole/walk`.
- **Shooter**: `Tiny RPG Forest/Artwork/sprites/mole` with `arrow.png` projectile.
- **Rusher**: Scaled down, fast animated `mole` sprite.
- **Boss**: Scaled $2.2\times$ `treant` with tinted color.
- **Player Bullet**: `Assets/bullets/Fire Effect and Bullet 16x16.png`.
- **Grenade Pickup**: `Assets/Tiny RPG Forest/Artwork/sprites/misc/gem/gem-1.png`.
- **Heart Icons**: `hearts-1.png` (full) and `hearts-2.png` (empty).
- **Death Animation**: Frame sequence `enemy-death-1.png` to `enemy-death-6.png`.
- **Hit & Explosion VFX**: `Assets/Fire Effect.prefab` and particle effects.

#### 6.2 Damage Flash Effect
- Entities receiving damage trigger `DamageFlash`:
  - Changes `SpriteRenderer.color` to pure White or Bright Red for $0.1\text{s}$, then restores original color.

---

### Module 7: Physics, Layers & Collision Matrix

To ensure deterministic collision handling without spurious interactions, the following 2D Layer Collision Matrix is defined:

```
+-------------------+--------+------------+-------+-----------+---------+----------+--------+
| Layer             | Player | PlayerProj | Enemy | EnemyProj | Grenade | Boundary | Pickup |
+-------------------+--------+------------+-------+-----------+---------+----------+--------+
| Player            |   -    |     NO     |  YES  |    YES    |   NO    |   YES    |  YES   |
| PlayerProjectile  |   NO   |     -      |  YES  |    NO     |   NO    |   YES    |   NO   |
| Enemy             |  YES   |    YES     |  YES  |    NO     |   YES   |   YES    |   NO   |
| EnemyProjectile   |  YES   |     NO     |  NO   |     -     |   NO    |   YES    |   NO   |
| Grenade           |   NO   |     NO     |  YES  |    NO     |    -    |   YES    |   NO   |
| Boundary (Walls)  |  YES   |    YES     |  YES  |    YES    |   YES   |    -     |   NO   |
| Pickup            |  YES   |     NO     |  NO   |    NO     |   NO    |    NO    |   -    |
+-------------------+--------+------------+-------+-----------+---------+----------+--------+
```

*Key collision isolation rules*:
- Player projectiles DO NOT collide with the Player or other Player projectiles.
- Enemy projectiles DO NOT collide with Enemies.
- Pickups only interact with the Player.

---

## 5. Architectural Blueprint & Class Contracts

```
                                  +------------------+
                                  |   GameManager    |
                                  +--------+---------+
                                           |
                   +-----------------------+-----------------------+
                   |                       |                       |
                   v                       v                       v
          +-----------------+     +-----------------+     +-----------------+
          |    UIManager    |     |  EnemySpawner   |     |  PlayerEntity   |
          +-----------------+     +--------+--------+     +--------+--------+
                                           |                       |
                                  +--------+--------+      +-------+-------+
                                  |                 |      |       |       |
                                  v                 v      v       v       v
                            +-----------+    +----------+ +----+ +-----+ +------+
                            | EnemyBase |    |   Boss   | |Move| |Shoot| |Health|
                            +-----+-----+    +----------+ +----+ +-----+ +------+
                                  |
                   +--------------+--------------+
                   |              |              |
                   v              v              v
              +---------+    +---------+    +---------+
              | Chaser  |    | Shooter |    | Rusher  |
              +---------+    +---------+    +---------+
```

### Class Interfaces & Contracts

1. **`PlayerHealth`**:
   - Properties: `int MaxHealth = 5`, `int CurrentHealth`, `bool IsInvulnerable`.
   - Methods: `public void TakeDamage(int damage)`, `private IEnumerator InvulnerabilityRoutine()`.
   - Events: `public event Action<int, int> OnHealthChanged`, `public event Action OnPlayerDied`.

2. **`PlayerMovement`**:
   - Properties: `float moveSpeed = 5f`, `Vector2 minBounds`, `Vector2 maxBounds`.
   - Methods: `void Update()`, `void FixedUpdate()`.

3. **`PlayerShooting`**:
   - Properties: `Transform firePoint`, `GameObject bulletPrefab`, `float fireRate = 0.2f`.
   - Methods: `void Shoot()`.

4. **`GrenadeThrower`**:
   - Properties: `int GrenadeCount`, `int MaxGrenades = 5`, `GameObject grenadePrefab`.
   - Methods: `public void AddGrenades(int count)`, `public void ThrowGrenade(Vector2 targetPos)`.
   - Events: `public event Action<int> OnGrenadeCountChanged`.

5. **`EnemyBase`**:
   - Fields: `int maxHP`, `int currentHP`, `float moveSpeed`, `int scoreValue`, `float dropChance`.
   - Methods: `public virtual void TakeDamage(int damage)`, `protected virtual void Die()`.

6. **`BossController : EnemyBase`**:
   - Fields: `float burstInterval = 4.5f`, `int radialBulletCount = 16`, `GameObject bossBulletPrefab`.
   - Methods: `private IEnumerator RadialBurstRoutine()`, `protected override void Die()`.
   - Events: `public static event Action<float> OnBossHealthChanged`, `public static event Action OnBossSpawned`, `public static event Action OnBossKilled`.

7. **`EnemySpawner`**:
   - Fields: `GameObject[] enemyPrefabs`, `Transform playerTransform`.
   - Methods: `public void SetBossActive(bool active)`, `private void SpawnRandomEnemy()`.

8. **`GameManager`**:
   - Singleton instance.
   - Properties: `int CurrentScore`, `int HighScore`, `GameState CurrentState`.
   - Methods: `public void AddScore(int points)`, `public void PauseGame()`, `public void ResumeGame()`, `public void TriggerGameOver()`, `public void ContinueEndless()`, `public void RestartGame()`.

9. **`UIManager`**:
   - Methods: `public void UpdateHearts(int currentHP, int maxHP)`, `public void UpdateScore(int score)`, `public void UpdateHighScore(int highScore)`, `public void UpdateGrenades(int count)`, `public void UpdateBossHealth(float ratio)`, `public void ShowPanel(GameState state)`.

---

## 6. Edge Cases

| # | Feature | Input / Condition | Expected Technical Behavior |
|---|---|---|---|
| 1 | Player Damage | Rapid consecutive hits within 0.1s (e.g. overlapping enemies) | Only 1 HP is subtracted; i-frames ($1.0\text{s}$) block all subsequent damage instances. |
| 2 | Player Movement | Moving diagonally against arena wall border | Wall constraint clamps perpendicular axis; player continues sliding along free axis without getting stuck. |
| 3 | Grenade Pickup | Player inventory full ($5$ grenades) touches grenade pickup | Pickup remains in world; grenade count does not exceed $5$. |
| 4 | Grenade Throw | Key `E` or RMB pressed when grenade inventory is $0$ | Nothing spawns; no exception thrown; audio/visual "empty" click optional. |
| 5 | Grenade Target | Cursor placed beyond maximum throw distance ($> 7\text{u}$) | Target coordinate is clamped along throw vector at max throw distance ($7.0\text{u}$). |
| 6 | Grenade Target | Cursor placed directly on player position ($\|\vec{v}\| \approx 0$) | Spawns grenade at player position with zero travel distance and explodes after fuse. |
| 7 | Boss Trigger | Player score jumps from 490 to 520 in a single grenade blast | Score $\ge 500$ condition triggers immediately; boss spawns; boolean guard prevents second spawn. |
| 8 | Boss Defeat | Boss HP reaches 0 while several regular enemies remain active | Boss dies, awards 500 pts; regular enemies continue tracking player normally without error. |
| 9 | Pause State | ESC pressed while Game Over panel or Victory modal is open | ESC is ignored or handles menu navigation safely without unpausing dead player state. |
| 10 | Scene Reload | Player clicks Restart on Game Over screen | `Time.timeScale` is reset to $1.0\text{f}$; all coroutines cleanly terminated; fresh state initialized. |
| 11 | High Score | Player achieves lower score than saved high score | High score remains untouched in PlayerPrefs; only beaten high score updates disk. |
| 12 | Spawner Concurrency | Max active enemies cap reached ($25$) | Spawner pauses spawning until an active enemy is destroyed or despawned. |
| 13 | Shooter Target | Player dies while Shooter enemy is midway through attack reload | Shooter detects null/dead player and idles safely without `NullReferenceException`. |
| 14 | Rapid Shooting | Player clicks Left Mouse Button 20 times in 1 second | Cooldown timer throttles fire rate to max $5\text{ shots/sec}$; no bullet overlap. |
| 15 | Boss Burst | Boss radial burst bullets reach boundary walls | Projectiles destroy themselves on wall collision or timeout; no infinite travel. |

---

## 7. Acceptance Criteria Verification Matrix

| AC Identifier | Requirement | Verification Method | Pass Threshold |
|---|---|---|---|
| **AC-01** | Player Movement & Aiming | Manual test & fixed-update inspect | 8-way WASD smooth motion, rotation tracks cursor, no border escape. |
| **AC-02** | 5 HP & i-Frames | Damage injection test | Starts at 5 HP; exactly 1 HP deducted per hit; i-frame flash active; 0 HP triggers Game Over. |
| **AC-03** | Endless Spawning & Scaling | Time/Score progression test | Enemies spawn off-screen; interval drops to 0.6s; max count scales up to 25. |
| **AC-04** | 3 Distinct Enemy Types | Behavioral observation test | Chaser charges; Shooter maintains distance and fires; Rusher moves at high speed with 1 HP. |
| **AC-05** | Grenade Mechanic | Drop, pickup, throw, AoE test | Enemies drop item; touches add to HUD; E/RMB throws; AoE eliminates grouped enemies. |
| **AC-06** | Boss Encounter (500 pts) | Score trigger & boss combat test | Spawns at 500 pts; dedicated HP bar updates; fires 360° radial ring; defeat allows endless continue. |
| **AC-07** | UI, Menus & High Score | State transitions & restart test | HUD displays 5 hearts, score, high score, grenades; Pause (ESC/P); Game Over; PlayerPrefs persists high score. |
| **AC-08** | Visuals & Tiny RPG Forest | Asset & animation inspection | Sprites from Tiny RPG Forest; damage flash feedback; hit & explosion particles. |
| **AC-09** | Compiler & Runtime Stability | Unity MCP console check (`read_console`) | **0 compiler errors**, **0 runtime exceptions** across entire gameplay session. |

---

## 8. Conclusion & Implementation Recommendations

The formal technical specification above provides an unambiguous blueprint for the implementation team:
1. All core formulas (spawn decay, concurrency scaling, throw clamping, radial projectile angles) are explicitly defined.
2. Collision layers must be configured cleanly to prevent projectile misfires.
3. Decoupled architecture using C# events ensures that UI, Spawner, and Audio modules remain isolated from core entity logic.
4. Acceptance criteria and edge cases provide an actionable test harness for the QA and Sentinel agents.
