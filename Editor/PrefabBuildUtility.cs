#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrefabBuildUtility
{
    public static void BeginBuild(params string[] wipeFolders)
    {
        PathingUtility.EnsureAllFolders();
        foreach (string folder in wipeFolders)
            PathingUtility.DeleteFolderAndRecreate(folder);
        SaveRefresh();
    }

    public static void SaveRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static Mesh WriteMesh(string path, Mesh mesh)
    {
        AssetDatabase.CreateAsset(mesh, path);
        SaveRefresh();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    public static GameObject CreateRoot(string name, Mesh mesh, params Material[] materials)
    {
        var root = new GameObject(name);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterials = materials;
        return root;
    }

    public static void AddBoxCollider(GameObject root, Vector3 size, Vector3 centre)
    {
        var col = root.AddComponent<BoxCollider>();
        col.size = size;
        col.center = centre;
    }

    public static void AddMeshCollider(GameObject root, Mesh mesh)
    {
        root.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    public static Transform AddChild(Transform parent, string name, Vector3 localPos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localRotation = Quaternion.identity;
        return t;
    }

    public static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    public static void FinishBuild(string message)
    {
        SaveRefresh();
        Debug.Log(message);
    }
}
#endif