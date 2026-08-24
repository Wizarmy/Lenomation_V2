using UnityEngine;
using System.Collections.Generic;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    [Header("Runtime")]
    public bool isRunning = false;          // global start/stop (Spacebar)

    // -------------------------------------------------
    // Data
    // -------------------------------------------------
    // Grid position → Conveyor
    private readonly Dictionary<Vector2Int, Conveyor> grid = new Dictionary<Vector2Int, Conveyor>();

    // All active conveyors (for easy iteration)
    private readonly List<Conveyor> allConveyors = new List<Conveyor>();

    // -------------------------------------------------
    // Lifecycle
    // -------------------------------------------------
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
    // Registration
    // -------------------------------------------------
    public void Register(Conveyor conveyor)
    {
        if (conveyor == null) return;

        Vector2Int key = WorldToGrid(conveyor.transform.position);

        // Replace if something is already there
        if (grid.ContainsKey(key))
        {
            var old = grid[key];
            if (old != null && old != conveyor)
                Unregister(old);
        }

        grid[key] = conveyor;

        if (!allConveyors.Contains(conveyor))
            allConveyors.Add(conveyor);

        // Immediately try to connect it
        RebuildConnectionsAround(conveyor);
    }

    public void Unregister(Conveyor conveyor)
    {
        if (conveyor == null) return;

        Vector2Int key = WorldToGrid(conveyor.transform.position);

        if (grid.TryGetValue(key, out var existing) && existing == conveyor)
            grid.Remove(key);

        allConveyors.Remove(conveyor);

        // Clear its own connection
        conveyor.nextConveyor = null;

        // Anyone who was pointing to this belt needs to be updated
        foreach (var other in allConveyors)
        {
            if (other.nextConveyor == conveyor)
                other.nextConveyor = null;
        }
    }

    // -------------------------------------------------
    // Network / Connections
    // -------------------------------------------------
    /// <summary>
    /// Rebuilds the nextConveyor link for every belt in the scene.
    /// Call after large changes (load, mass place, direction change, etc.)
    /// </summary>
    public void RebuildAllConnections()
    {
        foreach (var conv in allConveyors)
        {
            conv.nextConveyor = FindNextConveyor(conv);
        }
    }

    /// <summary>
    /// Cheap local rebuild – only updates the given belt and its neighbours.
    /// </summary>
    public void RebuildConnectionsAround(Conveyor conveyor)
    {
        if (conveyor == null) return;

        // Update this belt
        conveyor.nextConveyor = FindNextConveyor(conveyor);

        // Update any belt that might now point to (or used to point to) this one
        Vector2Int myGrid = WorldToGrid(conveyor.transform.position);

        // Check the four cardinal neighbours
        Vector2Int[] offsets = {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        foreach (var offset in offsets)
        {
            if (grid.TryGetValue(myGrid + offset, out var neighbour) && neighbour != null)
            {
                neighbour.nextConveyor = FindNextConveyor(neighbour);
            }
        }
    }

    private Conveyor FindNextConveyor(Conveyor current)
    {
        if (current == null) return null;

        Vector3 exitPos = current.GetExitWorldPositionPublic();
        Vector3 exitDir = current.GetExitWorldDirectionPublic();

        // Look a short distance past the exit into the next cell
        Vector3 searchPos = exitPos + exitDir.normalized * 0.55f;
        Vector2Int nextGrid = WorldToGrid(searchPos);

        if (grid.TryGetValue(nextGrid, out var next) && next != current)
            return next;

        return null;
    }

    // -------------------------------------------------
    // Queries
    // -------------------------------------------------
    public Conveyor GetConveyorAt(Vector2Int gridPos)
    {
        grid.TryGetValue(gridPos, out var c);
        return c;
    }

    public Conveyor GetConveyorAt(Vector3 worldPos)
    {
        return GetConveyorAt(WorldToGrid(worldPos));
    }

    public Conveyor GetNextConveyor(Conveyor current)
    {
        // Prefer the cached link if it exists
        if (current != null && current.nextConveyor != null)
            return current.nextConveyor;

        return FindNextConveyor(current);
    }

    public IReadOnlyList<Conveyor> GetAllConveyors() => allConveyors;

    public int Count => allConveyors.Count;

    // -------------------------------------------------
    // Helpers
    // -------------------------------------------------
    public static Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x),
            Mathf.RoundToInt(worldPos.z)
        );
    }

    // -------------------------------------------------
    // Debug
    // -------------------------------------------------
    void OnDrawGizmosSelected()
    {
        // Optional: draw connection lines in the editor
        Gizmos.color = Color.cyan;
        foreach (var conv in allConveyors)
        {
            if (conv == null || conv.nextConveyor == null) continue;

            Vector3 from = conv.GetExitWorldPositionPublic();
            Vector3 to   = conv.nextConveyor.transform.position + Vector3.up * 0.2f;
            Gizmos.DrawLine(from, to);
        }
    }
}