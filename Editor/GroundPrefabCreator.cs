#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class GroundPrefabCreator
{
    [MenuItem("Automation/Create Ground Prefab")]
    public static void CreateGroundPrefab()
    {
        PathingUtility.EnsureAllFolders();
        PathingUtility.DeleteIfExists(PathingConfig.GroundFolder + "Ground.prefab");
        PathingUtility.DeleteIfExists(PathingConfig.GroundFolder + "GroundMesh.asset");
        PathingUtility.DeleteIfExists(PathingConfig.MaterialFolder + "Ground.mat");

        var mat = VisualsUtility.GetOrCreateMaterial("Ground", GroundConfig.GroundColor);
        Mesh mesh = PrefabBuildUtility.WriteMesh(
            PathingConfig.GroundFolder + "GroundMesh.asset",
            VisualsUtility.CreateQuadMesh(
                GroundConfig.GroundSize, GroundConfig.GroundSize,
                GroundConfig.GroundY, "GroundMesh"));

        var root = PrefabBuildUtility.CreateRoot("Ground", mesh, mat);
        PrefabBuildUtility.AddMeshCollider(root, mesh);
        PrefabBuildUtility.SavePrefab(root, PathingConfig.GroundFolder + "Ground.prefab");
        PrefabBuildUtility.FinishBuild("Ground prefab created successfully.");
    }
}
#endif