using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Quaternion originalRotation;
    public Rigidbody board_rb;
    float max_rotation = 35f;
    public Transform player_transform;
    
    private float targetYRotation = 0f;
    private float baseYRotation = 0f;
    
    void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = originalRotation;
        
        // Calculate base rotation from velocity
        CalculateBaseRotation();
        
        // Handle Q/E key offsets
        if (Keyboard.current.qKey.isPressed) {
            targetYRotation = baseYRotation - 60f;
        }
        else if (Keyboard.current.eKey.isPressed) {
            targetYRotation = baseYRotation + 60f;
        }
        else {
            targetYRotation = baseYRotation;
        }
        
        // Lerp to target rotation
        Vector3 currentEuler = player_transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(currentEuler.x, targetYRotation, currentEuler.z);
        player_transform.rotation = Quaternion.Slerp(player_transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    void CalculateBaseRotation()
    {
        if (board_rb.linearVelocity.x > 0.1f)
        {
            baseYRotation = max_rotation;
        }
        else if (board_rb.linearVelocity.x < -0.1f)
        {
            baseYRotation = -max_rotation;
        }
        else
        {
            baseYRotation = 0f;
        }
    }
}