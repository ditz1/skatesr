using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Quaternion originalRotation;
    public Rigidbody board_rb;
    float max_rotation = 35f;
    public Transform player_transform;
    public Transform skater_mesh_transform;
    public Transform board_transform;
    
    private float targetYRotation = 0f;
    private float baseYRotation = 0f;
    private float facingYawOffset = 0f;
    private bool isTurning180 = false;
    private float turn180StartTime = 0f;
    private float turn180Duration = 0.35f;
    private float turn180AngleRemaining = 0f;
    private float turn180Direction = 1f;

    float GetSignedYaw(Transform t)
    {
        return Mathf.DeltaAngle(0f, t.localEulerAngles.y);
    }

    public bool YawIsPositive() {
        return GetSignedYaw(skater_mesh_transform) >= 0f;
    }
    
    void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = originalRotation;

        if (skater_mesh_transform != null)
        {
            float signedYaw = GetSignedYaw(skater_mesh_transform);
            float offsetX = signedYaw >= 0f ? -0.2f : 0.2f;
            Vector3 lp = skater_mesh_transform.localPosition;
            //skater_mesh_transform.localPosition = new Vector3(offsetX, lp.y, lp.z);
            // lerp position instead
            skater_mesh_transform.localPosition = Vector3.Lerp(skater_mesh_transform.localPosition, new Vector3(offsetX, lp.y, lp.z), Time.deltaTime * 20f);
        }

        if (isTurning180)
        {
            Update180Turn();
            return;
        }
        
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
        Quaternion targetRotation = Quaternion.Euler(currentEuler.x, facingYawOffset + targetYRotation, currentEuler.z);
        player_transform.rotation = Quaternion.Slerp(player_transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    public void Start180Turn(float direction, float duration)
    {
        if (isTurning180) return;

        isTurning180 = true;
        turn180StartTime = Time.time;
        turn180Duration = duration;
        turn180AngleRemaining = direction;
        turn180Direction = Mathf.Sign(direction);
    }

    void Update180Turn()
    {
        float normalized = Mathf.Clamp01((Time.time - turn180StartTime) / turn180Duration);
        float degreesPerSecond = 180f / turn180Duration;
        float step = turn180Direction * degreesPerSecond * Time.deltaTime;

        if (Mathf.Abs(step) > Mathf.Abs(turn180AngleRemaining))
        {
            step = turn180AngleRemaining;
        }

        skater_mesh_transform.Rotate(0f, step, 0f, Space.World);
        board_transform.Rotate(0f, step, 0f, Space.World);
        //player_transform.Rotate(0f, step, 0f, Space.World);
        
        turn180AngleRemaining -= step;

        if (normalized >= 1f || Mathf.Approximately(turn180AngleRemaining, 0f))
        {
            turn180AngleRemaining = 0f;
            isTurning180 = false;
        }
        //Debug.Log("player_transform rotation: " + player_transform.rotation.eulerAngles.y);

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