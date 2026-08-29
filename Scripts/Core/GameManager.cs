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
    [Min(2)] public int loopSize = 5;

    [Header("Packages")]
    public int packagesPerLoop = 4;

    struct InserterSpec
    {
        public Vector2Int cell;
        public Vector2Int pickup;
        public Vector2Int drop;

        public InserterSpec(int cx, int cz, int px, int pz, int dx, int dz)
        {
            cell   = new Vector2Int(cx, cz);
            pickup = new Vector2Int(px, pz);
            drop   = new Vector2Int(dx, dz);
        }
    }

    static readonly InserterSpec[] Inserters =
    {
        new InserterSpec( 0,  3, 1,  3,  -1,  3),
        new InserterSpec(-3,  0, -3,  1, -3, -1),
        new InserterSpec( 0, -3, -1, -3,  1, -3),
        //new InserterSpec( 3,  0,  3,  -1,  3, 1),
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

        for (int i = 0; i < Inserters.Length; i++)
        {
            var spec = Inserters[i];
            SpawnInserter(spec.cell, spec.pickup, spec.drop);
        }

        SpawnChest(new Vector2Int(7,  3), 1, 1);
        SpawnChest(new Vector2Int(7, -3), 1, 1);

        SpawnInserter(new Vector2Int(6,  3), new Vector2Int(7,  3), new Vector2Int(5,  3));
        SpawnInserter(new Vector2Int(6, -3), new Vector2Int(5, -3), new Vector2Int(7, -3));

        var cm = ConveyorManager.Instance;
        if (cm == null) return;

        for (int i = -1; i < 2; i++)
            cm.PlaceStraight(new Vector2Int(7, i), 180f, 1, BeltDirection.Clockwise);

        SpawnInserter(new Vector2Int(7,  2), new Vector2Int(7,  1), new Vector2Int(7,  3));
        SpawnInserter(new Vector2Int(7, -2), new Vector2Int(7, -3), new Vector2Int(7, -1));
    }

    public void SpawnInserter(Vector2Int cell, Vector2Int pickupCell, Vector2Int dropOffCell)
    {
        var pm = PrefabManager.Instance;
        var cm = ConveyorManager.Instance;
        if (pm == null || pm.GetInserter() == null || cm == null) return;

        if (!GridOccupancy.CanPlace(cell, Vector2Int.one))
        {
            Debug.LogWarning($"Occupied at {cell}");
            return;
        }

        var go = Instantiate(pm.GetInserter(), CoreConfig.CellCenter(cell), Quaternion.identity);
        go.name = $"Inserter_{cell.x}_{cell.y}";

        var ins = go.GetComponent<Inserter>();
        if (ins == null) return;

        ins.SetGridPosition(cell);
        ins.Connect(
            SocketAt(cm, pickupCell,  cell),
            SocketAt(cm, dropOffCell, cell));
    }

    static ConnectionPoint SocketAt(ConveyorManager cm, Vector2Int target, Vector2Int inserterCell)
    {
        Conveyor belt = cm.GetAt(target);
        if (belt != null && belt.connectionPoint != null)
            return belt.connectionPoint;

        Container chest = Container.GetAt(target);
        if (chest != null)
            return chest.PortFacing(inserterCell);

        Debug.LogWarning($"No socket at {target}.");
        return null;
    }

    public void SpawnBeltLoop(Vector2Int origin, int sizeX, int sizeZ, bool reverse = false)
    {
        var cm = ConveyorManager.Instance;
        if (cm == null) return;

        LoopBounds(origin, sizeX, sizeZ, out int x0, out int z0, out int x1, out int z1);

        var dir = reverse ? BeltDirection.Clockwise : BeltDirection.AntiClockwise;

        cm.PlaceCorner(new Vector2Int(x0, z0), 270f, 1, dir);
        cm.PlaceCorner(new Vector2Int(x0, z1),   0f, 1, dir);
        cm.PlaceCorner(new Vector2Int(x1, z1),  90f, 1, dir);
        cm.PlaceCorner(new Vector2Int(x1, z0), 180f, 1, dir);

        for (int z = z0 + 1; z <= z1 - 1; z++)
        {
            cm.PlaceStraight(new Vector2Int(x0, z), 180f, 1, dir);
            cm.PlaceStraight(new Vector2Int(x1, z),   0f, 1, dir);
        }
        for (int x = x0 + 1; x <= x1 - 1; x++)
        {
            cm.PlaceStraight(new Vector2Int(x, z0),  90f, 1, dir);
            cm.PlaceStraight(new Vector2Int(x, z1), 270f, 1, dir);
        }
    }

    void SpawnLoopPackages(Vector2Int origin, int sizeX, int sizeZ, int count)
    {
        var cm = ConveyorManager.Instance;
        if (cm == null) return;

        var cells = LoopCells(origin, sizeX, sizeZ);
        if (cells.Count == 0) return;

        count = Mathf.Clamp(count, 1, cells.Count);
        float step = cells.Count / (float)count;

        for (int i = 0; i < count; i++)
        {
            int idx = Mathf.Min(cells.Count - 1, Mathf.RoundToInt(i * step));
            var rider = cm.SpawnPackage(cells[idx]);
            if (rider == null) continue;

            var pkg = rider.GetComponent<Package>();
            if (pkg != null)
                pkg.SetItem(ItemConfig.RandomOre());
        }
    }

    static List<Vector2Int> LoopCells(Vector2Int origin, int sizeX, int sizeZ)
    {
        LoopBounds(origin, sizeX, sizeZ, out int x0, out int z0, out int x1, out int z1);

        var cells = new List<Vector2Int>((x1 - x0 + z1 - z0) * 2);
        for (int x = x0; x <= x1; x++) cells.Add(new Vector2Int(x, z0));
        for (int z = z0 + 1; z <= z1; z++) cells.Add(new Vector2Int(x1, z));
        for (int x = x1 - 1; x >= x0; x--) cells.Add(new Vector2Int(x, z1));
        for (int z = z1 - 1; z > z0; z--) cells.Add(new Vector2Int(x0, z));
        return cells;
    }

    static void LoopBounds(Vector2Int origin, int sizeX, int sizeZ,
                           out int x0, out int z0, out int x1, out int z1)
    {
        sizeX = Mathf.Max(2, sizeX);
        sizeZ = Mathf.Max(2, sizeZ);
        x0 = origin.x - (sizeX - 1) / 2;
        z0 = origin.y - (sizeZ - 1) / 2;
        x1 = x0 + sizeX - 1;
        z1 = z0 + sizeZ - 1;
    }

    public void SpawnGround()
    {
        var pm = PrefabManager.Instance;
        if (pm == null || pm.groundPrefab == null) return;
        if (GameObject.Find("Ground") != null) return;

        var go = Instantiate(
            pm.groundPrefab,
            new Vector3(0f, GroundConfig.GroundY, 0f),
            Quaternion.identity);
        go.name = "Ground";
    }

    public Container SpawnChest(Vector2Int cell, int portsX = 1, int portsZ = 1)
    {
        var pm = PrefabManager.Instance;
        if (pm == null) return null;

        GameObject prefab = pm.GetChest(portsX, portsZ);
        if (prefab == null) return null;

        Vector2Int fp = ContainerConfig.GetFootprint(portsX, portsZ);
        if (!GridOccupancy.CanPlace(cell, fp))
        {
            Debug.LogWarning($"Occupied at {cell}");
            return null;
        }

        var go = Instantiate(prefab, CoreConfig.CellCenter(cell), Quaternion.identity);
        go.name = $"Chest_{portsX}x{portsZ}_{cell.x}_{cell.y}";

        var chest = go.GetComponent<Container>();
        if (chest != null)
            chest.SetGridPosition(cell);

        return chest;
    }
}