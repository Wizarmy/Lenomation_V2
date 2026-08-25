using UnityEngine;

public enum BeltDirection
{
    Clockwise,
    AntiClockwise
}

/// <summary>
/// Shared logistics constants: belts, packages, ground, materials, asset paths.
/// Container-specific values → ContainerConfig
/// Inserter-specific values → InserterConfig
/// </summary>
public static class LogisticsConfig
{
    // =====================================================================
    // Belt dimensions (1×1 grid friendly)
    // =====================================================================
    public const float BeltWidth  = 0.4f;
    public const float BeltHeight = 0.15f;
    public const float BeltLength = 1.0f;

    public static float HalfBeltWidth  => BeltWidth  * 0.5f;
    public static float HalfBeltHeight => BeltHeight * 0.5f;
    public static float HalfBeltLength => BeltLength * 0.5f;

    // =====================================================================
    // Grid
    // =====================================================================
    public const float TileSize = 1f;

    // =====================================================================
    // Curve / End Cap
    // =====================================================================
    public const int   CurveSegments     = 24;
    public const float CornerInnerRadius = 0.5f - (BeltWidth / 2f);
    public const float CornerOuterRadius = 0.5f + (BeltWidth / 2f);
    public const float EndCapRadius      = BeltWidth / 2f;
    public static readonly Vector3 CornerCentreOffset = new Vector3(0.5f, 0f, -0.5f);

    public static float StraightPathLength => BeltLength;
    public static float CornerPathLength =>
        ((CornerInnerRadius + CornerOuterRadius) * 0.5f) * (Mathf.PI * 0.5f);

    // =====================================================================
    // Direction arrows
    // =====================================================================
    public const float ArrowSize    = 0.035f;
    public const float ArrowDepth   = 0.01f;
    public const float ArrowSpacing = 0.09f;
    public const float SideOffset   = 0.012f;

    // =====================================================================
    // Runtime defaults (belts)
    // =====================================================================
    public const float DefaultMoveSpeed = 1f;
    public const int   MaxItemsPerBelt  = 3;

    public static float[] GetSlotProgresses() => new float[] { 0.20f, 0.50f, 0.80f };

    // =====================================================================
    // Package / item visuals
    // =====================================================================
    public const float PackageSize       = 0.18f;
    public static float PackageHalfSize  => PackageSize * 0.5f;
    public const float PackageIconHeight = 0.01f;
    public const float PackageIconScale  = 0.16f;

    // =====================================================================
    // Ground
    // =====================================================================
    public const float GroundSize = 64f;
    public const float GroundY    = 0f;
    public static readonly Color GroundColor = new Color(0.12f, 0.12f, 0.13f);

    // =====================================================================
    // Materials (industrial unlit look – belts / shared)
    // =====================================================================
    public static readonly Color TopColor    = new Color(0.22f, 0.22f, 0.25f);
    public static readonly Color BottomColor = new Color(0.08f, 0.08f, 0.09f);
    public static readonly Color SideColor   = new Color(0.35f, 0.35f, 0.38f);
    public static readonly Color EndCapColor = new Color(1f, 0.5f, 0.5f);
    public static readonly Color ArrowColor  = Color.white;

    // =====================================================================
    // Asset paths
    // =====================================================================
    public const string PrefabFolder    = "Assets/Prefabs/Logistics/";
    public const string ArrowFolder     = PrefabFolder + "Arrows/";
    public const string ConveyorFolder  = PrefabFolder + "Conveyors/";
    public const string StraightFolder  = ConveyorFolder + "Straight/";
    public const string CornerFolder    = ConveyorFolder + "Corners/";
    public const string EndCapFolder    = ConveyorFolder + "EndCaps/";
    public const string ContainerFolder = PrefabFolder + "Containers/";
    public const string InserterFolder  = PrefabFolder + "Inserters/";
    public const string ItemFolder      = PrefabFolder + "Items/";
    public const string GroundFolder    = PrefabFolder + "Ground/";
    public const string MaterialFolder  = "Assets/Materials/";
}