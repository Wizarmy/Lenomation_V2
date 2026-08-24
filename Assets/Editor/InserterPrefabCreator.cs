#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class InserterPrefabCreator
{
    private const float BaseSize = 0.7f;
    private const float BaseHeight = 0.15f;
    private const float ArmLength = 1f;
    private const float ArmThickness = 0.08f;

    [MenuItem("Automation/Create Inserter Prefabs")]
    public static void CreateInserterPrefabs()
    {
        LogisticsPrefabUtility.EnsureAllFolders();

        for (int level = 1; level <= 5; level++)
        {
            LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.InserterFolder}Inserter_L{level}.prefab");
        }
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.InserterFolder}InserterBaseMesh.asset");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.InserterFolder}InserterArmMesh.asset");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.MaterialFolder}InserterBase.mat");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.MaterialFolder}InserterArm.mat");

        Material baseMat = LogisticsPrefabUtility.GetOrCreateMaterial("InserterBase", new Color(0.25f, 0.25f, 0.28f));
        Material armMat  = LogisticsPrefabUtility.GetOrCreateMaterial("InserterArm",  new Color(0.6f, 0.55f, 0.3f));

        Mesh baseMesh = CreateBaseMesh();
        AssetDatabase.CreateAsset(baseMesh, $"{ConveyorConfig.InserterFolder}InserterBaseMesh.asset");

        Mesh armMesh = CreateArmMesh();
        AssetDatabase.CreateAsset(armMesh, $"{ConveyorConfig.InserterFolder}InserterArmMesh.asset");

        LogisticsPrefabUtility.EnsureArrowPrefab();

        for (int level = 1; level <= 5; level++)
        {
            CreateInserterPrefab(level, baseMesh, armMesh, baseMat, armMat);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Inserter prefabs L1–L5 created successfully.");
    }
    
    // ------------------------------------------------------------------
    // Prefab (now per level)
    // ------------------------------------------------------------------
    private static void CreateInserterPrefab(int level, Mesh baseMesh, Mesh armMesh, Material baseMat, Material armMat)
    {
        string path = $"{ConveyorConfig.InserterFolder}Inserter_L{level}.prefab";

        GameObject root = new GameObject($"Inserter_L{level}");

        // === Base ===
        var baseGO = new GameObject("Base");
        baseGO.transform.SetParent(root.transform, false);

        var mf = baseGO.AddComponent<MeshFilter>();
        mf.sharedMesh = baseMesh;

        var mr = baseGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = baseMat;

        var col = baseGO.AddComponent<BoxCollider>();
        col.size = new Vector3(BaseSize, BaseHeight, BaseSize);

        // === Arm ===
        var armGO = new GameObject("Arm");
        armGO.transform.SetParent(root.transform, false);
        armGO.transform.localPosition = new Vector3(0f, BaseHeight * 0.5f + 0.05f, 0f);

        var armMF = armGO.AddComponent<MeshFilter>();
        armMF.sharedMesh = armMesh;

        var armMR = armGO.AddComponent<MeshRenderer>();
        armMR.sharedMaterial = armMat;

        // === Direction Arrows (level amount on each side) ===
        AddDirectionArrows(root, level);

        // === Component ===
        var inserter = root.AddComponent<Inserter>();
        // You can later expose level if needed: inserter.level = level;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ------------------------------------------------------------------
    // Arrows – matches your current placement style + level support
    // ------------------------------------------------------------------
    private static void AddDirectionArrows(GameObject root, int level)
    {
        float side = BaseSize / 2f;

        // Re-use the same spacing logic as conveyors
        float spacing = ConveyorConfig.ArrowSpacing;
        float totalWidth = (level - 1) * spacing;
        float start = -totalWidth * 0.5f;

        for (int i = 0; i < level; i++)
        {
            float offset = start + i * spacing;

            // Pick-up side (+Z) – your current rotation
            LogisticsPrefabUtility.InstantiateArrow(root.transform, $"TopSideArrow_{i + 1}",
                new Vector3(offset, 0, side),
                Quaternion.Euler(0f, 270f, 90f));

            // Drop-off side (-Z) – your current rotation
            LogisticsPrefabUtility.InstantiateArrow(root.transform, $"BottomSideArrow_{i + 1}",
                new Vector3(offset, 0, -side),
                Quaternion.Euler(0f, 270f, 90f));
        }
    }

    // ------------------------------------------------------------------
    // Meshes (unchanged from your version)
    // ------------------------------------------------------------------
    private static Mesh CreateBaseMesh()
    {
        float h = BaseSize * 0.5f;
        float y = BaseHeight * 0.5f;

        Vector3[] verts = {
            new Vector3(-h, -y, -h), new Vector3( h, -y, -h),
            new Vector3( h, -y,  h), new Vector3(-h, -y,  h),
            new Vector3(-h,  y, -h), new Vector3( h,  y, -h),
            new Vector3( h,  y,  h), new Vector3(-h,  y,  h)
        };

        int[] tris = {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 5, 1, 0, 4, 5,
            3, 2, 6, 3, 6, 7,
            0, 3, 7, 0, 7, 4,
            1, 5, 6, 1, 6, 2
        };

        Mesh mesh = new Mesh { name = "InserterBaseMesh" };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateArmMesh()
    {
        float len = ArmLength;
        float t = ArmThickness * 0.5f;

        Vector3[] verts = {
            new Vector3(-t, -t, 0), new Vector3( t, -t, 0),
            new Vector3( t,  t, 0), new Vector3(-t,  t, 0),
            new Vector3(-t, -t, len), new Vector3( t, -t, len),
            new Vector3( t,  t, len), new Vector3(-t,  t, len)
        };

        int[] tris = {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            3, 7, 6, 3, 6, 2,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5
        };

        Mesh mesh = new Mesh { name = "InserterArmMesh" };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif