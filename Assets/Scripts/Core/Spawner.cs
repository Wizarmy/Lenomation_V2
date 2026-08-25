using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Spawner : MonoBehaviour
{
    public static Spawner Instance { get; private set; }

    [Header("Belt Settings")]
    [Range(1, 5)]
    public int beltLevel = 1;
    public int loopSize = 4;
    public BeltDirection loopDirection = BeltDirection.AntiClockwise;

    [Header("Test Content")]
    public bool spawnTestChest = true;
    public bool spawnTestInserters = true;
    public bool spawnTestPackages = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // -------------------------------------------------
    // Public API
    // -------------------------------------------------
    public void SpawnEverything(Vector3 loopOrigin, float loopYRotation = 0f)
    {
        SpawnGround();
        SpawnBeltLoop(loopOrigin, loopYRotation);

        if (spawnTestChest)
            SpawnChest(loopOrigin + new Vector3(-4f, 0f, 2f), 0f);

        if (spawnTestInserters)
        {
            SpawnInserter(new Vector3(-1f, 0f, 2f), 0f, preferConveyorAsSource: true);
            SpawnInserter(new Vector3(-1f, 0f, 1f), 180f, preferConveyorAsSource: false);
        }

        if (spawnTestPackages)
            SpawnTestPackages();
    }

    public void ClearEverything()
    {
        DestroyAll<Package>();
        DestroyAll<Conveyor>();
        DestroyAll<Container>();
        DestroyAll<Inserter>();

        foreach (var t in FindObjectsByType<Transform>())
        {
            if (t && t.name.Contains("EndCapConveyor"))
                DestroyImmediate(t.gameObject);
        }
    }
    
    public void SpawnGround()
    {
        if (PrefabManager.Instance == null || PrefabManager.Instance.groundPrefab == null)
            return;

        // Avoid duplicates
        if (GameObject.Find("Ground") != null)
            return;

        GameObject go = Instantiate(
            PrefabManager.Instance.groundPrefab,
            Vector3.zero,
            Quaternion.identity);
        go.name = "Ground";
    }

    // -------------------------------------------------
    // Belt Loop
    // -------------------------------------------------
    public void SpawnBeltLoop(Vector3 origin, float yRotation = 0f)
    {
        var pm = PrefabManager.Instance;
        if (pm == null) return;

        GameObject straightPrefab = pm.GetStraight(beltLevel);
        GameObject cornerPrefab   = pm.GetCorner(beltLevel);

        if (straightPrefab == null || cornerPrefab == null)
        {
            Debug.LogError($"Missing prefabs for Level {beltLevel}");
            return;
        }

        Quaternion rot = Quaternion.Euler(0f, yRotation, 0f);
        float s = loopSize;

        // Helper to transform local offset into world position
        Vector3 Pos(Vector3 local) => origin + rot * local;

        // Corners
        SpawnBelt(cornerPrefab, Pos(new Vector3(0, 0, 0)), yRotation + 270f);
        SpawnBelt(cornerPrefab, Pos(new Vector3(s, 0, 0)), yRotation + 180f);
        SpawnBelt(cornerPrefab, Pos(new Vector3(s, 0, s)), yRotation +  90f);
        SpawnBelt(cornerPrefab, Pos(new Vector3(0, 0, s)), yRotation +   0f);

        // Straight sides
        for (int i = 1; i < loopSize; i++)
        {
            SpawnBelt(straightPrefab, Pos(new Vector3(i, 0, 0)),     yRotation + 90f);   // bottom
            SpawnBelt(straightPrefab, Pos(new Vector3(s, 0, i)),     yRotation +  0f);   // right
            SpawnBelt(straightPrefab, Pos(new Vector3(s - i, 0, s)), yRotation + 270f);  // top
            SpawnBelt(straightPrefab, Pos(new Vector3(0, 0, s - i)), yRotation + 180f);  // left
        }
    }

    void SpawnBelt(GameObject prefab, Vector3 position, float yRot)
    {
        GameObject go = Instantiate(prefab, position, Quaternion.Euler(0f, yRot, 0f));
        go.name = prefab.name;

        var conv = go.GetComponent<Conveyor>();
        if (conv != null)
            conv.SetDirection(loopDirection);
    }

    public GameObject SpawnChest(Vector3 position, float yRotation = 0f, int portsX = 3, int portsZ = 3)
    {
        if (PrefabManager.Instance == null) return null;

        GameObject prefab = PrefabManager.Instance.GetChest(portsX, portsZ)
                            ?? PrefabManager.Instance.chestPrefab;
        if (prefab == null) return null;

        GameObject chest = Instantiate(prefab, position, Quaternion.Euler(0f, yRotation, 0f));
        chest.name = prefab.name;
        return chest;
    }

    public GameObject SpawnInserter(Vector3 position, float yRotation, bool preferConveyorAsSource)
    {
        if (PrefabManager.Instance == null) return null;

        GameObject prefab = PrefabManager.Instance.GetInserter(1);
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, position, Quaternion.Euler(0f, yRotation, 0f));
        go.name = preferConveyorAsSource ? "Inserter_BeltToChest" : "Inserter_ChestToBelt";

        LinkInserter(go.GetComponent<Inserter>(), position, preferConveyorAsSource);
        return go;
    }

    public void SpawnTestPackages()
    {
        if (PrefabManager.Instance == null || PrefabManager.Instance.packagePrefab == null) return;

        Conveyor target = null;
        foreach (var c in FindObjectsByType<Conveyor>())
        {
            if (!c.isCorner) { target = c; break; }
        }
        if (target == null) return;

        ItemData dummy = ScriptableObject.CreateInstance<ItemData>();
        dummy.itemName = "Test Ore";

        for (int i = 0; i < 3; i++)
        {
            GameObject pkgGO = Instantiate(PrefabManager.Instance.packagePrefab);
            Package pkg = pkgGO.GetComponent<Package>();
            target.TryAddItem(dummy, 1, pkg);
        }
    }

    // -------------------------------------------------
    // Linking helper
    // -------------------------------------------------
    void LinkInserter(Inserter inserter, Vector3 inserterPos, bool preferConveyorAsSource)
    {
        if (inserter == null) return;

        ConnectionPoint source = null;
        ConnectionPoint dest   = null;
        float bestSrcDist = InserterConfig.MaxLinkDistance;
        float bestDstDist = InserterConfig.MaxLinkDistance;

        foreach (var cp in FindObjectsByType<ConnectionPoint>())
        {
            float dist = Vector3.Distance(cp.transform.position, inserterPos);

            if (preferConveyorAsSource)
            {
                if (cp.AsConveyor != null && dist < bestSrcDist) { bestSrcDist = dist; source = cp; }
                if (cp.AsContainer != null && dist < bestDstDist) { bestDstDist = dist; dest = cp; }
            }
            else
            {
                if (cp.AsContainer != null && dist < bestSrcDist) { bestSrcDist = dist; source = cp; }
                if (cp.AsConveyor != null && dist < bestDstDist) { bestDstDist = dist; dest = cp; }
            }
        }

        if (source != null) inserter.LinkSource(source);
        if (dest   != null) inserter.LinkDestination(dest);
    }

    // -------------------------------------------------
    // Helpers
    // -------------------------------------------------
    void DestroyAll<T>() where T : Component
    {
        foreach (var obj in FindObjectsByType<T>())
            if (obj) DestroyImmediate(obj.gameObject);
        var ground = GameObject.Find("Ground");
        if (ground != null)
            DestroyImmediate(ground);
    }
}