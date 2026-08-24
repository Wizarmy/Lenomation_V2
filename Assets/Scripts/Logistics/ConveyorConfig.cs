using UnityEngine;

public enum BeltDirection
{
    Clockwise,
    AntiClockwise
}

public static class ConveyorConfig
{
    // === Core dimensions (1×1 grid friendly) ===
    public const float Width  = 0.4f;
    public const float Height = 0.15f;
    public const float Length = 1.0f;

    public static float HalfWidth  => Width  * 0.5f;
    public static float HalfHeight => Height * 0.5f;
    public static float HalfLength => Length * 0.5f;

    // === Curve / End Cap settings ===
    public const int   CurveSegments     = 24;
    public const float CornerInnerRadius = 0.5f-(Width/2f);
    public const float CornerOuterRadius = 0.5f+(Width/2f);          
    public const float EndCapRadius = Width / 2f;          // = HalfWidth
    public static readonly Vector3 CornerCentreOffset = new Vector3(0.5f, 0f, -0.5f); // (HalfLength, 0, -HalfLength)
    
    // === Path lengths (for consistent speed) ===
    public static float StraightPathLength => Length;                                    // 1.0
    public static float CornerPathLength  => ((CornerInnerRadius + CornerOuterRadius) * 0.5f) * (Mathf.PI * 0.5f);           // ≈ 0.7854

    // === Arrows ===
    public const float ArrowSize     = 0.035f;
    public const float ArrowDepth    = 0.01f;
    public const float ArrowSpacing  = 0.09f;
    public const float SideOffset    = 0.012f;

    // === Runtime defaults ===
    public const float DefaultMoveSpeed = 1f;
    public const int   MaxItemsPerBelt  = 3;          // ← hard limit of 3

// Package spacing helpers (used by Conveyor)
    public static float[] GetSlotProgresses() => new float[] { 0.20f, 0.50f, 0.80f }; // nicely centred with gaps

    // === Materials (Unlit industrial look) ===
    public static readonly Color TopColor     = new Color(0.22f, 0.22f, 0.25f);
    public static readonly Color BottomColor  = new Color(0.08f, 0.08f, 0.09f);
    public static readonly Color SideColor    = new Color(0.35f, 0.35f, 0.38f);
    public static readonly Color EndCapColor  = new Color(1f, 0.5f, 0.5f);
    public static readonly Color ArrowColor   = Color.white;
    
    // === Package / Item settings ===
    public const float PackageSize = 0.18f;          // Cube side length – 3 fit on a 1.0 belt with nice gaps
    public static float PackageHalfSize => PackageSize * 0.5f;

    public const float PackageIconHeight = 0.01f;    // How far the icon sits above the cube top
    public const float PackageIconScale  = 0.16f;    // Size of the icon sprite/quad

    // === Paths ===
    public const string PrefabFolder     = "Assets/Prefabs/Logistics/";
    public const string ArrowFolder      = PrefabFolder + "Arrows/";
    public const string ConveyorFolder   = PrefabFolder + "Conveyors/";
    public const string StraightFolder   = ConveyorFolder + "Straight/";
    public const string CornerFolder     = ConveyorFolder + "Corners/";
    public const string EndCapFolder     = ConveyorFolder + "EndCaps/";
    public const string ContainerFolder  = PrefabFolder + "Containers/";
    public const string InserterFolder   = PrefabFolder + "Inserters/";
    public const string ItemFolder       = PrefabFolder + "Items/";
    public const string MaterialFolder   = "Assets/Materials/";
}