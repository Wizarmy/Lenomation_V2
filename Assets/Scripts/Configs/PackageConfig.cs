using UnityEngine;

public static class PackageConfig
{
    public const float PackageSize = CoreConfig.TileSize / 3f;
    public static float HalfPackageSize => PackageSize * 0.5f;

    public static readonly Color DefaultTop    = new Color(0.82f, 0.68f, 0.42f);
    public static readonly Color DefaultBottom = new Color(0.45f, 0.35f, 0.20f);
    public static readonly Color DefaultFront  = new Color(0.72f, 0.56f, 0.32f);
    public static readonly Color DefaultBack   = new Color(0.68f, 0.52f, 0.30f);
    public static readonly Color DefaultLeft   = new Color(0.70f, 0.54f, 0.31f);
    public static readonly Color DefaultRight  = new Color(0.70f, 0.54f, 0.31f);

    public const int FaceTop    = 0;
    public const int FaceBottom = 1;
    public const int FaceFront  = 2;
    public const int FaceBack   = 3;
    public const int FaceLeft   = 4;
    public const int FaceRight  = 5;

    public const string PrefabPath = PathingConfig.ItemFolder + "Package.prefab";
    public const string MeshPath   = PathingConfig.ItemFolder + "PackageMesh.asset";
}