using UnityEngine;
using UnityEngine.InputSystem;

public class TrickController : MonoBehaviour
{
    public bool isPerformingTrick = false;
    private int currentTrick = -1;
    private float trickProgress = 0f;
    private float trickDuration = 0.5f;
    
    private Vector3 startEulerAngles;
    private Vector3 targetEulerAngles;

    float max_rotation = 35f;

    public Rigidbody board_rb;

    void Update()
    {
        if (isPerformingTrick) {
            PerformTrick();
        } else {
            HandleRotation();
        }
    }

    public void StartTrick(int trickType)
    {
        if (isPerformingTrick) {
            Debug.Log("Already performing a trick!");
            return;
        }
        
        Debug.Log($"Starting trick type: {trickType}");
        currentTrick = trickType;
        isPerformingTrick = true;
        trickProgress = 0f;
        startEulerAngles = transform.rotation.eulerAngles;
        
        switch (trickType) {
            case 0:
                Kickflip();
                break;
            case 1:
                Shuvit();
                break;
            case 2:
                Heelflip();
                break;
            case 3:
                VarialKickflip();
                break;
            case 4:
                VarialHeelflip();
                break;
            default:
                isPerformingTrick = false;
                return;
        }
        
        Debug.Log($"Start angles: {startEulerAngles}, Target angles: {targetEulerAngles}");
    }

    void PerformTrick()
    {
        trickProgress += Time.deltaTime / trickDuration;
        
        if (trickProgress >= 1f) {
            transform.rotation = Quaternion.Euler(targetEulerAngles);
            isPerformingTrick = false;
            trickProgress = 0f;
            Debug.Log("Trick complete!");
        } else {
            Vector3 currentEulerAngles = Vector3.Lerp(startEulerAngles, targetEulerAngles, trickProgress);
            transform.rotation = Quaternion.Euler(currentEulerAngles);
        }
    }

    void Kickflip() {
        targetEulerAngles = startEulerAngles + new Vector3(0, 0, 360);
    }

    void Shuvit() {
        targetEulerAngles = startEulerAngles + new Vector3(0, 180, 0);
    }

    void VarialKickflip() {
        targetEulerAngles = startEulerAngles + new Vector3(0, 180, 360); 
    }

    void VarialHeelflip() {
        targetEulerAngles = startEulerAngles + new Vector3(0, -180, -360); 
    }

    void Heelflip() {
        targetEulerAngles = startEulerAngles + new Vector3(0, 0, -360);
    }

    // Determines if board is closer to 0° (regular) or 180° (switch)
    float GetNearestBaseYRotation()
    {
        float currentY = transform.rotation.eulerAngles.y;
        
        // Normalize to -180 to 180 range
        if (currentY > 180f) currentY -= 360f;
        
        // Check which base angle (0 or 180/-180) is closer
        float distanceTo0 = Mathf.Abs(Mathf.DeltaAngle(currentY, 0f));
        float distanceTo180 = Mathf.Abs(Mathf.DeltaAngle(currentY, 180f));
        
        // Return the nearest base angle
        return distanceTo0 < distanceTo180 ? 0f : 180f;
    }

    void HandleRotation()
    {
        // Get the nearest base rotation (0° or 180°)
        float baseRotation = GetNearestBaseYRotation();
        float targetY;
        
        if (board_rb.linearVelocity.x > 0.3f)
        {
            // Turning right
            targetY = baseRotation + max_rotation;
        }
        else if (board_rb.linearVelocity.x < -0.3f)
        {
            // Turning left
            targetY = baseRotation - max_rotation;
        }
        else
        {
            // Going straight - return to base rotation
            targetY = baseRotation;
        }
        
        // Smoothly rotate to target
        Vector3 currentEuler = transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(currentEuler.x, targetY, currentEuler.z);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }
}