using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    private Transform target; // The transform to follow (found automatically)
    
    [Header("Offset Settings")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 2f, -5f); // Offset from target
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f); // Where to look at on the target
    
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 10f; // How quickly camera follows
    [SerializeField] private bool smoothFollow = true; // Use smooth following
    
    [Header("Rotation Settings")]
    [SerializeField] private bool followTargetRotation = true; // Follow target's rotation
    [SerializeField] private float rotationSpeed = 5f; // Rotation follow speed
    
    private void Start()
    {
        FindTarget();
    }
    
    private void FindTarget()
    {
        GameObject skater = GameObject.FindGameObjectWithTag("Skater");
        
        if (skater != null)
        {
            target = skater.transform;
            Debug.Log("CameraController: Found target with tag 'Skater'");
        }
        else
        {
            Debug.LogError("CameraController: No GameObject with tag 'Skater' found in scene!");
        }
    }
    
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        
        FollowTarget();
    }
    
    private void FollowTarget()
    {
        // Calculate desired position
        Vector3 desiredPosition;
        
        if (followTargetRotation)
        {
            // Apply offset relative to target's rotation
            desiredPosition = target.position + target.rotation * positionOffset;
        }
        else
        {
            // Apply offset in world space
            desiredPosition = target.position + positionOffset;
        }
        
        // Move camera to desired position
        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = desiredPosition;
        }
        
        // Calculate look-at position
        Vector3 lookAtPosition = target.position + lookAtOffset;
        
        // Rotate camera to look at target
        if (smoothFollow)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.LookAt(lookAtPosition);
        }
    }
    
    // Public method to set the target at runtime (if needed)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // Public method to set position offset at runtime
    public void SetPositionOffset(Vector3 newOffset)
    {
        positionOffset = newOffset;
    }
}