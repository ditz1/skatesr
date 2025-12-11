using UnityEngine;

public class BoardGroundDetect : MonoBehaviour
{
    [Header("References")]
    public Transform nose;
    public Transform tail;

    [Header("Settings")]
    [Tooltip("Distance threshold to align with ground")]
    float alignmentThreshold = 0.5f;
    
    [Tooltip("How fast the board rotates to match ground")]
    float rotationSpeed = 10f;
    
    [Tooltip("Layers to detect as ground")]
    LayerMask groundLayer = 1;
    
    [Header("Debug")]
    bool showDebugRays = true;

    public bool isGrounded = false;

    void Update()
    {
        RaycastHit noseHit;
        RaycastHit tailHit;
        
        // Cast rays straight down
        bool noseHitGround = Physics.Raycast(nose.position, Vector3.down, out noseHit, alignmentThreshold, groundLayer);
        bool tailHitGround = Physics.Raycast(tail.position, Vector3.down, out tailHit, alignmentThreshold, groundLayer);
        
        // Debug rays
        if (showDebugRays)
        {
            Debug.DrawRay(nose.position, Vector3.down * alignmentThreshold, noseHitGround ? Color.green : Color.red);
            Debug.DrawRay(tail.position, Vector3.down * alignmentThreshold, tailHitGround ? Color.green : Color.red);
        }
        
        // Only align if both rays hit ground within threshold
        if (noseHitGround && tailHitGround)
        {
            isGrounded = true;
            // Get the ground contact points
            Vector3 groundNosePoint = noseHit.point;
            Vector3 groundTailPoint = tailHit.point;
            
            // Calculate the direction from tail to nose along the ground surface
            Vector3 groundDirection = (groundNosePoint - groundTailPoint).normalized;
            
            // Calculate the up vector perpendicular to the ground slope
            // Use the cross product of ground direction and world right to get proper up vector
            Vector3 rightVector = Vector3.Cross(groundDirection, Vector3.up).normalized;
            Vector3 upVector = Vector3.Cross(rightVector, groundDirection).normalized;
            
            // Create target rotation: board forward = ground direction, board up = perpendicular to slope
            Quaternion targetRotation = Quaternion.LookRotation(groundDirection, upVector);
            
            // Preserve the Y rotation (yaw) from current rotation to maintain steering
            Vector3 currentEuler = transform.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(targetEuler.x, currentEuler.y, targetEuler.z);
            
            // Smoothly rotate to match ground
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            
            // Debug: show the target direction
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

    void OnDrawGizmos()
    {
        if (nose == null || tail == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(nose.position, 0.1f);
        Gizmos.DrawWireSphere(tail.position, 0.1f);
        Gizmos.DrawLine(nose.position, tail.position);
    }
}