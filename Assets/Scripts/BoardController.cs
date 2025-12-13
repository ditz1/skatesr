using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

    public bool in_manual = false;

   // Grind variables
    private Transform grindStart;
    private Transform grindEnd;
    private float grindSpeed = 5f;
    private float grindAlignSpeed = 10f;
    private float grindProgress = 0f;
    private float grindCooldown = 0f; 
    private float grindCooldownDuration = 0.025f;

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

        // Make sure TrickController has reference to BoardGroundDetect
        if (trickController != null && boardGroundDetect != null)
        {
            trickController.boardGroundDetect = boardGroundDetect;
        }
    }

    void Update()
    {
        // Tick down grind cooldown
        if (grindCooldown > 0)
        {
            grindCooldown -= Time.deltaTime;
        }

        // Only allow normal movement when NOT grinding
        if (!in_grind)
        {
            PushForward();

            if (!trickController.isPerformingTrick || !boardGroundDetect.isGrounded) {
                Move(moveInput);
            }
        }

        trickController.isGrounded = boardGroundDetect.isGrounded;

        HandleManualTilt();
        HandleJump();
        HandleTrick();
        HandleGrind(); // This handles the grinding movement

        if (Keyboard.current.aKey.isPressed) {
            moveInput = -1;
        } else if (Keyboard.current.dKey.isPressed) {
            moveInput = 1;
        } else {
            moveInput = 0;
        }
    }

    public bool IsGrindOnCooldown()
    {
        return grindCooldown > 0;
    }

    void HandleGrind()
    {
        if (in_grind && grindStart != null && grindEnd != null)
        {
            // Completely freeze the rigidbody - we'll handle all movement manually
            rb.constraints = RigidbodyConstraints.FreezeAll;
            FollowGrindRail();
        }
        else
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }
    
    void FollowGrindRail()
    {
        Vector3 startPos = grindStart.position;
        Vector3 endPos = grindEnd.position;
        Vector3 railDirection = (endPos - startPos).normalized;
        float railTotalLength = Vector3.Distance(startPos, endPos);
    
        // Check if we've reached the end
        if (grindProgress >= railTotalLength)
        {
            EndGrind();
            return;
        }
    
        // Simply increment progress - no recalculation from position
        grindProgress += grindSpeed * Time.deltaTime;
    
        // Clamp to rail length
        grindProgress = Mathf.Clamp(grindProgress, 0, railTotalLength);
    
        // Calculate position on the rail based on progress
        Vector3 newPosition = startPos + (railDirection * grindProgress);

        // Apply y offset
        newPosition += new Vector3(0, 0.5f, 0);

        transform.position = Vector3.Lerp(transform.position, newPosition, grindSpeed * Time.deltaTime * 2.0f);
    
        // Align board rotation with rail direction
        Quaternion targetRotation = Quaternion.LookRotation(railDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, grindAlignSpeed * Time.deltaTime);
    }
    
    public void StartGrind(Transform startPoint, Transform endPoint)
    {
        // Don't start grinding if we're in cooldown
        if (grindCooldown > 0)
        {
            return;
        }
    
        in_grind = true;
        grindStart = startPoint;
        grindEnd = endPoint;
    
        // Calculate initial progress along the rail based on current position
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 railDirection = (endPos - startPos).normalized;
    
        Vector3 startToPlayer = transform.position - startPos;
        grindProgress = Mathf.Max(0, Vector3.Dot(startToPlayer, railDirection));
    
        Debug.Log("Started grinding! Initial progress: " + grindProgress);
    }

    
    public void EndGrind()
    {
        in_grind = false;
        grindStart = null;
        grindEnd = null;
    
        // Unfreeze the rigidbody
        rb.constraints = RigidbodyConstraints.None;
    
        // Give a small forward velocity when exiting the grind
        rb.linearVelocity = transform.forward * moveSpeed;
    
        // Start cooldown to prevent immediate re-grind
        grindCooldown = grindCooldownDuration;
    
        Debug.Log("Ended grind!");
    }

    void HandleManualTilt()
    {
        float manual_tilt_threshold = 0.65f;
        float turn_tilt_threshold = 0.3f;
        // Nose manual
        if (Keyboard.current.wKey.isPressed) {
            boardGroundDetect.RaiseNose();
            boardGroundDetect.alignmentThreshold = manual_tilt_threshold;

            in_manual = true;
        } else if (Keyboard.current.wKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetNose();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;

            in_manual = false;
        }

        // Tail manual
        if (Keyboard.current.sKey.isPressed) {
            boardGroundDetect.RaiseTail();
            boardGroundDetect.alignmentThreshold = manual_tilt_threshold;

            in_manual = true;
        } else if (Keyboard.current.sKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTail();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;

            in_manual = false;
        }


        // Frontside Backside turns
        if ((Keyboard.current.qKey.isPressed && !boardGroundDetect.isManuallyTurning) && (!boardGroundDetect.isGrounded || in_grind)) { 
            boardGroundDetect.TurnBoardFrontside();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;
        } else if (Keyboard.current.qKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTurnBoardFrontside();
        } else if ((Keyboard.current.eKey.isPressed && !boardGroundDetect.isManuallyTurning) && (!boardGroundDetect.isGrounded || in_grind)) { 
            boardGroundDetect.TurnBoardBackside();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;
        } else if (Keyboard.current.eKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTurnBoardBackside();
        }
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
        // If grinding, end the grind first so rigidbody can move
        if (in_grind)
        {
            EndGrind();
        }

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