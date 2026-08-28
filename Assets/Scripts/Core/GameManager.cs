using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Belt loops")]
    public bool spawnLoopsOnStart = true;
    public Vector2Int[] loopOrigins =
    {
        new Vector2Int(-3,  3),
        new Vector2Int( 3,  3),
        new Vector2Int(-3, -3),
        new Vector2Int( 3, -3),
    };
    [Min(2)] public int loopSize = 3;

    [Header("Packages")]
    public int packagesPerLoop = 4;

    static readonly Color[] PackageColors =
    {
        new Color(0.85f, 0.25f, 0.20f),
        new Color(0.20f, 0.55f, 0.85f),
        new Color(0.25f, 0.70f, 0.30f),
        new Color(0.90f, 0.75f, 0.15f),
        new Color(0.70f, 0.30f, 0.80f),
        new Color(0.95f, 0.55f, 0.15f),
    };

    void Start()
    {
        if (PrefabManager.Instance == null)
            gameObject.AddComponent<PrefabManager>();

        if (ConveyorManager.Instance == null)
            gameObject.AddComponent<ConveyorManager>();

        SpawnGround();

        if (!spawnLoopsOnStart) return;

        int size = Mathf.Max(2, loopSize);
        for (int i = 0; i < loopOrigins.Length; i++)
        {
            bool reverse = (i & 1) == 1;
            SpawnBeltLoop(loopOrigins[i], size, size, reverse);
            SpawnLoopPackages(loopOrigins[i], size, size, packagesPerLoop);
        }
        
        SpawnInserter(new Vector2Int( 0,  3), new Vector2Int(1,  3), new Vector2Int( -1,  3));
        SpawnInserter(new Vector2Int(-3,  0), new Vector2Int(-3,  1), new Vector2Int(-3, -1));
        SpawnInserter(new Vector2Int( 0, -3), new Vector2Int(-1, -3), new Vector2Int( 1, -3));
        SpawnInserter(new Vector2Int( 3,  0), new Vector2Int( 3,  -1), new Vector2Int( 3, 1));
    }

    public void SpawnInserter(Vector2Int cell, Vector2Int pickupCell, Vector2Int dropOffCell)
    {
        var pm = PrefabManager.Instance;
        var cm = ConveyorManager.Instance;
        if (pm == null || pm.GetInserter() == null || cm == null) return;

        Vector3 world = new Vector3(cell.x + 0.5f, 0f, cell.y + 0.5f);
        var go = Instantiate(pm.GetInserter(), world, Quaternion.identity);
        go.name = $"Inserter_{cell.x}_{cell.y}";

        var placeable = go.GetComponent<Placeable>();
        if (placeable != null)
            placeable.SetGridPosition(cell);

        var ins = go.GetComponent<Inserter>();
        if (ins == null) return;

        ins.Connect(
            SocketAt(cm, pickupCell),
            SocketAt(cm, dropOffCell));
    }

    static ConnectionPoint SocketAt(ConveyorManager cm, Vector2Int cell)
    {
        Conveyor belt = cm.GetAt(cell);
        if (belt == null)
        {
            Debug.LogWarning($"No belt at {cell} to connect.");
            return null;
        }
        if (belt.connectionPoint == null)
        {
            Debug.LogWarning($"Belt at {cell} has no ConnectionPoint. Rebuild conveyor prefabs.");
            return null;
        }
        return belt.connectionPoint;
    }

    public void SpawnBeltLoop(Vector2Int origin, int sizeX, int sizeZ, bool reverse = false)
    {
        var cm = ConveyorManager.Instance;
        if (cm == null) return;

        sizeX = Mathf.Max(2, sizeX);
        sizeZ = Mathf.Max(2, sizeZ);

        int x0 = origin.x - (sizeX - 1) / 2;
        int z0 = origin.y - (sizeZ - 1) / 2;
        int x1 = x0 + sizeX - 1;
        int z1 = z0 + sizeZ - 1;

        var cornerDir   = reverse ? BeltDirection.Clockwise : BeltDirection.AntiClockwise;
        var straightDir = reverse ? BeltDirection.Clockwise : BeltDirection.AntiClockwise;

        cm.PlaceCorner(new Vector2Int(x0, z0), 270f, 1, cornerDir);
        cm.PlaceCorner(new Vector2Int(x0, z1),   0f, 1, cornerDir);
        cm.PlaceCorner(new Vector2Int(x1, z1),  90f, 1, cornerDir);
        cm.PlaceCorner(new Vector2Int(x1, z0), 180f, 1, cornerDir);

        for (int z = z0 + 1; z <= z1 - 1; z++)
        {
            cm.PlaceStraight(new Vector2Int(x0, z), 180f, 1, straightDir);
            cm.PlaceStraight(new Vector2Int(x1, z),   0f, 1, straightDir);
        }
        for (int x = x0 + 1; x <= x1 - 1; x++)
        {
            cm.PlaceStraight(new Vector2Int(x, z0),  90f, 1, straightDir);
            cm.PlaceStraight(new Vector2Int(x, z1), 270f, 1, straightDir);
        }
    }

    void SpawnLoopPackages(Vector2Int origin, int sizeX, int sizeZ, int count)
    {
        var cells = LoopCells(origin, sizeX, sizeZ);
        if (cells.Count == 0) return;

        count = Mathf.Clamp(count, 1, cells.Count);
        float step = cells.Count / (float)count;

        for (int i = 0; i < count; i++)
        {
            int idx = Mathf.Min(cells.Count - 1, Mathf.RoundToInt(i * step));
            var rider = ConveyorManager.Instance.SpawnPackage(cells[idx]);
            if (rider == null) continue;

            Color c = PackageColors[i % PackageColors.Length];
            TintPackage(rider.gameObject, c);
        }
    }

    static List<Vector2Int> LoopCells(Vector2Int origin, int sizeX, int sizeZ)
    {
        sizeX = Mathf.Max(2, sizeX);
        sizeZ = Mathf.Max(2, sizeZ);
        int x0 = origin.x - (sizeX - 1) / 2;
        int z0 = origin.y - (sizeZ - 1) / 2;
        int x1 = x0 + sizeX - 1;
        int z1 = z0 + sizeZ - 1;

        var cells = new List<Vector2Int>();
        for (int x = x0; x <= x1; x++) cells.Add(new Vector2Int(x, z0));
        for (int z = z0 + 1; z <= z1; z++) cells.Add(new Vector2Int(x1, z));
        for (int x = x1 - 1; x >= x0; x--) cells.Add(new Vector2Int(x, z1));
        for (int z = z1 - 1; z > z0; z--) cells.Add(new Vector2Int(x0, z));
        return cells;
    }

    static void TintPackage(GameObject go, Color color)
    {
        var pkg = go.GetComponent<Package>();
        if (pkg != null)
        {
            pkg.SetColor(color);
            return;
        }

        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;
    }

    public void SpawnGround()
    {
        if (PrefabManager.Instance == null || PrefabManager.Instance.groundPrefab == null)
            return;

        if (GameObject.Find("Ground") != null)
            return;

        var go = Instantiate(
            PrefabManager.Instance.groundPrefab,
            new Vector3(0f, GroundConfig.GroundY, 0f),
            Quaternion.identity);
        go.name = "Ground";
    }
}