using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ConnectionPoint : MonoBehaviour
{
    public ConnectionType kind = ConnectionType.Both;
    public float size = PackageConfig.PackageSize * 1.2f;

    public event Action<Package> PackageEntered;
    public event Action<Package> PackageExited;

    public Package Occupant { get; private set; }

    public Vector2Int Cell => CoreConfig.WorldToCell(transform.position);

    public bool AllowsPickup  => kind == ConnectionType.Pickup  || kind == ConnectionType.Both;
    public bool AllowsDropOff => kind == ConnectionType.DropOff || kind == ConnectionType.Both;

    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * size;
        col.center = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        var pkg = other.GetComponentInParent<Package>();
        if (pkg == null) return;
        Occupant = pkg;
        PackageEntered?.Invoke(pkg);
    }

    void OnTriggerExit(Collider other)
    {
        var pkg = other.GetComponentInParent<Package>();
        if (pkg == null) return;
        if (Occupant == pkg)
            Occupant = null;
        PackageExited?.Invoke(pkg);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Color c = kind switch
        {
            ConnectionType.Pickup  => new Color(0.2f, 0.8f, 0.3f, 0.7f),
            ConnectionType.DropOff => new Color(0.9f, 0.35f, 0.15f, 0.7f),
            _                      => new Color(0.3f, 0.6f, 1f, 0.7f)
        };
        Gizmos.color = c;
        Gizmos.DrawWireCube(transform.position, Vector3.one * size);
    }
#endif
}