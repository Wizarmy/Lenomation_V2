#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ItemPrefabCreator
{
    [MenuItem("Automation/Create Item Prefabs")]
    public static void CreateItemPrefabs()
    {
        PrefabBuildUtility.BeginBuild(PathingConfig.ItemFolder);

        var mat  = VisualsUtility.GetOrCreateMaterial("Package", PackageConfig.DefaultColor);
        var mats = new[] { mat, mat, mat, mat, mat, mat };

        float s = PackageConfig.PackageSize;
        Mesh mesh = PrefabBuildUtility.WriteMesh(
            PackageConfig.MeshPath,
            VisualsUtility.CreateCubeMeshSixFaces(new Vector3(s, s, s), "PackageMesh"));

        var root = PrefabBuildUtility.CreateRoot("Package", mesh, mats);
        PrefabBuildUtility.AddBoxCollider(root, Vector3.one * s, Vector3.zero);

        var pkg = root.AddComponent<Package>();
        pkg.color = PackageConfig.DefaultColor;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        PrefabBuildUtility.SavePrefab(root, PackageConfig.PrefabPath);
        PrefabBuildUtility.FinishBuild("Package prefab created: " + PackageConfig.PrefabPath);
    }
}
#endif