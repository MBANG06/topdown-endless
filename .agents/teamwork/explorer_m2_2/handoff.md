# Investigation & Architecture Handoff Report — Explorer M2.2

**Author**: `explorer_m2_2` (teamwork_preview_explorer)  
**Date**: 2026-09-22  
**Target Milestone**: Milestone 2 (M2) — Modular Map Segment Spawning & Object Pooling  
**Working Directory**: `.agents/teamwork/explorer_m2_2/`  
**Reference Documents**:
- `.agents/teamwork/ORIGINAL_REQUEST.md` (Requirement R2, Acceptance Criteria)
- `PROJECT.md` (Features 8, 9, 10, 11; Interface Contracts)
- `.agents/teamwork/explorer_2/handoff.md`
- `.agents/teamwork/explorer_m2_1/handoff.md` (MapSegment.cs Architecture)
- `.agents/teamwork/explorer_m2_3/handoff.md` (MapSegmentPool.cs & MapManager.cs Architecture)

---

## 1. Observation

### 1.1 Sliced-Objects Sprite Asset Catalog & Exact World Dimensions
Inspection of `Assets/Tiny RPG Forest/Artwork/Environment/sliced-objects/` via file metadata analysis and Unity MCP Roslyn reflection (`execute_code`):
All sprites in this directory use `spritePixelsToUnits: 16`, `spriteMode: 1` (Single Sprite), `filterMode: 0` (Point), `textureType: 8` (Sprite 2D/UI). At 16 Pixels Per Unit (PPU), pixel dimensions translate directly into Unity world units ($1\text{ unit} = 16\text{ px}$):

| File Name | GUID | Pixel Dimensions | World Units $(W \times H)$ | Recommended Collider Type & Size |
|---|---|---|---|---|
| `rock.png` | `37eaf6e537ca749adaaea0a3e900d411` | $31 \times 29\text{ px}$ | $1.94\text{u} \times 1.81\text{u}$ | `CircleCollider2D` (Radius: 0.8u) |
| `rock-monument.png` | `2eb82ac66de274383af71b2eb2c75da0` | $101 \times 90\text{ px}$ | $6.31\text{u} \times 5.63\text{u}$ | `BoxCollider2D` (Size: 2.4u $\times$ 5.6u at scale 0.38x) |
| `tree-orange.png` | `8c69f3cd16f534bec8236a6484f500d4` | $67 \times 80\text{ px}$ | $4.19\text{u} \times 5.00\text{u}$ | `BoxCollider2D` trunk (Size: 1.6u $\times$ 1.8u, Offset: 0, -1.0u) |
| `tree-pink.png` | `c008626f5108946299e1a584ba85fa5b` | $67 \times 80\text{ px}$ | $4.19\text{u} \times 5.00\text{u}$ | `BoxCollider2D` trunk (Size: 1.6u $\times$ 1.8u, Offset: 0, -1.0u) |
| `tree-dried.png` | `db07cc93e21f1457d9a1364618027fbb` | $101 \times 106\text{ px}$ | $6.31\text{u} \times 6.63\text{u}$ | `BoxCollider2D` trunk (Size: 2.0u $\times$ 2.2u) |
| `bush.png` | `dbc624f7b3a644dcf989e434e63d9a98` | $29 \times 24\text{ px}$ | $1.81\text{u} \times 1.50\text{u}$ | `CapsuleCollider2D` (Size: 1.4u $\times$ 0.8u) |
| `bush-tall.png` | `aa94c6da14ecf4a8690f02549a9ac6a7` | $18 \times 29\text{ px}$ | $1.13\text{u} \times 1.81\text{u}$ | `CapsuleCollider2D` (Size: 1.0u $\times$ 1.6u) |
| `trunk.png` | `606bc7833699b46bfb26e8141d2a64a7` | $38 \times 33\text{ px}$ | $2.38\text{u} \times 2.06\text{u}$ | `CircleCollider2D` (Radius: 0.9u) |
| `sign.png` | `b52a57bea9b6a47e096e15fd1bbe2fb0` | $19 \times 22\text{ px}$ | $1.19\text{u} \times 1.38\text{u}$ | Decorative or `BoxCollider2D` (Size: 0.8u $\times$ 0.8u) |

All 9 sprites were loaded into memory via `AssetDatabase.LoadAssetAtPath<Sprite>()` through Unity MCP without null references or missing sub-assets.

### 1.2 Collision Tagging & Trigger Passthrough Rules
1. **Registered Project Tags**: Only `"Colliders"` and `"Enemy"` exist as custom user tags (`TagManager.asset`). Any other tag string is rejected by Unity Editor.
2. **Solid Obstacles vs Trigger Passthrough**:
   - `Bullet.cs` (lines 67–71):
     ```csharp
     Collider2D hitCollider = hitObj.GetComponent<Collider2D>();
     if (hitCollider != null && hitCollider.isTrigger && damageable == null) return;
     ```
   - `EnemyBullet.cs` (lines 65–71):
     ```csharp
     Collider2D hitCollider = hitObj.GetComponent<Collider2D>();
     bool isPlayer = hitObj.CompareTag("Player") || ...;
     if (hitCollider != null && hitCollider.isTrigger && !isPlayer) return;
     ```
   - **Critical Rule**: All boundary walls and obstacle colliders **must have `isTrigger = false`**. If `isTrigger` were `true`, player bullets, enemy bullets, and rigidbodies would pass straight through walls and obstacles.
3. **Tagging Requirement**:
   - In `GrenadeProjectile.cs` (line 124): `bool isWall = target.CompareTag("Colliders") || ...`
   - In `ChallengerM2Tests.cs` (line 145): `EnemyBullet Auto-Destruction on Solid Arena Walls` checks `CompareTag("Colliders")`.
   - Therefore, all boundary wall GameObjects and obstacle GameObjects **must be assigned Tag `"Colliders"`** and **Layer 0 (`Default`)**.

### 1.3 Segment Dimensions & Boundary Clamping Invariants
From `PROJECT.md` and `ScrollingMapTests.cs` (lines 415–435, 1210–1222):
- Standardized length along Y: `segmentLength = 20.0f` units ($Y \in [0.0, 20.0]$ local).
- Standardized width along X: `segmentWidth = 15.0f` units.
- Lateral boundaries: Left Wall centered at $X = -7.5$, Right Wall centered at $X = +7.5$.
  - With `BoxCollider2D` size `(1.0, 20.0)` centered at $(\pm 7.5, 10.0, 0.0)$, the inner faces of the walls reside at $X = -7.0$ and $X = +7.0$, establishing a $14.0\text{u}$ inner playing channel (or $15.0\text{u}$ when walls are positioned at $X = \pm 8.0$).
- Lateral obstacle constraint (`T2_SCM_F04_03`): obstacle placement must never exceed boundaries $[-7.5, +7.5]$.
- Guaranteed corridor width: strictly $\ge 4.0\text{u}$ unblocked clearance across all segments.

---

## 2. Logic Chain

```
[Observation 1.1: Tiny RPG Forest Sliced-Objects Assets]
   │
   ├─► Verified exact sprite dimensions at 16 PPU:
   │   - rock.png: 1.94 x 1.81u
   │   - rock-monument.png: 6.31 x 5.63u
   │   - tree-orange.png / tree-pink.png: 4.19 x 5.00u
   │   - bush.png: 1.81 x 1.50u
   │   => Native, beautiful top-down pixel art assets exist and are immediately loadable!
   │
[Observation 1.2: Collision Mechanics & Physics Contracts]
   │
   ├─► Both Bullet.cs and EnemyBullet.cs pass through triggers (isTrigger = true).
   │   Player and Enemies have Dynamic Rigidbody2D stopped by non-trigger colliders.
   │   GrenadeProjectile detonates on tag "Colliders".
   │   => All segment boundary walls and obstacles MUST have:
   │      - Collider2D.isTrigger = false
   │      - GameObject.tag = "Colliders"
   │      - GameObject.layer = 0 ("Default")
   │
[Observation 1.3 & Requirement R2: Prefab Specifications]
   │
   ├─► Prefab 1: MapSegment_Corridor.prefab
   │   - Margins: Flanked by trees and rocks along X ∈ [-6.0, -5.0] and X ∈ [+5.0, +6.0].
   │   - Central Corridor: Completely unblocked along X ∈ [-4.0, +4.0].
   │   - Navigable Width: 4.0 - (-4.0) = 8.0 units (strictly >= 4.0u).
   │
   ├─► Prefab 2: MapSegment_ChokePoint.prefab
   │   - Central Island: Rock monument at X ∈ [-1.2, +1.2], Y ∈ [7.0, 13.0] (center: (0, 10)).
   │   - Left Corridor: X ∈ [-7.0, -1.5] (width = 5.5 units).
   │   - Right Corridor: X ∈ [+1.5, +7.0] (width = 5.5 units).
   │   - Both corridors strictly >= 4.0u, allowing strategic split-lane flanking.
   │
   ├─► Prefab 3: MapSegment_Slalom.prefab
   │   - Lower Deflector at Y=6.0: Extends from Left Wall (X = -7.0) to X = -1.0 (width 6.0u).
   │     Right opening: X ∈ [-1.0, +7.0] -> width = 8.0 units (>= 6.0u).
   │   - Upper Deflector at Y=14.0: Extends from Right Wall (X = +7.0) to X = +1.0 (width 6.0u).
   │     Left opening: X ∈ [-7.0, +1.0] -> width = 8.0 units (>= 6.0u).
   │   - Neck clearance: Distance between deflector tips (-1.0, 6.0) and (+1.0, 14.0) is 8.25 units!
   │   - Guarantees continuous navigable route with width >= 6.0 units everywhere.
   │
[Requirement R2 & unityMCP Automation]
   │
   └─► Programmatic Builder: Dedicated Editor MenuItem script
       "Assets/scripts/Editor/MapSegmentPrefabBuilder.cs"
       - Creates Assets/Prefabs/MapSegments/
       - Constructs GameObjects, attaches MapSegment, colliders, sprites, spawn points
       - Saves via PrefabUtility.SaveAsPrefabAsset()
       - Can be invoked in 1 click or via unityMCP execute_menu_item / execute_code.
```

---

## 3. Caveats

1. **Compilation Precondition**: `MapSegmentPrefabBuilder.cs` references `MapSegment`. Therefore, `MapSegment.cs` (designed in `explorer_m2_1/handoff.md`) must be created in `Assets/scripts/` before compiling and running the builder.
2. **Local Scale Invariant**: The root GameObject of each segment prefab must have `localScale = (1, 1, 1)`. Scaling the root would distort the collider dimensions and the 20-unit vertical spacing interval.
3. **Sorting Layer & Order**:
   - Background Floor: Order in Layer = `-10`.
   - Boundary Walls: Invisible colliders (no renderer) or Order = `0`.
   - Obstacles (Trees/Rocks/Bushes): Order in Layer = `10` (or Y-sorted via Transparency Sort Mode).
   - This ensures trees and monuments render naturally over ground tiles and items.
4. **Spawn Points Separation**: Enemy and item spawn points are empty GameObjects with `Transform` components only (no colliders or renderers) to avoid phantom collisions.

---

## 4. Conclusion & Complete Design Specifications

### 4.1 Common Structural Blueprint for All 3 Prefabs
Every map segment prefab adheres to this standardized hierarchy:

```
MapSegment_<Type> (Root GameObject, Local Position: (0, 0, 0), Local Scale: (1, 1, 1))
├── [Component: MapSegment]
│     segmentLength = 20.0f
│     segmentWidth = 15.0f
│     minCorridorWidth = (8.0f | 5.5f | 6.0f)
│     leftWallCollider -> Wall_Left
│     rightWallCollider -> Wall_Right
│     enemySpawnPoints -> [EnemySpawn_1, EnemySpawn_2, ...]
│     itemSpawnPoints -> [ItemSpawn_1]
│
├── Boundaries (Container, Transform: (0, 0, 0))
│   ├── Wall_Left  (BoxCollider2D: Center (-7.5, 10, 0), Size (1.0, 20.0), Tag: "Colliders", isTrigger: false)
│   └── Wall_Right (BoxCollider2D: Center (+7.5, 10, 0), Size (1.0, 20.0), Tag: "Colliders", isTrigger: false)
│
├── Obstacles (Container, Transform: (0, 0, 0))
│   └── [Child Obstacles with SpriteRenderer + Collider2D, Tag: "Colliders", isTrigger: false]
│
└── SpawnPoints (Container, Transform: (0, 0, 0))
    ├── EnemySpawn_1 (Transform)
    ├── EnemySpawn_2 (Transform)
    ├── EnemySpawn_3 (Transform)
    └── ItemSpawn_1  (Transform)
```

---

### 4.2 Blueprint 1: `MapSegment_Corridor.prefab` (Open Flanked Highway)
- **Target Path**: `Assets/Prefabs/MapSegments/MapSegment_Corridor.prefab`
- **Concept**: High-speed highway with dense foliage on left and right margins, leaving a wide central passage.
- **Corridor Clearance**: Unblocked central width = **8.0 units** ($X \in [-4.0, +4.0]$).

#### Visual & Collider Components
| Object Name | Local Position $(X, Y, Z)$ | Sprite Asset | Collider Type | Collider Size / Radius | Tag |
|---|---|---|---|---|---|
| `Wall_Left` | `(-7.5, 10.0, 0.0)` | None | `BoxCollider2D` | `(1.0, 20.0)` | `Colliders` |
| `Wall_Right` | `(+7.5, 10.0, 0.0)` | None | `BoxCollider2D` | `(1.0, 20.0)` | `Colliders` |
| `Tree_L1` | `(-5.5, 4.0, 0.0)` | `tree-orange.png` | `BoxCollider2D` | Size: `(1.6, 1.8)`, Offset: `(0, -1.0)` | `Colliders` |
| `Rock_L1` | `(-5.5, 10.0, 0.0)` | `rock.png` | `CircleCollider2D` | Radius: `0.8` | `Colliders` |
| `Tree_L2` | `(-5.5, 16.0, 0.0)` | `tree-pink.png` | `BoxCollider2D` | Size: `(1.6, 1.8)`, Offset: `(0, -1.0)` | `Colliders` |
| `Bush_L` | `(-6.0, 7.0, 0.0)` | `bush.png` | `CapsuleCollider2D` | Size: `(1.4, 0.8)` | `Colliders` |
| `Tree_R1` | `(+5.5, 4.0, 0.0)` | `tree-pink.png` | `BoxCollider2D` | Size: `(1.6, 1.8)`, Offset: `(0, -1.0)` | `Colliders` |
| `Rock_R1` | `(+5.5, 10.0, 0.0)` | `rock.png` | `CircleCollider2D` | Radius: `0.8` | `Colliders` |
| `Tree_R2` | `(+5.5, 16.0, 0.0)` | `tree-orange.png` | `BoxCollider2D` | Size: `(1.6, 1.8)`, Offset: `(0, -1.0)` | `Colliders` |
| `Bush_R` | `(+6.0, 13.0, 0.0)` | `bush.png` | `CapsuleCollider2D` | Size: `(1.4, 0.8)` | `Colliders` |

#### Spawn Points
| Name | Local Position $(X, Y, Z)$ | Tactical Purpose |
|---|---|---|
| `EnemySpawn_1` | `(0.0, 5.0, 0.0)` | Mid-lane advance chaser |
| `EnemySpawn_2` | `(-2.5, 12.0, 0.0)` | Left lane shooter kiter |
| `EnemySpawn_3` | `(+2.5, 17.0, 0.0)` | Right lane rusher ambusher |
| `ItemSpawn_1` | `(0.0, 10.0, 0.0)` | Center reward (grenade pickup) |

---

### 4.3 Blueprint 2: `MapSegment_ChokePoint.prefab` (Split Dual-Corridor / Central Island)
- **Target Path**: `Assets/Prefabs/MapSegments/MapSegment_ChokePoint.prefab`
- **Concept**: Massive central ancient monument splits the route into two parallel corridors, forcing tactical decisions and flanking.
- **Corridor Clearance**:
  - Left Corridor: $X \in [-7.0, -1.5]$ (width = **5.5 units**).
  - Right Corridor: $X \in [+1.5, +7.0]$ (width = **5.5 units**).
  - Central Island: $X \in [-1.2, +1.2]$, $Y \in [7.0, 13.0]$ (width = 2.4 units, height = 6.0 units).

#### Visual & Collider Components
| Object Name | Local Position $(X, Y, Z)$ | Sprite Asset | Collider Type | Collider Size / Radius | Tag |
|---|---|---|---|---|---|
| `Wall_Left` | `(-7.5, 10.0, 0.0)` | None | `BoxCollider2D` | `(1.0, 20.0)` | `Colliders` |
| `Wall_Right` | `(+7.5, 10.0, 0.0)` | None | `BoxCollider2D` | `(1.0, 20.0)` | `Colliders` |
| `Monument_Core` | `(0.0, 10.0, 0.0)` | `rock-monument.png` (scale 0.38, 1.0, 1.0) | `BoxCollider2D` | `(2.4, 5.6)` | `Colliders` |
| `Monument_RockTop`| `(0.0, 13.0, 0.0)` | `rock.png` | `CircleCollider2D` | Radius: `0.8` | `Colliders` |
| `Monument_RockBot`| `(0.0, 7.0, 0.0)` | `rock.png` | `CircleCollider2D` | Radius: `0.8` | `Colliders` |
| `Bush_BL` | `(-6.2, 2.0, 0.0)` | `bush.png` | `CircleCollider2D` | Radius: `0.6` | `Colliders` |
| `Bush_BR` | `(+6.2, 2.0, 0.0)` | `bush.png` | `CircleCollider2D` | Radius: `0.6` | `Colliders` |
| `Bush_TL` | `(-6.2, 18.0, 0.0)` | `bush.png` | `CircleCollider2D` | Radius: `0.6` | `Colliders` |
| `Bush_TR` | `(+6.2, 18.0, 0.0)` | `bush.png` | `CircleCollider2D` | Radius: `0.6` | `Colliders` |

#### Spawn Points
| Name | Local Position $(X, Y, Z)$ | Tactical Purpose |
|---|---|---|
| `EnemySpawn_1` | `(-4.0, 6.0, 0.0)` | Left lane entrance guard |
| `EnemySpawn_2` | `(+4.0, 6.0, 0.0)` | Right lane entrance guard |
| `EnemySpawn_3` | `(-4.0, 14.0, 0.0)` | Left lane exit pincer |
| `EnemySpawn_4` | `(+4.0, 14.0, 0.0)` | Right lane exit pincer |
| `ItemSpawn_1` | `(0.0, 3.0, 0.0)` | Pre-fork supply drop |

---

### 4.4 Blueprint 3: `MapSegment_Slalom.prefab` (Winding S-Curve / Alternating Deflectors)
- **Target Path**: `Assets/Prefabs/MapSegments/MapSegment_Slalom.prefab`
- **Concept**: Alternating horizontal deflector barriers force the player to weave in an S-curve, breaking bullet lines-of-sight and rewarding agile movement.
- **Corridor Clearance**:
  - Lower Deflector at $Y = 6.0$: Extends from $X = -7.0$ to $X = -1.0$. Passable opening on right is $X \in [-1.0, +7.0]$ (width = **8.0 units** $\ge 6.0\text{u}$).
  - Upper Deflector at $Y = 14.0$: Extends from $X = +7.0$ to $X = +1.0$. Passable opening on left is $X \in [-7.0, +1.0]$ (width = **8.0 units** $\ge 6.0\text{u}$).
  - S-Curve Diagonal Neck: Euclidean distance between deflector tips $(-1.0, 6.0)$ and $(+1.0, 14.0)$ is **8.25 units**.
  - Minimum navigable width throughout the trajectory: strictly $\ge 6.0\text{u}$.

#### Visual & Collider Components
| Object Name | Local Position $(X, Y, Z)$ | Sprite Asset | Collider Type | Collider Size / Radius | Tag |
|---|---|---|---|---|---|
| `Wall_Left` | `(-7.5, 10.0, 0.0)` | None | `BoxCollider2D` | `(1.0, 20.0)` | `Colliders` |
| `Wall_Right` | `(+7.5, 10.0, 0.0)` | None | `BoxCollider2D` | `(1.0, 20.0)` | `Colliders` |
| `Deflector_Lower` | `(-4.0, 6.0, 0.0)` | Composite | `BoxCollider2D` | `(6.0, 1.5)` | `Colliders` |
| `├ Tree_Def1_Base` | `(-6.0, 6.0, 0.0)` | `tree-orange.png` | None (visual) | N/A | Untagged |
| `├ Rock_Def1_Mid` | `(-3.5, 6.0, 0.0)` | `rock.png` | None (visual) | N/A | Untagged |
| `└ Rock_Def1_Tip` | `(-1.2, 6.0, 0.0)` | `rock.png` | None (visual) | N/A | Untagged |
| `Deflector_Upper` | `(+4.0, 14.0, 0.0)` | Composite | `BoxCollider2D` | `(6.0, 1.5)` | `Colliders` |
| `├ Tree_Def2_Base` | `(+6.0, 14.0, 0.0)` | `tree-pink.png` | None (visual) | N/A | Untagged |
| `├ Rock_Def2_Mid` | `(+3.5, 14.0, 0.0)` | `rock.png` | None (visual) | N/A | Untagged |
| `└ Rock_Def2_Tip` | `(+1.2, 14.0, 0.0)` | `rock.png` | None (visual) | N/A | Untagged |

#### Spawn Points
| Name | Local Position $(X, Y, Z)$ | Tactical Purpose |
|---|---|---|
| `EnemySpawn_1` | `(+3.5, 5.0, 0.0)` | Turn 1 apex ambush |
| `EnemySpawn_2` | `(0.0, 10.0, 0.0)` | Mid-curve intersection skirmish |
| `EnemySpawn_3` | `(-3.5, 15.0, 0.0)` | Turn 2 apex ambush |
| `ItemSpawn_1` | `(+3.0, 10.0, 0.0)` | Safe diagonal side pocket |

---

### 4.5 Recommended Programmatic Prefab Builder: `MapSegmentPrefabBuilder.cs`

Below is the complete, production-ready C# Editor script to be placed at:  
`Assets/scripts/Editor/MapSegmentPrefabBuilder.cs`

```csharp
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Turnkey Editor utility script to programmatically construct, configure,
/// and save the 3 standardized MapSegment prefabs into Assets/Prefabs/MapSegments/.
/// </summary>
public static class MapSegmentPrefabBuilder
{
    private const string PrefabDir = "Assets/Prefabs/MapSegments";
    private const string ArtDir = "Assets/Tiny RPG Forest/Artwork/Environment/sliced-objects";

    [MenuItem("Tools/Build Map Segment Prefabs")]
    public static void BuildAllPrefabs()
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        // Load Sprites
        Sprite rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/rock.png");
        Sprite monumentSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/rock-monument.png");
        Sprite treeOrangeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/tree-orange.png");
        Sprite treePinkSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/tree-pink.png");
        Sprite bushSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/bush.png");

        BuildCorridorPrefab(rockSprite, treeOrangeSprite, treePinkSprite, bushSprite);
        BuildChokePointPrefab(monumentSprite, rockSprite, bushSprite);
        BuildSlalomPrefab(treeOrangeSprite, treePinkSprite, rockSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MapSegmentPrefabBuilder] Successfully created 3 MapSegment prefabs in " + PrefabDir);
    }

    private static void BuildCorridorPrefab(Sprite rock, Sprite treeOrange, Sprite treePink, Sprite bush)
    {
        var root = new GameObject("MapSegment_Corridor");
        try
        {
            var seg = root.AddComponent<MapSegment>();
            seg.segmentLength = 20.0f;
            seg.segmentWidth = 15.0f;
            seg.minCorridorWidth = 8.0f;

            // Boundaries
            var boundaries = new GameObject("Boundaries");
            boundaries.transform.SetParent(root.transform, false);
            CreateBoundaryWalls(boundaries.transform, seg);

            // Obstacles
            var obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(root.transform, false);

            // Left Margin (X: -6 to -5)
            CreateTree(obstacles.transform, "Tree_L1", new Vector3(-5.5f, 4.0f, 0f), treeOrange);
            CreateRock(obstacles.transform, "Rock_L1", new Vector3(-5.5f, 10.0f, 0f), rock);
            CreateTree(obstacles.transform, "Tree_L2", new Vector3(-5.5f, 16.0f, 0f), treePink);
            CreateBush(obstacles.transform, "Bush_L", new Vector3(-6.0f, 7.0f, 0f), bush);

            // Right Margin (X: +5 to +6)
            CreateTree(obstacles.transform, "Tree_R1", new Vector3(5.5f, 4.0f, 0f), treePink);
            CreateRock(obstacles.transform, "Rock_R1", new Vector3(5.5f, 10.0f, 0f), rock);
            CreateTree(obstacles.transform, "Tree_R2", new Vector3(5.5f, 16.0f, 0f), treeOrange);
            CreateBush(obstacles.transform, "Bush_R", new Vector3(6.0f, 13.0f, 0f), bush);

            // SpawnPoints
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform, false);
            var e1 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_1", new Vector3(0.0f, 5.0f, 0f));
            var e2 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_2", new Vector3(-2.5f, 12.0f, 0f));
            var e3 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_3", new Vector3(2.5f, 17.0f, 0f));
            var i1 = CreateSpawnPoint(spawnPoints.transform, "ItemSpawn_1", new Vector3(0.0f, 10.0f, 0f));

            seg.enemySpawnPoints = new Transform[] { e1, e2, e3 };
            seg.itemSpawnPoints = new Transform[] { i1 };

            SavePrefab(root, $"{PrefabDir}/MapSegment_Corridor.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildChokePointPrefab(Sprite monument, Sprite rock, Sprite bush)
    {
        var root = new GameObject("MapSegment_ChokePoint");
        try
        {
            var seg = root.AddComponent<MapSegment>();
            seg.segmentLength = 20.0f;
            seg.segmentWidth = 15.0f;
            seg.minCorridorWidth = 5.5f;

            // Boundaries
            var boundaries = new GameObject("Boundaries");
            boundaries.transform.SetParent(root.transform, false);
            CreateBoundaryWalls(boundaries.transform, seg);

            // Obstacles
            var obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(root.transform, false);

            // Central Island Monument (X: -1.2 to +1.2, Y: 7 to 13)
            var monumentGo = new GameObject("Monument_Core");
            monumentGo.transform.SetParent(obstacles.transform, false);
            monumentGo.transform.localPosition = new Vector3(0.0f, 10.0f, 0.0f);
            monumentGo.transform.localScale = new Vector3(0.38f, 1.0f, 1.0f);
            var srM = monumentGo.AddComponent<SpriteRenderer>();
            srM.sprite = monument;
            srM.sortingOrder = 10;
            var boxM = monumentGo.AddComponent<BoxCollider2D>();
            boxM.size = new Vector2(6.31f, 5.63f);
            boxM.isTrigger = false;
            monumentGo.tag = "Colliders";

            CreateRock(obstacles.transform, "Monument_RockTop", new Vector3(0.0f, 13.0f, 0f), rock);
            CreateRock(obstacles.transform, "Monument_RockBot", new Vector3(0.0f, 7.0f, 0f), rock);

            // Corner Bush Accents
            CreateBush(obstacles.transform, "Bush_BL", new Vector3(-6.2f, 2.0f, 0f), bush);
            CreateBush(obstacles.transform, "Bush_BR", new Vector3(6.2f, 2.0f, 0f), bush);
            CreateBush(obstacles.transform, "Bush_TL", new Vector3(-6.2f, 18.0f, 0f), bush);
            CreateBush(obstacles.transform, "Bush_TR", new Vector3(6.2f, 18.0f, 0f), bush);

            // SpawnPoints
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform, false);
            var e1 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_1", new Vector3(-4.0f, 6.0f, 0f));
            var e2 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_2", new Vector3(4.0f, 6.0f, 0f));
            var e3 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_3", new Vector3(-4.0f, 14.0f, 0f));
            var e4 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_4", new Vector3(4.0f, 14.0f, 0f));
            var i1 = CreateSpawnPoint(spawnPoints.transform, "ItemSpawn_1", new Vector3(0.0f, 3.0f, 0f));

            seg.enemySpawnPoints = new Transform[] { e1, e2, e3, e4 };
            seg.itemSpawnPoints = new Transform[] { i1 };

            SavePrefab(root, $"{PrefabDir}/MapSegment_ChokePoint.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildSlalomPrefab(Sprite treeOrange, Sprite treePink, Sprite rock)
    {
        var root = new GameObject("MapSegment_Slalom");
        try
        {
            var seg = root.AddComponent<MapSegment>();
            seg.segmentLength = 20.0f;
            seg.segmentWidth = 15.0f;
            seg.minCorridorWidth = 6.0f;

            // Boundaries
            var boundaries = new GameObject("Boundaries");
            boundaries.transform.SetParent(root.transform, false);
            CreateBoundaryWalls(boundaries.transform, seg);

            // Obstacles
            var obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(root.transform, false);

            // Lower Deflector Wall (Y = 6.0, extends from X = -7.0 inward to X = -1.0)
            var def1 = new GameObject("Deflector_Lower");
            def1.transform.SetParent(obstacles.transform, false);
            def1.transform.localPosition = new Vector3(-4.0f, 6.0f, 0f);
            var boxDef1 = def1.AddComponent<BoxCollider2D>();
            boxDef1.size = new Vector2(6.0f, 1.5f);
            boxDef1.isTrigger = false;
            def1.tag = "Colliders";

            CreateVisual(def1.transform, "Tree_Base", new Vector3(-2.0f, 0f, 0f), treeOrange);
            CreateVisual(def1.transform, "Rock_Mid", new Vector3(0.5f, 0f, 0f), rock);
            CreateVisual(def1.transform, "Rock_Tip", new Vector3(2.5f, 0f, 0f), rock);

            // Upper Deflector Wall (Y = 14.0, extends from X = +7.0 inward to X = +1.0)
            var def2 = new GameObject("Deflector_Upper");
            def2.transform.SetParent(obstacles.transform, false);
            def2.transform.localPosition = new Vector3(4.0f, 14.0f, 0f);
            var boxDef2 = def2.AddComponent<BoxCollider2D>();
            boxDef2.size = new Vector2(6.0f, 1.5f);
            boxDef2.isTrigger = false;
            def2.tag = "Colliders";

            CreateVisual(def2.transform, "Tree_Base", new Vector3(2.0f, 0f, 0f), treePink);
            CreateVisual(def2.transform, "Rock_Mid", new Vector3(-0.5f, 0f, 0f), rock);
            CreateVisual(def2.transform, "Rock_Tip", new Vector3(-2.5f, 0f, 0f), rock);

            // SpawnPoints
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform, false);
            var e1 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_1", new Vector3(3.5f, 5.0f, 0f));
            var e2 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_2", new Vector3(0.0f, 10.0f, 0f));
            var e3 = CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_3", new Vector3(-3.5f, 15.0f, 0f));
            var i1 = CreateSpawnPoint(spawnPoints.transform, "ItemSpawn_1", new Vector3(3.0f, 10.0f, 0f));

            seg.enemySpawnPoints = new Transform[] { e1, e2, e3 };
            seg.itemSpawnPoints = new Transform[] { i1 };

            SavePrefab(root, $"{PrefabDir}/MapSegment_Slalom.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateBoundaryWalls(Transform parent, MapSegment seg)
    {
        // Left Wall
        var leftWall = new GameObject("Wall_Left");
        leftWall.transform.SetParent(parent, false);
        leftWall.transform.localPosition = new Vector3(-7.5f, 10.0f, 0f);
        var colL = leftWall.AddComponent<BoxCollider2D>();
        colL.size = new Vector2(1.0f, 20.0f);
        colL.isTrigger = false;
        leftWall.tag = "Colliders";
        leftWall.layer = 0;
        seg.leftWallCollider = colL;

        // Right Wall
        var rightWall = new GameObject("Wall_Right");
        rightWall.transform.SetParent(parent, false);
        rightWall.transform.localPosition = new Vector3(7.5f, 10.0f, 0f);
        var colR = rightWall.AddComponent<BoxCollider2D>();
        colR.size = new Vector2(1.0f, 20.0f);
        colR.isTrigger = false;
        rightWall.tag = "Colliders";
        rightWall.layer = 0;
        seg.rightWallCollider = colR;
    }

    private static void CreateTree(Transform parent, string name, Vector3 pos, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.6f, 1.8f);
        box.offset = new Vector2(0f, -1.0f);
        box.isTrigger = false;
        go.tag = "Colliders";
        go.layer = 0;
    }

    private static void CreateRock(Transform parent, string name, Vector3 pos, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.8f;
        col.isTrigger = false;
        go.tag = "Colliders";
        go.layer = 0;
    }

    private static void CreateBush(Transform parent, string name, Vector3 pos, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.4f, 0.8f);
        cap.isTrigger = false;
        go.tag = "Colliders";
        go.layer = 0;
    }

    private static void CreateVisual(Transform parent, string name, Vector3 localPos, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
    }

    private static Transform CreateSpawnPoint(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success)
        {
            Debug.LogError($"[MapSegmentPrefabBuilder] Failed to save prefab at {path}");
        }
    }
}
#endif
```

---

## 5. Verification Method

### 5.1 Programmatic Execution via Unity MCP
Once `Assets/scripts/MapSegment.cs` and `Assets/scripts/Editor/MapSegmentPrefabBuilder.cs` are created by the Worker, execute the prefab generation with either command:

1. **Via `execute_menu_item`**:
   ```json
   {
     "menu_path": "Tools/Build Map Segment Prefabs"
   }
   ```
2. **Via `execute_code`**:
   ```csharp
   MapSegmentPrefabBuilder.BuildAllPrefabs();
   ```

### 5.2 Asset & Prefab Existence Inspection
Inspect the generated prefab assets via MCP `execute_code`:
```csharp
string[] paths = new[]
{
    "Assets/Prefabs/MapSegments/MapSegment_Corridor.prefab",
    "Assets/Prefabs/MapSegments/MapSegment_ChokePoint.prefab",
    "Assets/Prefabs/MapSegments/MapSegment_Slalom.prefab"
};

var sb = new System.Text.StringBuilder();
foreach (var p in paths)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
    if (prefab == null) { sb.AppendLine($"{p}: MISSING"); continue; }
    var seg = prefab.GetComponent<MapSegment>();
    sb.AppendLine($"{p}: FOUND, Length={seg.segmentLength}, Width={seg.segmentWidth}, MinCorridor={seg.minCorridorWidth}, Enemies={seg.enemySpawnPoints.Length}, Items={seg.itemSpawnPoints.Length}");
}
return sb.ToString();
```

Expected Output:
```
Assets/Prefabs/MapSegments/MapSegment_Corridor.prefab: FOUND, Length=20, Width=15, MinCorridor=8, Enemies=3, Items=1
Assets/Prefabs/MapSegments/MapSegment_ChokePoint.prefab: FOUND, Length=20, Width=15, MinCorridor=5.5, Enemies=4, Items=1
Assets/Prefabs/MapSegments/MapSegment_Slalom.prefab: FOUND, Length=20, Width=15, MinCorridor=6, Enemies=3, Items=1
```

### 5.3 Collision & Physics Automated Verification
Run test suites via `execute_menu_item`:
```json
{
  "menu_path": "E2E Tests/Run Scrolling Map Tests"
}
```
Verify that all 120 tests pass, confirming:
- `T1_SCM_F04_01` (Segment Length Standard 20u): PASS
- `T1_SCM_F04_02` (Segment Width Standard 15u): PASS
- `T1_SCM_F04_04` (Guaranteed Corridor Minimum Width $\ge 4.0\text{u}$): PASS
- `T2_SCM_F04_01` (Exact Corridor Width Minimum 4.0u): PASS
- `T2_SCM_F04_03` (Segment Lateral Wall Clamp $[-7.5, +7.5]$): PASS

### 5.4 Invalidation Conditions
- Any obstacle placed outside $X \in [-7.5, +7.5]$.
- Any segment corridor narrowing below 4.0 units.
- Any boundary wall or obstacle lacking the `"Colliders"` tag or having `isTrigger = true`.
- Prefab asset failing to load via `AssetDatabase.LoadAssetAtPath<GameObject>`.
