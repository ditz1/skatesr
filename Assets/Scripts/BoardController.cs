using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    float moveSpeed = 5f;
    float turnSpeed = 4f;
    int moveInput = 0;
    private Rigidbody rb;
    float timer = 0f; // timer to track if we have to increase movespeed

    // Jump variables
    float minJumpForce = 5f;      // Force for a quick tap
    float maxJumpForce = 15f;     // Force for holding 2+ seconds
    float maxJumpHoldTime = 2f;   // Maximum hold time for max force
    float jumpHoldTime = 0f;      // Tracks how long W is held
    bool isChargingJump = false;  // Tracks if we're charging a jump
    public BoardGroundDetect boardGroundDetect;
    public bool got_hit = false;
    Transform hit_transform;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Make sure gravity is enabled on the Rigidbody
        rb.useGravity = true;
    }

    void Update()
    {
        if (got_hit) {
            return;
        }
        PushForward();
        HandleJump();

        if (Keyboard.current.aKey.isPressed) {
            moveInput = -1;
        } else if (Keyboard.current.dKey.isPressed) {
            moveInput = 1;
        } else {
            moveInput = 0;
        }
        
        Move(moveInput);
        
        timer += Time.deltaTime;
        if (timer > 3f) {
            moveSpeed *= 1.1f;
            timer = 0f;
        }
    }

    void HandleJump()
    {
        if (!boardGroundDetect.isGrounded) return;
        // Start charging when W is first pressed
        if (Keyboard.current.wKey.wasPressedThisFrame) {
            isChargingJump = true;
            jumpHoldTime = 0f;
        }

        // Increment hold time while W is held (capped at maxJumpHoldTime)
        if (isChargingJump && Keyboard.current.wKey.isPressed) {
            jumpHoldTime += Time.deltaTime;
            jumpHoldTime = Mathf.Min(jumpHoldTime, maxJumpHoldTime);
        }

        // Apply jump force when W is released
        if (Keyboard.current.wKey.wasReleasedThisFrame && isChargingJump) {
            Jump();
            isChargingJump = false;
        }
    }

    void Jump()
    {
        // Calculate force based on how long the key was held
        float normalizedHoldTime = jumpHoldTime / maxJumpHoldTime; // 0 to 1
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, normalizedHoldTime);
        
        // Apply upward force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y + (jumpForce * 0.5f), rb.linearVelocity.z);
        Debug.Log("linear velocity: " + rb.linearVelocity);
    }

    // this is so that the sphere will always continue to move forward
    void PushForward() {
        // Only set X and Z velocity, preserve Y for jumping and gravity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveSpeed);
    }

    void Move(int input){
        // Only set X and Z velocity, preserve Y for jumping and gravity
        rb.linearVelocity = new Vector3(input * turnSpeed, rb.linearVelocity.y, moveSpeed);
    }
}