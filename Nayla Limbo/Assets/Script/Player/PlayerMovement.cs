using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float deadZone = 0.01f;
    public Rigidbody2D rb;
    public Animator animator;
    Vector2 movement;

    // Simpan arah terakhir
    float lastX;
    float lastY;

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        if (movement.sqrMagnitude > deadZone * deadZone)
        {
            Vector2 lastDir = GetLastDirection(movement);
            animator.SetFloat("LastX", lastDir.x);
            animator.SetFloat("LastY", lastDir.y);
        }

    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    Vector2 GetLastDirection(Vector2 move)
    {
        if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            return new Vector2(Mathf.Sign(move.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(move.y));
    }
}
