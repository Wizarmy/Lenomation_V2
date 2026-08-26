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
        
        SpawnGround();
        SpawnStraightBelt(new Vector2(0,0));
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

    void SpawnStraightBelt(Vector3 position, int beltLevel =1, float yRot =0f)
    {
        GameObject straightPrefab = PrefabManager.GetStraight(beltLevel);
        
        GameObject go = Instantiate(straightPrefab, position, Quaternion.Euler(0f, yRot, 0f));
        go.name = straightPrefab.name;

        var conv = go.GetComponent<Conveyor>();
        if (conv != null)
            conv.SetDirection(BeltDirection.AntiClockwise);
        conv.SetGridPosition(position);
    }

}
