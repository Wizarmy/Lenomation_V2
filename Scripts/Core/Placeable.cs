using UnityEngine;

public class Placeable : MonoBehaviour
{
    public Vector2 gridPosition;

    Vector2Int footprint = Vector2Int.one;

    public virtual Vector2Int Footprint
    {
        get => footprint;
        set => footprint = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
    }

    public virtual bool OccupiesGrid => true;

    public Vector2Int Cell => CoreConfig.ToCell(gridPosition);

    public void SetGridPosition(Vector2Int cell) =>
        SetGridPosition(new Vector2(cell.x, cell.y));

    public void SetGridPosition(Vector2 position)
    {
        if (isActiveAndEnabled && OccupiesGrid)
            GridOccupancy.Unregister(this);

        gridPosition = position;

        int gx = Mathf.RoundToInt(position.x);
        int gz = Mathf.RoundToInt(position.y);
        int w  = Mathf.Max(1, Footprint.x);
        int h  = Mathf.Max(1, Footprint.y);

        transform.position = CoreConfig.CellCenter(gx, gz, GroundConfig.GroundY)
                             + new Vector3(
                                 (w - 1) * CoreConfig.TileSize * 0.5f,
                                 0f,
                                 (h - 1) * CoreConfig.TileSize * 0.5f);

        if (isActiveAndEnabled && OccupiesGrid)
            GridOccupancy.Register(this);
    }

    protected virtual void OnEnable()
    {
        if (OccupiesGrid)
            GridOccupancy.Register(this);
    }

    protected virtual void OnDisable()
    {
        if (OccupiesGrid)
            GridOccupancy.Unregister(this);
    }
}