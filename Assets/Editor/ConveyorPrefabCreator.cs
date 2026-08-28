#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ConveyorPrefabCreator
{
    static Vector3 BeltBoxSize =>
        new Vector3(ConveyorConfig.BeltWidth, ConveyorConfig.BeltHeight, ConveyorConfig.BeltLength);

    static Vector3 BeltBoxCentre =>
        new Vector3(0f, ConveyorConfig.BeltHeight * 0.5f, 0f);

    static float PortHalf =>
        0.5f * CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge;

    [MenuItem("Automation/Create Conveyor Prefabs")]
    public static void CreateConveyorPrefabs()
    {
        PrefabBuildUtility.BeginBuild(
            PathingConfig.LinkFolder,
            PathingConfig.StraightFolder,
            PathingConfig.CornerFolder,
            PathingConfig.EndCapFolder);

        var top    = VisualsUtility.GetOrCreateMaterial("ConveyorTop",    ConveyorConfig.TopColor);
        var bottom = VisualsUtility.GetOrCreateMaterial("ConveyorBottom", ConveyorConfig.BottomColor);
        var side   = VisualsUtility.GetOrCreateMaterial("ConveyorSide",   ConveyorConfig.SideColor);
        var capMat = VisualsUtility.GetOrCreateMaterial("ConveyorEndCap", ConveyorConfig.EndCapColor);
        VisualsUtility.GetOrCreateMaterial("ConveyorArrow",     ConveyorConfig.ArrowColor);
        var railMat = VisualsUtility.GetOrCreateMaterial("ConveyorGuardRail", ConveyorConfig.GuardRailColor);

        PrefabManager.EnsureGuardRailPrefab();
        PrefabManager.EnsureEndCapGuardRailPrefab();

        float h = ConveyorConfig.BeltHeight;

        Mesh straightMesh = PrefabBuildUtility.WriteMesh(
            PathingConfig.StraightFolder + "StraightMesh.asset",
            VisualsUtility.CreateBoxMeshSubmeshes(
                new Vector3(ConveyorConfig.BeltWidth, h, ConveyorConfig.BeltLength),
                new Vector3(0f, h * 0.5f, 0f),
                "StraightMesh"));

        Mesh cornerMesh = PrefabBuildUtility.WriteMesh(
            PathingConfig.CornerFolder + "CornerMesh.asset",
            VisualsUtility.CreateCornerMesh("CornerMesh"));

        Mesh endCapMesh = PrefabBuildUtility.WriteMesh(
            PathingConfig.EndCapFolder + "EndCapMesh.asset",
            VisualsUtility.CreateEndCapMeshEllipse("EndCapMesh"));

        Mesh linkMesh = PrefabBuildUtility.WriteMesh(
            PathingConfig.LinkFolder + "LinkMesh.asset",
            VisualsUtility.CreateBoxMeshSubmeshes(
                new Vector3(ConveyorConfig.BeltWidth, h, ConveyorConfig.LinkLength),
                new Vector3(0f, h * 0.5f, 0f),
                "LinkMesh"));

        Mesh railMesh = PrefabBuildUtility.WriteMesh(
            PathingConfig.CornerFolder + "CornerGuardRailMesh.asset",
            VisualsUtility.CreateCornerGuardRailMesh());

        var deck = new[] { top, bottom, side };

        for (int level = 1; level <= 5; level++)
        {
            CreateStraightPrefab(level, straightMesh, deck);
            CreateCornerPrefab(level, cornerMesh, railMesh, railMat, deck);
        }

        CreateEndCapPrefab(endCapMesh, top, bottom, capMat);
        CreateLinkPrefab(linkMesh, deck);

        PrefabBuildUtility.FinishBuild("Conveyor prefabs rebuilt.");
    }

    static GameObject CreateBeltRoot(string name, Mesh mesh, Material[] materials, bool boxCollider)
    {
        var root = PrefabBuildUtility.CreateRoot(name, mesh, materials);
        if (boxCollider)
            PrefabBuildUtility.AddBoxCollider(root, BeltBoxSize, BeltBoxCentre);
        else
            PrefabBuildUtility.AddMeshCollider(root, mesh);
        return root;
    }

    static void CreateStraightPrefab(int level, Mesh mesh, Material[] deck)
    {
        string path = $"{PathingConfig.StraightFolder}StraightConveyor_L{level}.prefab";
        var root = CreateBeltRoot($"StraightConveyor_L{level}", mesh, deck, true);

        AddConveyorComponent(root, level, ConveyorPieceType.Straight);
        AddPorts(root,
            new Vector3(0f, ConveyorConfig.BeltHeight, -PortHalf),
            new Vector3(0f, ConveyorConfig.BeltHeight,  PortHalf));
        AddArrowsForLevel(root, level, isCorner: false);
        PrefabManager.InstantiateGuardRail(root.transform);
        AddMidConnection(root,false);

        PrefabBuildUtility.SavePrefab(root, path);
    }

    static void CreateCornerPrefab(int level, Mesh mesh, Mesh railMesh, Material railMat, Material[] deck)
    {
        string path = $"{PathingConfig.CornerFolder}CornerConveyor_L{level}.prefab";
        var root = CreateBeltRoot($"CornerConveyor_L{level}", mesh, deck, false);

        AddConveyorComponent(root, level, ConveyorPieceType.Corner);
        AddPorts(root,
            new Vector3(0f, ConveyorConfig.BeltHeight, -PortHalf),
            new Vector3(PortHalf, ConveyorConfig.BeltHeight, 0f));
        AddArrowsForLevel(root, level, isCorner: true);
        AddMidConnection(root, true);

        var rail = PrefabBuildUtility.AddChild(
            root.transform, "GuardRail",
            new Vector3(0f, ConveyorConfig.BeltHeight + ConveyorConfig.GuardRailHeight * 0.5f, 0f));
        rail.gameObject.AddComponent<MeshFilter>().sharedMesh = railMesh;
        rail.gameObject.AddComponent<MeshRenderer>().sharedMaterial = railMat;

        PrefabBuildUtility.SavePrefab(root, path);
    }

    static void CreateLinkPrefab(Mesh mesh, Material[] deck)
    {
        var root = CreateBeltRoot("LinkConveyor", mesh, deck, true);
        AddConveyorComponent(root, 1, ConveyorPieceType.Link);

        var rail = PrefabManager.InstantiateGuardRail(root.transform);
        rail.transform.localScale = new Vector3(1f, 1f,
            ConveyorConfig.LinkLength / ConveyorConfig.BeltLength);

        var col = root.GetComponent<BoxCollider>();
        if (col != null)
            col.size = new Vector3(ConveyorConfig.BeltWidth, ConveyorConfig.BeltHeight, ConveyorConfig.LinkLength);

        PrefabBuildUtility.SavePrefab(root, PathingConfig.LinkFolder + "LinkConveyor.prefab");
    }

    static void CreateEndCapPrefab(Mesh mesh, Material top, Material bottom, Material endCap)
    {
        var root = CreateBeltRoot("EndCapConveyor", mesh, new[] { top, bottom, endCap }, false);
        AddConveyorComponent(root, 1, ConveyorPieceType.EndCap);
        PrefabManager.InstantiateEndCapGuardRail(root.transform);
        PrefabBuildUtility.SavePrefab(root, PathingConfig.EndCapFolder + "EndCapConveyor.prefab");
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

    static void AddPorts(GameObject root, Vector3 entryLocal, Vector3 exitLocal)
    {
        var entry = PrefabBuildUtility.AddChild(root.transform, "Entry", entryLocal);
        var exit  = PrefabBuildUtility.AddChild(root.transform, "Exit",  exitLocal);
        var conv  = root.GetComponent<Conveyor>();
        conv.entryPoint = entry;
        conv.exitPoint  = exit;
    }

    static void AddArrowsForLevel(GameObject root, int level, bool isCorner)
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
            return;
        }

        var placements = GetPositionsCorner(level);
        for (int i = 0; i < level; i++)
        {
            var p = placements[i];
            PrefabManager.InstantiateArrow(
                root.transform, $"Arrow_{i + 1}",
                p.position, Quaternion.Euler(0f, p.angle, 90f));
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
        float groupOffset = ((level - 1) / 2) * angleGap;
        float startAngle = 135f - groupOffset;

        var placements = new List<ArrowPlacement>();
        for (int i = 0; i < level; i++)
        {
            float pathAngle = startAngle + i * angleGap;
            float arrowYAngle = 45f + groupOffset - i * angleGap;
            float rad = pathAngle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * outerRadius,
                0f,
                Mathf.Sin(rad) * outerRadius) + centreOffset;
            placements.Add(new ArrowPlacement(arrowYAngle, pos));
        }
        return placements;
    }
    
    static void AddMidConnection(GameObject root, bool isCorner)
    {
        var go = new GameObject("Connection");
        go.transform.SetParent(root.transform, false);

        if (isCorner)
        {
            float midR = (ConveyorConfig.CornerInnerRadius + ConveyorConfig.CornerOuterRadius) * 0.5f;
            float ang  = Mathf.PI * 0.75f; // 135°
            Vector3 c  = new Vector3(0.5f, 0f, -0.5f);
            go.transform.localPosition = c + new Vector3(
                Mathf.Cos(ang) * midR,
                ConveyorConfig.BeltHeight,
                Mathf.Sin(ang) * midR);
            go.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }
        else
        {
            go.transform.localPosition = new Vector3(0f, ConveyorConfig.BeltHeight, 0f);
            go.transform.localRotation = Quaternion.identity;
        }

        var cp = go.AddComponent<ConnectionPoint>();
        cp.kind = ConnectionType.Both;
        cp.size = PackageConfig.PackageSize * 1.2f;

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * cp.size;

        var conv = root.GetComponent<Conveyor>();
        if (conv != null)
            conv.connectionPoint = cp;
    }
}
#endif