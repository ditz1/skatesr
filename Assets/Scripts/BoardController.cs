using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    float moveSpeed = 5f;
    float turnSpeed = 4f;
    int moveInput = 0;
    private Rigidbody rb;

    // Jump variables
    float minJumpForce = 5f;
    float maxJumpForce = 15f;
    float maxJumpHoldTime = 2f;
    float jumpHoldTime = 0f;
    bool isChargingJump = false;

    public BoardGroundDetect boardGroundDetect;
    public TrickController trickController;
    public PlayerController playerController;

    public bool got_hit = false;
    
    public bool isResettingRotation = false;
    

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
    }

    void Update()
    {
        PushForward();
        
        if (!trickController.isPerformingTrick || !boardGroundDetect.isGrounded) {
            Move(moveInput);
        }
        
        HandleJump();
        HandleTrick();

        if (Keyboard.current.aKey.isPressed) {
            moveInput = -1;
        } else if (Keyboard.current.dKey.isPressed) {
            moveInput = 1;
        } else {
            moveInput = 0;
        }

    }

    void HandleJump()
    {
        if (!boardGroundDetect.isGrounded) return;
        
        if (Keyboard.current.wKey.wasPressedThisFrame) {
            isChargingJump = true;
            jumpHoldTime = 0f;
        }

        if (isChargingJump && Keyboard.current.wKey.isPressed) {
            jumpHoldTime += Time.deltaTime;
            jumpHoldTime = Mathf.Min(jumpHoldTime, maxJumpHoldTime);
        }

        if (Keyboard.current.wKey.wasReleasedThisFrame && isChargingJump) {
            Jump();
            isChargingJump = false;
        }
    }

   


    void HandleTrick()
    {
        // Only air tricks when not grounded
        if (trickController.isPerformingTrick) return;
        
        if (Keyboard.current.leftShiftKey.isPressed){
            trickController.StartTrick(5);
        } else if (Keyboard.current.spaceKey.isPressed){
            trickController.StartTrick(6);
        }

        if (boardGroundDetect.isGrounded) return;
        if (Keyboard.current.jKey.isPressed) {
            trickController.StartTrick(0);
        } else if (Keyboard.current.kKey.isPressed) {
            trickController.StartTrick(1);
        } else if (Keyboard.current.lKey.isPressed) {
            trickController.StartTrick(2);
        } else if (Keyboard.current.uKey.isPressed) {
            trickController.StartTrick(3);
        } else if (Keyboard.current.iKey.isPressed) {
            trickController.StartTrick(4);
        }
    }

    void Jump()
    {
        float normalizedHoldTime = jumpHoldTime / maxJumpHoldTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, normalizedHoldTime);
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        Debug.Log("Jump force: " + jumpForce);
    }

    void PushForward() {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveSpeed);
    }

    void Move(int input){
        rb.linearVelocity = new Vector3(input * turnSpeed, rb.linearVelocity.y, moveSpeed);
    }

    
}