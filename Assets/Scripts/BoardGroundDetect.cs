using UnityEngine;

public class BoardGroundDetect : MonoBehaviour
{
    [Header("References")]
    public Transform nose;
    public Transform tail;
    public TrickController trickController;
    public BoardController boardController;

    [Header("Settings")]
    [Tooltip("Distance threshold to align with ground")]
    public float alignmentThreshold = 0.3f;
    
    [Tooltip("How fast the board rotates to match ground")]
    float rotationSpeed = 10f;
    
    [Tooltip("Layers to detect as ground")]
    LayerMask groundLayer = 1;

    [Header("Manual Tilt Settings")]
    [Tooltip("Maximum rotation angle for manual tilt (in degrees)")]
    float maxTiltAngle = 45f;
    
    [Tooltip("Speed at which the board rotates to target angle")]
    float tiltSpeed = 5f;
    
    [Header("Debug")]
    bool showDebugRays = true;

    public bool isGrounded = false;
    public bool isManuallyTurning = false;

    float originalXRotation;
    float targetXRotation;
    float originalYRotation;
    float targetYRotation;
    
    bool isNoseRaised = false;
    bool isTailRaised = false;

    void Start()
    {
        
        // Store the original X rotation
        originalXRotation = transform.localEulerAngles.x;
        targetXRotation = originalXRotation;
        originalYRotation = transform.localEulerAngles.y;
        targetYRotation = originalYRotation;
    }

    void Update()
    {
        UpdateManualRotations();
        
        RaycastHit noseHit;
        RaycastHit tailHit;
        
        bool noseHitGround = Physics.Raycast(nose.position, Vector3.down, out noseHit, alignmentThreshold, groundLayer);
        bool tailHitGround = Physics.Raycast(tail.position, Vector3.down, out tailHit, alignmentThreshold, groundLayer);
        
        if (showDebugRays)
        {
            Debug.DrawRay(nose.position, Vector3.down * alignmentThreshold, noseHitGround ? Color.green : Color.red);
            Debug.DrawRay(tail.position, Vector3.down * alignmentThreshold, tailHitGround ? Color.green : Color.red);
        }
        
        
        // CRITICAL: Don't override rotation during reset
        if (boardController != null && boardController.isResettingRotation)
        {
            isGrounded = noseHitGround && tailHitGround;
            return;
        }
        
        
        if (noseHitGround || tailHitGround)
        {
            isGrounded = true;
            trickController.isGrounded = true;
        
            // When landing normally (not grinding), reset the manual turning state
            if (isManuallyTurning && !boardController.in_grind)
            {
                isManuallyTurning = false;
            }
        
            // Don't apply ground rotation if manually turning (includes grinding)
            if (!isManuallyTurning)
            {
                Vector3 groundNosePoint = noseHit.point;
                Vector3 groundTailPoint = tailHit.point;
        
                Vector3 groundDirection = (groundNosePoint - groundTailPoint).normalized;
        
                Vector3 rightVector = Vector3.Cross(groundDirection, Vector3.up).normalized;
                Vector3 upVector = Vector3.Cross(rightVector, groundDirection).normalized;
        
                Quaternion targetRotation = Quaternion.LookRotation(groundDirection, upVector);
        
                Vector3 currentEuler = transform.eulerAngles;
                Vector3 targetEuler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(targetEuler.x, currentEuler.y, targetEuler.z);
        
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
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
        }

    }

    void UpdateManualRotations()
    {
        Vector3 currentRotation = transform.localEulerAngles;
        
        // Update X rotation (nose/tail)
        float newXRotation = Mathf.LerpAngle(currentRotation.x, targetXRotation, Time.deltaTime * tiltSpeed);
        
        // Update Y rotation (frontside/backside) - only if manually turning
        float newYRotation = currentRotation.y;
        if (isManuallyTurning)
        {
            newYRotation = Mathf.LerpAngle(currentRotation.y, targetYRotation, Time.deltaTime * tiltSpeed);
        }
        
        // Set both at once - no fighting!
        transform.localEulerAngles = new Vector3(newXRotation, newYRotation, currentRotation.z);
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
        isNoseRaised = true;
        
        // Rotate nose up (negative X rotation)
        targetXRotation = originalXRotation - maxTiltAngle;
    }

    public void ResetNose()
    {
        isNoseRaised = false;
        
        // Return to original rotation
        targetXRotation = originalXRotation;
    }

    public void RaiseTail() 
    {
        isTailRaised = true;
        
        // Rotate tail up (positive X rotation)
        targetXRotation = originalXRotation + maxTiltAngle;
    }

    public void ResetTail()
    {
        isTailRaised = false;
        
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
    }
}