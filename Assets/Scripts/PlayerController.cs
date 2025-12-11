using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Quaternion originalRotation;
    public Rigidbody board_rb;
    float max_rotation = 35f;
    public Transform player_transform;
    
    void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
       
        
        transform.rotation = originalRotation;
        HandleRotation();
      
    }

    void HandleRotation()
    {
        if (board_rb.linearVelocity.x > 0.1f)
        {
            player_transform.rotation = Quaternion.Euler(0, max_rotation, 0);
        }
        else if (board_rb.linearVelocity.x < -0.1f)
        {
            player_transform.rotation = Quaternion.Euler(0, -max_rotation, 0);

        } else {
            player_transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}