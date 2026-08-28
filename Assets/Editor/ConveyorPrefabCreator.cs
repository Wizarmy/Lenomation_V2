#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ConveyorPrefabCreator
{
    static string StraightMeshPath => PathingConfig.StraightFolder + "StraightMesh.asset";
    static string EndCapMeshPath   => PathingConfig.EndCapFolder   + "EndCapMesh.asset";
    static string EndCapPrefabPath => PathingConfig.EndCapFolder   + "EndCapConveyor.prefab";
    static string LinkPrefabPath => PathingConfig.LinkFolder + "LinkConveyor.prefab";


   [MenuItem("Automation/Create Conveyor Prefabs")]
public static void CreateConveyorPrefabs()
{
    PathingUtility.EnsureAllFolders();
    
    PathingUtility.DeleteFolderAndRecreate(PathingConfig.LinkFolder);
    PathingUtility.DeleteFolderAndRecreate(PathingConfig.StraightFolder);
    PathingUtility.DeleteFolderAndRecreate(PathingConfig.CornerFolder);
    PathingUtility.DeleteFolderAndRecreate(PathingConfig.EndCapFolder);
    

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    // 2. Materials
    var top    = VisualsUtility.GetOrCreateMaterial("ConveyorTop",    ConveyorConfig.TopColor);
    var bottom = VisualsUtility.GetOrCreateMaterial("ConveyorBottom", ConveyorConfig.BottomColor);
    var side   = VisualsUtility.GetOrCreateMaterial("ConveyorSide",   ConveyorConfig.SideColor);
    var capMat = VisualsUtility.GetOrCreateMaterial("ConveyorEndCap", ConveyorConfig.EndCapColor);
    VisualsUtility.GetOrCreateMaterial("ConveyorArrow",     ConveyorConfig.ArrowColor);
    VisualsUtility.GetOrCreateMaterial("ConveyorGuardRail", ConveyorConfig.GuardRailColor);

    // 3. Guard-rail prefabs (folders are empty, so Ensure* must build new)
    PrefabManager.EnsureGuardRailPrefab();
    PrefabManager.EnsureEndCapGuardRailPrefab();

    // 4. Belt meshes — write, refresh, reload by path (stable GUID for prefabs)
    float h = ConveyorConfig.BeltHeight;

    string straightMeshPath = PathingConfig.StraightFolder + "StraightMesh.asset";
    var straightMesh = VisualsUtility.CreateBoxMeshSubmeshes(
        new Vector3(ConveyorConfig.BeltWidth, h, ConveyorConfig.BeltLength),
        new Vector3(0f, h * 0.5f, 0f),
        "StraightMesh");
    AssetDatabase.CreateAsset(straightMesh, straightMeshPath);
    
    string cornerMeshPath = PathingConfig.CornerFolder + "CornerMesh.asset";
    var cornerMesh = VisualsUtility.CreateCornerMesh("CornerMesh");
    AssetDatabase.CreateAsset(cornerMesh, cornerMeshPath);

    string endCapMeshPath = PathingConfig.EndCapFolder + "EndCapMesh.asset";
    var endCapMesh = VisualsUtility.CreateEndCapMeshEllipse("EndCapMesh");
    AssetDatabase.CreateAsset(endCapMesh, endCapMeshPath);
    
    // after endcap mesh...
    string linkMeshPath = PathingConfig.LinkFolder + "LinkMesh.asset";
    var linkMesh = VisualsUtility.CreateBoxMeshSubmeshes(
        new Vector3(ConveyorConfig.BeltWidth, h, ConveyorConfig.LinkLength),
        new Vector3(0f, h * 0.5f, 0f),
        "LinkMesh");
    AssetDatabase.CreateAsset(linkMesh, linkMeshPath);
    
    string railPath = PathingConfig.CornerFolder + "CornerGuardRailMesh.asset";
    var railMesh = VisualsUtility.CreateCornerGuardRailMesh();
    AssetDatabase.CreateAsset(railMesh, railPath);
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    railMesh = AssetDatabase.LoadAssetAtPath<Mesh>(railPath);

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    straightMesh = AssetDatabase.LoadAssetAtPath<Mesh>(straightMeshPath);
    endCapMesh   = AssetDatabase.LoadAssetAtPath<Mesh>(endCapMeshPath);
    linkMesh = AssetDatabase.LoadAssetAtPath<Mesh>(linkMeshPath);

    // 5. Prefabs using the reloaded meshes
    for (int level = 1; level <= 5; level++)
    {
        CreateStraightPrefab(level, straightMesh, top, bottom, side);
        CreateCornerPrefab(level,cornerMesh, railMesh, top, bottom, side);
    }



    CreateEndCapPrefab(endCapMesh, top, bottom, capMat);
    CreateLinkPrefab(linkMesh, top, bottom, side);

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Conveyor prefabs rebuilt.");
}

    static GameObject CreateBase(string name, Mesh mesh, Material[] materials, bool boxCollider)
    {
        var root = new GameObject(name);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterials = materials;

        if (boxCollider)
        {
            var col = root.AddComponent<BoxCollider>();
            col.size   = new Vector3(ConveyorConfig.BeltWidth, ConveyorConfig.BeltHeight, ConveyorConfig.BeltLength);
            col.center = new Vector3(0f, ConveyorConfig.BeltHeight * 0.5f, 0f);
        }
        else
        {
            var col = root.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        return root;
    }

    static void CreateStraightPrefab(int level, Mesh mesh, Material top, Material bottom, Material side)
    {
        string path = $"{PathingConfig.StraightFolder}StraightConveyor_L{level}.prefab";
        var root = CreateBase($"StraightConveyor_L{level}", mesh, new[] { top, bottom, side }, true);

        AddConveyorComponent(root, level, ConveyorPieceType.Straight);
        AddEntryExitPorts(root);
        AddArrowsForLevel(root, level);
        PrefabManager.InstantiateGuardRail(root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
    
    static void CreateCornerPrefab(int level,Mesh mesh, Mesh railMesh, Material top, Material bottom, Material side)
    {
        string path = $"{PathingConfig.CornerFolder}CornerConveyor_L{level}.prefab";
        var root = CreateBase($"CornerConveyor_L{level}", mesh, new[] { top, bottom, side }, false);
        AddConveyorComponent(root, 1, ConveyorPieceType.Corner);

        float y = ConveyorConfig.BeltHeight;
        float half = 0.5f * CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge;

        var entry = new GameObject("Entry").transform;
        entry.SetParent(root.transform, false);
        entry.localPosition = new Vector3(0f, y, -half);          // -Z

        var exit = new GameObject("Exit").transform;
        exit.SetParent(root.transform, false);
        exit.localPosition = new Vector3(half, y, 0f);            // +X

        var conv = root.GetComponent<Conveyor>();
        conv.entryPoint = entry;
        conv.exitPoint  = exit;
        
        AddArrowsForLevel(root, level,true);

        var railGo = new GameObject("GuardRail");
        railGo.transform.SetParent(root.transform, false);
        railGo.transform.localPosition = new Vector3(
            0f,
            ConveyorConfig.BeltHeight + ConveyorConfig.GuardRailHeight * 0.5f,
            0f);
        railGo.AddComponent<MeshFilter>().sharedMesh = railMesh;
        railGo.AddComponent<MeshRenderer>().sharedMaterial =
            VisualsUtility.GetOrCreateMaterial("ConveyorGuardRail", ConveyorConfig.GuardRailColor);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
    
    static void CreateLinkPrefab(Mesh mesh, Material top, Material bottom, Material side)
    {
        var root = CreateBase("LinkConveyor", mesh, new[] { top, bottom, side }, true);
        AddConveyorComponent(root, 1, ConveyorPieceType.Link);

        var rail = PrefabManager.InstantiateGuardRail(root.transform);
        float zScale = ConveyorConfig.LinkLength / ConveyorConfig.BeltLength;
        rail.transform.localScale = new Vector3(1f, 1f, zScale);

        var col = root.GetComponent<BoxCollider>();
        if (col != null)
            col.size = new Vector3(ConveyorConfig.BeltWidth, ConveyorConfig.BeltHeight, ConveyorConfig.LinkLength);

        PrefabUtility.SaveAsPrefabAsset(root, LinkPrefabPath);
        Object.DestroyImmediate(root);
    }

    static void CreateEndCapPrefab(Mesh mesh, Material top, Material bottom, Material endCap)
    {
        var root = CreateBase("EndCapConveyor", mesh, new[] { top, bottom, endCap }, false);
        AddConveyorComponent(root, 1, ConveyorPieceType.EndCap);
        PrefabManager.InstantiateEndCapGuardRail(root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, EndCapPrefabPath);
        Object.DestroyImmediate(root);
    }

    static void AddConveyorComponent(GameObject root, int level, ConveyorPieceType type)
    {
        var conv = root.AddComponent<Conveyor>();
        conv.maxItems  = ConveyorConfig.MaxItemsPerBelt;
        conv.moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        conv.beltLevel = level;
        conv.pieceType = type;
        conv.direction = BeltDirection.Clockwise;
    }

    static void AddEntryExitPorts(GameObject root)
    {
        float y = ConveyorConfig.BeltHeight;
        float half = 0.5f * CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge;

        var entry = new GameObject("Entry").transform;
        entry.SetParent(root.transform, false);
        entry.localPosition = new Vector3(0f, y, -half);

        var exit = new GameObject("Exit").transform;
        exit.SetParent(root.transform, false);
        exit.localPosition = new Vector3(0f, y, half);

        var conv = root.GetComponent<Conveyor>();
        conv.entryPoint = entry;
        conv.exitPoint  = exit;
    }

    static void AddArrowsForLevel(GameObject root, int level, bool isCorner=false)
    {
        if (!isCorner)
        {

            var positions = GetPositionsStraight(level);
            float side = ConveyorConfig.BeltWidth * 0.5f;

            for (int i = 0; i < level; i++)
            {
                Vector3 pos = positions[i];
                PrefabManager.InstantiateArrow(root.transform, $"Arrow_L{i + 1}_Left",
                    new Vector3(-side, pos.y, pos.z), Quaternion.Euler(0f, 0f, 90f));
                PrefabManager.InstantiateArrow(root.transform, $"Arrow_L{i + 1}_Right",
                    new Vector3(side, pos.y, pos.z), Quaternion.Euler(0f, 0f, 90f));
            }
        }
        else
        {
            var arrowPlacements = GetPositionsCorner(level);

            for (int i = 0; i < level; i++)
            {
                var placement = arrowPlacements[i];

                PrefabManager.InstantiateArrow(
                    root.transform,
                    $"Arrow_{i + 1}",
                    placement.position,
                    Quaternion.Euler(0f, placement.angle, 90f));
            }
        }
    }

    static List<Vector3> GetPositionsStraight(int level)
    {
        level = Mathf.Clamp(level, 1, 5);
        var list = new List<Vector3>();
        float y = ConveyorConfig.BeltHeight * 0.5f;
        float spacing = ConveyorConfig.ArrowSpacing;
        float startZ = -((level - 1) * spacing) * 0.5f;
        for (int i = 0; i < level; i++)
            list.Add(new Vector3(0f, y, startZ + i * spacing));
        return list;
    }
    
    public static List<ArrowPlacement> GetPositionsCorner(int level)
    {
        level = Mathf.Clamp(level, 1, 5);

        float outerRadius = ConveyorConfig.CornerOuterRadius;
        Vector3 centreOffset = new Vector3(0.5f, ConveyorConfig.HalfBeltHeight, -0.5f);
        float angleGap = 10f;

        // How far the group of arrows is shifted so it stays centred
        float groupOffset = ((level - 1) / 2) * angleGap;

        // Starting angle of the first arrow (the group is centred around 135°)
        float startAngle = 135f - groupOffset;

        var placements = new List<ArrowPlacement>();

        for (int i = 0; i < level; i++)
        {
            float pathAngle = startAngle + (i * angleGap);          // position on the curve
            float arrowYAngle = 45f + groupOffset - (i * angleGap); // rotation of the arrow itself

            float rad = pathAngle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * outerRadius,
                0f,
                Mathf.Sin(rad) * outerRadius
            ) + centreOffset;

            placements.Add(new ArrowPlacement(arrowYAngle, pos));
        }

        return placements;
    }
    
}
#endif