using UnityEngine;
using UnityEngine.InputSystem;

public class TrickController : MonoBehaviour
{
    public bool isPerformingTrick = false;
    private int currentTrick = -1;
    private float trickProgress = 0f;
    private float trickDuration = 0.5f;
    private float trickStartTime = 0f;
    
    private Vector3 startEulerAngles;
    private Vector3 targetEulerAngles;
    public bool isGrounded = false;
    public BoardGroundDetect boardGroundDetect;

    float max_rotation = 35f;

    public Rigidbody board_rb;

    void Update()
    {
        if (isPerformingTrick) {
            // Cancel trick if we land while performing it
            if (isGrounded)
            {
                CancelTrick();
                return;
            }
            PerformTrick();
        } else {
            // Only handle automatic rotation if not manually turning
            if (boardGroundDetect != null && !boardGroundDetect.isManuallyTurning)
            {
                HandleRotation();
            }
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
        trickStartTime = Time.time;
        startEulerAngles = transform.rotation.eulerAngles;
        
        SetTrickRotation(trickType);
        
        Debug.Log($"Start angles: {startEulerAngles}, Target angles: {targetEulerAngles}");
    }

    public void UpgradeToCombo(int comboTrickType)
    {
        Debug.Log($"Upgrading trick {currentTrick} to combo trick {comboTrickType}");
        
        // Reset the trick with new target
        currentTrick = comboTrickType;
        trickProgress = 0f;
        trickStartTime = Time.time;
        startEulerAngles = transform.rotation.eulerAngles;
        
        SetTrickRotation(comboTrickType);
        
        Debug.Log($"Combo - Start angles: {startEulerAngles}, Target angles: {targetEulerAngles}");
    }

    public bool IsInComboWindow(float comboWindowTime)
    {
        return Time.time - trickStartTime <= comboWindowTime;
    }

    public int GetCurrentTrick()
    {
        return currentTrick;
    }

    void SetTrickRotation(int trickType)
    {
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

    void CancelTrick()
    {
        Debug.Log("Trick cancelled - landed!");
        isPerformingTrick = false;
        currentTrick = -1;
        trickProgress = 0f;

        // Reset rotation to nearest clean angle (0, 90, 180, 270)
        Vector3 currentEuler = transform.rotation.eulerAngles;

        // Snap to nearest 90-degree increment for Y rotation
        float nearestY = Mathf.Round(currentEuler.y / 90f) * 90f;

        // Keep X and Z as they are (ground detection will handle X)
        transform.rotation = Quaternion.Euler(currentEuler.x, nearestY, currentEuler.z);
    }

    void Kickflip() {
        targetEulerAngles = startEulerAngles + new Vector3(0, 0, 360);
    }

    void Shuvit() {
        // instead of always turning 180 degrees, to account for boardslide should
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