using UnityEngine;

public class Placeable : MonoBehaviour
{
    public Vector2 gridPosition;
    
    public void SetGridPosition(Vector2 position)
    {
        gridPosition = position;
        transform.position = new Vector3(0.5f+gridPosition.x, 0, gridPosition.y+0.5f);
    }
    
}
