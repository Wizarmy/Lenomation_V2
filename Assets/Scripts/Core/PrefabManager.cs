using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance { get; private set; }

    [Header("Conveyors")]
    public GameObject[] straightPrefabs = new GameObject[5];
    public GameObject[] cornerPrefabs   = new GameObject[5];
    public GameObject[] endCapPrefabs   = new GameObject[5];

    [Header("Items")]
    public GameObject packagePrefab;

    [Header("Containers")]
    public GameObject chestPrefab;

    [Header("Inserters")]
    public GameObject[] inserterPrefabs = new GameObject[5];   // L1–L5

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
        straightPrefabs = LoadLevelPrefabs(ConveyorConfig.StraightFolder, "StraightConveyor_L");
        cornerPrefabs   = LoadLevelPrefabs(ConveyorConfig.CornerFolder,   "CornerConveyor_L");
        endCapPrefabs   = LoadLevelPrefabs(ConveyorConfig.EndCapFolder,   "EndCapConveyor_L");
        inserterPrefabs = LoadLevelPrefabs(ConveyorConfig.InserterFolder, "Inserter_L");

        packagePrefab = LoadSingle(ConveyorConfig.ItemFolder + "Package.prefab",
                                   "Prefabs/Logistics/Items/Package");

        chestPrefab = LoadSingle(ConveyorConfig.ContainerFolder + "Chest.prefab",
                                 "Prefabs/Logistics/Containers/Chest");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
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

    // Convenience getters
    public GameObject GetStraight(int level)  => straightPrefabs[Mathf.Clamp(level - 1, 0, 4)];
    public GameObject GetCorner(int level)    => cornerPrefabs[Mathf.Clamp(level - 1, 0, 4)];
    public GameObject GetEndCap(int level)    => endCapPrefabs[Mathf.Clamp(level - 1, 0, 4)];
    public GameObject GetInserter(int level)  => inserterPrefabs[Mathf.Clamp(level - 1, 0, 4)];
}