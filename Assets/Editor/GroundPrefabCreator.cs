#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class GroundPrefabCreator
{
    [MenuItem("Automation/Create Ground Prefab")]
    public static void CreateGroundPrefab()
    {
        LogisticsPrefabUtility.EnsureAllFolders();
        // Ensure the Ground subfolder exists
        if (!AssetDatabase.IsValidFolder(LogisticsConfig.GroundFolder.TrimEnd('/')))
            AssetDatabase.CreateFolder(LogisticsConfig.PrefabFolder.TrimEnd('/'), "Ground");

        LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.GroundFolder}Ground.prefab");
        LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.GroundFolder}GroundMesh.asset");
        LogisticsPrefabUtility.DeleteIfExists($"{LogisticsConfig.MaterialFolder}Ground.mat");

        Material mat = LogisticsPrefabUtility.GetOrCreateMaterial("Ground", LogisticsConfig.GroundColor);

        Mesh mesh = CreateGroundMesh();
        AssetDatabase.CreateAsset(mesh, $"{LogisticsConfig.GroundFolder}GroundMesh.asset");

        GameObject root = new GameObject("Ground");
        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // Simple collider so raycasts / placement can hit it later
        var col = root.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        // Optional: tag it for easy finding
        root.tag = "Ground";          // create the tag in Project Settings if needed

        string path = $"{LogisticsConfig.GroundFolder}Ground.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ground prefab created successfully.");
    }

    private static Mesh CreateGroundMesh()
    {
        float half = LogisticsConfig.GroundSize * 0.5f;
        float y    = LogisticsConfig.GroundY;

        // Single quad, centred on origin, top face at y = 0
        Vector3[] verts = {
            new Vector3(-half, y, -half),
            new Vector3( half, y, -half),
            new Vector3( half, y,  half),
            new Vector3(-half, y,  half)
        };

        int[] tris = { 0, 2, 1, 0, 3, 2 };   // upward facing

        Vector2[] uvs = {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        Mesh mesh = new Mesh { name = "GroundMesh" };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif