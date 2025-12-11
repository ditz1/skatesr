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
            case 5:
                Manual();
                break;
            case 6:
                ManualBack();
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

    void Manual() {
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x + 3f, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);        
    }

    void ManualBack() {
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x - 3f, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }

    void HandleRotation()
    {
        if (board_rb.linearVelocity.x > 0.3f)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, max_rotation, transform.rotation.eulerAngles.z);
        }
        else if (board_rb.linearVelocity.x < -0.3f)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, -max_rotation, transform.rotation.eulerAngles.z);

        } else {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, 0, transform.rotation.eulerAngles.z);
        }
    }
}