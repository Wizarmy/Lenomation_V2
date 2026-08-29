using UnityEngine;

public static class InserterConfig
{
    public const string PrefabPath = PathingConfig.InserterFolder + "Inserter.prefab";

    public static readonly Color BaseColor  = new Color(0.22f, 0.24f, 0.28f);
    public static readonly Color TowerColor = new Color(0.32f, 0.36f, 0.42f);
    public static readonly Color BoomColor  = new Color(0.55f, 0.45f, 0.22f);
    public static readonly Color GrabColor  = new Color(0.75f, 0.28f, 0.18f);

    public static Vector3 BaseSize => new Vector3(
        ConveyorConfig.BeltWidth,
        ConveyorConfig.BeltHeight,
        ConveyorConfig.BeltLength);

    public static float BoomHeight =>
        ConveyorConfig.BeltHeight + PackageConfig.HalfPackageSize;

    public static float TowerHeight =>
        Mathf.Max(0.05f, BoomHeight - BaseSize.y);

    public static Vector3 TowerSize =>
        new Vector3(0.18f, TowerHeight, 0.18f);

    public static readonly Vector3 Boom0Size = new Vector3(0.12f, 0.10f, 0.50f);
    public static readonly Vector3 Boom1Size = new Vector3(0.10f, 0.08f, 0.42f);
    public static readonly Vector3 Boom2Size = new Vector3(0.08f, 0.07f, 0.36f);

    public const int BoomSections = 3;
    public const float BoomOverlap = 0.12f;
    public const float Boom0Nested = 0.08f;

    public const float MagnetRadius = 0.06f;
    public const float MagnetHeight = 0.05f;

    public const float DefaultYaw    = 0f;
    public const float DefaultExtend = 0f;

    public const float RetractSpeed = 2.5f;
    public const float SlewSpeed    = 180f;
    public const float ExtendSpeed  = 2.0f;
    public const float YawArrive    = 1.0f;
    public const float ExtendArrive = 0.01f;
    public const float LiftSpeed = 0.8f;
}