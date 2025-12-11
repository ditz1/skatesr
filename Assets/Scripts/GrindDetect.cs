using UnityEngine;

public class GrindDetect : MonoBehaviour
{

    public Transform grind_height;
    BoardController boardController;
   

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name);
        // on any collision with any object
        boardController = collision.gameObject.GetComponent<BoardController>();
        if (boardController != null)
        {
            if (collision.gameObject.transform.position.y > grind_height.position.y) {
                boardController.in_grind = true;
            } else {
                boardController.in_grind = false;
            }
        }
        
    }

    void OnCollisionExit(Collision collision)
    {
        boardController = collision.gameObject.GetComponent<BoardController>();
        if (boardController != null)
        {
            boardController.in_grind = false;
        }
    }

}
