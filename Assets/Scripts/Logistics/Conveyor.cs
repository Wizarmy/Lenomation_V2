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

    public bool HasEntry => !isEndCap && entryPoint != null;
    public bool HasExit  => !isEndCap && exitPoint != null;

    [Header("Connection (auto-detected)")]
    public Conveyor nextConveyor;
    public Conveyor previousConveyor;

    float pathLength;
    Transform cachedTransform;
    bool arrowsNeedFlip;

    void Awake()
    {
        cachedTransform = transform;
        moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        maxItems  = ConveyorConfig.MaxItemsPerBelt;

        pathLength = pieceType switch
        {
            ConveyorPieceType.Corner => ConveyorConfig.CornerPathLength,
            ConveyorPieceType.EndCap => 0f,
            _                        => ConveyorConfig.StraightPathLength
        };
    }

    void Start()
    {
        ApplyDirectionVisuals();
    }

    public void SetDirection(BeltDirection newDirection)
    {
        direction = newDirection;
        ApplyDirectionVisuals();

        if (ConveyorManager.Instance != null && !isEndCap)
            ConveyorManager.Instance.RebuildConnectionsAround(this);
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
}