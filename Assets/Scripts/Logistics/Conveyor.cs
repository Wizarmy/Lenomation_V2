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

    /// <summary>Convenience; prefer pieceType for new code.</summary>
    public bool isCorner => pieceType == ConveyorPieceType.Corner;
    public bool isEndCap => pieceType == ConveyorPieceType.EndCap;

    [Header("Ports (null on EndCap)")]
    public Transform entryPoint;   // items enter here
    public Transform exitPoint;    // items leave toward nextConveyor

    public bool HasEntry => !isEndCap && entryPoint != null;
    public bool HasExit  => !isEndCap && exitPoint != null;

    [Header("Connection (auto-detected)")]
    public Conveyor nextConveyor;

    private float pathLength;
    private Transform cachedTransform;
    private bool arrowsNeedFlip = false;

    void Awake()
    {
        cachedTransform = transform;
        moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        maxItems  = ConveyorConfig.MaxItemsPerBelt;

        pathLength = pieceType switch
        {
            ConveyorPieceType.Corner  => ConveyorConfig.CornerPathLength,
            ConveyorPieceType.EndCap  => 0f,
            _                         => ConveyorConfig.StraightPathLength
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
    }

    // -------------------------------------------------
    // Direction & Arrows
    // -------------------------------------------------
    private void ApplyDirectionVisuals()
    {
        // Endcaps: no directional arrow flip needed
        if (isEndCap) return;

        // Corners: natural = Clockwise
        // Straights: natural = AntiClockwise
        bool shouldFlip = isCorner
            ? (direction == BeltDirection.AntiClockwise)
            : (direction == BeltDirection.Clockwise);

        if (shouldFlip != arrowsNeedFlip)
        {
            FlipAllArrows();
            arrowsNeedFlip = shouldFlip;
        }
    }

    private void FlipAllArrows()
    {
        foreach (Transform child in cachedTransform)
        {
            if (child.name.StartsWith("Arrow"))
                child.localRotation *= Quaternion.Euler(0f, 180f, 0f);
        }
    }

    // -------------------------------------------------
    // Linking helper (call from manager / spawner)
    // -------------------------------------------------
    public bool TrySetNext(Conveyor other)
    {
        if (!HasExit || other == null || !other.HasEntry)
            return false;

        nextConveyor = other;
        return true;
    }

    public void ClearNext()
    {
        nextConveyor = null;
    }
}