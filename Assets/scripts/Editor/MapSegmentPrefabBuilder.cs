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
        BuildChokePointPrefab(monumentSprite, rockSprite, bushSprite, treeOrangeSprite, treePinkSprite);
        BuildSlalomPrefab(treeOrangeSprite, treePinkSprite, rockSprite);
        BuildBossArenaPrefab(monumentSprite, rockSprite, treeOrangeSprite, treePinkSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MapSegmentPrefabBuilder] Successfully created 4 MapSegment prefabs in " + PrefabDir);
    }

    [MenuItem("Tools/Build Boss Arena Prefab")]
    public static void BuildBossArenaPrefabMenu()
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        Sprite rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/rock.png");
        Sprite monumentSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/rock-monument.png");
        Sprite treeOrangeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/tree-orange.png");
        Sprite treePinkSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/tree-pink.png");

        BuildBossArenaPrefab(monumentSprite, rockSprite, treeOrangeSprite, treePinkSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MapSegmentPrefabBuilder] Successfully created MapSegment_BossArena.prefab in " + PrefabDir);
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

            // Visible forest edge over the invisible side walls (x = +/-7.5, y 0..20)
            DressBoundaryWall(obstacles.transform, -7.5f, 0f, 20f, treeOrange, treePink, rock);
            DressBoundaryWall(obstacles.transform, 7.5f, 0f, 20f, treePink, treeOrange, rock);

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

    private static void BuildChokePointPrefab(Sprite monument, Sprite rock, Sprite bush, Sprite treeOrange, Sprite treePink)
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

            // Visible forest edge over the invisible side walls (x = +/-7.5, y 0..20)
            DressBoundaryWall(obstacles.transform, -7.5f, 0f, 20f, treeOrange, treePink, rock);
            DressBoundaryWall(obstacles.transform, 7.5f, 0f, 20f, treePink, treeOrange, rock);

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

            // Visible forest edge over the invisible side walls (x = +/-7.5, y 0..20)
            DressBoundaryWall(obstacles.transform, -7.5f, 0f, 20f, treeOrange, treePink, rock);
            DressBoundaryWall(obstacles.transform, 7.5f, 0f, 20f, treePink, treeOrange, rock);

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

    public static void BuildBossArenaPrefab(Sprite monument, Sprite rock, Sprite treeOrange, Sprite treePink)
    {
        var root = new GameObject("MapSegment_BossArena");
        try
        {
            var seg = root.AddComponent<MapSegment>();
            seg.segmentLength = 24.0f;
            seg.segmentWidth = 18.0f;
            seg.minCorridorWidth = 16.0f;
            seg.isBossArena = true;

            // Boundaries
            var boundaries = new GameObject("Boundaries");
            boundaries.transform.SetParent(root.transform, false);

            // Left Wall (X = -9.0, Y = 12.0, size 1.0 x 24.0)
            var leftWall = new GameObject("Wall_Left");
            leftWall.transform.SetParent(boundaries.transform, false);
            leftWall.transform.localPosition = new Vector3(-9.0f, 12.0f, 0f);
            var colL = leftWall.AddComponent<BoxCollider2D>();
            colL.size = new Vector2(1.0f, 24.0f);
            colL.isTrigger = false;
            leftWall.tag = "Colliders";
            leftWall.layer = 0;
            seg.leftWallCollider = colL;

            // Right Wall (X = +9.0, Y = 12.0, size 1.0 x 24.0)
            var rightWall = new GameObject("Wall_Right");
            rightWall.transform.SetParent(boundaries.transform, false);
            rightWall.transform.localPosition = new Vector3(9.0f, 12.0f, 0f);
            var colR = rightWall.AddComponent<BoxCollider2D>();
            colR.size = new Vector2(1.0f, 24.0f);
            colR.isTrigger = false;
            rightWall.tag = "Colliders";
            rightWall.layer = 0;
            seg.rightWallCollider = colR;

            // Top Wall (X = 0, Y = 24.0, size 18.0 x 1.0)
            var topWall = new GameObject("Wall_Top");
            topWall.transform.SetParent(boundaries.transform, false);
            topWall.transform.localPosition = new Vector3(0.0f, 24.0f, 0f);
            var colTop = topWall.AddComponent<BoxCollider2D>();
            colTop.size = new Vector2(18.0f, 1.0f);
            colTop.isTrigger = false;
            topWall.tag = "Colliders";
            topWall.layer = 0;
            seg.topWallCollider = colTop;
            seg.topWall = topWall;

            // Camera Lock Anchor / Arena Center (X = 0, Y = 12.0)
            var arenaCenter = new GameObject("ArenaCenter");
            arenaCenter.transform.SetParent(root.transform, false);
            arenaCenter.transform.localPosition = new Vector3(0.0f, 12.0f, 0f);
            seg.cameraLockPoint = arenaCenter.transform;

            // Spawn Points
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform, false);

            var bossSpawn = CreateSpawnPoint(spawnPoints.transform, "BossSpawnPoint", new Vector3(0.0f, 18.0f, 0f));
            seg.bossSpawnPoint = bossSpawn;
            seg.enemySpawnPoints = new Transform[] { bossSpawn };

            var itemSpawn = CreateSpawnPoint(spawnPoints.transform, "ItemSpawn_1", new Vector3(0.0f, 6.0f, 0f));
            seg.itemSpawnPoints = new Transform[] { itemSpawn };

            // Perimeter Visual Decorations (non-blocking outside combat corridor)
            var decorations = new GameObject("Decorations");
            decorations.transform.SetParent(root.transform, false);

            if (monument != null)
            {
                CreateVisual(decorations.transform, "Monument_TopLeft", new Vector3(-7.5f, 22.0f, 0f), monument);
                CreateVisual(decorations.transform, "Monument_TopRight", new Vector3(7.5f, 22.0f, 0f), monument);
                CreateVisual(decorations.transform, "Monument_BotLeft", new Vector3(-7.5f, 2.0f, 0f), monument);
                CreateVisual(decorations.transform, "Monument_BotRight", new Vector3(7.5f, 2.0f, 0f), monument);
            }

            if (rock != null)
            {
                CreateVisual(decorations.transform, "Rock_L1", new Vector3(-8.2f, 8.0f, 0f), rock);
                CreateVisual(decorations.transform, "Rock_L2", new Vector3(-8.2f, 16.0f, 0f), rock);
                CreateVisual(decorations.transform, "Rock_R1", new Vector3(8.2f, 8.0f, 0f), rock);
                CreateVisual(decorations.transform, "Rock_R2", new Vector3(8.2f, 16.0f, 0f), rock);
            }

            // Visible forest edge over the invisible side walls (x = +/-9.0, y 0..24)
            DressBoundaryWall(decorations.transform, -9.0f, 0f, 24f, treeOrange, treePink, rock);
            DressBoundaryWall(decorations.transform, 9.0f, 0f, 24f, treePink, treeOrange, rock);

            SavePrefab(root, $"{PrefabDir}/MapSegment_BossArena.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// Dresses an invisible boundary wall with a deterministic tree/rock line so the
    /// play-area edge reads visually (forest edge) instead of an invisible collider.
    /// Visual-only (no colliders): zero gameplay/physics impact.
    /// </summary>
    private static void DressBoundaryWall(Transform parent, float wallX, float yStart, float yEnd, Sprite treeA, Sprite treeB, Sprite rock)
    {
        int i = 0;
        for (float y = yStart + 1.2f; y < yEnd - 0.5f; y += 2.5f, i++)
        {
            float xoff = (i % 2 == 0) ? 0.35f : -0.35f;
            Sprite s = (i % 4 == 3) ? rock : (((i % 2) == 0) ? treeA : treeB);
            if (s == null) continue;
            CreateVisual(parent, $"WallDress_{wallX}_{i}", new Vector3(wallX + xoff, y, 0f), s);
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
        // Canonical layout: walls live under Boundaries/ only. Drop stray
        // root-level duplicates from older builder versions (identical
        // position/size, unreferenced) so each prefab has exactly one wall set.
        foreach (var wallName in new[] { "Wall_Left", "Wall_Right", "Wall_Top" })
        {
            var stray = root.transform.Find(wallName);
            if (stray != null) Object.DestroyImmediate(stray.gameObject);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success)
        {
            Debug.LogError($"[MapSegmentPrefabBuilder] Failed to save prefab at {path}");
        }
    }
}
#endif
