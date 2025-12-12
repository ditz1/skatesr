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

    // Grind variables
    private List<Transform> grindPoints;
    private int currentGrindSegment = 0;
    private float grindSpeed = 5f;
    private float grindAlignSpeed = 10f;
    private float grindProgress = 0f;
    private float grindCooldown = 0f; 
    private float grindCooldownDuration = 0.10f;

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
        if (in_grind && grindPoints != null && grindPoints.Count >= 2)
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
        if (grindPoints == null || grindPoints.Count < 2 || currentGrindSegment >= grindPoints.Count - 1)
        {
            EndGrind();
            return;
        }

        Vector3 startPos = grindPoints[currentGrindSegment].position;
        Vector3 endPos = grindPoints[currentGrindSegment + 1].position;
        Vector3 railDirection = (endPos - startPos).normalized;
        float segmentLength = Vector3.Distance(startPos, endPos);

        // Check if we've reached the end of this segment
        if (grindProgress >= segmentLength)
        {
            // Move to next segment
            currentGrindSegment++;
            grindProgress = 0f;

            // Check if we've completed the entire rail
            if (currentGrindSegment >= grindPoints.Count - 1)
            {
                EndGrind();
                return;
            }

            // Update for next segment
            startPos = grindPoints[currentGrindSegment].position;
            endPos = grindPoints[currentGrindSegment + 1].position;
            railDirection = (endPos - startPos).normalized;
            segmentLength = Vector3.Distance(startPos, endPos);
        }

        // Move along current segment
        grindProgress += grindSpeed * Time.deltaTime;
        grindProgress = Mathf.Clamp(grindProgress, 0, segmentLength);

        // Calculate position on the rail based on progress
        Vector3 newPosition = startPos + (railDirection * grindProgress);

        // Apply y offset
        transform.position = newPosition + new Vector3(0, 0.5f, 0);

        // Align board rotation with rail direction
        Quaternion targetRotation = Quaternion.LookRotation(railDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, grindAlignSpeed * Time.deltaTime);
    }

    public void StartGrind(List<Transform> points)
    {
        // Don't start grinding if we're in cooldown
        if (grindCooldown > 0)
        {
            return;
        }

        if (points == null || points.Count < 2)
        {
            Debug.LogWarning("Need at least 2 grind points to start grinding!");
            return;
        }

        in_grind = true;
        grindPoints = points;
        currentGrindSegment = 0;

        // Calculate initial progress along the first segment based on current position
        Vector3 startPos = points[0].position;
        Vector3 endPos = points[1].position;
        Vector3 railDirection = (endPos - startPos).normalized;

        Vector3 startToPlayer = transform.position - startPos;
        grindProgress = Mathf.Max(0, Vector3.Dot(startToPlayer, railDirection));

        Debug.Log("Started grinding! Segments: " + (points.Count - 1));
    }

    public void EndGrind()
    {
        in_grind = false;
        grindPoints = null;
        currentGrindSegment = 0;
        grindProgress = 0f;

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

        if ((Keyboard.current.qKey.isPressed && !boardGroundDetect.isManuallyTurning) && (!boardGroundDetect.isGrounded || in_grind)) { 
            boardGroundDetect.TurnBoardFrontside();
            boardGroundDetect.alignmentThreshold = 0.4f;
        } else if (Keyboard.current.qKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTurnBoardFrontside();
        } else if ((Keyboard.current.eKey.isPressed && !boardGroundDetect.isManuallyTurning) && (!boardGroundDetect.isGrounded || in_grind)) { 
            boardGroundDetect.TurnBoardBackside();
            boardGroundDetect.alignmentThreshold = 0.4f;
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