using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns conveyor placement, neighbour linking, and endcap spawn/despawn.
/// Endcaps are visual children — they do not occupy a grid cell.
/// </summary>
public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    // Real belts only (not endcaps), keyed by grid (x, z)
    readonly Dictionary<Vector2Int, Conveyor> grid = new Dictionary<Vector2Int, Conveyor>();
    readonly List<Conveyor> allConveyors = new List<Conveyor>();

    [Header("Placement")]
    public int defaultBeltLevel = 1;

    const string EntryCapName = "EndCap_Entry";
    const string ExitCapName  = "EndCap_Exit";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>
    /// Place a straight belt on a grid cell. yRotation is 0/90/180/270.
    /// Returns the belt, or null if the cell is occupied.
    /// </summary>
    public Conveyor PlaceStraight(Vector2Int cell, float yRotation = 0f, int beltLevel = -1,
                                  BeltDirection travel = BeltDirection.AntiClockwise)
    {
        if (grid.ContainsKey(cell))
        {
            Debug.Log($"Conveyor already at {cell}");
            return null;
        }

        if (beltLevel < 1) beltLevel = defaultBeltLevel;

        GameObject prefab = PrefabManager.Instance.GetStraight(beltLevel);
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, yRotation, 0f));
        go.name = prefab.name;

        var conv = go.GetComponent<Conveyor>();
        if (conv == null)
        {
            Destroy(go);
            return null;
        }

        conv.pieceType = ConveyorPieceType.Straight;
        conv.SetGridPosition(new Vector2(cell.x, cell.y));
        conv.SetDirection(travel);

        Register(conv);
        RebuildConnectionsAround(conv);
        return conv;
    }

    public bool RemoveAt(Vector2Int cell)
    {
        if (!grid.TryGetValue(cell, out Conveyor conv))
            return false;

        Unregister(conv);
        Destroy(conv.gameObject);
        RebuildNeighboursOf(cell);
        return true;
    }

    public Conveyor GetAt(Vector2Int cell)
    {
        grid.TryGetValue(cell, out Conveyor c);
        return c;
    }

    public IReadOnlyList<Conveyor> GetAllConveyors() => allConveyors;

    public void RebuildAllConnections()
    {
        foreach (var conv in allConveyors)
            RefreshLinksAndCaps(conv);
    }

    public void RebuildConnectionsAround(Conveyor conveyor)
    {
        if (conveyor == null || conveyor.isEndCap) return;

        RefreshLinksAndCaps(conveyor);

        Vector2Int cell = CellOf(conveyor);
        foreach (var n in CardinalNeighbours(cell))
        {
            if (grid.TryGetValue(n, out Conveyor other) && other != null && !other.isEndCap)
                RefreshLinksAndCaps(other);
        }
    }

    // ------------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------------

    public void Register(Conveyor conveyor)
    {
        if (conveyor == null || conveyor.isEndCap) return;

        Vector2Int key = CellOf(conveyor);

        if (grid.TryGetValue(key, out Conveyor existing) && existing != null && existing != conveyor)
        {
            Debug.LogWarning($"Replacing conveyor at {key}");
            allConveyors.Remove(existing);
        }

        grid[key] = conveyor;
        if (!allConveyors.Contains(conveyor))
            allConveyors.Add(conveyor);
    }

    public void Unregister(Conveyor conveyor)
    {
        if (conveyor == null) return;

        Vector2Int key = CellOf(conveyor);
        if (grid.TryGetValue(key, out Conveyor at) && at == conveyor)
            grid.Remove(key);

        allConveyors.Remove(conveyor);
        conveyor.nextConveyor = null;
        conveyor.previousConveyor = null;
        ClearCaps(conveyor);
    }

    void RebuildNeighboursOf(Vector2Int cell)
    {
        foreach (var n in CardinalNeighbours(cell))
        {
            if (grid.TryGetValue(n, out Conveyor other) && other != null && !other.isEndCap)
                RefreshLinksAndCaps(other);
        }
    }

    // ------------------------------------------------------------------
    // Linking + endcaps
    // ------------------------------------------------------------------

    void RefreshLinksAndCaps(Conveyor conv)
    {
        if (conv == null || conv.isEndCap) return;

        Vector2Int cell   = CellOf(conv);
        Vector2Int travel = TravelDir(conv);          // outgoing
        Vector2Int behind = new Vector2Int(-travel.x, -travel.y);

        // --- outgoing ---
        Conveyor next = GetBeltAt(cell + travel);
        bool linksOut = CanLink(conv, next);
        conv.nextConveyor = linksOut ? next : null;

        // --- incoming ---
        Conveyor prev = GetBeltAt(cell + behind);
        bool linksIn = CanLink(prev, conv);
        conv.previousConveyor = linksIn ? prev : null;
        if (linksIn)
            prev.nextConveyor = conv;

        if (linksIn)  DestroyCap(conv, "EndCap_Entry");
        else          EnsureCap(conv, entry: true);

        if (linksOut) DestroyCap(conv, "EndCap_Exit");
        else          EnsureCap(conv, entry: false);
    }

    static bool CanLink(Conveyor from, Conveyor to)
    {
        if (from == null || to == null) return false;
        if (from.isEndCap || to.isEndCap) return false;
        if (!from.HasExit || !to.HasEntry) return false;

        // to must sit on the cell in front of from, and travel the same way
        // (its incoming side faces from)
        Vector2Int fromCell = CellOf(from);
        Vector2Int toCell   = CellOf(to);
        Vector2Int travel   = TravelDir(from);

        if (toCell != fromCell + travel) return false;
        if (TravelDir(to) != travel)     return false;

        return true;
    }

    Conveyor GetBeltAt(Vector2Int cell)
    {
        if (!grid.TryGetValue(cell, out Conveyor c)) return null;
        if (c == null || c.isEndCap) return null;
        return c;
    }

    // ------------------------------------------------------------------
    // Endcaps (children of the belt)
    // ------------------------------------------------------------------

    void EnsureCap(Conveyor conv, bool entry)
    {
        string capName = entry ? "EndCap_Entry" : "EndCap_Exit";
        if (conv.transform.Find(capName) != null) return;

        GameObject prefab = PrefabManager.Instance.GetEndCap();
        if (prefab == null)
        {
            Debug.LogError(
                $"[ConveyorManager] No EndCap prefab for level {conv.beltLevel}.");
            return;
        }

        // Natural straight path is local +Z.
        // Clockwise reverses item flow → entry/exit swap along local Z.
        bool flipped = !conv.isCorner && conv.direction == BeltDirection.Clockwise;

        // Open side along local Z:
        //   entry → -Z   (or +Z if flipped)
        //   exit  → +Z   (or -Z if flipped)
        float entryExitPointValue = CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge;
        float alongZ = entry ? -entryExitPointValue : entryExitPointValue;
        if (flipped) alongZ = -alongZ;

        // Mesh: flat on -X, bulge +X.
        // Empirically for +Z travel: entry yaw 90, exit yaw 270.
        float yaw = entry ? 90f : 270f;
        if (flipped) yaw = entry ? 270f : 90f;

        GameObject cap = Instantiate(prefab, conv.transform);
        cap.name = capName;
        cap.transform.localPosition = new Vector3(0f, 0f, alongZ);
        cap.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        cap.transform.localScale    = Vector3.one;

        var capConv = cap.GetComponent<Conveyor>();
        if (capConv != null)
        {
            capConv.pieceType    = ConveyorPieceType.EndCap;
            capConv.entryPoint   = null;
            capConv.exitPoint    = null;
            capConv.nextConveyor = null;
        }
    }

    static void DestroyCap(Conveyor conv, string capName)
    {
        Transform t = conv.transform.Find(capName);
        if (t != null)
            Destroy(t.gameObject);
    }

    static void ClearCaps(Conveyor conv)
    {
        DestroyCap(conv, EntryCapName);
        DestroyCap(conv, ExitCapName);
    }

    // ------------------------------------------------------------------
    // Direction / grid helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Cardinal travel on the grid. Straight natural path is local +Z.
    /// Clockwise on a straight reverses travel (matches arrow flip).
    /// </summary>
    public static Vector2Int TravelDir(Conveyor conv)
    {
        Vector3 fwd = conv.transform.forward;
        if (!conv.isCorner && conv.direction == BeltDirection.Clockwise)
            fwd = -fwd;

        if (Mathf.Abs(fwd.x) >= Mathf.Abs(fwd.z))
            return new Vector2Int(fwd.x >= 0f ? 1 : -1, 0);

        return new Vector2Int(0, fwd.z >= 0f ? 1 : -1);
    }

    public static Vector2Int CellOf(Conveyor conv)
    {
        return new Vector2Int(
            Mathf.RoundToInt(conv.gridPosition.x),
            Mathf.RoundToInt(conv.gridPosition.y));
    }

    public static Vector2Int WorldToCell(Vector3 world)
    {
        return new Vector2Int(
            Mathf.FloorToInt(world.x / CoreConfig.TileSize),
            Mathf.FloorToInt(world.z / CoreConfig.TileSize));
    }

    static IEnumerable<Vector2Int> CardinalNeighbours(Vector2Int cell)
    {
        yield return cell + Vector2Int.up;      // +Z in our y=z mapping
        yield return cell + Vector2Int.down;
        yield return cell + Vector2Int.left;
        yield return cell + Vector2Int.right;
    }
}