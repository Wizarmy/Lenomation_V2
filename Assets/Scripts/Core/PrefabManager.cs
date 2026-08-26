using UnityEditor;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance { get; private set; }

    [Header("Ground")]
    public GameObject groundPrefab;
    
    [Header("Conveyors")]
    public static GameObject[] straightPrefabs = new GameObject[5];
    
    [Header("Arrow")]
    public GameObject straightArrowPrefab;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        PathingUtility.EnsureAllFolders();

        LoadAllPrefabs();
    }

    public void LoadAllPrefabs()
    {
        groundPrefab = LoadSingle(PathingConfig.GroundFolder + "Ground.prefab",
            "Prefabs/Logistics/Ground/Ground");
        
        straightPrefabs = LoadLevelPrefabs(PathingConfig.StraightFolder, "StraightConveyor_L");

        straightArrowPrefab = EnsureArrowPrefab();
    }
    
   GameObject[] LoadLevelPrefabs(string folder, string baseName)
    {
        GameObject[] result = new GameObject[5];

        for (int i = 0; i < 5; i++)
        {
            int level = i + 1;
            string path = $"{folder}{baseName}{level}.prefab";

#if UNITY_EDITOR
            result[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            result[i] = Resources.Load<GameObject>($"Prefabs/Logistics/{baseName}{level}");
#endif

            if (result[i] == null)
                Debug.LogError($"Could not load prefab: {path}");
        }

        return result;
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
    
    public static GameObject EnsureArrowPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowConfig.ArrowPrefabPath);
        if (existing != null)
            return existing;
        

        Material arrowMat = VisualsUtility.GetOrCreateMaterial("ConveyorArrow", ArrowConfig.ArrowColor);

        Mesh arrowMesh = VisualsUtility.CreateStraightArrowMesh();
        AssetDatabase.CreateAsset(arrowMesh, ArrowConfig.ArrowMeshPath);

        GameObject root = new GameObject("StraightArrow");
        root.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = arrowMat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ArrowConfig.ArrowPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }
    
    public static GameObject InstantiateArrow(
        Transform parent, string name, Vector3 localPos, Quaternion localRot, float scale = 1f)
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
    
    public static GameObject GetStraight(int level)  => straightPrefabs[Mathf.Clamp(level - 1, 0, 4)];


}
