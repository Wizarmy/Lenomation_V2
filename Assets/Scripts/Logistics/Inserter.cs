using UnityEngine;

public class Inserter : MonoBehaviour
{
    enum Phase
    {
        Idle,
        Retract,
        SlewPickup,
        Extend,
        Attach,
        Lift,
        RetractLoaded,
        SlewDrop,
        WaitDrop,
        ExtendDrop,
        Release
    }

    const float DropGap = 1.25f;

    [Header("Pose")]
    public float yaw;
    [Range(0f, 1f)] public float extend;

    [Header("Joints")]
    public Transform tower;
    public Transform slew;
    public Transform boom0;
    public Transform boom1;
    public Transform boom2;
    public Transform grab;
    public Transform magnet;

    [Header("Ports")]
    public ConnectionPoint pickup;
    public ConnectionPoint dropOff;

    Phase phase = Phase.Idle;
    float targetYaw;
    float targetExtend;
    float pickupYaw, pickupExtend;
    float dropYaw, dropExtend;
    float boomY;
    float liftY;
    Package held;
    Conveyor boundDropBelt;

    void Awake()
    {
        boomY = InserterConfig.BoomHeight;
        liftY = boomY + ConveyorConfig.GuardRailHeight + 0.06f;
        ApplyPose();
    }

    void OnEnable()
    {
        BindPad(pickup, true);
        BindPad(dropOff, true);
        BindDropBelt(true);
    }

    void OnDisable()
    {
        BindPad(pickup, false);
        BindPad(dropOff, false);
        BindDropBelt(false);
    }

    void Start()
    {
        BindDropBelt(true);
        CacheSockets();
        if (pickup != null)
            BeginPickup();
    }

    void LateUpdate()
    {
        Tick(Time.deltaTime);
        ApplyPose();
    }

    public void Connect(ConnectionPoint pick, ConnectionPoint drop)
    {
        BindPad(pickup, false);
        BindPad(dropOff, false);
        BindDropBelt(false);

        pickup  = pick;
        dropOff = drop;

        BindPad(pickup, true);
        BindPad(dropOff, true);
        BindDropBelt(true);
        CacheSockets();

        if (pickup != null)
            BeginPickup();
    }

    void CacheSockets()
    {
        CacheSocketPose(pickup,  out pickupYaw, out pickupExtend);
        CacheSocketPose(dropOff, out dropYaw,   out dropExtend);
    }

    void CacheSocketPose(ConnectionPoint point, out float yawDeg, out float ext)
    {
        yawDeg = 0f;
        ext = 0f;
        if (point == null) return;
        ComputeTarget(point, out yawDeg, out ext);
    }

    public void BeginPickup()
    {
        if (pickup == null) return;
        targetYaw    = pickupYaw;
        targetExtend = pickupExtend;
        phase = Phase.Retract;
    }

    public Conveyor PickupBelt  =>
        pickup  != null ? pickup.GetComponentInParent<Conveyor>()  : null;

    public Conveyor DropOffBelt =>
        dropOff != null ? dropOff.GetComponentInParent<Conveyor>() : null;

    void BindPad(ConnectionPoint pad, bool on)
    {
        if (pad == null) return;

        if (pad == pickup)
        {
            if (on) pad.PackageEntered += OnPickupEntered;
            else    pad.PackageEntered -= OnPickupEntered;
        }

        if (pad == dropOff)
        {
            if (on)
            {
                pad.PackageEntered += OnDropEntered;
                pad.PackageExited  += OnDropExited;
            }
            else
            {
                pad.PackageEntered -= OnDropEntered;
                pad.PackageExited  -= OnDropExited;
            }
        }
    }

    void BindDropBelt(bool on)
    {
        if (boundDropBelt != null)
            boundDropBelt.RidersChanged -= OnDropRidersChanged;

        boundDropBelt = on ? DropOffBelt : null;

        if (boundDropBelt != null)
            boundDropBelt.RidersChanged += OnDropRidersChanged;
    }

    void OnPickupEntered(Package pkg)
    {
        if (phase != Phase.Attach || pkg == null || pkg == held) return;
        Take(pkg);
        phase = Phase.Lift;
    }

    void OnDropEntered(Package pkg)
    {
        if (pkg == null || pkg == held) return;
        if (phase == Phase.ExtendDrop || phase == Phase.Release)
            phase = Phase.WaitDrop;
    }

    void OnDropExited(Package pkg)
    {
        TryResumeDrop();
    }

    void OnDropRidersChanged()
    {
        TryResumeDrop();
    }

    void TryResumeDrop()
    {
        if (phase != Phase.WaitDrop) return;
        if (DropPadClear())
            StartDropExtend();
    }

    bool DropPadClear()
    {
        if (dropOff == null) return false;

        Package occ = dropOff.Occupant;
        if (occ != null && occ != held)
            return false;

        Conveyor belt = DropOffBelt;
        if (belt == null)
            return occ == null || occ == held;

        float pad  = belt.PathLength * 0.5f;
        float need = PackageConfig.PackageSize * DropGap;
        return !BeltBlocked(belt, pad, need);
    }

    bool BeltBlocked(Conveyor belt, float dist, float need)
    {
        if (belt == null) return false;
        var list = belt.riders;
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            if (r == null || !r.enabled) continue;
            if (held != null && r.transform == held.transform) continue;
            if (Mathf.Abs(r.Distance - dist) < need)
                return true;
        }
        return false;
    }

    void StartDropExtend()
    {
        targetExtend = dropExtend;
        phase = Phase.ExtendDrop;
    }

    void Take(Package pkg)
    {
        var rider = pkg.GetComponent<PackageRider>();
        if (rider != null)
            rider.Detach();

        var col = pkg.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        held = pkg;
        held.transform.SetParent(grab, true);
        held.transform.localRotation = Quaternion.identity;
        held.transform.localPosition = new Vector3(
            0f,
            0f,
            InserterConfig.MagnetHeight + PackageConfig.HalfPackageSize);
    }

    void PutDown()
    {
        if (held == null) return;
        if (!DropPadClear())
        {
            phase = Phase.WaitDrop;
            return;
        }

        var pkg = held;
        held = null;
        pkg.transform.SetParent(null, true);

        var col = pkg.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Conveyor belt = DropOffBelt;
        var rider = pkg.GetComponent<PackageRider>();
        if (rider == null)
            rider = pkg.gameObject.AddComponent<PackageRider>();

        if (belt != null)
        {
            rider.enabled = true;
            rider.Attach(belt, belt.PathLength * 0.5f);
        }
        else
        {
            rider.Detach();
        }
    }

    void Tick(float dt)
    {
        switch (phase)
        {
            case Phase.Retract:
                extend = Mathf.MoveTowards(extend, 0f, InserterConfig.RetractSpeed * dt);
                boomY  = Mathf.MoveTowards(boomY, InserterConfig.BoomHeight, InserterConfig.LiftSpeed * dt);
                if (extend <= InserterConfig.ExtendArrive)
                {
                    extend = 0f;
                    if (pickup != null)
                    {
                        targetYaw    = pickupYaw;
                        targetExtend = pickupExtend;
                        phase = Phase.SlewPickup;
                    }
                    else
                    {
                        phase = Phase.Idle;
                    }
                }
                break;

            case Phase.SlewPickup:
                yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, InserterConfig.SlewSpeed * dt);
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) <= InserterConfig.YawArrive)
                {
                    yaw   = targetYaw;
                    phase = Phase.Extend;
                }
                break;

            case Phase.Extend:
                extend = Mathf.MoveTowards(extend, targetExtend, InserterConfig.ExtendSpeed * dt);
                if (Mathf.Abs(extend - targetExtend) <= InserterConfig.ExtendArrive)
                {
                    extend = targetExtend;
                    phase  = Phase.Attach;
                    if (pickup != null && pickup.Occupant != null && pickup.Occupant != held)
                        OnPickupEntered(pickup.Occupant);
                }
                break;

            case Phase.Attach:
                break;

            case Phase.Lift:
                boomY = Mathf.MoveTowards(boomY, liftY, InserterConfig.LiftSpeed * dt);
                if (Mathf.Abs(boomY - liftY) <= 0.002f)
                {
                    boomY = liftY;
                    phase = Phase.RetractLoaded;
                }
                break;

            case Phase.RetractLoaded:
                extend = Mathf.MoveTowards(extend, 0f, InserterConfig.RetractSpeed * dt);
                if (extend <= InserterConfig.ExtendArrive)
                {
                    extend = 0f;
                    if (dropOff != null)
                    {
                        targetYaw = dropYaw;
                        phase = Phase.SlewDrop;
                    }
                    else
                    {
                        phase = Phase.Idle;
                    }
                }
                break;

            case Phase.SlewDrop:
                yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, InserterConfig.SlewSpeed * dt);
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) <= InserterConfig.YawArrive)
                {
                    yaw   = targetYaw;
                    phase = Phase.WaitDrop;
                    if (DropPadClear())
                        StartDropExtend();
                }
                break;

            case Phase.WaitDrop:
                break;

            case Phase.ExtendDrop:
                if (!DropPadClear())
                {
                    phase = Phase.WaitDrop;
                    break;
                }
                extend = Mathf.MoveTowards(extend, targetExtend, InserterConfig.ExtendSpeed * dt);
                if (Mathf.Abs(extend - targetExtend) <= InserterConfig.ExtendArrive)
                {
                    extend = targetExtend;
                    phase  = Phase.Release;
                }
                break;

            case Phase.Release:
                if (!DropPadClear())
                {
                    phase = Phase.WaitDrop;
                    break;
                }
                PutDown();
                if (phase == Phase.WaitDrop)
                    break;
                phase = Phase.Retract;
                break;
        }
    }

    void ComputeTarget(ConnectionPoint point, out float yawDeg, out float ext)
    {
        Vector3 from = slew != null ? slew.position : transform.position;
        Vector3 to   = point.transform.position;
        Vector3 d    = to - from;
        d.y = 0f;

        yawDeg = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        float side = PackageConfig.HalfPackageSize + InserterConfig.MagnetHeight * 0.5f;
        ext = ExtendForDistance(Mathf.Max(0f, d.magnitude - side));
    }

    static float SlideMax(float parentLen, float selfLen) =>
        Mathf.Max(0f, (parentLen + selfLen) * 0.5f - InserterConfig.BoomOverlap);

    static float GrabReach(float t)
    {
        float s1 = SlideMax(InserterConfig.Boom0Size.z, InserterConfig.Boom1Size.z) * t;
        float s2 = SlideMax(InserterConfig.Boom1Size.z, InserterConfig.Boom2Size.z) * t;
        return InserterConfig.Boom0Size.z * 0.5f
             + s1 + s2
             + InserterConfig.Boom2Size.z * 0.5f
             + InserterConfig.MagnetHeight * 0.5f;
    }

    static float ExtendForDistance(float dist)
    {
        float min = GrabReach(0f);
        float max = GrabReach(1f);
        if (dist <= min) return 0f;
        if (dist >= max) return 1f;
        return (dist - min) / (max - min);
    }

    public void ApplyPose()
    {
        if (slew != null)
        {
            slew.localPosition = new Vector3(0f, boomY, 0f);
            slew.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        if (tower != null)
        {
            float shaft = Mathf.Max(0.05f, boomY - InserterConfig.BaseSize.y);
            tower.localPosition = new Vector3(0f, InserterConfig.BaseSize.y + shaft * 0.5f, 0f);
            float rest = InserterConfig.TowerSize.y;
            tower.localScale = new Vector3(1f, shaft / Mathf.Max(0.0001f, rest), 1f);
        }

        float t = Mathf.Clamp01(extend);
        Slide(boom1, InserterConfig.Boom0Size.z, InserterConfig.Boom1Size.z, t);
        Slide(boom2, InserterConfig.Boom1Size.z, InserterConfig.Boom2Size.z, t);
    }

    static void Slide(Transform section, float parentLen, float selfLen, float t)
    {
        if (section == null) return;
        section.localPosition = new Vector3(0f, 0f, t * SlideMax(parentLen, selfLen));
    }
}