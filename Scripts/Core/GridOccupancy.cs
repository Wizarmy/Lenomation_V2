using System.Collections.Generic;
using UnityEngine;

public static class GridOccupancy
{
    static readonly Dictionary<Vector2Int, Placeable> cells = new();

    public static bool CanPlace(Vector2Int origin, Vector2Int footprint)
    {
        int w = Mathf.Max(1, footprint.x);
        int h = Mathf.Max(1, footprint.y);
        for (int z = 0; z < h; z++)
        for (int x = 0; x < w; x++)
        {
            if (cells.ContainsKey(origin + new Vector2Int(x, z)))
                return false;
        }
        return true;
    }

    public static void Register(Placeable p)
    {
        if (p == null || !p.OccupiesGrid) return;

        Vector2Int origin = p.Cell;
        Vector2Int fp = p.Footprint;
        int w = Mathf.Max(1, fp.x);
        int h = Mathf.Max(1, fp.y);

        for (int z = 0; z < h; z++)
        for (int x = 0; x < w; x++)
            cells[origin + new Vector2Int(x, z)] = p;
    }

    public static void Unregister(Placeable p)
    {
        if (p == null) return;

        var remove = new List<Vector2Int>();
        foreach (var kv in cells)
        {
            if (kv.Value == p)
                remove.Add(kv.Key);
        }
        for (int i = 0; i < remove.Count; i++)
            cells.Remove(remove[i]);
    }

    public static Placeable GetAt(Vector2Int cell)
    {
        cells.TryGetValue(cell, out Placeable p);
        return p;
    }

    public static void Clear() => cells.Clear();
}