#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ConveyorPrefabCreator
{
    static string StraightMeshPath => PathingConfig.StraightFolder + "StraightMesh.asset";
    static string EndCapMeshPath   => PathingConfig.EndCapFolder   + "EndCapMesh.asset";
    static string EndCapPrefabPath => PathingConfig.EndCapFolder   + "EndCapConveyor.prefab";

   [MenuItem("Automation/Create Conveyor Prefabs")]
public static void CreateConveyorPrefabs()
{
    PathingUtility.EnsureAllFolders();

    PathingUtility.DeleteFolderAndRecreate(PathingConfig.StraightFolder);
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

    string endCapMeshPath = PathingConfig.EndCapFolder + "EndCapMesh.asset";
    var endCapMesh = VisualsUtility.CreateEndCapMeshEllipse("EndCapMesh");
    AssetDatabase.CreateAsset(endCapMesh, endCapMeshPath);

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    straightMesh = AssetDatabase.LoadAssetAtPath<Mesh>(straightMeshPath);
    endCapMesh   = AssetDatabase.LoadAssetAtPath<Mesh>(endCapMeshPath);

    // 5. Prefabs using the reloaded meshes
    for (int level = 1; level <= 5; level++)
        CreateStraightPrefab(level, straightMesh, top, bottom, side);

    CreateEndCapPrefab(endCapMesh, top, bottom, capMat);

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

    static void AddArrowsForLevel(GameObject root, int level)
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

    static System.Collections.Generic.List<Vector3> GetPositionsStraight(int level)
    {
        level = Mathf.Clamp(level, 1, 5);
        var list = new System.Collections.Generic.List<Vector3>();
        float y = ConveyorConfig.BeltHeight * 0.5f;
        float spacing = ConveyorConfig.ArrowSpacing;
        float startZ = -((level - 1) * spacing) * 0.5f;
        for (int i = 0; i < level; i++)
            list.Add(new Vector3(0f, y, startZ + i * spacing));
        return list;
    }
}
#endif