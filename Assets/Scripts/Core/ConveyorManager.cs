using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns conveyor placement, neighbour linking, endcaps, and mid-tile links.
/// Endcaps and links are visual children — they do not occupy a grid cell.
/// </summary>
public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    readonly Dictionary<Vector2Int, Conveyor> grid = new Dictionary<Vector2Int, Conveyor>();
    readonly List<Conveyor> allConveyors = new List<Conveyor>();

    [Header("Placement")]
    public int defaultBeltLevel = 1;

    const string EntryCapName = "EndCap_Entry";
    const string ExitCapName  = "EndCap_Exit";
    const string LinkName     = "ConveyorLink";

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

    public Conveyor PlaceCorner(Vector2Int cell, float yRotation = 0f,
                                BeltDirection travel = BeltDirection.Clockwise)
    {
        if (grid.ContainsKey(cell))
        {
            Debug.Log($"Conveyor already at {cell}");
            return null;
        }

        GameObject prefab = PrefabManager.Instance.GetCorner();
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, yRotation, 0f));
        go.name = prefab.name;

        var conv = go.GetComponent<Conveyor>();
        if (conv == null)
        {
            Destroy(go);
            return null;
        }

        conv.pieceType = ConveyorPieceType.Corner;
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
        if (conveyor == null || !IsBelt(conveyor)) return;

        RefreshLinksAndCaps(conveyor);

        Vector2Int cell = CellOf(conveyor);
        foreach (var n in CardinalNeighbours(cell))
        {
            if (grid.TryGetValue(n, out Conveyor other) && IsBelt(other))
                RefreshLinksAndCaps(other);
        }
    }

    // ------------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------------

    public void Register(Conveyor conveyor)
    {
        if (!IsBelt(conveyor)) return;

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
        ClearVisuals(conveyor);
    }

    void RebuildNeighboursOf(Vector2Int cell)
    {
        foreach (var n in CardinalNeighbours(cell))
        {
            if (grid.TryGetValue(n, out Conveyor other) && IsBelt(other))
                RefreshLinksAndCaps(other);
        }
    }

    // ------------------------------------------------------------------
    // Linking + endcaps + mid links
    // ------------------------------------------------------------------

    void RefreshLinksAndCaps(Conveyor conv)
    {
        if (!IsBelt(conv)) return;

        Vector2Int cell   = CellOf(conv);
        Vector2Int travel = TravelDir(conv);
        Vector2Int behind = IncomingDir(conv);

        Conveyor next = GetBeltAt(cell + travel);
        bool linksOut = CanLink(conv, next);
        conv.nextConveyor = linksOut ? next : null;

        Conveyor prev = GetBeltAt(cell + behind);
        bool linksIn = CanLink(prev, conv);
        conv.previousConveyor = linksIn ? prev : null;
        if (linksIn)
            prev.nextConveyor = conv;

        if (linksIn)  DestroyChild(conv, EntryCapName);
        else          EnsureCap(conv, entry: true);

        if (linksOut) DestroyChild(conv, ExitCapName);
        else          EnsureCap(conv, entry: false);

        RefreshLink(conv);
    }

    void RefreshLink(Conveyor conv)
    {
        // Corner exit is local +X — straight-link offset is local +Z only.
        if (conv.isCorner)
            return;

        bool needLink = conv.nextConveyor != null;
        Transform existing = conv.transform.Find(LinkName);

        if (!needLink)
        {
            if (existing != null) Destroy(existing.gameObject);
            return;
        }

        if (existing != null) return;

        GameObject prefab = PrefabManager.Instance != null
            ? PrefabManager.Instance.GetLink()
            : null;
        if (prefab == null)
        {
            Debug.LogError("[ConveyorManager] No LinkConveyor prefab.");
            return;
        }

        var go = Instantiate(prefab, conv.transform);
        go.name = LinkName;
        go.transform.localPosition = new Vector3(
            0f, 0f,
            ConveyorConfig.HalfBeltLength + ConveyorConfig.LinkLength * 0.5f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var link = go.GetComponent<Conveyor>();
        if (link != null)
        {
            link.pieceType        = ConveyorPieceType.Link;
            link.entryPoint       = null;
            link.exitPoint        = null;
            link.nextConveyor     = null;
            link.previousConveyor = null;
        }
    }

    static bool CanLink(Conveyor from, Conveyor to)
    {
        if (!IsBelt(from) || !IsBelt(to)) return false;
        if (!from.HasExit || !to.HasEntry) return false;

        Vector2Int fromCell = CellOf(from);
        Vector2Int toCell   = CellOf(to);

        if (toCell != fromCell + TravelDir(from)) return false;
        if (IncomingDir(to) != TravelDir(from))   return false;

        return true;
    }

    Conveyor GetBeltAt(Vector2Int cell)
    {
        if (!grid.TryGetValue(cell, out Conveyor c)) return null;
        return IsBelt(c) ? c : null;
    }

    static bool IsBelt(Conveyor c)
    {
        if (c == null) return false;
        if (c.isEndCap) return false;
        if (c.pieceType == ConveyorPieceType.Link) return false;
        return true;
    }

    // ------------------------------------------------------------------
    // Endcaps
    // ------------------------------------------------------------------

    void EnsureCap(Conveyor conv, bool entry)
    {
        string capName = entry ? EntryCapName : ExitCapName;
        if (conv.transform.Find(capName) != null) return;

        GameObject prefab = PrefabManager.Instance != null
            ? PrefabManager.Instance.GetEndCap()
            : null;
        if (prefab == null)
        {
            Debug.LogError("[ConveyorManager] No EndCap prefab.");
            return;
        }

        GetCapLocal(conv, entry, out Vector3 localPos, out float yaw);

        GameObject cap = Instantiate(prefab, conv.transform);
        cap.name = capName;
        cap.transform.localPosition = localPos;
        cap.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        cap.transform.localScale    = Vector3.one;

        var capConv = cap.GetComponent<Conveyor>();
        if (capConv != null)
        {
            capConv.pieceType        = ConveyorPieceType.EndCap;
            capConv.entryPoint       = null;
            capConv.exitPoint        = null;
            capConv.nextConveyor     = null;
            capConv.previousConveyor = null;
        }
    }

    static void GetCapLocal(Conveyor conv, bool entry, out Vector3 localPos, out float yaw)
    {
        float dist = CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge;

        if (conv.isCorner)
        {
            // Default corner: entry -Z, exit +X
            if (entry)
            {
                localPos = new Vector3(0f, 0f, -dist);
                yaw = 90f;
            }
            else
            {
                localPos = new Vector3(dist, 0f, 0f);
                yaw = 0f; // bulge +X already faces out; tweak if the cap looks wrong
            }

            if (conv.direction == BeltDirection.AntiClockwise)
            {
                localPos = -localPos;
                yaw += 180f;
            }
            return;
        }

        bool flipped = conv.direction == BeltDirection.Clockwise;
        float alongZ = entry ? -dist : dist;
        if (flipped) alongZ = -alongZ;

        yaw = entry ? 90f : 270f;
        if (flipped) yaw = entry ? 270f : 90f;

        localPos = new Vector3(0f, 0f, alongZ);
    }

    static void DestroyChild(Conveyor conv, string childName)
    {
        Transform t = conv.transform.Find(childName);
        if (t != null)
            Destroy(t.gameObject);
    }

    static void ClearVisuals(Conveyor conv)
    {
        DestroyChild(conv, EntryCapName);
        DestroyChild(conv, ExitCapName);
        DestroyChild(conv, LinkName);
    }

    // ------------------------------------------------------------------
    // Direction / grid
    // ------------------------------------------------------------------

    /// <summary>Grid step of items leaving this piece.</summary>
    public static Vector2Int TravelDir(Conveyor conv)
    {
        Vector3 outDir;

        if (conv.isCorner)
        {
            // Default: out local +X. AntiClockwise: out local -Z.
            outDir = conv.direction == BeltDirection.AntiClockwise
                ? -conv.transform.forward
                :  conv.transform.right;
        }
        else
        {
            outDir = conv.transform.forward;
            if (conv.direction == BeltDirection.Clockwise)
                outDir = -outDir;
        }

        return ToCardinal(outDir);
    }

    /// <summary>Grid step from the neighbour that feeds this piece.</summary>
    public static Vector2Int IncomingDir(Conveyor conv)
    {
        if (!conv.isCorner)
            return new Vector2Int(-TravelDir(conv).x, -TravelDir(conv).y);

        // Default: in from local -Z. AntiClockwise: in from local -X.
        Vector3 inn = conv.direction == BeltDirection.AntiClockwise
            ? -conv.transform.right
            : -conv.transform.forward;

        return ToCardinal(inn);
    }

    static Vector2Int ToCardinal(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            return new Vector2Int(dir.x >= 0f ? 1 : -1, 0);
        return new Vector2Int(0, dir.z >= 0f ? 1 : -1);
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
        yield return cell + Vector2Int.up;
        yield return cell + Vector2Int.down;
        yield return cell + Vector2Int.left;
        yield return cell + Vector2Int.right;
    }
}