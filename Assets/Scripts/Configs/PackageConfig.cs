using UnityEngine;

public static class PackageConfig
{
    public const float PackageSize = CoreConfig.TileSize / 3f;
    public static float HalfPackageSize => PackageSize * 0.5f;

    public static readonly Color DefaultColor = new Color(0.82f, 0.68f, 0.42f);

    public const int FaceTop    = 0;
    public const int FaceBottom = 1;
    public const int FaceFront  = 2;
    public const int FaceBack   = 3;
    public const int FaceLeft   = 4;
    public const int FaceRight  = 5;

    public const string PrefabPath = PathingConfig.ItemFolder + "Package.prefab";
    public const string MeshPath   = PathingConfig.ItemFolder + "PackageMesh.asset";
}