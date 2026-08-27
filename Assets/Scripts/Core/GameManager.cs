using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Debug")]
    public bool clearExistingOnStart = true;

    private void Start()
    {
        if (PrefabManager.Instance == null)
            gameObject.AddComponent<PrefabManager>();

        if (ConveyorManager.Instance == null)
            gameObject.AddComponent<ConveyorManager>();

        SpawnGround();

        // Example: two belts in a line, should link and drop the shared endcaps
        ConveyorManager.Instance.PlaceStraight(new Vector2Int(0, 0), yRotation: 0f);
        ConveyorManager.Instance.PlaceStraight(new Vector2Int(0, 1), yRotation: 0f);
        
        // Vertical in the XZ plane = travel along +X (yRot 90)
        ConveyorManager.Instance.PlaceStraight(new Vector2Int(2, 0), yRotation: 90f);
        
        ConveyorManager.Instance.PlaceStraight(new Vector2Int(-3, 0), 0f);
        ConveyorManager.Instance. PlaceCorner(new Vector2Int(-3, 1), 0f);
        ConveyorManager.Instance. PlaceStraight(new Vector2Int(-2, 1), 90f);
    
    }
    
    public void SpawnGround()
    {
        if (PrefabManager.Instance == null || PrefabManager.Instance.groundPrefab == null)
            return;

        // Avoid duplicates
        if (GameObject.Find("Ground") != null)
            return;

        var go = Instantiate(
            PrefabManager.Instance.groundPrefab,
            new Vector3(0f, GroundConfig.GroundY, 0f),
            Quaternion.identity);
        go.name = "Ground";
    }

  
}
