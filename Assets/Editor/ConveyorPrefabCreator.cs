#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConveyorPrefabCreator : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Menu
    // -------------------------------------------------------------------------
    [MenuItem("Automation/Create Conveyor Prefabs")]
    public static void CreateConveyorPrefabs()
    {
        PathingUtility.EnsureAllFolders();

        for (int level = 1; level <= 5; level++)
        {
            PathingUtility.DeleteIfExists(
                $"{PathingConfig.StraightFolder}StraightConveyor_L{level}.prefab");
            PathingUtility.DeleteIfExists(
                $"{PathingConfig.EndCapFolder}EndCapConveyor_L{level}.prefab");
        }

        PathingUtility.DeleteIfExists($"{PathingConfig.StraightFolder}StraightMesh.asset");
        PathingUtility.DeleteIfExists($"{PathingConfig.EndCapFolder}EndCapMesh.asset");

        Material topMat    = VisualsUtility.GetOrCreateMaterial("ConveyorTop",    ConveyorConfig.TopColor);
        Material bottomMat = VisualsUtility.GetOrCreateMaterial("ConveyorBottom", ConveyorConfig.BottomColor);
        Material sideMat   = VisualsUtility.GetOrCreateMaterial("ConveyorSide",   ConveyorConfig.SideColor);
        Material endCapMat = VisualsUtility.GetOrCreateMaterial("ConveyorEndCap", ConveyorConfig.EndCapColor);
        VisualsUtility.GetOrCreateMaterial("ConveyorArrow", ConveyorConfig.ArrowColor);

        float h = beltHeight;

        // Straight: bottom sits on local Y = 0
        Mesh straightMesh = VisualsUtility.CreateBoxMeshSubmeshes(
            new Vector3(beltWidth, h, beltLength),
            new Vector3(0f, h * 0.5f, 0f),
            "StraightMesh");
        AssetDatabase.CreateAsset(straightMesh, $"{PathingConfig.StraightFolder}StraightMesh.asset");

        // EndCap ellipse mesh (tile-centred, guardrail included in VisualsUtility)
        Mesh endCapMesh = VisualsUtility.CreateEndCapMeshEllipse("EndCapMesh");
        AssetDatabase.CreateAsset(endCapMesh, $"{PathingConfig.EndCapFolder}EndCapMesh.asset");

        for (int level = 1; level <= 5; level++)
        {
            CreateStraightPrefab(level, straightMesh, topMat, bottomMat, sideMat);
            CreateEndCapPrefab(level, endCapMesh, topMat, bottomMat, endCapMat);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Conveyor prefabs (straight + endcap) created.");
    }

    // -------------------------------------------------------------------------
    // Config accessors (supports Width/Height/Length or BeltWidth/…)
    // -------------------------------------------------------------------------
    static float beltWidth = ConveyorConfig.BeltWidth;

    static float beltHeight = ConveyorConfig.BeltHeight;

    static float beltLength = ConveyorConfig.BeltLength;

    // Toggle if your ConveyorConfig only has one naming style
    const bool HasBeltNamed = false;

    // -------------------------------------------------------------------------
    // Base object
    // -------------------------------------------------------------------------
    private static GameObject CreateBaseConveyorObject(
        string name, Mesh mesh, Material[] materials, bool useBoxCollider = false)
    {
        GameObject root = new GameObject(name);

        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterials = materials;

        if (useBoxCollider)
        {
            var col = root.AddComponent<BoxCollider>();
            col.size   = new Vector3(beltWidth, beltHeight, beltLength);
            col.center = new Vector3(0f, beltHeight * 0.5f, 0f);
        }
        else
        {
            var col = root.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        return root;
    }

    // -------------------------------------------------------------------------
    // Straight
    // -------------------------------------------------------------------------
    private static void CreateStraightPrefab(
        int level, Mesh mesh, Material top, Material bottom, Material side)
    {
        string path = $"{PathingConfig.StraightFolder}StraightConveyor_L{level}.prefab";

        Material[] materials = { top, bottom, side };
        GameObject root = CreateBaseConveyorObject(
            $"StraightConveyor_L{level}", mesh, materials, useBoxCollider: true);

        AddConveyorComponent(root, level, ConveyorPieceType.Straight);
        AddEntryExitPorts(root, ConveyorPieceType.Straight);
        AddArrowsForLevel(root, level, isCorner: false);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // -------------------------------------------------------------------------
    // EndCap
    // -------------------------------------------------------------------------
    private static void CreateEndCapPrefab(
        int level, Mesh mesh, Material top, Material bottom, Material endCap)
    {
        string path = $"{PathingConfig.EndCapFolder}EndCapConveyor_L{level}.prefab";

        Material[] materials = { top, bottom, endCap };
        GameObject root = CreateBaseConveyorObject(
            $"EndCapConveyor_L{level}", mesh, materials, useBoxCollider: false);

        // EndCap: no entry/exit ports
        AddConveyorComponent(root, level, ConveyorPieceType.EndCap);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // -------------------------------------------------------------------------
    // Conveyor component
    // -------------------------------------------------------------------------
    private static void AddConveyorComponent(GameObject root, int level, ConveyorPieceType type)
    {
        var conv = root.AddComponent<Conveyor>();
        conv.maxItems  = ConveyorConfig.MaxItemsPerBelt;
        conv.moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        conv.beltLevel = level;
        conv.pieceType = type;
        conv.direction = BeltDirection.Clockwise;
    }

    // -------------------------------------------------------------------------
    // Entry / Exit ports (straight & corner only)
    // -------------------------------------------------------------------------
    private static void AddEntryExitPorts(GameObject root, ConveyorPieceType type)
    {
        if (type == ConveyorPieceType.EndCap)
            return;

        float y    = beltHeight;
        float half = 0.5f * CoreConfig.TileSize;

        Vector3 entryLocal = new Vector3(0f, y, -half);
        Vector3 exitLocal  = new Vector3(0f, y,  half);

        if (type == ConveyorPieceType.Corner)
        {
            // Entry from -Z, exit toward +X (match your corner mesh when you add it)
            entryLocal = new Vector3(0f, y, -half);
            exitLocal  = new Vector3(half, y, 0f);
        }

        var entry = new GameObject("Entry").transform;
        entry.SetParent(root.transform, false);
        entry.localPosition = entryLocal;

        var exit = new GameObject("Exit").transform;
        exit.SetParent(root.transform, false);
        exit.localPosition = exitLocal;

        var conv = root.GetComponent<Conveyor>();
        conv.entryPoint = entry;
        conv.exitPoint  = exit;
    }

    // -------------------------------------------------------------------------
    // Arrows
    // -------------------------------------------------------------------------
    private static void AddArrowsForLevel(GameObject root, int level, bool isCorner)
    {
        if (!isCorner)
        {
            var positions = GetPositionsStraight(level);
            float side = beltWidth * 0.5f;

            for (int i = 0; i < level; i++)
            {
                Vector3 pos = positions[i];

                PrefabManager.InstantiateArrow(
                    root.transform,
                    $"Arrow_L{i + 1}_Left",
                    new Vector3(-side, pos.y, pos.z),
                    Quaternion.Euler(0f, 0f, 90f));

                PrefabManager.InstantiateArrow(
                    root.transform,
                    $"Arrow_L{i + 1}_Right",
                    new Vector3(side, pos.y, pos.z),
                    Quaternion.Euler(0f, 0f, 90f));
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

    public static List<Vector3> GetPositionsStraight(int level)
    {
        level = Mathf.Clamp(level, 1, 5);
        var positions = new List<Vector3>();

        // On top of the belt deck
        float y = beltHeight;
        float spacing = ConveyorConfig.ArrowSpacing;
        float totalWidth = (level - 1) * spacing;
        float startZ = -totalWidth * 0.5f;

        for (int i = 0; i < level; i++)
        {
            float z = startZ + i * spacing;
            positions.Add(new Vector3(0f, y, z));
        }

        return positions;
    }

    public static List<ArrowPlacement> GetPositionsCorner(int level)
    {
        level = Mathf.Clamp(level, 1, 5);

        float outerRadius = ConveyorConfig.CornerOuterRadius;
        Vector3 centreOffset = Vector3.zero;
        float angleGap = 10f;

        float groupOffset = ((level - 1) / 2) * angleGap;
        float startAngle = 135f - groupOffset;

        var placements = new List<ArrowPlacement>();

        for (int i = 0; i < level; i++)
        {
            float pathAngle   = startAngle + (i * angleGap);
            float arrowYAngle = 45f + groupOffset - (i * angleGap);

            float rad = pathAngle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * outerRadius,
                beltHeight,
                Mathf.Sin(rad) * outerRadius
            ) + centreOffset;

            placements.Add(new ArrowPlacement(arrowYAngle, pos));
        }

        return placements;
    }
}
#endif