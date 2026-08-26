using UnityEditor;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance { get; private set; }

    [Header("Ground")]
    public GameObject groundPrefab;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadAllPrefabs();
    }

    public void LoadAllPrefabs()
    {
        groundPrefab = LoadSingle(PathingConfig.GroundFolder + "Ground.prefab",
            "Prefabs/Logistics/Ground/Ground");
    }
    
    GameObject LoadSingle(string editorPath, string resourcesPath)
    {
#if UNITY_EDITOR
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorPath);
#else
        var prefab = Resources.Load<GameObject>(resourcesPath);
#endif
        if (prefab == null)
            Debug.LogError($"Could not load prefab: {editorPath}");
        return prefab;
    }


}
