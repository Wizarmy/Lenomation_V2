using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;
    public float boostMultiplier = 3.5f;
    public float verticalSpeed = 10f;

    [Header("Look")]
    public float lookSensitivity = 0.15f;
    public float minPitch = -89f;
    public float maxPitch = 89f;

    // Internal
    private float pitch;
    private float yaw;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        // Cursor starts unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Only look while Right Mouse Button is held
        if (!mouse.rightButton.isPressed) return;

        Vector2 delta = mouse.delta.ReadValue();

        yaw   += delta.x * lookSensitivity;
        pitch -= delta.y * lookSensitivity;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 move = Vector3.zero;

        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.dKey.isPressed) move += transform.right;

        if (keyboard.eKey.isPressed) move += Vector3.up;
        if (keyboard.qKey.isPressed) move += Vector3.down;

        if (move.sqrMagnitude > 0f)
        {
            move.Normalize();

            float speed = moveSpeed;
            if (keyboard.leftShiftKey.isPressed)
                speed *= boostMultiplier;

            Vector3 horizontal = new Vector3(move.x, 0f, move.z);
            Vector3 vertical   = new Vector3(0f, move.y, 0f);

            transform.position += horizontal * speed * Time.deltaTime;
            transform.position += vertical   * verticalSpeed * Time.deltaTime;
        }
    }
}