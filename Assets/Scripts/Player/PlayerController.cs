using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    // These will hold the current input values
    private Vector2 moveInput;
    private bool interactPressed;

    // The actual Input Actions created in code
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction buildModeAction;   // optional – for later when you add building

    private void Awake()
    {
        // ========== CREATE THE ACTIONS ==========
        
        // Movement (WASD + Gamepad left stick)
        moveAction = new InputAction("Move", InputActionType.Value);
        
        // Keyboard composite (WASD)
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Gamepad stick
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Interact (E key or A / Cross button on gamepad)
        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonSouth");

        // Optional: Toggle build mode (B key or Y / Triangle)
        buildModeAction = new InputAction("BuildMode", InputActionType.Button);
        buildModeAction.AddBinding("<Keyboard>/b");
        buildModeAction.AddBinding("<Gamepad>/buttonNorth");

        // ========== ENABLE THEM ==========
        moveAction.Enable();
        interactAction.Enable();
        buildModeAction.Enable();

        // ========== SUBSCRIBE TO EVENTS ==========
        moveAction.performed += OnMove;
        moveAction.canceled  += OnMove;

        interactAction.performed += OnInteract;
        buildModeAction.performed += OnBuildMode;
    }

    // ========== INPUT CALLBACKS ==========
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        interactPressed = true;
        Debug.Log("Interact pressed!");   // Replace this later with real machine interaction
    }

    private void OnBuildMode(InputAction.CallbackContext context)
    {
        Debug.Log("Build mode toggled!"); // You will connect this later to your building system
    }

    // ========== MOVEMENT ==========
    private void Update()
    {
        // Simple movement on the XZ plane
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Move
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

            // Smoothly rotate the player to face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Reset the one-frame interact flag
        interactPressed = false;
    }

    // ========== CLEANUP ==========
    private void OnDestroy()
    {
        // Always disable and dispose when the object is destroyed
        moveAction.Disable();
        interactAction.Disable();
        buildModeAction.Disable();

        moveAction.Dispose();
        interactAction.Dispose();
        buildModeAction.Dispose();
    }

    // Public getters so other scripts can check input if needed
    public bool WasInteractPressed() => interactPressed;
    public Vector2 GetMoveInput() => moveInput;
}