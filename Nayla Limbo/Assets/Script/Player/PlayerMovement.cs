using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float deadZone = 0.01f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public VirtualJoystick joystick; // assign kalau pakai joystick

    Vector2 movement;
    float lastX;
    float lastY;

    void Update()
    {
        // ===== Input Handling =====
        Vector2 inputVec;

        if (joystick != null) // kalau ada joystick, pakai itu
        {
            inputVec = joystick.Direction; // sudah normalized (-1..1)
        }
        else // fallback: keyboard (Editor / PC)
        {
            inputVec.x = Input.GetAxisRaw("Horizontal");
            inputVec.y = Input.GetAxisRaw("Vertical");
        }

        movement = inputVec;

        // ===== Animator Parameters =====
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        // Simpan arah terakhir untuk Idle
        if (movement.sqrMagnitude > deadZone * deadZone)
        {
            Vector2 last = (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
                ? new Vector2(Mathf.Sign(movement.x), 0f)
                : new Vector2(0f, Mathf.Sign(movement.y));

            lastX = last.x;
            lastY = last.y;

            animator.SetFloat("LastX", lastX);
            animator.SetFloat("LastY", lastY);
        }
    }

    void FixedUpdate()
    {
        // ===== Physics Movement =====
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
