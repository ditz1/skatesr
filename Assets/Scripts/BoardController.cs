using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;
    public float maxSpeed = 10f; // Maximum horizontal speed
    
    public Transform cameraTransform;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void FixedUpdate()
    {
        float moveInput = 0f;
        float turnInput = 0f;

        // W/S for forward/backward movement
        if (Keyboard.current.wKey.isPressed) moveInput += 1f;
        if (Keyboard.current.sKey.isPressed) moveInput -= 1f;

        // A/D for turning left/right
        if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
        if (Keyboard.current.dKey.isPressed) turnInput += 1f;

        // Get camera's forward direction (flattened to XZ plane)
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        // Apply forward/backward force away from camera
        Vector3 movement = cameraForward * moveInput * moveSpeed;
        rb.AddForce(movement);

        // Apply turning by rotating the velocity
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float turnAmount = turnInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.linearVelocity = turnRotation * rb.linearVelocity;
        }

        // Cap the horizontal speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }
}