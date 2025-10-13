using UnityEngine;

public class Movement : MonoBehaviour
{
    private CharacterController controller;

    public float speed = 5f;

    public float turnSpeed = 180f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movDir;
        
        transform.Rotate(0, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0);
        movDir = transform.forward * Input.GetAxis("Vertical") * speed;
        
        controller.Move(movDir * Time.deltaTime - Vector3.up * 0.1f);
    }
}
