using UnityEngine;

public class BlockadeKillSkater : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name);
        // on any collision with any object
        BoardController boardController = collision.gameObject.GetComponent<BoardController>();
        if (boardController != null)
        {
            boardController.got_hit = true;
        }
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePosition;
        // Destroy(collision.gameObject);
        
    }
}
