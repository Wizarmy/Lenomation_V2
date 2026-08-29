using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : Placeable
{
    static readonly List<Container> All = new List<Container>();

    [Header("Inventory")]
    public int slotCount = 4;

    [Header("Footprint (tiles)")]
    public int footprintX = 1;
    public int footprintZ = 1;

    public ConnectionPoint[] ports;

    public event Action ContentsChanged;

    readonly List<Package> stored = new List<Package>();

    public override Vector2Int Footprint
    {
        get => new Vector2Int(footprintX, footprintZ);
        set
        {
            footprintX = Mathf.Max(1, value.x);
            footprintZ = Mathf.Max(1, value.y);
            base.Footprint = new Vector2Int(footprintX, footprintZ);
        }
    }

    public int Count => stored.Count;
    public bool IsFull => stored.Count >= slotCount;

    void Awake()
    {
        Footprint = new Vector2Int(footprintX, footprintZ);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!All.Contains(this))
            All.Add(this);
    }

    protected override void OnDisable()
    {
        All.Remove(this);
        base.OnDisable();
    }

    public bool ContainsCell(Vector2Int cell)
    {
        Vector2Int o = Cell;
        return cell.x >= o.x && cell.x < o.x + footprintX
            && cell.y >= o.y && cell.y < o.y + footprintZ;
    }

    public static Container GetAt(Vector2Int cell)
    {
        for (int i = 0; i < All.Count; i++)
        {
            Container c = All[i];
            if (c != null && c.ContainsCell(cell))
                return c;
        }
        return null;
    }

    public ConnectionPoint PortFacing(Vector2Int otherCell)
    {
        if (ports == null || ports.Length == 0)
            return null;

        Vector3 to = CoreConfig.CellCenter(otherCell) - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 1e-8f)
            return ports[0];
        to.Normalize();

        ConnectionPoint best = null;
        float bestDot = float.NegativeInfinity;
        for (int i = 0; i < ports.Length; i++)
        {
            ConnectionPoint p = ports[i];
            if (p == null) continue;

            Vector3 d = p.transform.position - transform.position;
            d.y = 0f;
            float dot = Vector3.Dot(d.normalized, to);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = p;
            }
        }
        return best;
    }

    public bool TryInsert(Package pkg)
    {
        if (pkg == null || IsFull) return false;

        var rider = pkg.GetComponent<PackageRider>();
        if (rider != null)
            rider.Detach();

        var col = pkg.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        pkg.transform.SetParent(transform, false);
        pkg.transform.localPosition = new Vector3(0f, ContainerConfig.ChestHeight * 0.5f, 0f);
        pkg.gameObject.SetActive(false);

        stored.Add(pkg);
        ContentsChanged?.Invoke();
        return true;
    }

    public bool TryExtract(out Package pkg)
    {
        pkg = null;
        if (stored.Count == 0) return false;

        int last = stored.Count - 1;
        pkg = stored[last];
        stored.RemoveAt(last);

        pkg.gameObject.SetActive(true);
        pkg.transform.SetParent(null, true);

        var col = pkg.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        ContentsChanged?.Invoke();
        return true;
    }
}