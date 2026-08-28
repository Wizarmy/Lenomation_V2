using UnityEditor;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance { get; private set; }

    [Header("Ground")]
    public GameObject groundPrefab;
    
    [Header("Conveyors")]
    public  GameObject[] straightPrefabs = new GameObject[5];
    public GameObject[] cornerPrefabs=new GameObject[5];
    public GameObject endCapPrefab;
    public GameObject linkPrefab;
    
    [Header("Items")]
    public GameObject packagePrefab;
    
    [Header("Arrow")]
    public GameObject straightArrowPrefab;
    
    [Header("GuardRail")]
    public GameObject guardRailPrefab;
    public GameObject endCapGuardRailPrefab;

    public static string EndCapGuardRailMeshPath =>
        PathingConfig.EndCapFolder + "EndCapGuardRailMesh.asset";
    public static string EndCapGuardRailPrefabPath =>
        PathingConfig.EndCapFolder + "EndCapGuardRail.prefab";

    public static string GuardRailMeshPath =>
        PathingConfig.StraightFolder + "GuardRailMesh.asset";
    public static string GuardRailPrefabPath =>
        PathingConfig.StraightFolder + "GuardRail.prefab";
    
    public static string EndCapPrefabPath =>
        PathingConfig.EndCapFolder + "EndCapConveyor.prefab";
    
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
        endCapPrefab = LoadSingle(
            PathingConfig.EndCapFolder + "EndCapConveyor.prefab",
            "Prefabs/Logistics/EndCapConveyor");
        
        linkPrefab = LoadSingle(
            PathingConfig.LinkFolder + "LinkConveyor.prefab",
            "Prefabs/Logistics/LinkConveyor");
        
        cornerPrefabs = LoadLevelPrefabs(PathingConfig.CornerFolder,"CornerConveyor_L");
        
        packagePrefab = LoadSingle(
            PackageConfig.PrefabPath,
            "Prefabs/Logistics/Items/Package");


        straightArrowPrefab = EnsureArrowPrefab();
        guardRailPrefab = EnsureGuardRailPrefab();
        endCapGuardRailPrefab = EnsureEndCapGuardRailPrefab();
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
    
    public static GameObject EnsureMeshPrefab(
        string prefabPath, string meshPath, string objectName, System.Func<Mesh> buildMesh)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        PathingUtility.EnsureAllFolders();
        var mat = VisualsUtility.GetOrCreateMaterial("ConveyorGuardRail", ConveyorConfig.GuardRailColor);
        var mesh = buildMesh();
        AssetDatabase.CreateAsset(mesh, meshPath);

        var root = new GameObject(objectName);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = mat;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static GameObject EnsureGuardRailPrefab() =>
        EnsureMeshPrefab(GuardRailPrefabPath, GuardRailMeshPath, "GuardRail",
            VisualsUtility.CreateStraightGuardRailMesh);

    public static GameObject EnsureEndCapGuardRailPrefab() =>
        EnsureMeshPrefab(EndCapGuardRailPrefabPath, EndCapGuardRailMeshPath, "EndCapGuardRail",
            VisualsUtility.CreateEndCapGuardRailMesh);
    
    static GameObject InstantiateRail(GameObject prefab, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "GuardRail";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(
            0f, ConveyorConfig.BeltHeight + ConveyorConfig.GuardRailHeight * 0.5f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    public static GameObject InstantiateGuardRail(Transform parent) =>
        InstantiateRail(EnsureGuardRailPrefab(), parent);

    public static GameObject InstantiateEndCapGuardRail(Transform parent) =>
        InstantiateRail(EnsureEndCapGuardRailPrefab(), parent);
    
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
    
    public GameObject GetStraight(int level) => straightPrefabs[Mathf.Clamp(level - 1, 0, 4)];

    public GameObject GetEndCap() => endCapPrefab;
    public GameObject GetLink() => linkPrefab;
    public GameObject GetCorner(int level) => cornerPrefabs[Mathf.Clamp(level - 1, 0, 4)];
    public GameObject GetPackage() => packagePrefab;

}
