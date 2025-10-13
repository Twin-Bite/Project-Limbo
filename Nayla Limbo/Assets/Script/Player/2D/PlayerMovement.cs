using UnityEngine;
using UnityEngine.InputSystem; // <- penting

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float deadZone = 0.01f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public VirtualJoystick joystick; // drag joystick disini lewat inspector

    private PlayerInputActions inputActions;
    private Vector2 movement;
    private Vector2 lastMove;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        // Input dari New Input System (keyboard/gamepad)
        Vector2 inputVector = inputActions.Player.Move.ReadValue<Vector2>();

        // Input dari joystick UI (prioritas kalau dipakai)
        Vector2 joyVector = joystick.Direction;

        if (joyVector.magnitude > deadZone)
            movement = joyVector;
        else
            movement = inputVector;

        // Normalisasi biar diagonal ga lebih cepat
        if (movement.magnitude > 1f)
            movement.Normalize();

        // Animator
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        // Idle arah terakhir
        if (movement.sqrMagnitude > deadZone)
        {
            lastMove = movement;
            animator.SetFloat("LastX", lastMove.x);
            animator.SetFloat("LastY", lastMove.y);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}