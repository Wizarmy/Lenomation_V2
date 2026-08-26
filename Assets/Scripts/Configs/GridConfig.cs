using UnityEngine;

public static class GridConfig
{
    public const int FromSize = -GroundConfig.GroundSize/2;
    public const int ToSize = GroundConfig.GroundSize/2;
    public const float HeightOffset = 0.01f;                 
    public const float LabelOffset = 0.02f;                 
    public static readonly Color MajorColor = new Color(1f, 1f, 1f, 0.35f);
    public static readonly Color MinorColor = new Color(1f, 1f, 1f, 0.12f);
    public static readonly Color LabelColor = new Color(1f, 1f, 1f, 0.75f);
    public const int MajorEvery = 10;             // stronger line every N cells
    public const int LabelRadiusTiles = 10;
}
