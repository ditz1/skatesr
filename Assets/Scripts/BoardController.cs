using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    float moveSpeed = 5f;
    float turnSpeed = 4f;
    int moveInput = 0;
    private Rigidbody rb;
    private float gravity = -1.00f;
    float timer = 0f; // timer to track if we have to increase movespeed

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        PushForward();


        if (Keyboard.current.aKey.isPressed) {
            moveInput = -1;
        } else if (Keyboard.current.dKey.isPressed) {
            moveInput = 1;
        } else {
            moveInput = 0;
        }
        
        Move(moveInput);
        
        timer += Time.deltaTime;
        if (timer > 1f) {
            moveSpeed += 0.1f;
            timer = 0f;
        }
    }

    // this is so that the sphere will always continue to move forward
    void PushForward() {
        // add force in positive z direction
        // cannot use forward because it is a sphere
        rb.linearVelocity = new Vector3(0, gravity, 1) * moveSpeed;
    }

    void Move(int input){
        rb.linearVelocity = new Vector3(input * turnSpeed, gravity, moveSpeed);
    }

}