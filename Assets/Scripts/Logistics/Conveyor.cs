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
    public bool isCorner = false;
    public BeltDirection direction = BeltDirection.Clockwise;

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

        // Cache the real travel distance of this piece
        pathLength = isCorner 
            ? ConveyorConfig.CornerPathLength 
            : ConveyorConfig.StraightPathLength;
    }
    
    void Start()
    {
        ApplyDirectionVisuals();
    }
    
    public void SetDirection(BeltDirection newDirection)
    {
        direction = newDirection;
        ApplyDirectionVisuals();

      /*  // Tell the manager to update connections around this belt
        if (ConveyorManager.Instance != null)
            ConveyorManager.Instance.RebuildConnectionsAround(this);*/
    }

    // -------------------------------------------------
    // Direction & Arrows
    // -------------------------------------------------
    private void ApplyDirectionVisuals()
    {
        // Corners: natural = Clockwise
        // Straights: natural = AntiClockwise (opposite)
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
            {
                child.localRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
        }
    }
    
}
