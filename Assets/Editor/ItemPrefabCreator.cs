#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ItemPrefabCreator
{
    [MenuItem("Automation/Create Item Prefabs")]
    public static void CreateItemPrefabs()
    {
        LogisticsPrefabUtility.EnsureAllFolders();

        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.ItemFolder}Package.prefab");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.ItemFolder}PackageMesh.asset");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.MaterialFolder}Package.mat");

        Material packageMat = LogisticsPrefabUtility.GetOrCreateMaterial("Package", new Color(0.45f, 0.45f, 0.48f));

        Mesh packageMesh = CreatePackageCubeMesh();
        AssetDatabase.CreateAsset(packageMesh, $"{ConveyorConfig.ItemFolder}PackageMesh.asset");

        CreatePackagePrefab(packageMesh, packageMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Item prefabs created successfully (Package).");
    }
    

    // ------------------------------------------------------------------
    // Package Prefab
    // ------------------------------------------------------------------
    private static void CreatePackagePrefab(Mesh mesh, Material mat)
    {
        string path = $"{ConveyorConfig.ItemFolder}Package.prefab";

        GameObject root = new GameObject("Package");

        // === Cube body ===
        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // Optional: very small collider if you ever need it for physics
        // var col = root.AddComponent<BoxCollider>();
        // col.size = Vector3.one * ConveyorConfig.PackageSize;

        // === Icon (SpriteRenderer on top) ===
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(root.transform, false);

        // Sit just above the top face of the cube
        float y = ConveyorConfig.PackageHalfSize + ConveyorConfig.PackageIconHeight;
        iconGO.transform.localPosition = new Vector3(0f, y, 0f);

        // Flat on the top (or rotate later if you prefer billboard)
        iconGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        iconGO.transform.localScale = Vector3.one * ConveyorConfig.PackageIconScale;

        var sr = iconGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;          // Draw above most things
        // sr.sprite will be set at runtime by Package.SetItem()

        // === Package component ===
        var package = root.AddComponent<Package>();
        package.iconRenderer = sr;

        // Save
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ------------------------------------------------------------------
    // Simple cube mesh (matches ConveyorConfig.PackageSize)
    // ------------------------------------------------------------------
    private static Mesh CreatePackageCubeMesh()
    {
        float h = ConveyorConfig.PackageHalfSize;

        Vector3[] vertices = {
            // Bottom
            new Vector3(-h, -h, -h), new Vector3( h, -h, -h),
            new Vector3( h, -h,  h), new Vector3(-h, -h,  h),
            // Top
            new Vector3(-h,  h, -h), new Vector3( h,  h, -h),
            new Vector3( h,  h,  h), new Vector3(-h,  h,  h)
        };

        int[] triangles = {
            // Bottom
            0, 2, 1, 0, 3, 2,
            // Top
            4, 5, 6, 4, 6, 7,
            // Front
            3, 6, 2, 3, 7, 6,
            // Back
            0, 1, 5, 0, 5, 4,
            // Left
            0, 4, 7, 0, 7, 3,
            // Right
            1, 2, 6, 1, 6, 5
        };

        Mesh mesh = new Mesh { name = "PackageMesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif