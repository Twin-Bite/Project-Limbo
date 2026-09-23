using UnityEngine;

public class Movement : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    public float speed = 5f;
    public float turnSpeed = 180f;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector3 movDir;
        transform.Rotate(0, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0); //Kiri Kanan Input
        
        // Input W sama S
        float verticalInput = Input.GetAxis("Vertical");
        movDir = transform.forward * verticalInput * speed;

        controller.Move(movDir * Time.deltaTime - Vector3.up * 0.1f);
        if (animator != null)
        {
            animator.SetFloat("Speed", verticalInput);
        }
    }
}
