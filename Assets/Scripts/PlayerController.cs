using UnityEngine;

public class PlayerController : MonoBehaviour
{

    Quaternion originalRotation;
    public Rigidbody board_rb;
    float max_rotation = 35f;
    void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = originalRotation;
        UpdateRotation();
    }

    void UpdateRotation()
    {
        if (board_rb.linearVelocity.x > 0.1f)
        {
            transform.rotation = Quaternion.Euler(0, max_rotation, 0);
        }
        else if (board_rb.linearVelocity.x < -0.1f)
        {
            transform.rotation = Quaternion.Euler(0, -max_rotation, 0);
        }
    }
}
