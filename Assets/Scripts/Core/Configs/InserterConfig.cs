using UnityEngine;

/// <summary>
/// Inserter specifics. Package size, tile/grid, and paths stay in LogisticsConfig.
/// </summary>
public static class InserterConfig
{
    // =====================================================================
    // Foundation / Mast
    // =====================================================================
    public const float BaseSize   = 0.7f;
    public const float BaseHeight = 0.15f;
    public const float MastWidth  = 0.12f;
    public const float MastHeight = 0.55f;

    // =====================================================================
    // Outer arm
    // =====================================================================
    public static float OuterArmLength => BaseSize * 0.5f;
    public const float OuterArmThickness = 0.10f;

    // =====================================================================
    // Telescope
    // =====================================================================
    public const float InnerArmLength    = 0.75f;
    public const float InnerArmThickness = 0.07f;
    public const float TelescopeMinZ     = 0.02f;

    public static float MinReach => TelescopeMinZ + InnerArmLength * 0.25f;
    public static float FullArmLength => OuterArmLength + InnerArmLength;

    // =====================================================================
    // Cable + Magnet
    // =====================================================================
    public const float CableLength        = 0.28f;
    public const float CableThickness     = 0.015f;
    public const float CableMinLength     = 0.04f;
    public const float MagnetRadius       = 0.05f;
    public const float MagnetHeight       = 0.04f;
    public const float PackageHoldOffsetY = -0.06f;

    // =====================================================================
    // Behaviour (package size from logistics)
    // =====================================================================
    public const float SwingSpeed       = 180f;
    public const float ExtendSpeed      = 3.5f;
    public const float CableSpeed       = 2.5f;
    public const float Cooldown         = 0.4f;
    public const float MaxLinkDistance  = 1.2f;
    public const float PackageClearance = 0.02f;

    /// <summary>Visual package height for cable hang math.</summary>
    public static float PackageHeight => LogisticsConfig.PackageSize;

    public const float AngleTolerance = 3f;
    public const float PosTolerance   = 0.02f;

    // =====================================================================
    // Colours
    // =====================================================================
    public static readonly Color BaseColor      = new Color(0.25f, 0.25f, 0.28f);
    public static readonly Color MastColor      = new Color(0.35f, 0.35f, 0.38f);
    public static readonly Color ArmColor       = new Color(0.6f, 0.55f, 0.3f);
    public static readonly Color TelescopeColor = new Color(0.7f, 0.65f, 0.35f);
    public static readonly Color CableColor     = new Color(0.15f, 0.15f, 0.15f);
    public static readonly Color MagnetColor    = new Color(0.55f, 0.15f, 0.12f);

    public static readonly Color ArrowUnlinked = new Color(1f, 0.85f, 0.2f);
    public static readonly Color ArrowLinked   = new Color(0.2f, 0.9f, 0.3f);
    public static readonly Color ArrowBlocked  = new Color(0.95f, 0.25f, 0.2f);
}