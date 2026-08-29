using UnityEngine;

public class PackageRider : MonoBehaviour
{
    public Conveyor Current { get; private set; }
    public float Distance { get; private set; }
    public Package Package { get; private set; } 
    
    void Awake()
    {
        Package = GetComponent<Package>();
    }

    public void Attach(Conveyor belt, float distance = 0f)
    {
        SetBelt(belt);
        Distance = Mathf.Max(0f, distance);
        enabled = belt != null;
        if (belt != null)
            Snap();
    }

    public void Detach()
    {
        SetBelt(null);
        enabled = false;
    }

    void OnEnable()
    {
        if (Current != null)
            Current.RegisterRider(this);
    }

    void OnDisable()
    {
        if (Current != null)
            Current.UnregisterRider(this);
    }

    void Update()
    {
        if (Current == null) return;

        float speed = Current.PathLength / Current.PathDuration;
        float nextDist = Distance + speed * Time.deltaTime;
        float cap = ForwardLimit();
        float overflow = nextDist - Current.PathLength;

        if (nextDist >= cap && CanHop(Current.nextConveyor, overflow, out Conveyor next))
        {
            SetBelt(next);
            Distance = Mathf.Max(0f, overflow);
        }
        else
        {
            Distance = Mathf.Min(nextDist, cap);
        }

        Snap();
    }

    float ForwardLimit()
    {
        float space = PackageConfig.MinSpacing;
        float cap = Current.PathLength;

        if (Current.nextConveyor == null)
            cap -= PackageConfig.HalfPackageSize;
        else
            cap = Mathf.Min(cap, LimitFromNext(Current.nextConveyor, space));

        var list = Current.riders;
        for (int i = 0; i < list.Count; i++)
        {
            var other = list[i];
            if (other == null || other == this || !other.enabled) continue;
            if (other.Distance > Distance + 1e-4f)
                cap = Mathf.Min(cap, other.Distance - space);
        }

        return Mathf.Max(Distance, cap);
    }

    float LimitFromNext(Conveyor next, float space)
    {
        float minN = float.PositiveInfinity;
        var list = next.riders;
        for (int i = 0; i < list.Count; i++)
        {
            var other = list[i];
            if (other == null || !other.enabled) continue;
            minN = Mathf.Min(minN, other.Distance);
        }
        if (minN > 1e8f)
            return Current.PathLength;

        Vector3 start = Current.EvaluatePosition(0f);
        Vector3 npos  = next.EvaluatePosition(minN);
        Vector3 delta = npos - start;
        delta.y = 0f;

        Vector3 unit = Current.EvaluateForward(0f);
        float along = Vector3.Dot(delta, unit);
        return along - space;
    }

    static bool CanHop(Conveyor next, float overflow, out Conveyor hopped)
    {
        hopped = next;
        if (next == null || overflow < 0f) return false;
        return !EntryBlocked(next, overflow);
    }

    static bool EntryBlocked(Conveyor belt, float incomingDist)
    {
        float space = PackageConfig.MinSpacing;
        Vector3 incoming = belt.EvaluatePosition(Mathf.Max(0f, incomingDist));
        incoming.y = 0f;

        var list = belt.riders;
        for (int i = 0; i < list.Count; i++)
        {
            var other = list[i];
            if (other == null || !other.enabled) continue;
            Vector3 p = other.transform.position;
            p.y = 0f;
            if ((p - incoming).sqrMagnitude < space * space)
                return true;
        }
        return false;
    }

    void SetBelt(Conveyor belt)
    {
        if (Current == belt) return;
        if (Current != null)
            Current.UnregisterRider(this);
        Current = belt;
        if (Current != null)
            Current.RegisterRider(this);
    }

    void Snap()
    {
        if (Current == null) return;

        Vector3 p = Current.EvaluatePosition(Distance);
        p.y = Current.transform.position.y
              + ConveyorConfig.BeltHeight
              + PackageConfig.HalfPackageSize;

        transform.SetPositionAndRotation(p, Current.EvaluateRotation(Distance));
    }
}