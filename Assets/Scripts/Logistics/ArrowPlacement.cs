using UnityEngine;

public struct ArrowPlacement
{
    public float angle;
    public Vector3 position;

    public ArrowPlacement(float angle, Vector3 position)
    {
        this.angle = angle;
        this.position = position;
    }
}