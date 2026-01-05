using UnityEngine;

public class BoardGroundDetect : MonoBehaviour
{
    [Header("References")]
    public Transform nose;
    public Transform tail;
    public TrickController trickController;
    public BoardController boardController;
    public ParticleSystem grind_particles;
    public AudioManager audioManager;

    [Header("Settings")]
    [Tooltip("Distance threshold to align with ground")]
    public float alignmentThreshold = 0.3f;
    [Tooltip("Extra distance added to ground rays to stay latched to ramps/lips")]
    public float raycastBuffer = 0.1f;
    [Tooltip("Cast an additional center ray to stabilize grounding on curves/ramps")]
    public bool useCenterRay = true;
    
    [Tooltip("How fast the board rotates to match ground")]
    float rotationSpeed = 15f;
    
    [Tooltip("Layers to detect as ground")]
    LayerMask groundLayer = 1;

    [Header("Manual Tilt Settings")]
    [Tooltip("Maximum rotation angle for manual tilt (in degrees)")]
    float maxTiltAngle = 45f;
    
    [Tooltip("Speed at which the board rotates to target angle")]
    float tiltSpeed = 5f;
    
    [Header("Airborne Relax Settings")]
    [Tooltip("Time to hold the last grounded pitch before relaxing")]
    public float airborneHoldTime = 0.12f;
    [Tooltip("Speed to relax pitch back toward target after hold")]
    public float airborneRelaxSpeed = 1.25f;

    [Header("Trick Ray Settings")]
    [Tooltip("Keep rays world-down for this long after a trick ends")]
    public float trickRayHoldTime = 0.2f;
    
    [Header("Debug")]
    bool showDebugRays = true;

    public bool isGrounded = false;
    public bool isManuallyTurning = false;
    public bool noseHitGroundLast = false;
    public bool tailHitGroundLast = false;
    public bool centerHitGroundLast = false;
    
    [Header("External Tilt")]
    [Tooltip("Additional X tilt applied by external controllers (degrees)")]
    public float externalXTilt = 0f;

    bool just_landed = false;

    float originalXRotation;
    float targetXRotation;
    float originalYRotation;
    float targetYRotation;
    float lastGroundedXRotation;
    float airborneTimer = 0f;
    float trickRayTimer = 0f;
    bool wasTricking = false;
    
    void Start()
    {
        
        // Store the original X rotation
        originalXRotation = transform.localEulerAngles.x;
        targetXRotation = originalXRotation;
        originalYRotation = transform.localEulerAngles.y;
        targetYRotation = originalYRotation;
        lastGroundedXRotation = originalXRotation;
    }

    void Update()
    {

        if (boardController.in_grind) {
            grind_particles.Play();
            audioManager.Play("grind");
            //Debug.Log("Playing grind particles");
        } else {
            //grind_particles.Stop();
            grind_particles.Stop();
        }


        UpdateManualRotations();

        CheckForLanding();

        bool wasGrounded = isGrounded;
        bool isTricking = trickController != null && trickController.isPerformingTrick;

        // Keep world-down rays for a short time after tricks to avoid mid-flip orientations
        if (isTricking)
        {
            trickRayTimer = trickRayHoldTime;
        }
        else
        {
            trickRayTimer = Mathf.Max(0f, trickRayTimer - Time.deltaTime);
        }
        wasTricking = isTricking;

        // During tricks the board spins; keep rays world-down to avoid erratic casts
        Vector3 downDir = (isTricking || trickRayTimer > 0f) ? Vector3.down : -transform.up;
        float rayLength = alignmentThreshold + raycastBuffer;

        RaycastHit noseHit;
        RaycastHit tailHit;
        RaycastHit centerHit = new RaycastHit();
        
        bool noseHitGround = Physics.Raycast(nose.position, downDir, out noseHit, rayLength, groundLayer);
        bool tailHitGround = Physics.Raycast(tail.position, downDir, out tailHit, rayLength, groundLayer);
        bool centerHitGround = useCenterRay && Physics.Raycast(transform.position, downDir, out centerHit, rayLength, groundLayer);

        noseHitGroundLast = noseHitGround;
        tailHitGroundLast = tailHitGround;
        centerHitGroundLast = centerHitGround;
        
        if (showDebugRays)
        {
            Debug.DrawRay(nose.position, downDir * rayLength, noseHitGround ? Color.green : Color.red);
            Debug.DrawRay(tail.position, downDir * rayLength, tailHitGround ? Color.green : Color.red);
            if (useCenterRay)
            {
                Debug.DrawRay(transform.position, downDir * rayLength, centerHitGround ? Color.green : Color.red);
            }
        }
        
        
        // CRITICAL: Don't override rotation during reset
        if (boardController != null && boardController.isResettingRotation)
        {
            isGrounded = noseHitGround && tailHitGround;
            return;
        }
        
        
        if (noseHitGround || tailHitGround || centerHitGround)
        {
            isGrounded = true;
            trickController.isGrounded = true;
            airborneTimer = 0f;
        
            // When landing normally (not grinding), reset the manual turning state
            if (isManuallyTurning && !boardController.in_grind)
            {
                isManuallyTurning = false;
            }
        
            // Don't apply ground rotation if manually turning (includes grinding)
            if (!isManuallyTurning)
            {
                Vector3 groundDirection = transform.forward;
                if (noseHitGround && tailHitGround)
                {
                    groundDirection = (noseHit.point - tailHit.point).normalized;
                }
                else if (noseHitGround)
                {
                    groundDirection = Vector3.ProjectOnPlane(transform.forward, noseHit.normal).normalized;
                }
                else if (tailHitGround)
                {
                    groundDirection = Vector3.ProjectOnPlane(transform.forward, tailHit.normal).normalized;
                }

                Vector3 averagedNormal = Vector3.zero;
                if (noseHitGround) averagedNormal += noseHit.normal;
                if (tailHitGround) averagedNormal += tailHit.normal;
                if (centerHitGround) averagedNormal += centerHit.normal;
                if (averagedNormal.sqrMagnitude < 0.0001f)
                {
                    averagedNormal = transform.up;
                }
                else
                {
                    averagedNormal.Normalize();
                }

                Vector3 rightVector = Vector3.Cross(groundDirection, averagedNormal).normalized;
                if (rightVector.sqrMagnitude < 0.0001f)
                {
                    rightVector = transform.right;
                }
                Vector3 upVector = Vector3.Cross(rightVector, groundDirection).normalized;

                Quaternion targetRotation = Quaternion.LookRotation(groundDirection, upVector);
        
                Vector3 currentEuler = transform.eulerAngles;
                Vector3 targetEuler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(targetEuler.x, currentEuler.y, targetEuler.z);
        
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                lastGroundedXRotation = transform.localEulerAngles.x;
                if (boardController == null || !boardController.in_manual)
                {
                    targetXRotation = lastGroundedXRotation;
                }
        
                // Update originalYRotation to match where we landed
                originalYRotation = transform.localEulerAngles.y;
                targetYRotation = originalYRotation;
        
                if (showDebugRays)
                {
                    Debug.DrawRay(transform.position, groundDirection * 2f, Color.blue);
                    Debug.DrawRay(transform.position, upVector * 2f, Color.cyan);
                }
            }
        } else {
            isGrounded = false;
            airborneTimer += Time.deltaTime;
        }

    }

    void CheckForLanding()
    {
        if (isGrounded && !just_landed)
        {
            just_landed = true;
            //audioManager.Play("landing");
            if (!boardController.in_grind) {
                audioManager.Play("land");
            }
        } else if (!isGrounded && just_landed) {
            just_landed = false;
        }
    }

    void UpdateManualRotations()
    {
        Vector3 currentRotation = transform.localEulerAngles;
        float desiredX = targetXRotation;
        
        // Hold ramp-derived pitch briefly, then ease back toward target (usually flat)
        if (!isGrounded)
        {
            if (airborneTimer < airborneHoldTime)
            {
                desiredX = lastGroundedXRotation;
            }
            else
            {
                float relaxT = Mathf.Clamp01((airborneTimer - airborneHoldTime) * airborneRelaxSpeed);
                desiredX = Mathf.LerpAngle(lastGroundedXRotation, targetXRotation, relaxT);
            }
        }

        // Update X rotation (nose/tail)
        float newXRotation = Mathf.LerpAngle(currentRotation.x, desiredX, Time.deltaTime * tiltSpeed);
        float finalXRotation = newXRotation + externalXTilt;
        
        // Update Y rotation (frontside/backside) - only if manually turning
        float newYRotation = currentRotation.y;
        if (isManuallyTurning)
        {
            newYRotation = Mathf.LerpAngle(currentRotation.y, targetYRotation, Time.deltaTime * tiltSpeed);
        }
        
        // Set both at once - no fighting!
        transform.localEulerAngles = new Vector3(finalXRotation, newYRotation, currentRotation.z);
    }

    public enum GrindAnchor
    {
        Center,
        Nose,
        Tail
    }

    public GrindAnchor GetPreferredGrindAnchor()
    {
        // Single-point contact picks that anchor; otherwise center
        if (tailHitGroundLast && !noseHitGroundLast && !centerHitGroundLast)
            return GrindAnchor.Tail;
        if (noseHitGroundLast && !tailHitGroundLast && !centerHitGroundLast)
            return GrindAnchor.Nose;
        return GrindAnchor.Center;
    }

    public void TurnBoardFrontside()
    {
        isManuallyTurning = true;
        // Clockwise rotation = decrease Y rotation
        targetYRotation -= 60f;
    }

    public void ResetTurnBoardFrontside()
    {
        isManuallyTurning = false;
        // Keep current rotation as target when released
        targetYRotation = transform.localEulerAngles.y;
    }

    public void TurnBoardBackside()
    {
        isManuallyTurning = true;
        // Counter-clockwise rotation = increase Y rotation
        targetYRotation += 60f;
    }

    public void ResetTurnBoardBackside()
    {
        isManuallyTurning = false;
        // Keep current rotation as target when released
        targetYRotation = transform.localEulerAngles.y;
    }

    public void RaiseNose() 
    {        
        // Rotate nose up (negative X rotation)
        if (Mathf.Abs(transform.localEulerAngles.y) > 160f){
            targetXRotation = originalXRotation + maxTiltAngle;
        } else {
            targetXRotation = originalXRotation - maxTiltAngle;
        }
    }


    public void ResetNose()
    {        
        // Return to original rotation
        targetXRotation = originalXRotation;
    }

    public void RaiseTail() 
    {        
        // Rotate tail up (positive X rotation)
        if (Mathf.Abs(transform.localEulerAngles.y) > 160f){
            targetXRotation = originalXRotation - maxTiltAngle;
        } else {
            targetXRotation = originalXRotation + maxTiltAngle;
        }
    }

    public void ResetTail()
    {        
        // Return to original rotation
        targetXRotation = originalXRotation;
    }

    void OnDrawGizmos()
    {
        if (nose == null || tail == null) return;
        
        Gizmos.color = Color.yellow;
        if (boardController.in_grind) {
            Gizmos.color = Color.green;
        }
        Gizmos.DrawWireSphere(nose.position, 0.1f);
        Gizmos.DrawWireSphere(tail.position, 0.1f);
        Gizmos.DrawLine(nose.position, tail.position);

        Gizmos.color = Color.red;
        if (trickController.is_in_trick_line){
            Gizmos.color = Color.green;
        }
        Vector3 t = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z - 1.0f);
        Gizmos.DrawWireCube(t, new Vector3(0.1f, 0.1f, 0.1f));
    }
}