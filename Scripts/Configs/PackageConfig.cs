using UnityEngine;

public static class PackageConfig
{
    public static readonly Color DefaultColor = new Color(0.82f, 0.68f, 0.42f);

    public const int FaceTop    = 0;
    public const int FaceBottom = 1;
    public const int FaceFront  = 2;
    public const int FaceBack   = 3;
    public const int FaceLeft   = 4;
    public const int FaceRight  = 5;
    
    public const float PackGap = 0.10f;

    public static float PackageSize =>
        ConveyorConfig.BeltLength / (3f + 2f * PackGap);

    public static float HalfPackageSize => PackageSize * 0.5f;
    public static float MinSpacing     => PackageSize * (1f + PackGap);

    public const string PrefabPath = PathingConfig.ItemFolder + "Package.prefab";
    public const string MeshPath   = PathingConfig.ItemFolder + "PackageMesh.asset";
}