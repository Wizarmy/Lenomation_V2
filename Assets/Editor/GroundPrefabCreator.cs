#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class GroundPrefabCreator
{
    [MenuItem("Automation/Create Ground Prefab")]
    public static void CreateGroundPrefab()
    {
        PathingUtility.EnsureAllFolders();
        // Ensure the Ground subfolder exists
        if (!AssetDatabase.IsValidFolder(PathingConfig.GroundFolder.TrimEnd('/')))
            AssetDatabase.CreateFolder(PathingConfig.PrefabFolder.TrimEnd('/'), "Ground");

        PathingUtility.DeleteIfExists($"{PathingConfig.GroundFolder}Ground.prefab");
        PathingUtility.DeleteIfExists($"{PathingConfig.GroundFolder}GroundMesh.asset");
        PathingUtility.DeleteIfExists($"{PathingConfig.MaterialFolder}Ground.mat");

        Material mat = VisualsUtility.GetOrCreateMaterial("Ground", GroundConfig.GroundColor);

        Mesh mesh = VisualsUtility.CreateQuadMesh(
            GroundConfig.GroundSize,
            GroundConfig.GroundSize,
            GroundConfig.GroundY,
            "GroundMesh");
        AssetDatabase.CreateAsset(mesh, $"{PathingConfig.GroundFolder}GroundMesh.asset");

        GameObject root = new GameObject("Ground");
        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // Simple collider so raycasts / placement can hit it later
        var col = root.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
        
        string path = $"{PathingConfig.GroundFolder}Ground.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ground prefab created successfully.");
    }
    
}
#endif