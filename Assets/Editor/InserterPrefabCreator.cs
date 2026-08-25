#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class InserterPrefabCreator
{
    [MenuItem("Automation/Create Inserter Prefabs")]
    public static void CreateInserterPrefabs()
    {
        LogisticsPrefabUtility.EnsureAllFolders();
        CleanupOldAssets();

        var baseMat      = Mat("InserterBase",      InserterConfig.BaseColor);
        var mastMat      = Mat("InserterMast",      InserterConfig.MastColor);
        var armMat       = Mat("InserterArm",       InserterConfig.ArmColor);
        var telescopeMat = Mat("InserterTelescope", InserterConfig.TelescopeColor);
        var cableMat     = Mat("InserterCable",     InserterConfig.CableColor);
        var magnetMat    = Mat("InserterMagnet",    InserterConfig.MagnetColor);

        Mesh baseMesh = CreateBoxMesh(
            new Vector3(InserterConfig.BaseSize, InserterConfig.BaseHeight, InserterConfig.BaseSize),
            Vector3.zero, "InserterBaseMesh");

        Mesh mastMesh = CreateBoxMesh(
            new Vector3(InserterConfig.MastWidth, InserterConfig.MastHeight, InserterConfig.MastWidth),
            new Vector3(0f, InserterConfig.MastHeight * 0.5f, 0f), "InserterMastMesh");

        Mesh armMesh = CreateBoxMesh(
            new Vector3(InserterConfig.OuterArmThickness, InserterConfig.OuterArmThickness, InserterConfig.OuterArmLength),
            new Vector3(0f, 0f, InserterConfig.OuterArmLength * 0.5f), "InserterArmMesh");

        Mesh telescopeMesh = CreateBoxMesh(
            new Vector3(InserterConfig.InnerArmThickness, InserterConfig.InnerArmThickness, InserterConfig.InnerArmLength),
            new Vector3(0f, 0f, InserterConfig.InnerArmLength * 0.5f), "InserterTelescopeMesh");

        Mesh cableMesh = CreateBoxMesh(
            new Vector3(InserterConfig.CableThickness, InserterConfig.CableLength, InserterConfig.CableThickness),
            new Vector3(0f, -InserterConfig.CableLength * 0.5f, 0f), "InserterCableMesh");

        Mesh magnetMesh = CreateCylinderMesh(
            InserterConfig.MagnetRadius, InserterConfig.MagnetHeight, "InserterMagnetMesh");

        SaveMesh(baseMesh);
        SaveMesh(mastMesh);
        SaveMesh(armMesh);
        SaveMesh(telescopeMesh);
        SaveMesh(cableMesh);
        SaveMesh(magnetMesh);

        LogisticsPrefabUtility.EnsureArrowPrefab();

        for (int level = 1; level <= 5; level++)
        {
            CreateInserterPrefab(level,
                baseMesh, mastMesh, armMesh, telescopeMesh, cableMesh, magnetMesh,
                baseMat, mastMat, armMat, telescopeMat, cableMat, magnetMat);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Inserter prefabs L1–L5 created.");
    }

    private static void CreateInserterPrefab(
        int level,
        Mesh baseMesh, Mesh mastMesh, Mesh armMesh, Mesh telescopeMesh,
        Mesh cableMesh, Mesh magnetMesh,
        Material baseMat, Material mastMat, Material armMat, Material telescopeMat,
        Material cableMat, Material magnetMat)
    {
        string path = $"{LogisticsConfig.InserterFolder}Inserter_L{level}.prefab";
        var root = new GameObject($"Inserter_L{level}");

        var baseGO = AddMeshChild(root.transform, "Base", baseMesh, baseMat,
            new Vector3(0f, InserterConfig.BaseHeight * 0.5f, 0f));
        var col = baseGO.AddComponent<BoxCollider>();
        col.size = new Vector3(InserterConfig.BaseSize, InserterConfig.BaseHeight, InserterConfig.BaseSize);

        AddMeshChild(root.transform, "Mast", mastMesh, mastMat,
            new Vector3(0f, InserterConfig.BaseHeight, 0f));

        var slew = new GameObject("Slew");
        slew.transform.SetParent(root.transform, false);
        slew.transform.localPosition = new Vector3(0f, InserterConfig.BaseHeight + InserterConfig.MastHeight, 0f);

        var arm = AddMeshChild(slew.transform, "Arm", armMesh, armMat, Vector3.zero);

        var telescope = AddMeshChild(arm.transform, "Telescope", telescopeMesh, telescopeMat,
            new Vector3(0f, 0f, InserterConfig.TelescopeMinZ));

        var tip = new GameObject("Tip");
        tip.transform.SetParent(telescope.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0f, InserterConfig.InnerArmLength);

        AddMeshChild(tip.transform, "Cable", cableMesh, cableMat, Vector3.zero);

        var hook = new GameObject("Hook");
        hook.transform.SetParent(tip.transform, false);
        hook.transform.localPosition = Vector3.zero;

        AddMeshChild(hook.transform, "Magnet", magnetMesh, magnetMat,
            new Vector3(0f, -InserterConfig.CableLength, 0f));

        AddDirectionArrows(baseGO, level);
        root.AddComponent<Inserter>();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static GameObject AddMeshChild(
        Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static Material Mat(string name, Color color) =>
        LogisticsPrefabUtility.GetOrCreateMaterial(name, color);

    private static void SaveMesh(Mesh mesh) =>
        AssetDatabase.CreateAsset(mesh, $"{LogisticsConfig.InserterFolder}{mesh.name}.asset");

    private static void CleanupOldAssets()
    {
        string[] names = {
            "InserterBaseMesh", "InserterMastMesh", "InserterArmMesh",
            "InserterTelescopeMesh", "InserterCableMesh", "InserterMagnetMesh"
        };
        foreach (var n in names)
            LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.InserterFolder}{n}.asset");

        string[] mats = {
            "InserterBase", "InserterMast", "InserterArm",
            "InserterTelescope", "InserterCable", "InserterMagnet"
        };
        foreach (var n in mats)
            LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.MaterialFolder}{n}.mat");

        for (int level = 1; level <= 5; level++)
            LogisticsPrefabUtility.DeleteIfExists(
                $"{LogisticsConfig.InserterFolder}Inserter_L{level}.prefab");
    }

    private static void AddDirectionArrows(GameObject parent, int level)
    {
        float side = InserterConfig.BaseSize * 0.5f;
        float spacing = LogisticsConfig.ArrowSpacing;
        float start = -(level - 1) * spacing * 0.5f;

        for (int i = 0; i < level; i++)
        {
            float offset = start + i * spacing;

            LogisticsPrefabUtility.InstantiateArrow(parent.transform, $"TopSideArrow_{i + 1}",
                new Vector3(offset, 0f, side), Quaternion.Euler(0f, 270f, 90f));
            LogisticsPrefabUtility.InstantiateArrow(parent.transform, $"BottomSideArrow_{i + 1}",
                new Vector3(offset, 0f, -side), Quaternion.Euler(0f, 270f, 90f));
        }
    }

    private static Mesh CreateBoxMesh(Vector3 size, Vector3 centre, string name)
    {
        Vector3 h = size * 0.5f;

        Vector3[] verts = {
            centre + new Vector3(-h.x, -h.y, -h.z),
            centre + new Vector3( h.x, -h.y, -h.z),
            centre + new Vector3( h.x, -h.y,  h.z),
            centre + new Vector3(-h.x, -h.y,  h.z),
            centre + new Vector3(-h.x,  h.y, -h.z),
            centre + new Vector3( h.x,  h.y, -h.z),
            centre + new Vector3( h.x,  h.y,  h.z),
            centre + new Vector3(-h.x,  h.y,  h.z),
        };

        int[] tris = {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 5, 1, 0, 4, 5,
            3, 6, 7, 3, 2, 6,
            0, 7, 4, 0, 3, 7,
            1, 6, 2, 1, 5, 6,
        };

        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateCylinderMesh(float radius, float height, string name, int segments = 12)
    {
        float halfH = height * 0.5f;
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        verts.Add(new Vector3(0f,  halfH, 0f));
        verts.Add(new Vector3(0f, -halfH, 0f));

        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            verts.Add(new Vector3(x,  halfH, z));
            verts.Add(new Vector3(x, -halfH, z));
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int t0 = 2 + i * 2, b0 = t0 + 1;
            int t1 = 2 + next * 2, b1 = t1 + 1;

            tris.Add(0); tris.Add(t1); tris.Add(t0);
            tris.Add(1); tris.Add(b0); tris.Add(b1);
            tris.Add(t0); tris.Add(t1); tris.Add(b1);
            tris.Add(t0); tris.Add(b1); tris.Add(b0);
        }

        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif