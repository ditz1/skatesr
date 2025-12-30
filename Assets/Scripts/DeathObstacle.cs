using UnityEngine;

public class DeathObstacle : MonoBehaviour
{


    void OnCollisionEnter(Collision collision)
    {
        BoardController boardController = collision.gameObject.GetComponent<BoardController>();
        if (boardController != null)
        {
            // Use the slam pathway so ragdoll and freeze logic run
            boardController.TriggerSlam(collision.relativeVelocity);
        }
    }
}
