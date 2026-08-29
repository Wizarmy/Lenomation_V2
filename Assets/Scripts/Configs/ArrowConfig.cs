using UnityEngine;

public static class ArrowConfig
{
    public const string ArrowPrefabPath = PathingConfig.ArrowFolder + "StraightArrow.prefab";
    public const string ArrowMeshPath   = PathingConfig.ArrowFolder + "StraightArrowMesh.asset";
    public const string ArrowMatPath    = PathingConfig.MaterialFolder + "ConveyorArrow.mat";

    public const float ArrowSize  = 0.035f;
    public const float ArrowDepth = 0.01f;

    public static readonly Color ArrowColor = Color.white;
}