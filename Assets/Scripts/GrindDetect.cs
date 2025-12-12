using UnityEngine;

public class GrindDetect : MonoBehaviour
{
    public Transform grind_height;
    public Transform start_grind;
    public Transform end_grind;
    
    [Tooltip("How close to the rail the player needs to be to start grinding")]
    public float grindSnapDistance = 0.5f;
    
    BoardController boardController;

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name);
        boardController = collision.gameObject.GetComponent<BoardController>();
        
        if (boardController != null)
        {
            // IMPORTANT: Check cooldown first
            if (boardController.IsGrindOnCooldown())
            {
                Debug.Log("Grind on cooldown, ignoring collision");
                return;
            }
            
            // Check if player is above minimum grind height
            if (collision.gameObject.transform.position.y > grind_height.position.y) 
            {
                // Check if player is close enough to the grind rail
                Vector3 closestPoint = GetClosestPointOnGrindRail(collision.gameObject.transform.position);
                float distanceToRail = Vector3.Distance(collision.gameObject.transform.position, closestPoint);
                
                if (distanceToRail < grindSnapDistance)
                {
                    boardController.StartGrind(start_grind, end_grind);
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        boardController = collision.gameObject.GetComponent<BoardController>();
        if (boardController != null)
        {
            // IMPORTANT: Only end grind if they're actually grinding
            // This prevents interfering with jump-initiated exits
            if (boardController.in_grind)
            {
                Debug.Log("Collision exit - ending grind");
                boardController.EndGrind();
            }
        }
    }
    
    // Helper function to find closest point on the grind rail
    Vector3 GetClosestPointOnGrindRail(Vector3 position)
    {
        if (start_grind == null || end_grind == null) return position;
        
        Vector3 startPos = start_grind.position;
        Vector3 endPos = end_grind.position;
        Vector3 railDirection = endPos - startPos;
        float railLength = railDirection.magnitude;
        railDirection.Normalize();
        
        Vector3 startToPosition = position - startPos;
        float projectionLength = Vector3.Dot(startToPosition, railDirection);
        projectionLength = Mathf.Clamp(projectionLength, 0, railLength);
        
        return startPos + railDirection * projectionLength;
    }

    // Optional: Draw the grind rail in the editor
    void OnDrawGizmos()
    {
        if (start_grind != null && end_grind != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start_grind.position, end_grind.position);
            Gizmos.DrawWireSphere(start_grind.position, 0.2f);
            Gizmos.DrawWireSphere(end_grind.position, 0.2f);
        }
        
        if (grind_height != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grind_height.position, 0.3f);
        }
    }
}