#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConveyorPrefabCreator : MonoBehaviour
{
    [MenuItem("Automation/Create Conveyor Prefabs")]
    public static void CreateConveyorPrefabs()
    {
        PathingUtility.EnsureAllFolders();

        for (int level = 1; level <= 5; level++)
        {
            PathingUtility.DeleteIfExists(
                $"{PathingConfig.StraightFolder}StraightConveyor_L{level}.prefab");
        }
        
        PathingUtility.DeleteIfExists($"{PathingConfig.StraightFolder}StraightMesh.asset");
        
        Material topMat    = VisualsUtility.GetOrCreateMaterial("ConveyorTop",    ConveyorConfig.TopColor);
        Material bottomMat = VisualsUtility.GetOrCreateMaterial("ConveyorBottom", ConveyorConfig.BottomColor);
        Material sideMat   = VisualsUtility.GetOrCreateMaterial("ConveyorSide",   ConveyorConfig.SideColor);
        Material endCapMat = VisualsUtility.GetOrCreateMaterial("ConveyorEndCap", ConveyorConfig.EndCapColor);
        VisualsUtility.GetOrCreateMaterial("ConveyorArrow", ConveyorConfig.ArrowColor);
        
        // Straight: full-tile footprint, slight visual inset, centred on origin
        Mesh straightMesh = VisualsUtility.CreateBoxMeshSubmeshes(
            new Vector3(
                ConveyorConfig.BeltWidth,
                ConveyorConfig.BeltHeight,
                ConveyorConfig.BeltLength),
            Vector3.zero,
            "StraightMesh");
        AssetDatabase.CreateAsset(straightMesh, $"{PathingConfig.StraightFolder}StraightMesh.asset");

        for (int level = 1; level <= 5; level++)
        {
            CreateStraightPrefab(level, straightMesh, topMat, bottomMat, sideMat);
        }

    }
    
    private static GameObject CreateBaseConveyorObject(
        string name, Mesh mesh, Material[] materials, bool useBoxCollider = false)
    {
        GameObject root = new GameObject(name);

        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterials = materials;

        if (useBoxCollider)
        {
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(
                ConveyorConfig.BeltWidth,
                ConveyorConfig.BeltHeight,
                ConveyorConfig.BeltLength);
        }
        else
        {
            var col = root.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        return root;
    }
    
    private static void CreateStraightPrefab(
        int level, Mesh mesh, Material top, Material bottom, Material side)
    {
        string path = $"{PathingConfig.StraightFolder}StraightConveyor_L{level}.prefab";

        Material[] materials = { top, bottom, side };
        GameObject root = CreateBaseConveyorObject(
            $"StraightConveyor_L{level}", mesh, materials, useBoxCollider: true);

        AddConveyorComponent(root, level, isCorner: false);
        AddArrowsForLevel(root, level, isCorner: false);
      //  AddConnectionPoint(root, root.GetComponent<Conveyor>());

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
    
    private static void AddConveyorComponent(GameObject root, int level, bool isCorner)
    {
        var conv = root.AddComponent<Conveyor>();
        conv.maxItems = ConveyorConfig.MaxItemsPerBelt;
        conv.moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        conv.beltLevel = level;
        conv.isCorner = isCorner;
        conv.direction = BeltDirection.Clockwise;
    }
    
    // ------------------------------------------------------------------
    // Arrows
    // ------------------------------------------------------------------
    private static void AddArrowsForLevel(GameObject root, int level, bool isCorner)
    {
        if (!isCorner)
        {
            var positions = GetPositionsStraight(level);
            float side = ConveyorConfig.BeltWidth * 0.5f;

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

        float y = 0;
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
