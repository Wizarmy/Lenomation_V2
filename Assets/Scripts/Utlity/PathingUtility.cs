using UnityEditor;
using UnityEngine;

public class PathingUtility : MonoBehaviour
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
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
    
    
    private static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            CreateFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
