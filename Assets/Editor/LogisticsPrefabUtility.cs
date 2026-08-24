#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class LogisticsPrefabUtility
{
    public const string ArrowPrefabPath = ConveyorConfig.ArrowFolder + "StraightArrow.prefab";
    public const string ArrowMeshPath   = ConveyorConfig.ArrowFolder + "StraightArrowMesh.asset";
    public const string ArrowMatPath    = ConveyorConfig.MaterialFolder + "ConveyorArrow.mat";

    public static GameObject EnsureArrowPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
        if (existing != null)
            return existing;

        EnsureAllFolders();

        Material arrowMat = GetOrCreateMaterial("ConveyorArrow", ConveyorConfig.ArrowColor);

        Mesh arrowMesh = CreateStraightArrowMesh();
        AssetDatabase.CreateAsset(arrowMesh, ArrowMeshPath);

        GameObject root = new GameObject("StraightArrow");
        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = arrowMesh;
        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = arrowMat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ArrowPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return prefab;
    }

    public static GameObject InstantiateArrow(Transform parent, string name, Vector3 localPos, Quaternion localRot, float scale = 1f)
    {
        GameObject prefab = EnsureArrowPrefab();
        GameObject arrow = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        arrow.name = name;
        arrow.transform.SetParent(parent, false);
        arrow.transform.localPosition = localPos;
        arrow.transform.localRotation = localRot;
        arrow.transform.localScale = Vector3.one * scale;
        return arrow;
    }

    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------
    public static void EnsureAllFolders()
    {
        CreateFolder("Assets/Prefabs");
        CreateFolder("Assets/Prefabs/Logistics");
        CreateFolder(ConveyorConfig.ArrowFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.ConveyorFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.StraightFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.CornerFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.EndCapFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.ContainerFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.InserterFolder.TrimEnd('/'));
        CreateFolder(ConveyorConfig.ItemFolder.TrimEnd('/'));
        CreateFolder("Assets/Materials");
    }
    
    public static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
    
    private static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            CreateFolder(parent);               // recursive safety

        AssetDatabase.CreateFolder(parent, folderName);
    }

    public static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"{ConveyorConfig.MaterialFolder}{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static Mesh CreateStraightArrowMesh()
    {
        float size  = ConveyorConfig.ArrowSize;
        float depth = ConveyorConfig.ArrowDepth;
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