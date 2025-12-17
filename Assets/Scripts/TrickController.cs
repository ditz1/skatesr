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
    public HUDManager hudManager;

    public bool is_in_trick_line = false;
    private int tricks_in_current_line = 0;
    private bool landed_last_trick_in_manual = false;
    private bool has_landed_current_trick = false;

    float max_rotation = 45f;

    public Rigidbody board_rb;
    public BoardController boardController;
    

    void Update()
    {
        if (boardController.is_dead)
        {
            hudManager.is_slammed = true;
        } else {
            hudManager.is_slammed = false;
        }

        hudManager.is_in_trick_line = is_in_trick_line;
        hudManager.tricks_in_current_line = tricks_in_current_line;

        if (isGrounded && !has_landed_current_trick && currentTrick != -1)
        {
            HandleLanding();
        }

        if (isPerformingTrick) {
            if (isGrounded)
            {
                CancelTrick();
                return;
            }
            PerformTrick();
        } else {
            if (boardGroundDetect != null && !boardGroundDetect.isManuallyTurning)
            {
                HandleRotation();
            }
        }

        if (is_in_trick_line && landed_last_trick_in_manual && !boardController.in_manual && !isPerformingTrick && isGrounded && !boardController.in_grind)
        {
            EndTrickLine();
        }
    }

    public void StartTrick(int trickType)
    {
        if (isPerformingTrick) {
            return;
        }


        has_landed_current_trick = false;

        if (is_in_trick_line && landed_last_trick_in_manual)
        {
            tricks_in_current_line++;
            landed_last_trick_in_manual = false;
            UpdateMultiplier();
        }

        currentTrick = trickType;
        isPerformingTrick = true;
        trickProgress = 0f;
        trickStartTime = Time.time;
        startEulerAngles = transform.rotation.eulerAngles;

        SetTrickRotation(trickType);

    }

    void HandleLanding()
    {
        has_landed_current_trick = true;

        if (boardController.in_manual)
        {
            landed_last_trick_in_manual = true;

            if (!is_in_trick_line)
            {
                StartTrickLine();
            }
        }
        else
        {
            if (!boardController.in_grind)
            {
                EndTrickLine();
            }
        }
    }

    void StartTrickLine()
    {
        is_in_trick_line = true;
        tricks_in_current_line = 1;
        UpdateMultiplier();
    }

    void EndTrickLine()
    {
        // only end trick line if we get here and are not moving upward
        if (board_rb.linearVelocity.y > 0.1f)
        {
            return;
        }
        if (is_in_trick_line)
        {
        }

        is_in_trick_line = false;
        tricks_in_current_line = 0;
        landed_last_trick_in_manual = false;

        hudManager.UpdateScoreMultiplier(1f);
    }

    void UpdateMultiplier()
    {
        float multiplier = 1f + ((tricks_in_current_line - 1) * 0.5f);
        hudManager.UpdateScoreMultiplier(multiplier);
    }

    public void UpgradeToCombo(int comboTrickType)
    {
        
        // Reset the trick with new target
        currentTrick = comboTrickType;
        trickProgress = 0f;
        trickStartTime = Time.time;
        startEulerAngles = transform.rotation.eulerAngles;
        
        SetTrickRotation(comboTrickType);
        
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
        if (startEulerAngles.x != 0) startEulerAngles.x = 0;
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
        HandleTrickScore(trickType);
        hudManager.AddTrickToQueue(trickType);
    }

    void PerformTrick()
    {
        trickProgress += Time.deltaTime / trickDuration;
        
        if (trickProgress >= 1f) {
            transform.rotation = Quaternion.Euler(targetEulerAngles);
            isPerformingTrick = false;
            trickProgress = 0f;
        } else {
            Vector3 currentEulerAngles = Vector3.Lerp(startEulerAngles, targetEulerAngles, trickProgress);
            transform.rotation = Quaternion.Euler(currentEulerAngles);
        }
    }

    void CancelTrick()
    {
        isPerformingTrick = false;
        currentTrick = -1;
        trickProgress = 0f;
        has_landed_current_trick = false;

        if (!boardController.in_manual && !boardController.in_grind)
        {
            EndTrickLine();
        }

        Vector3 currentEuler = transform.rotation.eulerAngles;
        float nearestY = Mathf.Round(currentEuler.y / 90f) * 90f;
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

    void HandleTrickScore(int trick)
    {
        switch (trick) {
            case 0:
                hudManager.UpdateAddScore(100f);
                break;
            case 1:
                hudManager.UpdateAddScore(100f);
                break;
            case 2:
                hudManager.UpdateAddScore(100f);
                break;
            case 3:
                hudManager.UpdateAddScore(300f);
                break;
            case 4:
                hudManager.UpdateAddScore(300f);
                break;
            default:
                break;
        }
    }

    void HandleTrickMultiplier()
    {

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