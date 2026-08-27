#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PathingUtility
{
    public static void EnsureAllFolders()
    {
        CreateFolder("Assets/Prefabs");
        CreateFolder("Assets/Prefabs/Logistics");
        CreateFolder(PathingConfig.ArrowFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.ConveyorFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.StraightFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.CornerFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.EndCapFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.ContainerFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.InserterFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.ItemFolder.TrimEnd('/'));
        CreateFolder(PathingConfig.GroundFolder.TrimEnd('/'));
        CreateFolder("Assets/Materials");
    }

    public static void DeleteIfExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    /// <summary>Deletes every asset in a folder (not the folder itself).</summary>
    public static void DeleteAssetsInFolder(string folder)
    {
        folder = folder.TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(folder)) return;

        string[] guids = AssetDatabase.FindAssets("", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                continue;
            AssetDatabase.DeleteAsset(path);
        }
    }
    
    public static void DeleteFolderAndRecreate(string folder)
    {
        folder = folder.TrimEnd('/');
        if (AssetDatabase.IsValidFolder(folder))
            AssetDatabase.DeleteAsset(folder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EnsureAllFolders(); // recreates Straight/ and EndCaps/
    }

    static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string name   = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            CreateFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif