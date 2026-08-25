#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConveyorPrefabCreator
{
   [MenuItem("Automation/Create Conveyor Prefabs")]
public static void CreateConveyorPrefabs()
{
    LogisticsPrefabUtility.EnsureAllFolders();

    // ========== DELETE OLD FILES ==========
    for (int level = 1; level <= 5; level++)
    {
        LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.StraightFolder}StraightConveyor_L{level}.prefab");
        LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.CornerFolder}CornerConveyor_L{level}.prefab");
        LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.EndCapFolder}EndCapConveyor_L{level}.prefab");
    }

    LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.StraightFolder}StraightMesh.asset");
    LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.CornerFolder}CornerMesh.asset");
    LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.EndCapFolder}EndCapMesh.asset");
    // (Arrow is handled by LogisticsPrefabUtility)

    // Materials...
    Material topMat    = LogisticsPrefabUtility.GetOrCreateMaterial("ConveyorTop",    LogisticsConfig.TopColor);
    Material bottomMat = LogisticsPrefabUtility.GetOrCreateMaterial("ConveyorBottom", LogisticsConfig.BottomColor);
    Material sideMat   = LogisticsPrefabUtility.GetOrCreateMaterial("ConveyorSide",   LogisticsConfig.SideColor);
    Material endCapMat = LogisticsPrefabUtility.GetOrCreateMaterial("ConveyorEndCap", LogisticsConfig.EndCapColor);
    Material arrowMat  = LogisticsPrefabUtility.GetOrCreateMaterial("ConveyorArrow",  LogisticsConfig.ArrowColor);

    // Shared meshes
    Mesh straightMesh = CreateStraightMesh();
    AssetDatabase.CreateAsset(straightMesh, $"{LogisticsConfig.StraightFolder}StraightMesh.asset");

    Mesh cornerMesh = CreateCurvedCornerMesh();
    AssetDatabase.CreateAsset(cornerMesh, $"{LogisticsConfig.CornerFolder}CornerMesh.asset");

    Mesh endCapMesh = CreateEndCapMesh();
    AssetDatabase.CreateAsset(endCapMesh, $"{LogisticsConfig.EndCapFolder}EndCapMesh.asset");

    // Ensure shared arrow
    GameObject arrowPrefab = LogisticsPrefabUtility.EnsureArrowPrefab();

    // Create all 5 levels
    for (int level = 1; level <= 5; level++)
    {
        CreateStraightPrefab(level, straightMesh, topMat, bottomMat, sideMat, arrowPrefab);
        CreateCornerPrefab(level, cornerMesh, topMat, bottomMat, sideMat, arrowPrefab);
        CreateEndCapPrefab(level, endCapMesh, endCapMat, topMat, bottomMat);
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Conveyor prefabs (L1–L5) created successfully.");
}

    private static GameObject CreateBaseConveyorObject(string name, Mesh mesh, Material[] materials, bool useBoxCollider = false)
    {
        GameObject root = new GameObject(name);

        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterials = materials;

        if (useBoxCollider)
        {
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(LogisticsConfig.BeltWidth, LogisticsConfig.BeltHeight, LogisticsConfig.BeltLength);
        }
        else
        {
            var col = root.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        return root;
    }

    private static void AddConveyorComponent(GameObject root, int level, int maxItems, bool isCorner)
    {
        var conv = root.AddComponent<Conveyor>();
    
        // Defaults – direction will be set properly when the belt is laid/spawned
        conv.maxItems = maxItems;
        conv.moveSpeed = LogisticsConfig.DefaultMoveSpeed;
        conv.beltLevel = level;
        conv.isCorner = isCorner;
    
        // Start with Clockwise (the natural direction of our current corner mesh)
        // The real direction is applied later via SetDirection() when the belt is placed
        conv.direction = BeltDirection.Clockwise;
    }
    
    private static void AddConnectionPoint(GameObject root, Conveyor conveyor)
    {
        GameObject cpGO = new GameObject("ConnectionPoint");
        cpGO.transform.SetParent(root.transform, false);
        cpGO.transform.localPosition = Vector3.zero;          // centre of the belt
        cpGO.transform.localRotation = Quaternion.identity;

        var cp = cpGO.AddComponent<ConnectionPoint>();
        cp.type = ConnectionType.Both;
        cp.owner = conveyor;
        cp.radius = 0.45f;                                    // matches package + a bit
    }
    
    // ------------------------------------------------------------------
    // Prefab Creation
    // ------------------------------------------------------------------
    private static void CreateStraightPrefab(int level, Mesh mesh, Material top, Material bottom, Material side,
                                             GameObject arrowPrefab)
    {
        string path = $"{LogisticsConfig.StraightFolder}StraightConveyor_L{level}.prefab";


        Material[] materials = { top, bottom, side };
        GameObject root = CreateBaseConveyorObject($"StraightConveyor_L{level}", mesh, materials, useBoxCollider: true);

        AddConveyorComponent(root, level, LogisticsConfig.MaxItemsPerBelt, false);
        AddArrowsForLevel(root, level, false, arrowPrefab);
        
        // Straight
        var conv = root.GetComponent<Conveyor>();
        AddConnectionPoint(root, conv);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateCornerPrefab(int level, Mesh mesh, Material top, Material bottom, Material side,
                                           GameObject arrowPrefab)
    {
        string path = $"{LogisticsConfig.CornerFolder}CornerConveyor_L{level}.prefab";

        Material[] materials = { top, bottom, side, side };
        GameObject root = CreateBaseConveyorObject($"CornerConveyor_L{level}", mesh, materials);

        AddConveyorComponent(root, level, LogisticsConfig.MaxItemsPerBelt, true);
        AddArrowsForLevel(root, level, true, arrowPrefab);
        
        // Corner
        var conv = root.GetComponent<Conveyor>();
        AddConnectionPoint(root, conv);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateEndCapPrefab(int level, Mesh mesh, Material endCap, Material top, Material bottom)
    {
        string path = $"{LogisticsConfig.EndCapFolder}EndCapConveyor_L{level}.prefab";

        Material[] materials = { endCap, top, bottom };
        GameObject root = CreateBaseConveyorObject($"EndCapConveyor_L{level}", mesh, materials);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ------------------------------------------------------------------
// Arrows
// ------------------------------------------------------------------
    private static void AddArrowsForLevel(GameObject root, int level, bool isCorner,
        GameObject arrowPrefab)
    {
        // arrowPrefab is no longer needed – LogisticsPrefabUtility always
        // uses the shared StraightArrow prefab via EnsureArrowPrefab().

        if (!isCorner)
        {
            var positions = ConveyorArrowHelper.GetPositionsStraight(level);
            float side = LogisticsConfig.HalfBeltWidth + LogisticsConfig.SideOffset;

            for (int i = 0; i < level; i++)
            {
                Vector3 pos = positions[i];

                // Left side
                LogisticsPrefabUtility.InstantiateArrow(
                    root.transform,
                    $"Arrow_L{i + 1}_Left",
                    new Vector3(-side, pos.y, pos.z),
                    Quaternion.Euler(0f, 0f, 90f));

                // Right side
                LogisticsPrefabUtility.InstantiateArrow(
                    root.transform,
                    $"Arrow_L{i + 1}_Right",
                    new Vector3(side, pos.y, pos.z),
                    Quaternion.Euler(0f, 0f, 90f));
            }
        }
        else
        {
            var arrowPlacements = ConveyorArrowHelper.GetPositionsCorner(level);

            for (int i = 0; i < level; i++)
            {
                var placement = arrowPlacements[i];

                LogisticsPrefabUtility.InstantiateArrow(
                    root.transform,
                    $"Arrow_{i + 1}",
                    placement.position,
                    Quaternion.Euler(0f, placement.angle, 90f));
            }
        }
    }
    
    // ------------------------------------------------------------------
    // Meshes
    // ------------------------------------------------------------------
    private static Mesh CreateStraightMesh()
    {
        float hw = LogisticsConfig.HalfBeltWidth;
        float hh = LogisticsConfig.HalfBeltHeight;
        float hl = LogisticsConfig.HalfBeltLength;

        Vector3[] vertices = {
            new Vector3(-hw, -hh, -hl), new Vector3( hw, -hh, -hl),
            new Vector3( hw, -hh,  hl), new Vector3(-hw, -hh,  hl),
            new Vector3(-hw,  hh, -hl), new Vector3( hw,  hh, -hl),
            new Vector3( hw,  hh,  hl), new Vector3(-hw,  hh,  hl)
        };

        int[] top    = { 4, 6, 5, 4, 7, 6 };
        int[] bottom = { 0, 1, 2, 0, 2, 3 };
        int[] sides  = { 0, 3, 7, 0, 7, 4, 1, 6, 2, 1, 5, 6 };

        Mesh mesh = new Mesh { name = "StraightMesh" };
        mesh.vertices = vertices;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(top, 0);
        mesh.SetTriangles(bottom, 1);
        mesh.SetTriangles(sides, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateCurvedCornerMesh()
    {
        float hh = LogisticsConfig.HalfBeltHeight;
        int segments = LogisticsConfig.CurveSegments;
        float innerRadius = LogisticsConfig.CornerInnerRadius;
        float outerRadius = LogisticsConfig.CornerOuterRadius;
        Vector3 centreAdjust = LogisticsConfig.CornerCentreOffset;

        List<Vector3> vertices = new List<Vector3>();
        List<int> topTris = new List<int>();
        List<int> bottomTris = new List<int>();
        List<int> shortSideTris = new List<int>();
        List<int> longSideTris = new List<int>();

        float angleDegrees = 90f;
        float segmentAngleDegrees = 90f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            float sinAngle = Mathf.Sin(angle);
            float cosAngle = Mathf.Cos(angle);

            vertices.Add(new Vector3(cosAngle * innerRadius,  hh, sinAngle * innerRadius) + centreAdjust);
            vertices.Add(new Vector3(cosAngle * outerRadius,  hh, sinAngle * outerRadius) + centreAdjust);
            vertices.Add(new Vector3(cosAngle * innerRadius, -hh, sinAngle * innerRadius) + centreAdjust);
            vertices.Add(new Vector3(cosAngle * outerRadius, -hh, sinAngle * outerRadius) + centreAdjust);

            angleDegrees += segmentAngleDegrees;
        }

        for (int i = 0; i < segments; i++)
        {
            int curr = i * 4;
            int next = (i + 1) * 4;

            topTris.Add(curr); topTris.Add(next + 1); topTris.Add(curr + 1);
            topTris.Add(curr); topTris.Add(next); topTris.Add(next + 1);

            bottomTris.Add(curr + 2); bottomTris.Add(curr + 3); bottomTris.Add(next + 3);
            bottomTris.Add(curr + 2); bottomTris.Add(next + 3); bottomTris.Add(next + 2);

            shortSideTris.Add(curr + 0); shortSideTris.Add(curr + 2); shortSideTris.Add(curr + 4);
            shortSideTris.Add(curr + 2); shortSideTris.Add(curr + 6); shortSideTris.Add(curr + 4);

            longSideTris.Add(curr + 1); longSideTris.Add(curr + 5); longSideTris.Add(curr + 3);
            longSideTris.Add(curr + 3); longSideTris.Add(curr + 5); longSideTris.Add(curr + 7);
        }

        Mesh mesh = new Mesh { name = "CurvedCornerMesh" };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 4;
        mesh.SetTriangles(topTris, 0);
        mesh.SetTriangles(bottomTris, 1);
        mesh.SetTriangles(shortSideTris, 2);
        mesh.SetTriangles(longSideTris, 3);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateEndCapMesh()
    {
        float hh = LogisticsConfig.HalfBeltHeight;
        int segments = LogisticsConfig.CurveSegments;
        float radius = LogisticsConfig.EndCapRadius;

        List<Vector3> vertices = new List<Vector3>();
        List<int> endCapSides = new List<int>();
        List<int> topTris = new List<int>();
        List<int> bottomTris = new List<int>();

        vertices.Add(new Vector3(0, hh, 0));
        vertices.Add(new Vector3(0, -hh, 0));

        float angleDegrees = 90f;
        float segmentAngleDegrees = 180f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            float sinAngle = Mathf.Sin(angle);
            float cosAngle = Mathf.Cos(angle);

            vertices.Add(new Vector3(cosAngle * radius, hh, sinAngle * radius));
            vertices.Add(new Vector3(cosAngle * radius, -hh, sinAngle * radius));

            angleDegrees -= segmentAngleDegrees;
        }

        for (int i = 0; i < segments; i++)
        {
            int curr = i * 2;

            topTris.Add(0); topTris.Add(curr + 2); topTris.Add(curr + 4);
            bottomTris.Add(1); bottomTris.Add(curr + 5); bottomTris.Add(curr + 3);

            endCapSides.Add(curr + 2); endCapSides.Add(curr + 3); endCapSides.Add(curr + 4);
            endCapSides.Add(curr + 4); endCapSides.Add(curr + 3); endCapSides.Add(curr + 5);
        }

        Mesh mesh = new Mesh { name = "EndCapMesh" };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 3;
        mesh.SetTriangles(endCapSides, 0);
        mesh.SetTriangles(topTris, 1);
        mesh.SetTriangles(bottomTris, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateStraightArrowMesh()
    {
        float size  = LogisticsConfig.ArrowSize;
        float depth = LogisticsConfig.ArrowDepth;
        float halfD = depth * 0.5f;

        Vector3[] verts = {
            new Vector3(0,  halfD,  size * 0.9f),
            new Vector3(-size * 0.55f,  halfD, -size * 0.7f),
            new Vector3( size * 0.55f,  halfD, -size * 0.7f),
            new Vector3(0, -halfD,  size * 0.9f),
            new Vector3(-size * 0.55f, -halfD, -size * 0.7f),
            new Vector3( size * 0.55f, -halfD, -size * 0.7f)
        };

        int[] tris = {
            0, 2, 1,
            3, 4, 5,
            0, 1, 4, 0, 4, 3,
            1, 2, 5, 1, 5, 4,
            2, 0, 3, 2, 3, 5
        };

        Mesh mesh = new Mesh { name = "StraightArrowMesh" };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    
}
#endif