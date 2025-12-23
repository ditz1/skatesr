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
    float minJumpForce = 4.0f;
    float maxJumpForce = 8.0f;
    float maxJumpHoldTime = 2f;
    float jumpHoldTime = 0f;
    bool isChargingJump = false;

    public BoardGroundDetect boardGroundDetect;
    public TrickController trickController;
    public PlayerController playerController;
    public Animator animator;

    public bool in_grind = false;
    
    public bool isResettingRotation = false;

    public bool in_manual = false;

    [Header("180 Turn Settings")]
    public float turn180Duration = 0.35f;
    public bool isTurning180 = false;
    float turn180StartTime;
    float turn180AngleRemaining;
    float turn180Direction = 1f;
    bool lockTurnInput = false;
    bool rotateBoardThisTurn = true;
    bool manualTurnStateBefore180 = false;
    bool pendingScaleFlip = false;

    // Grind variables
    private Transform grindStart;
    private Transform grindEnd;
    private float grindSpeed = 5f;
    private float grindAlignSpeed = 10f;
    private float grindProgress = 0f;
    private float grindCooldown = 0f; 
    private float grindCooldownDuration = 0.025f;


    // Grind State Trackers
    // basically just need to track if the player is trying to boardslide
    // and dont want to go through and calculate y rotation so do it by input
    int tweaked_y_rot = 0;
    int tweaked_x_rot = 0;



    public bool is_dead = false;
    Vector3 last_wall_hit;
    int wall_hit_frames = 20;
    int buffer_frames = 50;

    public bool can_play = false;
    

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
        if (!can_play) return;

        if (is_dead)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RespawnPlayer();
            }
            return;
        }

        // skateidle should only play if there is not any other animation playing
        //if ()

        // Tick down grind cooldown
        if (grindCooldown > 0)
        {
            grindCooldown -= Time.deltaTime;
        }

        Handle180TurnInput();
        Update180Turn();
        ApplyPendingScaleFlip();

        // Only allow normal movement when NOT grinding
        if (!in_grind)
        {
            PushForward();

            if (!trickController.isPerformingTrick || !boardGroundDetect.isGrounded) {
                Move(moveInput);
            }
        }

        TiltBoardOnJump();

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


        CheckForStoppageForward();
    }

    void Handle180TurnInput()
    {
        if (boardGroundDetect != null && boardGroundDetect.isGrounded) return;
        if (in_grind) return;
        if (isTurning180) return;

        // Reset input lock once keys are released
        if (!Keyboard.current.zKey.isPressed && !Keyboard.current.cKey.isPressed)
        {
            lockTurnInput = false;
        }

        if (lockTurnInput) return;

        if (Keyboard.current.zKey.isPressed)
        {
            Start180Turn(-180f);
            lockTurnInput = true;
        }
        else if (Keyboard.current.cKey.isPressed)
        {
            Start180Turn(180f);
            lockTurnInput = true;
        }
    }

    void Start180Turn(float direction)
    {
        if (isTurning180) return;

        isTurning180 = true;
        rotateBoardThisTurn = trickController == null || !trickController.isPerformingTrick;
        turn180StartTime = Time.time;
        turn180AngleRemaining = direction;
        turn180Direction = Mathf.Sign(direction);

        if (boardGroundDetect != null)
        {
            manualTurnStateBefore180 = boardGroundDetect.isManuallyTurning;
            boardGroundDetect.isManuallyTurning = true;
        }

        if (playerController != null)
        {
            playerController.Start180Turn(direction, turn180Duration);
        }
    }

    void Update180Turn()
    {
        if (!isTurning180) return;

        float normalized = Mathf.Clamp01((Time.time - turn180StartTime) / turn180Duration);
        float degreesPerSecond = 180f / turn180Duration;
        float step = turn180Direction * degreesPerSecond * Time.deltaTime;

        // Clamp so we never overshoot
        if (Mathf.Abs(step) > Mathf.Abs(turn180AngleRemaining))
        {
            step = turn180AngleRemaining;
        }

        if (rotateBoardThisTurn)
        {
            transform.Rotate(0f, step, 0f, Space.World);
        }

        // if (playerController.skater_mesh_transform.rotation.y > 0){
        //     playerController.skater_mesh_transform.localPosition = new Vector3(-0.2f, playerController.skater_mesh_transform.localPosition.y, playerController.skater_mesh_transform.localPosition.z);
        // } else {
        //     playerController.skater_mesh_transform.localPosition = new Vector3(0.2f, playerController.skater_mesh_transform.localPosition.y, playerController.skater_mesh_transform.localPosition.z);
        // }

        // Always count down even if the board isn't rotating (e.g., during tricks)
        turn180AngleRemaining -= step;

        if (normalized >= 1f || Mathf.Approximately(turn180AngleRemaining, 0f))
        {
            Finish180Turn();
        }
    }

    void Finish180Turn()
    {
        pendingScaleFlip = true;

        if (boardGroundDetect != null)
        {
            boardGroundDetect.isManuallyTurning = manualTurnStateBefore180;
        }

        turn180AngleRemaining = 0f;
        isTurning180 = false;

        if (playerController.YawIsPositive()) {
            Vector3 scale = new Vector3(-1f, 1f, 1f);
            playerController.skater_mesh_transform.localScale = scale;
        } else {
            Vector3 scale = new Vector3(1f, 1f, 1f);
            playerController.skater_mesh_transform.localScale = scale;
        }
    }

    void ApplyPendingScaleFlip()
    {
        if (!pendingScaleFlip) return;
        if (boardGroundDetect == null) return;
        if (!boardGroundDetect.isGrounded) return;

        Transform scaleTarget = playerController != null && playerController.skater_mesh_transform != null
            ? playerController.skater_mesh_transform
            : transform;
        
        pendingScaleFlip = false;
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

    void QueueGrindType()
    {
        /*
        -1 -1
        -1  1
        -1  0
         1 -1
         1  1
         1  0
         0 -1
         0  1
         0  0
        */
        if (tweaked_y_rot == -1 && tweaked_x_rot == -1) {
            trickController.hudManager.AddTrickToQueue(10); // tailslide
        } else if (tweaked_y_rot == -1 && tweaked_x_rot == 1) {
            trickController.hudManager.AddTrickToQueue(11); // noseslide
        } else if (tweaked_y_rot == -1 && tweaked_x_rot == 0) {
            trickController.hudManager.AddTrickToQueue(7); // back board

        } else if (tweaked_y_rot == 1 && tweaked_x_rot == 0) {
            trickController.hudManager.AddTrickToQueue(6); // front board
        } else if (tweaked_y_rot == 1 && tweaked_x_rot == -1) {
            trickController.hudManager.AddTrickToQueue(7); // back board
        } else if (tweaked_y_rot == 1 && tweaked_x_rot == 1) {
            trickController.hudManager.AddTrickToQueue(10); // tailslide

        } else if (tweaked_y_rot == 0 && tweaked_x_rot == -1) {
            trickController.hudManager.AddTrickToQueue(9); // 5-0
        } else if (tweaked_y_rot == 0 && tweaked_x_rot == 0) {
            trickController.hudManager.AddTrickToQueue(5); // nosegrind
        } else if (tweaked_y_rot == 0 && tweaked_x_rot == 1) {
            trickController.hudManager.AddTrickToQueue(8); // nosegrind
        }
        

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

        QueueGrindType();
    
        // Calculate initial progress along the rail based on current position
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 railDirection = (endPos - startPos).normalized;
    
        Vector3 startToPlayer = transform.position - startPos;
        grindProgress = Mathf.Max(0, Vector3.Dot(startToPlayer, railDirection));
    
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
    
    }

    void HandleManualTilt()
    {
        float manual_tilt_threshold = 0.65f;
        float turn_tilt_threshold = 0.4f;
        boardGroundDetect.alignmentThreshold = 0.5f;
        // Nose manual
        if (Keyboard.current.wKey.isPressed) {
            boardGroundDetect.RaiseNose();
            boardGroundDetect.alignmentThreshold = manual_tilt_threshold;
            animator.Play("manual");
            tweaked_x_rot = 1;
            in_manual = true;
        } else if (Keyboard.current.wKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetNose();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;
            tweaked_x_rot = 0;
            in_manual = false;
        }

        // Tail manual
        if (Keyboard.current.sKey.isPressed) {
            boardGroundDetect.RaiseTail();
            boardGroundDetect.alignmentThreshold = manual_tilt_threshold;
            animator.Play("nosemanual");
            tweaked_x_rot = -1;
            in_manual = true;
        } else if (Keyboard.current.sKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTail();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;
            tweaked_x_rot = 0;
            in_manual = false;
        }


        // Frontside Backside turns
        if ((Keyboard.current.qKey.isPressed && !boardGroundDetect.isManuallyTurning) && (!boardGroundDetect.isGrounded || in_grind)) { 
            boardGroundDetect.TurnBoardFrontside();
            tweaked_y_rot = 1;
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;
        } else if (Keyboard.current.qKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTurnBoardFrontside();
            tweaked_y_rot = 0;
        } else if ((Keyboard.current.eKey.isPressed && !boardGroundDetect.isManuallyTurning) && (!boardGroundDetect.isGrounded || in_grind)) { 
            boardGroundDetect.TurnBoardBackside();
            boardGroundDetect.alignmentThreshold = turn_tilt_threshold;
            tweaked_y_rot = -1;
        } else if (Keyboard.current.eKey.wasReleasedThisFrame) {
            boardGroundDetect.ResetTurnBoardBackside();
            tweaked_y_rot = 0;
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
            animator.Play("kickflip");
        }
        else if (Keyboard.current.kKey.isPressed) {
            trickController.StartTrick(1); // shuvit
            animator.Play("shuvit");
        }
        else if (Keyboard.current.lKey.isPressed) {
            trickController.StartTrick(2); // heelflip
            animator.Play("kickflip");
        }
        // Backup single key options (U and I keys for direct combo access)
        else if (Keyboard.current.uKey.isPressed) {
            trickController.StartTrick(3); // varial kickflip
            animator.Play("shuvit");
        }
        else if (Keyboard.current.iKey.isPressed) {
            trickController.StartTrick(4); // varial heelflip
            animator.Play("shuvit");
        }
    }

    void Jump()
    {

        animator.Play("ollie");
        // If grinding, end the grind first so rigidbody can move
        if (in_grind)
        {
            EndGrind();
        }

        float normalizedHoldTime = jumpHoldTime / maxJumpHoldTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, normalizedHoldTime);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

   void TiltBoardOnJump() {
        // Reset immediately if performing a trick
        if (trickController.isPerformingTrick) {
            boardGroundDetect.ResetNose();
            return;
        }

        // Tilt up once at the start of jump
        if (rb.linearVelocity.y > 2.0f && !boardGroundDetect.isGrounded) {
            boardGroundDetect.RaiseNose();
        } 
        // Tilt down once when velocity drops OR when we land
        else if (rb.linearVelocity.y < 1.5f || boardGroundDetect.isGrounded) {
            boardGroundDetect.ResetNose();
        }
        
    }

    void PushForward() {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveSpeed);
    }

    void Move(int input){
        rb.linearVelocity = new Vector3(input * turnSpeed, rb.linearVelocity.y, moveSpeed);
    }

    void RespawnPlayer()
    {
        // Move the player slightly forward and up to avoid obstacles
        float forwardOffset = 2.5f;
        float heightOffset = 0.6f;

        Vector3 respawnPosition = transform.position + (transform.forward * forwardOffset) + (Vector3.up * heightOffset);

        in_grind = false;
        rb.constraints = RigidbodyConstraints.None;
        transform.position = respawnPosition;

        // Clear velocity so normal movement can resume cleanly
        rb.linearVelocity = Vector3.zero;

        // Reset tracking used for stuck detection
        wall_hit_frames = 50;
        buffer_frames = 50;
        last_wall_hit = respawnPosition;

        is_dead = false;
        trickController.hudManager.is_slammed = false;
    }

    void CheckForStoppageForward() {
        if (buffer_frames > 0) {
            buffer_frames--;
            return;
        }
        
        wall_hit_frames--;
        // When countdown reaches 0, check if player is stuck
        if (wall_hit_frames <= 0 && !in_grind) {
            // Check if player hasn't moved forward enough (stuck/hit wall)
            // If current z position is NOT significantly ahead of the old position, they're stuck
            if ((transform.position.z - last_wall_hit.z) < 0.005f) {
                is_dead = true;
                Debug.Log("Player is stuck! Not moving forward enough.");
            }
            //Debug.Log("Player movement change: " + (transform.position.z - last_wall_hit.z));

            wall_hit_frames = 50;
            last_wall_hit = transform.position;
        }
    }
}