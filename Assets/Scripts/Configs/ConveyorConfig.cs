using UnityEngine;

public enum BeltDirection
{
    Clockwise,
    AntiClockwise
}

public static class ConveyorConfig
{
    public const float BeltWidth  = CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge * 2f;
    public const float BeltHeight = 0.15f;
    public const float BeltLength = CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge * 2f; // full tile
    
    public static float HalfBeltWidth  => BeltWidth  * 0.5f;
    public static float HalfBeltHeight => BeltHeight * 0.5f;
    public static float HalfBeltLength => BeltLength * 0.5f;
    
    public static float LinkLength => CoreConfig.DistanceFromTileEdge;
    
    // ConveyorConfig
    public const float GuardRailWidth  = 0.01f;
    public const float GuardRailHeight = 0.03f;
    public static readonly Color GuardRailColor = new Color(0.1f, 0.1f, 0.1f);
    
    public const float EndCapArcCurve = 1f / 32f;
    
    public static float StraightPathLength => BeltLength;
    
    public const int CurveSegments = 24;
    public static float CornerInnerRadius => CoreConfig.DistanceFromTileEdge;
    public static float CornerOuterRadius => CoreConfig.TileSize - CoreConfig.DistanceFromTileEdge;
    
    public static float CornerPathLength =>
        ((CornerInnerRadius + CornerOuterRadius) * 0.5f) * (Mathf.PI * 0.5f);
    
    public const float DefaultMoveSpeed = 1f;
    public const int   MaxItemsPerBelt  = 1;
    
    public static readonly Color TopColor    = new Color(0.22f, 0.22f, 0.25f);
    public static readonly Color BottomColor = new Color(0.08f, 0.08f, 0.09f);
    public static readonly Color SideColor   = new Color(0.35f, 0.35f, 0.38f);
    public static readonly Color EndCapColor = new Color(1f, 0.2f, 0.2f);
    public static readonly Color ArrowColor  = Color.white;
    
    public const float ArrowSpacing = 0.09f;
    public static float BeltPlacementY => GroundConfig.GroundY + HalfBeltHeight;
    
}
