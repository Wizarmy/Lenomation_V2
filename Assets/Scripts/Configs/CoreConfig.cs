using UnityEngine;

public static class CoreConfig
{
    public const float TileSize = 1f;
    public const float DistanceFromTileEdge = 0.1f;

    public static Vector3 CellCenter(int gx, int gz, float y = 0f) =>
        new Vector3((gx + 0.5f) * TileSize, y, (gz + 0.5f) * TileSize);

    public static Vector3 CellCenter(Vector2Int cell, float y = 0f) =>
        CellCenter(cell.x, cell.y, y);

    public static Vector2Int WorldToCell(Vector3 world) =>
        new Vector2Int(
            Mathf.FloorToInt(world.x / TileSize),
            Mathf.FloorToInt(world.z / TileSize));

    public static Vector2Int ToCell(Vector2 gridPosition) =>
        new Vector2Int(
            Mathf.RoundToInt(gridPosition.x),
            Mathf.RoundToInt(gridPosition.y));
}