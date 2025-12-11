using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    float moveSpeed = 5f;
    float turnSpeed = 4f;
    int moveInput = 0;
    private Rigidbody rb;

    // Jump variables
    float minJumpForce = 3.5f;
    float maxJumpForce = 7f;
    float maxJumpHoldTime = 2f;
    float jumpHoldTime = 0f;
    bool isChargingJump = false;

    public BoardGroundDetect boardGroundDetect;
    public TrickController trickController;
    public PlayerController playerController;

    public bool in_grind = false;
    
    public bool isResettingRotation = false;

    // Combo input buffer
    [Header("Trick Input Settings")]
    [Tooltip("Number of frames to allow combo input after starting a trick")]
    public int comboBufferFrames = 8;
    
    private float comboBufferTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        
        // Convert frames to seconds (assuming 60fps)
        comboBufferTime = comboBufferFrames / 60f;
    }

    void Update()
    {
        PushForward();
        trickController.isGrounded = boardGroundDetect.isGrounded;

        if (!trickController.isPerformingTrick || !boardGroundDetect.isGrounded) {
            Move(moveInput);
        }
        
        HandleManualTilt();
        HandleJump();
        HandleTrick();
        HandleGrind();

        if (Keyboard.current.aKey.isPressed) {
            moveInput = -1;
        } else if (Keyboard.current.dKey.isPressed) {
            moveInput = 1;
        } else {
            moveInput = 0;
        }
    }

    void HandleGrind()
    {
        if (in_grind) {
            // freeze x direction
            rb.constraints = RigidbodyConstraints.FreezePositionX;
        } else {
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    void HandleManualTilt()
    {
        
        if (Keyboard.current.wKey.isPressed) {
            boardGroundDetect.RaiseNose();
            boardGroundDetect.alignmentThreshold = 0.65f;

        } 
        else if (Keyboard.current.wKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetNose();
            boardGroundDetect.alignmentThreshold = 0.65f;

        }

        if (Keyboard.current.sKey.isPressed) {
            boardGroundDetect.RaiseTail();
            boardGroundDetect.alignmentThreshold = 0.4f;

        } 
        else if (Keyboard.current.sKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTail();
            boardGroundDetect.alignmentThreshold = 0.4f;
        }

        // if (Keyboard.current.qKey.isPressed && !boardGroundDetect.isGrounded) { // boardslide
        //     boardGroundDetect.TurnBoardFrontside();
        // } else if (Keyboard.current.qKey.wasReleasedThisFrame) {
        //     boardGroundDetect.ResetTurnBoardFrontside();
        // } else if (Keyboard.current.eKey.isPressed && !boardGroundDetect.isGrounded) { // boardslide
        //     boardGroundDetect.TurnBoardBackside();
        // } else if (Keyboard.current.eKey.wasReleasedThisFrame) {
        //     boardGroundDetect.ResetTurnBoardBackside();
        // }
    }


    void HandleJump()
    {
        if (!boardGroundDetect.isGrounded && !in_grind) return;
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame) {
            isChargingJump = true;
            jumpHoldTime = 0f;
        }

        if (isChargingJump && Keyboard.current.spaceKey.isPressed) {
            jumpHoldTime += Time.deltaTime;
            jumpHoldTime = Mathf.Min(jumpHoldTime, maxJumpHoldTime);
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame && isChargingJump) {            
            Jump();
            isChargingJump = false;
        }
    }

    void HandleTrick()
    {
        if (boardGroundDetect.isGrounded || in_grind) return;

        // If performing a trick, check if we're in the combo window
        if (trickController.isPerformingTrick)
        {
            // Only allow combo input during the buffer window
            if (trickController.IsInComboWindow(comboBufferTime))
            {
                int baseTrick = trickController.GetCurrentTrick();
                
                // Check for combo inputs based on what trick is already started
                if (baseTrick == 0) // Kickflip is active (J pressed)
                {
                    if (Keyboard.current.kKey.isPressed) {
                        trickController.UpgradeToCombo(3); // J+K = Varial Kickflip
                    }
                }
                else if (baseTrick == 1) // Shuvit is active (K pressed)
                {
                    if (Keyboard.current.jKey.isPressed) {
                        trickController.UpgradeToCombo(3); // K+J = Varial Kickflip
                    }
                    else if (Keyboard.current.lKey.isPressed) {
                        trickController.UpgradeToCombo(4); // K+L = Varial Heelflip
                    }
                }
                else if (baseTrick == 2) // Heelflip is active (L pressed)
                {
                    if (Keyboard.current.kKey.isPressed) {
                        trickController.UpgradeToCombo(4); // L+K = Varial Heelflip
                    }
                }
            }
            return; // Don't start new tricks while one is active
        }

        // Start single tricks when not performing any trick
        if (Keyboard.current.jKey.isPressed) {
            trickController.StartTrick(0); // kickflip
        }
        else if (Keyboard.current.kKey.isPressed) {
            trickController.StartTrick(1); // shuvit
        }
        else if (Keyboard.current.lKey.isPressed) {
            trickController.StartTrick(2); // heelflip
        }
        // Backup single key options (U and I keys for direct combo access)
        else if (Keyboard.current.uKey.isPressed) {
            trickController.StartTrick(3); // varial kickflip
        }
        else if (Keyboard.current.iKey.isPressed) {
            trickController.StartTrick(4); // varial heelflip
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