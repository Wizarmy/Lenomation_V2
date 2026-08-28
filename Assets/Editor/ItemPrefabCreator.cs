#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ItemPrefabCreator
{
    [MenuItem("Automation/Create Item Prefabs")]
    public static void CreateItemPrefabs()
    {
        PathingUtility.EnsureAllFolders();
        PathingUtility.DeleteFolderAndRecreate(PathingConfig.ItemFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var top    = VisualsUtility.GetOrCreateMaterial("PackageTop",    PackageConfig.DefaultTop);
        var bottom = VisualsUtility.GetOrCreateMaterial("PackageBottom", PackageConfig.DefaultBottom);
        var front  = VisualsUtility.GetOrCreateMaterial("PackageFront",  PackageConfig.DefaultFront);
        var back   = VisualsUtility.GetOrCreateMaterial("PackageBack",   PackageConfig.DefaultBack);
        var left   = VisualsUtility.GetOrCreateMaterial("PackageLeft",   PackageConfig.DefaultLeft);
        var right  = VisualsUtility.GetOrCreateMaterial("PackageRight",  PackageConfig.DefaultRight);

        float s = PackageConfig.PackageSize;
        var mesh = VisualsUtility.CreateCubeMeshSixFaces(new Vector3(s, s, s), "PackageMesh");
        AssetDatabase.CreateAsset(mesh, PackageConfig.MeshPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(PackageConfig.MeshPath);

        var root = new GameObject("Package");
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = root.AddComponent<MeshRenderer>();
        rend.sharedMaterials = new[] { top, bottom, front, back, left, right };

        var col = root.AddComponent<BoxCollider>();
        col.size = Vector3.one * s;
        col.center = Vector3.zero;

        var pkg = root.AddComponent<Package>();
        pkg.topColor    = PackageConfig.DefaultTop;
        pkg.bottomColor = PackageConfig.DefaultBottom;
        pkg.frontColor  = PackageConfig.DefaultFront;
        pkg.backColor   = PackageConfig.DefaultBack;
        pkg.leftColor   = PackageConfig.DefaultLeft;
        pkg.rightColor  = PackageConfig.DefaultRight;

        PrefabUtility.SaveAsPrefabAsset(root, PackageConfig.PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Package prefab created: " + PackageConfig.PrefabPath);
    }
}
#endif