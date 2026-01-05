using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    float moveSpeed = 5f;
    float turnSpeed = 4f;
    float moveInput = 0f;
    float currentTurnInput = 0f;
    float turnAcceleration = 2.5f; // how quickly turn input ramps up
    float turnDecay = 5f;          // how quickly turn input decays when released
    float maxTurnInput = 1f;       // cap for turn input magnitude
    public float CurrentTurnInput => currentTurnInput;
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
    public OutfitManager outfitManager;
    public AudioManager audioManager;
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
    float hit_min_distance_check = 0.003f;
    Vector3 last_wall_hit;
    int wall_hit_frames_max = 10;
    int wall_hit_frames;
    int buffer_frames_max = 50;
    int buffer_frames;

    public bool can_play = false;
    float preStartZPosition;
    bool preStartFrozen = false;
    

    // Combo input buffer
    [Header("Trick Input Settings")]
    [Tooltip("Number of frames to allow combo input after starting a trick")]
    public int comboBufferFrames = 8;
    
    private float comboBufferTime;

    // Grind anchor selection
    private BoardGroundDetect.GrindAnchor currentGrindAnchor = BoardGroundDetect.GrindAnchor.Center;
    private Vector3 grindAnchorLocalOffset = Vector3.zero;

    // Ollie tilt settings (manual rotation, no BoardGroundDetect helpers)
    float ollieTiltUpThreshold = 1.4f;
    float ollieLevelThreshold = 0.15f;
    float ollieMaxTiltDegrees = 15f;
    float ollieTiltUpSpeed = 4f;     // deg/sec while rising
    float ollieTiltDownSpeed = 8f;  // deg/sec while leveling
    float currentOllieTilt = 0f;

    void Start()
    {
        wall_hit_frames = wall_hit_frames_max;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        preStartZPosition = transform.position.z;
        
        // Convert frames to seconds (assuming 60fps)
        comboBufferTime = comboBufferFrames / 60f;

        // Make sure TrickController has reference to BoardGroundDetect
        if (trickController != null && boardGroundDetect != null)
        {
            trickController.boardGroundDetect = boardGroundDetect;
        }
    }

    void UpdateTurnInput()
    {
        float desired = 0f;
        if (Keyboard.current.aKey.isPressed) {
            desired = -1f;
        } else if (Keyboard.current.dKey.isPressed) {
            desired = 1f;
        }

        if (Mathf.Abs(desired) > 0f)
        {
            currentTurnInput = Mathf.MoveTowards(currentTurnInput, desired * maxTurnInput, turnAcceleration * Time.deltaTime);
        }
        else
        {
            currentTurnInput = Mathf.MoveTowards(currentTurnInput, 0f, turnDecay * Time.deltaTime);
        }

        moveInput = currentTurnInput;
    }

    void Update()
    {
        if (!can_play)
        {
            // Lock the player on the start line until play begins
            if (!preStartFrozen)
            {
                preStartFrozen = true;
                preStartZPosition = transform.position.z;
            }

            if (rb != null)
            {
                Vector3 v = rb.linearVelocity;
                v.z = 0f;
                rb.linearVelocity = v;
                rb.constraints = RigidbodyConstraints.FreezePositionZ;
            }

            Vector3 pos = transform.position;
            pos.z = preStartZPosition;
            transform.position = pos;
            return;
        }
        else if (preStartFrozen)
        {
            // Release constraints once play starts
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
            preStartFrozen = false;
        }

        
        

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

        // Manual ollie tilt handled in LateUpdate so it runs after ground alignment
        TiltBoardOnJump();

        trickController.isGrounded = boardGroundDetect.isGrounded;

        HandleManualTilt();
        HandleJump();
        HandleTrick();
        HandleGrind(); // This handles the grinding movement

        UpdateTurnInput();

       


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
            playerController.SetIKMirror(true);
        } else {
            Vector3 scale = new Vector3(1f, 1f, 1f);
            playerController.skater_mesh_transform.localScale = scale;
            playerController.SetIKMirror(false);
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

        // Align board rotation with rail direction
        Quaternion targetRotation = Quaternion.LookRotation(railDirection);

        // Offset the board so the chosen grind anchor stays on the rail line
        Vector3 targetAnchorOffset = targetRotation * grindAnchorLocalOffset;
        Vector3 targetBoardPos = newPosition + new Vector3(0, 0.5f, 0) - targetAnchorOffset;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, grindAlignSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetBoardPos, grindSpeed * Time.deltaTime * 2.0f);
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

        // Decide which part of the board should anchor to the rail at start
        currentGrindAnchor = BoardGroundDetect.GrindAnchor.Center;
        grindAnchorLocalOffset = Vector3.zero;
        if (boardGroundDetect != null)
        {
            currentGrindAnchor = boardGroundDetect.GetPreferredGrindAnchor();
            switch (currentGrindAnchor)
            {
                case BoardGroundDetect.GrindAnchor.Nose:
                    if (boardGroundDetect.nose != null)
                        grindAnchorLocalOffset = boardGroundDetect.nose.localPosition;
                    break;
                case BoardGroundDetect.GrindAnchor.Tail:
                    if (boardGroundDetect.tail != null)
                        grindAnchorLocalOffset = boardGroundDetect.tail.localPosition;
                    break;
                default:
                    grindAnchorLocalOffset = Vector3.zero;
                    break;
            }
        }

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
        grindAnchorLocalOffset = Vector3.zero;
        currentGrindAnchor = BoardGroundDetect.GrindAnchor.Center;
    
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
            //animator.Play("manual");
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
            //animator.Play("nosemanual");
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
        if (!boardGroundDetect.isGrounded && !in_grind)
        {
            if (isChargingJump)
            {
                isChargingJump = false;
                if (playerController != null)
                {
                    playerController.SetJumpCharging(false);
                }
            }
            return;
        }
        
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
        
        if (playerController != null)
        {
            playerController.SetJumpCharging(isChargingJump);
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
            //animator.Play("kickflip");
        }
        else if (Keyboard.current.kKey.isPressed) {
            trickController.StartTrick(1); // shuvit
            //animator.Play("shuvit");
        }
        else if (Keyboard.current.lKey.isPressed) {
            trickController.StartTrick(2); // heelflip
            //animator.Play("kickflip");
        }
        // Backup single key options (U and I keys for direct combo access)
        else if (Keyboard.current.uKey.isPressed) {
            trickController.StartTrick(3); // varial kickflip
            //animator.Play("shuvit");
        }
        else if (Keyboard.current.iKey.isPressed) {
            trickController.StartTrick(4); // varial heelflip
            //animator.Play("shuvit");
        }
    }

    void Jump()
    {

        //animator.Play("ollie");
        audioManager.Play("pop");

        // If grinding, end the grind first so rigidbody can move
        if (in_grind)
        {
            EndGrind();
        }

        float normalizedHoldTime = jumpHoldTime / maxJumpHoldTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, normalizedHoldTime);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void ResetOllieTilt()
    {
        currentOllieTilt = 0f;
        if (boardGroundDetect != null)
        {
            boardGroundDetect.externalXTilt = 0f;
        }
    }

    void TiltBoardOnJump() {
        // Manual tilt: rotate the board directly via externalXTilt, do not use BoardGroundDetect helpers
        if (rb == null || boardGroundDetect == null)
        {
            return;
        }

        bool airborne = !boardGroundDetect.isGrounded && !in_grind;
        bool performingTrick = trickController != null && trickController.isPerformingTrick;

        // If tricks or grinds take over, clear ollie tilt immediately
        if (performingTrick || in_grind || boardGroundDetect.isGrounded)
        {
            ResetOllieTilt();
            return;
        }

        float targetTilt = 0f;

        if (airborne)
        {
            // Full tilt while rising quickly, blend down as vertical speed drops
            if (rb.linearVelocity.y > ollieTiltUpThreshold)
            {
                targetTilt = ollieMaxTiltDegrees;
            }
            else if (rb.linearVelocity.y > ollieLevelThreshold)
            {
                float t = Mathf.InverseLerp(ollieLevelThreshold, ollieTiltUpThreshold, rb.linearVelocity.y);
                targetTilt = Mathf.Lerp(0f, ollieMaxTiltDegrees, t);
            }
        }

        // Reduce available tilt based on current board pitch (e.g., ramps already tilting the deck)
        float currentPitch = Mathf.DeltaAngle(0f, boardGroundDetect.transform.localEulerAngles.x);
        float availableTilt = Mathf.Max(0f, ollieMaxTiltDegrees - Mathf.Abs(currentPitch));

        // Enforce hard clamp using the remaining budget
        targetTilt = Mathf.Clamp(targetTilt, -availableTilt, availableTilt);

        // Choose speed based on whether we're tilting up or returning
        float tiltSpeed = targetTilt > currentOllieTilt ? ollieTiltUpSpeed : ollieTiltDownSpeed;
        float newTilt = Mathf.MoveTowards(currentOllieTilt, targetTilt, tiltSpeed * Time.deltaTime);
        currentOllieTilt = newTilt;

        // Match the old nose-up direction convention (flip sign when facing backwards)
        bool facingBackwards = Mathf.Abs(transform.localEulerAngles.y) > 160f;
        float signedTilt = facingBackwards ? newTilt : -newTilt;
        float appliedTilt = Mathf.Clamp(signedTilt, -availableTilt, availableTilt);
        boardGroundDetect.externalXTilt = appliedTilt;
    }

    // TiltBoardOnJump is driven from Update; no LateUpdate override needed

    void PushForward() {
        if (!in_grind && boardGroundDetect.isGrounded){
            audioManager.Play("roll");
        }
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveSpeed);
    }

    void Move(float input){
        rb.linearVelocity = new Vector3(input * turnSpeed, rb.linearVelocity.y, moveSpeed);
    }

    void RespawnPlayer()
    {
        if (outfitManager != null)
        {
            outfitManager.ResetCrumbleRagdoll();
        }

        // Move the player slightly forward and up to avoid obstacles
        float forwardOffset = 2.5f;
        float x_offset = 2.5f;
        float heightOffset = 0.6f;

        Vector3 respawnPosition = transform.position + (transform.forward * forwardOffset) + (Vector3.up * heightOffset) + (Vector3.right * x_offset);

        in_grind = false;
        rb.constraints = RigidbodyConstraints.None;
        transform.position = respawnPosition;

        // Clear velocity so normal movement can resume cleanly
        rb.linearVelocity = Vector3.zero;

        // Reset tracking used for stuck detection
        wall_hit_frames = wall_hit_frames_max;
        buffer_frames = buffer_frames_max;
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
            if ((transform.position.z - last_wall_hit.z) < hit_min_distance_check) {
                TriggerSlam();
                Debug.Log("Player is stuck! Not moving forward enough.");
            }
            //Debug.Log("Player movement change: " + (transform.position.z - last_wall_hit.z));

            wall_hit_frames = wall_hit_frames_max;
            last_wall_hit = transform.position;
        }
    }

    public void TriggerSlam()
    {
        Vector3 slamVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        TriggerSlam(slamVelocity);
    }

    public void TriggerSlam(Vector3 slamVelocity)
    {
        if (is_dead)
        {
            return;
        }

        is_dead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (outfitManager != null)
        {
            outfitManager.TriggerCrumbleRagdoll(slamVelocity, transform);
        }

        if (trickController != null && trickController.hudManager != null)
        {
            trickController.hudManager.is_slammed = true;
        }
    }
}