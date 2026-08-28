using UnityEngine;

public class PackageRider : MonoBehaviour
{
    public Conveyor Current { get; private set; }
    public float Distance { get; private set; }
    

    public void Attach(Conveyor belt, float distance = 0f)
    {
        Current = belt;
        Distance = Mathf.Max(0f, distance);
        Snap();
    }

    void Update()
    {
        if (Current == null) return;

        Distance += Current.PathLength / Current.PathDuration * Time.deltaTime;

        while (Current != null && Distance >= Current.PathLength)
        {
            float overflow = Distance - Current.PathLength;
            Conveyor next = Current.nextConveyor;
            if (next == null)
            {
                Distance = Current.PathLength;
                break;
            }
            Current = next;
            Distance = overflow;
        }

        Snap();
    }

    void Snap()
    {
        if (Current == null) return;

        Vector3 p = Current.EvaluatePosition(Distance);
        p.y = ConveyorConfig.BeltHeight + PackageConfig.HalfPackageSize;
        transform.position = p;
        transform.rotation = Current.EvaluateRotation(Distance);
    }
}