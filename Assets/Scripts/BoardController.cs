using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    Rigidbody rb;
    public Transform nose;
    public Transform tail;
    
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider backLeftWheel;
    public WheelCollider backRightWheel;
    
    public float motorTorque = 500f;
    public float brakeTorque = 1000f;
    public float maxSteerAngle = 30f;
    public float groundCheckDistance = 0.5f;
    
    private bool isGrounded;
    private Vector3 movementDirection;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        
        if (keyboard != null)
        {
            moveInput = Vector2.zero;
            
            if (keyboard.wKey.isPressed) moveInput.y += 1;
            if (keyboard.sKey.isPressed) moveInput.y -= 1;
            if (keyboard.aKey.isPressed) moveInput.x -= 1;
            if (keyboard.dKey.isPressed) moveInput.x += 1;
        }
        
        movementDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        
        if (movementDirection.magnitude > 0)
        {
            Debug.DrawRay(transform.position, movementDirection * 2f, Color.green);
        }
        
        CheckGround();
    }

    void FixedUpdate()
    {
        float motor = moveInput.y * motorTorque;
        float steer = moveInput.x * maxSteerAngle;
        
        frontLeftWheel.motorTorque = motor;
        frontRightWheel.motorTorque = motor;
        backLeftWheel.motorTorque = motor;
        backRightWheel.motorTorque = motor;
        
        frontLeftWheel.steerAngle = steer;
        frontRightWheel.steerAngle = steer;
        
        if (moveInput.y == 0)
        {
            frontLeftWheel.brakeTorque = brakeTorque;
            frontRightWheel.brakeTorque = brakeTorque;
            backLeftWheel.brakeTorque = brakeTorque;
            backRightWheel.brakeTorque = brakeTorque;
        }
        else
        {
            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
            backLeftWheel.brakeTorque = 0;
            backRightWheel.brakeTorque = 0;
        }
    }

    void CheckGround()
    {
        RaycastHit noseHit;
        RaycastHit tailHit;
        
        bool noseGrounded = Physics.Raycast(nose.position, -transform.up, out noseHit, groundCheckDistance);
        bool tailGrounded = Physics.Raycast(tail.position, -transform.up, out tailHit, groundCheckDistance);
        
        Debug.DrawRay(nose.position, -transform.up * groundCheckDistance, noseGrounded ? Color.red : Color.white);
        Debug.DrawRay(tail.position, -transform.up * groundCheckDistance, tailGrounded ? Color.red : Color.white);
        
        isGrounded = noseGrounded || tailGrounded;
    }
}