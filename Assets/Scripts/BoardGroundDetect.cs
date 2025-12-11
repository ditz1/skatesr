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
    float alignmentThreshold = 0.5f;
    
    [Tooltip("How fast the board rotates to match ground")]
    float rotationSpeed = 10f;
    
    [Tooltip("Layers to detect as ground")]
    LayerMask groundLayer = 1;

    [Header("Manual Tilt Settings")]
    [Tooltip("Maximum rotation angle for manual tilt (in degrees)")]
    float maxTiltAngle = 15f;
    
    [Tooltip("Speed at which the board rotates to target angle")]
    float tiltSpeed = 5f;
    
    [Header("Debug")]
    bool showDebugRays = true;

    public bool isGrounded = false;

    float originalXRotation;
    float targetXRotation;
    
    bool isNoseRaised = false;
    bool isTailRaised = false;

    void Start()
    {
        // Store the original X rotation
        originalXRotation = transform.localEulerAngles.x;
        targetXRotation = originalXRotation;
    }

    void Update()
    {
        // Handle manual tilting (should happen before ground alignment)
        UpdateManualTilt();
        
        RaycastHit noseHit;
        RaycastHit tailHit;
        
        bool noseHitGround = Physics.Raycast(nose.position, Vector3.down, out noseHit, alignmentThreshold, groundLayer);
        bool tailHitGround = Physics.Raycast(tail.position, Vector3.down, out tailHit, alignmentThreshold, groundLayer);
        
        if (showDebugRays)
        {
            Debug.DrawRay(nose.position, Vector3.down * alignmentThreshold, noseHitGround ? Color.green : Color.red);
            Debug.DrawRay(tail.position, Vector3.down * alignmentThreshold, tailHitGround ? Color.green : Color.red);
        }
        
        // CRITICAL: Don't rotate if performing a trick OR resetting
        if (trickController != null && trickController.isPerformingTrick)
        {
            isGrounded = false;
            return;
        }
        
        // CRITICAL: Don't override rotation during reset
        if (boardController != null && boardController.isResettingRotation)
        {
            isGrounded = noseHitGround && tailHitGround;
            return;
        }
        
        // Skip ground alignment if manually tilting
        if (isNoseRaised || isTailRaised)
        {
            isGrounded = noseHitGround && tailHitGround;
            return;
        }
        
        if (noseHitGround && tailHitGround)
        {
            isGrounded = true;
            
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
            
            if (showDebugRays)
            {
                Debug.DrawRay(transform.position, groundDirection * 2f, Color.blue);
                Debug.DrawRay(transform.position, upVector * 2f, Color.cyan);
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    void UpdateManualTilt()
    {
        // Smoothly rotate to target X rotation
        Vector3 currentRotation = transform.localEulerAngles;
        float newXRotation = Mathf.LerpAngle(currentRotation.x, targetXRotation, Time.deltaTime * tiltSpeed);
        
        transform.localEulerAngles = new Vector3(newXRotation, currentRotation.y, currentRotation.z);
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
        Gizmos.DrawWireSphere(nose.position, 0.1f);
        Gizmos.DrawWireSphere(tail.position, 0.1f);
        Gizmos.DrawLine(nose.position, tail.position);
    }
}