using UnityEngine;

public class Inserter : Placeable
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
    
    const float LiftArrive = 0.002f;

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
    Conveyor pickupBelt;
    Conveyor dropBelt;
    Container pickupBox;
    Container dropBox;

    public Conveyor PickupBelt  => pickupBelt;
    public Conveyor DropOffBelt => dropBelt;
    
    Vector3 HoldLocal => new Vector3(
        0f, 0f,
        InserterConfig.MagnetHeight + PackageConfig.HalfPackageSize);
    
    bool InGrabRange(Package pkg)
    {
        if (pkg == null || grab == null) return false;
        Vector3 dest = grab.TransformPoint(HoldLocal);
        Vector3 p = pkg.transform.position;
        dest.y = p.y;
        float arrive = PackageConfig.HalfPackageSize * 0.35f;
        return (p - dest).sqrMagnitude <= arrive * arrive;
    }

    void Awake()
    {
        boomY = InserterConfig.BoomHeight;
        liftY = boomY + ConveyorConfig.GuardRailHeight + 0.06f;
        ApplyPose();
    }

    void OnEnable()
    {
        BindPads(true);
        CacheSockets();
    }

    void OnDisable()
    {
        BindPads(false);
    }

    void Start()
    {
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
        BindPads(false);
        pickup  = pick;
        dropOff = drop;
        BindPads(true);
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

    void BindPads(bool on)
    {
        BindPickupPad(on);
        BindDropPad(on);
        BindSockets(on);
    }

    void BindPickupPad(bool on)
    {
        if (pickup == null) return;
        if (on) pickup.PackageEntered += OnPickupEntered;
        else    pickup.PackageEntered -= OnPickupEntered;
    }

    void BindDropPad(bool on)
    {
        if (dropOff == null) return;
        if (on)
        {
            dropOff.PackageEntered += OnDropEntered;
            dropOff.PackageExited  += OnDropExited;
        }
        else
        {
            dropOff.PackageEntered -= OnDropEntered;
            dropOff.PackageExited  -= OnDropExited;
        }
    }

    void BindSockets(bool on)
    {
        if (dropBelt != null)
            dropBelt.RidersChanged -= OnDropRidersChanged;
        if (dropBox != null)
            dropBox.ContentsChanged -= OnDropContentsChanged;

        pickupBelt = pickup  != null ? pickup.GetComponentInParent<Conveyor>()  : null;
        pickupBox  = pickup  != null ? pickup.GetComponentInParent<Container>() : null;
        dropBelt   = on && dropOff != null ? dropOff.GetComponentInParent<Conveyor>()  : null;
        dropBox    = on && dropOff != null ? dropOff.GetComponentInParent<Container>() : null;

        if (dropBelt != null)
            dropBelt.RidersChanged += OnDropRidersChanged;
        if (dropBox != null)
            dropBox.ContentsChanged += OnDropContentsChanged;
    }
    

    void OnDropEntered(Package pkg)
    {
        if (pkg == null || pkg == held) return;
        if (dropBox != null) return;
        if (dropBelt != null)
        {
            var rider = pkg.GetComponent<PackageRider>();
            if (rider == null || rider.Current != dropBelt) return;
        }
        if (phase == Phase.ExtendDrop || phase == Phase.Release)
            phase = Phase.WaitDrop;
    }

    void OnDropExited(Package pkg)   => TryResumeDrop();
    void OnDropRidersChanged()       => TryResumeDrop();
    void OnDropContentsChanged()     => TryResumeDrop();

    void TryResumeDrop()
    {
        if (phase != Phase.WaitDrop) return;
        if (DropPadClear())
            StartDropExtend();
    }

    bool DropPadClear()
    {
        if (dropBox != null)
            return !dropBox.IsFull;
        if (dropBelt == null)
            return dropOff == null || dropOff.Occupant == null || dropOff.Occupant == held;

        float pad  = dropBelt.PathLength * 0.5f;
        float need = PackageConfig.MinSpacing;
        return !BeltBlocked(dropBelt, pad, need);
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
    
    void OnPickupEntered(Package pkg)
    {
        if (phase != Phase.Attach) return;
        TryGrab();
    }

    bool CanTake(Package pkg)
    {
        if (pkg == null || pkg == held) return false;
        if (pickupBox != null) return false;
        if (pickupBelt == null) return false;

        var rider = pkg.GetComponent<PackageRider>();
        return rider != null && rider.enabled && rider.Current == pickupBelt;
    }

    void TryGrab()
    {
        if (held != null || phase != Phase.Attach) return;

        if (pickupBox != null)
        {
            if (pickupBox.TryExtract(out Package fromBox))
            {
                Take(fromBox);
                phase = Phase.Lift;
            }
            return;
        }

        Package cand = NearestRiderPackage(pickupBelt);
        if (cand == null || !CanTake(cand) || !InGrabRange(cand))
            return;

        Take(cand);
        phase = Phase.Lift;
    }

    static Package NearestRiderPackage(Conveyor belt)
    {
        if (belt == null) return null;

        float pad = belt.PathLength * 0.5f;
        PackageRider best = null;
        float bestAbs = float.MaxValue;
        var list = belt.riders;

        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            if (r == null || !r.enabled) continue;

            float d = Mathf.Abs(r.Distance - pad);
            if (d < bestAbs)
            {
                bestAbs = d;
                best = r;
            }
        }

        return best != null ? best.Package : null;
    }

  

    void Take(Package pkg)
    {
        var rider = pkg.GetComponent<PackageRider>();
        if (rider != null)
            rider.Detach();

        var col = pkg.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        held = pkg;
        held.gameObject.SetActive(true);
        held.transform.SetParent(grab, true);
        held.transform.localRotation = Quaternion.identity;
        held.transform.localPosition = new Vector3(
            0f, 0f,
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

        if (dropBox != null)
        {
            if (!dropBox.TryInsert(pkg))
            {
                phase = Phase.WaitDrop;
                return;   // still parented to grab, collider still off
            }
            held = null;
            return;
        }

        held = null;
        pkg.transform.SetParent(null, true);

        var col = pkg.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        var rider = pkg.GetComponent<PackageRider>();
        if (rider == null)
            rider = pkg.gameObject.AddComponent<PackageRider>();

        if (dropBelt != null)
        {
            rider.enabled = true;
            rider.Attach(dropBelt, dropBelt.PathLength * 0.5f);
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
                boomY = Mathf.MoveTowards(boomY, InserterConfig.BoomHeight, InserterConfig.LiftSpeed * dt);
                if (!MoveExtend(0f, InserterConfig.RetractSpeed, dt)) break;
                if (pickup == null) { phase = Phase.Idle; break; }
                targetYaw    = pickupYaw;
                targetExtend = pickupExtend;
                phase = Phase.SlewPickup;
                break;

            case Phase.SlewPickup:
                if (!MoveYaw(targetYaw, dt)) break;
                phase = Phase.Extend;
                break;

            case Phase.Extend:
                if (!MoveExtend(targetExtend, InserterConfig.ExtendSpeed, dt)) break;
                phase = Phase.Attach;
                TryGrab();
                break;

            case Phase.Attach:
                TryGrab();
                break;

            case Phase.Lift:
                boomY = Mathf.MoveTowards(boomY, liftY, InserterConfig.LiftSpeed * dt);
                if (Mathf.Abs(boomY - liftY) > LiftArrive) break;
                boomY = liftY;
                phase = Phase.RetractLoaded;
                break;

            case Phase.RetractLoaded:
                if (!MoveExtend(0f, InserterConfig.RetractSpeed, dt)) break;
                if (dropOff == null) { phase = Phase.Idle; break; }
                targetYaw = dropYaw;
                phase = Phase.SlewDrop;
                break;

            case Phase.SlewDrop:
                if (!MoveYaw(targetYaw, dt)) break;
                phase = Phase.WaitDrop;
                if (DropPadClear())
                    StartDropExtend();
                break;

            case Phase.WaitDrop:
                break;

            case Phase.ExtendDrop:
                if (!DropPadClear()) { phase = Phase.WaitDrop; break; }
                if (!MoveExtend(targetExtend, InserterConfig.ExtendSpeed, dt)) break;
                phase = Phase.Release;
                break;

            case Phase.Release:
                if (!DropPadClear()) { phase = Phase.WaitDrop; break; }
                PutDown();
                if (phase != Phase.WaitDrop)
                    phase = Phase.Retract;
                break;
        }
    }

    bool MoveYaw(float target, float dt)
    {
        yaw = Mathf.MoveTowardsAngle(yaw, target, InserterConfig.SlewSpeed * dt);
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, target)) > InserterConfig.YawArrive)
            return false;
        yaw = target;
        return true;
    }

    bool MoveExtend(float target, float speed, float dt)
    {
        extend = Mathf.MoveTowards(extend, target, speed * dt);
        if (Mathf.Abs(extend - target) > InserterConfig.ExtendArrive)
            return false;
        extend = target;
        return true;
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
        float boom0Z = Mathf.Lerp(
            InserterConfig.Boom0Nested,
            InserterConfig.Boom0Size.z * 0.5f,
            t);
        float s1 = SlideMax(InserterConfig.Boom0Size.z, InserterConfig.Boom1Size.z) * t;
        float s2 = SlideMax(InserterConfig.Boom1Size.z, InserterConfig.Boom2Size.z) * t;
        return boom0Z
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
        if (boom0 != null)
        {
            float nested = InserterConfig.Boom0Nested;
            float outZ   = InserterConfig.Boom0Size.z * 0.5f;
            boom0.localPosition = new Vector3(0f, 0f, Mathf.Lerp(nested, outZ, t));
        }
        Slide(boom1, InserterConfig.Boom0Size.z, InserterConfig.Boom1Size.z, t);
        Slide(boom2, InserterConfig.Boom1Size.z, InserterConfig.Boom2Size.z, t);
    }

    static void Slide(Transform section, float parentLen, float selfLen, float t)
    {
        if (section == null) return;
        section.localPosition = new Vector3(0f, 0f, t * SlideMax(parentLen, selfLen));
    }
}