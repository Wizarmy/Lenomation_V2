using UnityEngine;
using System.Collections.Generic;

public static class ConveyorArrowHelper
{
    public static List<Vector3> GetPositionsStraight(int level)
    {
        level = Mathf.Clamp(level, 1, 5);
        var positions = new List<Vector3>();

        float y = 0;
        float spacing = ConveyorConfig.ArrowSpacing;
        float totalWidth = (level - 1) * spacing;
        float startZ = -totalWidth * 0.5f;

        for (int i = 0; i < level; i++)
        {
            float z = startZ + i * spacing;
            positions.Add(new Vector3(0f, y, z));
        }

        return positions;
    }

    public static List<ArrowPlacement> GetPositionsCorner(int level)
    {
        level = Mathf.Clamp(level, 1, 5);

        float outerRadius = ConveyorConfig.CornerOuterRadius;
        Vector3 centreOffset = ConveyorConfig.CornerCentreOffset;
        float angleGap = 10f;

        // How far the group of arrows is shifted so it stays centred
        float groupOffset = ((level - 1) / 2) * angleGap;

        // Starting angle of the first arrow (the group is centred around 135°)
        float startAngle = 135f - groupOffset;

        var placements = new List<ArrowPlacement>();

        for (int i = 0; i < level; i++)
        {
            float pathAngle = startAngle + (i * angleGap);          // position on the curve
            float arrowYAngle = 45f + groupOffset - (i * angleGap); // rotation of the arrow itself

            float rad = pathAngle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * outerRadius,
                0f,
                Mathf.Sin(rad) * outerRadius
            ) + centreOffset;

            placements.Add(new ArrowPlacement(arrowYAngle, pos));
        }

        return placements;
    }

    public static Vector3 GetOutwardDirection(bool isCorner, bool isLeft = true)
    {
        if (isCorner)
        {
            Vector3 edgeDir = new Vector3(1f, 0f, 1f).normalized;
            return Vector3.Cross(Vector3.up, edgeDir).normalized;
        }

        return isLeft ? Vector3.left : Vector3.right;
    }
}