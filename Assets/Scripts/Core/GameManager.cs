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
