using UnityEngine;

public static class ContainerConfig
{
    public const int FootprintPadding = 0;
    public const float ChestHeight = 1.00f;

    public static float PortBottom => ConveyorConfig.BeltHeight;
    public const float PortInset         = 0.15f;
    public const float PortPadding       = 0.15f;
    public const float PortSurfaceOffset = 0.04f;

    public const float PortWidthScale  = 1.25f;
    public const float PortHeightScale = 1.65f;

    public static float PortWidth  => PackageConfig.PackageSize * PortWidthScale;
    public static float PortHeight => PackageConfig.PackageSize * PortHeightScale;
    public static float PortSize   => PackageConfig.PackageSize * 1.2f;

    public const int MinSlotCount = 4;

    public static int SlotCountForPorts(int portsX, int portsZ) =>
        Mathf.Max(MinSlotCount, 2 * (portsX + portsZ));


    public static Vector2Int GetFootprint(int portsX, int portsZ)
    {
        int pad = FootprintPadding;
        if ((pad & 1) != 0) pad++;
        return new Vector2Int(
            Mathf.Max(1, portsX + pad),
            Mathf.Max(1, portsZ + pad));
    }
    public static readonly Vector2Int[] ChestSizes =
    {
        new Vector2Int(1, 1),
        new Vector2Int(2, 1),
        new Vector2Int(2, 2),
        new Vector2Int(3, 2),
        new Vector2Int(3, 3),
        new Vector2Int(4, 3),
        new Vector2Int(4, 4),
    };

    public static string PrefabPath(int portsX, int portsZ) =>
        $"{PathingConfig.ContainerFolder}Chest_{portsX}x{portsZ}.prefab";

    public static string MeshPath(int portsX, int portsZ) =>
        $"{PathingConfig.ContainerFolder}Chest_{portsX}x{portsZ}_Mesh.asset";

    public static readonly Color ShellColor  = new Color(0.35f, 0.28f, 0.22f);
    public static readonly Color RecessColor = Color.gray;
    public static readonly Color TopColor    = new Color(0.42f, 0.34f, 0.26f);
}