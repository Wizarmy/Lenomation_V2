using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Placeable
{
    [Header("Settings")]
    public float moveSpeed = ConveyorConfig.DefaultMoveSpeed;
    public int maxItems = ConveyorConfig.MaxItemsPerBelt;

    [Header("Belt Level")]
    [Range(1, 5)]
    public int beltLevel = 1;

    [Header("Type & Direction")]
    public ConveyorPieceType pieceType = ConveyorPieceType.Straight;
    public BeltDirection direction = BeltDirection.Clockwise;

    public bool isCorner => pieceType == ConveyorPieceType.Corner;
    public bool isEndCap => pieceType == ConveyorPieceType.EndCap;

    [Header("Ports (null on EndCap)")]
    public Transform entryPoint;
    public Transform exitPoint;
    public ConnectionPoint connectionPoint;

    public bool HasEntry => !isEndCap && entryPoint != null;
    public bool HasExit  => !isEndCap && exitPoint != null;

    [Header("Connection (auto-detected)")]
    public Conveyor nextConveyor;
    public Conveyor previousConveyor;
    
    public readonly List<PackageRider> riders = new List<PackageRider>();

    float pathLength;
    Transform cachedTransform;
    bool arrowsNeedFlip;
    
    public event System.Action RidersChanged;

    public float PathLength => pathLength;

    public float PathDuration =>
        CoreConfig.TileSize / Mathf.Max(0.0001f, moveSpeed);

    void Awake()
    {
        cachedTransform = transform;
        moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        maxItems  = ConveyorConfig.MaxItemsPerBelt;
        RecalcPathLength();
    }

    void Start()
    {
        ApplyDirectionVisuals();
    }

    public void RecalcPathLength()
    {
        pathLength = pieceType switch
        {
            ConveyorPieceType.Corner => ConveyorConfig.CornerPathLength,
            ConveyorPieceType.EndCap => 0f,
            ConveyorPieceType.Link   => ConveyorConfig.LinkLength,
            _                        => StraightLengthWithLinks()
        };
    }

    float StraightLengthWithLinks()
    {
        float len = ConveyorConfig.StraightPathLength;
        if (nextConveyor     != null) len += ConveyorConfig.LinkLength;
        if (previousConveyor != null) len += ConveyorConfig.LinkLength;
        return len;
    }

    public void SetDirection(BeltDirection newDirection)
    {
        direction = newDirection;
        ApplyDirectionVisuals();

        if (ConveyorManager.Instance != null && !isEndCap)
            ConveyorManager.Instance.RebuildConnectionsAround(this);
    }

    // ------------------------------------------------------------------
    // Path eval  (distance along this piece, 0 = start of travel)
    // ------------------------------------------------------------------

    public Vector3 EvaluatePosition(float distance)
    {
        return EvaluatePosition01(Normalize(distance));
    }

    public Vector3 EvaluatePosition01(float t)
    {
        t = Mathf.Clamp01(t);
        if (isCorner) return EvaluateCorner(t);
        return EvaluateStraight(t);
    }

    public Vector3 EvaluateForward(float distance)
    {
        float t = Normalize(distance);
        const float eps = 0.02f;
        Vector3 a = EvaluatePosition01(Mathf.Max(0f, t - eps));
        Vector3 b = EvaluatePosition01(Mathf.Min(1f, t + eps));
        Vector3 dir = b - a;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-8f)
            dir = TravelWorldDir();
        return dir.normalized;
    }

    public Quaternion EvaluateRotation(float distance)
    {
        Vector3 fwd = EvaluateForward(distance);
        if (fwd.sqrMagnitude < 1e-8f)
            return cachedTransform != null ? cachedTransform.rotation : transform.rotation;
        return Quaternion.LookRotation(fwd, Vector3.up);
    }

    float Normalize(float distance)
    {
        if (pathLength <= 1e-6f) return 0f;
        return Mathf.Clamp01(distance / pathLength);
    }

    Vector3 EvaluateStraight(float t)
    {
        Transform self = cachedTransform != null ? cachedTransform : transform;
        Vector3 entry = entryPoint != null ? entryPoint.position : self.position;
        Vector3 exit  = exitPoint  != null ? exitPoint.position  : self.position;

        bool cw = direction == BeltDirection.Clockwise;
        Vector3 start = cw ? exit : entry;
        Vector3 end   = cw ? entry : exit;

        Vector3 travel = end - start;
        if (travel.sqrMagnitude < 1e-8f)
            travel = TravelWorldDir() * ConveyorConfig.StraightPathLength;

        Vector3 unit = travel.normalized;
        if (previousConveyor != null) start -= unit * ConveyorConfig.LinkLength;
        if (nextConveyor     != null) end   += unit * ConveyorConfig.LinkLength;

        return Vector3.Lerp(start, end, t);
    }

    Vector3 EvaluateCorner(float t)
    {
        // Local arc: centre SE (0.5, −0.5), in −Z, out +X.
        // Clockwise: π → π/2. AntiClockwise: π/2 → π.
        float midR = (ConveyorConfig.CornerInnerRadius + ConveyorConfig.CornerOuterRadius) * 0.5f;
        float angle = direction == BeltDirection.AntiClockwise
            ? Mathf.Lerp(Mathf.PI * 0.5f, Mathf.PI, t)
            : Mathf.Lerp(Mathf.PI, Mathf.PI * 0.5f, t);

        Vector3 local = new Vector3(
            0.5f + Mathf.Cos(angle) * midR,
            ConveyorConfig.BeltHeight,
            -0.5f + Mathf.Sin(angle) * midR);

        Transform self = cachedTransform != null ? cachedTransform : transform;
        return self.TransformPoint(local);
    }

    Vector3 TravelWorldDir()
    {
        Transform self = cachedTransform != null ? cachedTransform : transform;
        if (isCorner)
        {
            return direction == BeltDirection.AntiClockwise
                ? -self.forward
                :  self.right;
        }

        return direction == BeltDirection.Clockwise
            ? -self.forward
            :  self.forward;
    }

    void ApplyDirectionVisuals()
    {
        if (isEndCap) return;

        bool shouldFlip = isCorner
            ? (direction == BeltDirection.AntiClockwise)
            : (direction == BeltDirection.Clockwise);

        if (shouldFlip != arrowsNeedFlip)
        {
            FlipAllArrows();
            arrowsNeedFlip = shouldFlip;
        }
    }

    void FlipAllArrows()
    {
        foreach (Transform child in cachedTransform)
        {
            if (child.name.StartsWith("Arrow"))
                child.localRotation *= Quaternion.Euler(0f, 180f, 0f);
        }
    }
    
    public void RegisterRider(PackageRider rider)
    {
        if (rider != null && !riders.Contains(rider))
            riders.Add(rider);
        RidersChanged?.Invoke();
    }

    public void UnregisterRider(PackageRider rider)
    {
        if (riders.Remove(rider))
            RidersChanged?.Invoke();
    }
}