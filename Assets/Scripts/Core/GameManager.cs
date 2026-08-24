using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Debug")]
    public bool clearExistingOnStart = true;

    void Start()
    {
        // Ensure core managers exist
        if (ConveyorManager.Instance == null)
            gameObject.AddComponent<ConveyorManager>();

        if (PrefabManager.Instance == null)
            gameObject.AddComponent<PrefabManager>();

        if (Spawner.Instance == null)
            gameObject.AddComponent<Spawner>();

        if (clearExistingOnStart)
            Spawner.Instance.ClearEverything();

        Spawner.Instance.SpawnEverything(loopOrigin: Vector3.zero, loopYRotation: 0f);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (ConveyorManager.Instance != null)
            {
                ConveyorManager.Instance.isRunning = !ConveyorManager.Instance.isRunning;
                Debug.Log(ConveyorManager.Instance.isRunning ? "Conveyors STARTED" : "Conveyors STOPPED");
            }
        }
    }
}