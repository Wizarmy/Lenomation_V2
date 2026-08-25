using UnityEngine;

/// <summary>
/// Container / chest specifics. Shared grid, package, and belt values come from LogisticsConfig.
/// </summary>
public static class ContainerConfig
{
    // =====================================================================
    // Shell / footprint
    // =====================================================================
    public const int FootprintPadding = 1;
    public const float ChestHeight = 1.00f;

    /// <summary>Uses shared grid size.</summary>
    public static float TileSize => LogisticsConfig.TileSize;

    // =====================================================================
    // Ports (aligned to belts + packages)
    // =====================================================================
    /// <summary>Port floor = belt top (belts centred on Y).</summary>
    public static float PortBottom => LogisticsConfig.HalfBeltHeight;

    public const float PortInset         = 0.15f;
    public const float PortPadding       = 0.15f;
    public const float PortSurfaceOffset = 0.04f;
    public const float DefaultPortRadius = 0.35f;

    public const float PortWidthScale  = 1.1f;
    public const float PortHeightScale = 1.5f;

    public static float PortWidth  => LogisticsConfig.PackageSize * PortWidthScale;
    public static float PortHeight => LogisticsConfig.PackageSize * PortHeightScale;

    // =====================================================================
    // Inventory
    // =====================================================================
    public const int MinSlotCount = 4;

    public static int SlotCountForPorts(int portsX, int portsZ) =>
        Mathf.Max(MinSlotCount, 2 * (portsX + portsZ));

    public static Vector2Int GetFootprint(int portsX, int portsZ) =>
        new Vector2Int(portsX + FootprintPadding, portsZ + FootprintPadding);

    public static readonly (int x, int z)[] ChestSizes =
    {
        (1, 1), (2, 1), (2, 2), (3, 2), (3, 3), (4, 3), (4, 4),
    };

    // =====================================================================
    // Colours
    // =====================================================================
    public static readonly Color ShellColor  = new Color(0.35f, 0.28f, 0.22f);
    public static readonly Color RecessColor = Color.gray;
    public static readonly Color TopColor    = new Color(0.42f, 0.34f, 0.26f);
}