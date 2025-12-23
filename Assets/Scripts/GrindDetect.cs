using UnityEngine;

public class GrindDetect : MonoBehaviour
{
    public Transform grind_start;
    public Transform grind_end;
    
    [Tooltip("How close to the rail the player needs to be to start grinding")]
    float grindSnapDistance = 2.0f;
    
    BoardController boardController;

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Collision with: " + collision.gameObject.name);
        boardController = collision.gameObject.GetComponent<BoardController>();
        
        if (boardController != null)
        {
            // IMPORTANT: Check cooldown first
            if (boardController.IsGrindOnCooldown())
            {
                //Debug.Log("Grind on cooldown, ignoring collision");
                return;
            }
            
            // Check if we have valid grind points
            if (grind_start == null || grind_end == null)
            {
                //Debug.LogWarning("Need grind start and end points!");
                return;
            }
            
            // Check if player is above minimum grind height
            if (collision.gameObject.transform.position.y > grind_start.position.y) 
            {
                // Check if player is close enough to the grind rail
                Vector3 closestPoint = GetClosestPointOnGrindRail(collision.gameObject.transform.position);
                float distanceToRail = Vector3.Distance(collision.gameObject.transform.position, closestPoint);
                
                if (distanceToRail < grindSnapDistance)
                {
                    boardController.StartGrind(grind_start, grind_end);
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
            if (boardController.in_grind)
            {
                //Debug.Log("Collision exit - ending grind");
                boardController.EndGrind();
            }
        }
    }
    
    // Helper function to find closest point on the grind rail
    Vector3 GetClosestPointOnGrindRail(Vector3 position)
    {
        if (grind_start == null || grind_end == null) return position;
        
        Vector3 startPos = grind_start.position;
        Vector3 endPos = grind_end.position;
        Vector3 railDirection = endPos - startPos;
        float railLength = railDirection.magnitude;
        railDirection.Normalize();
        
        Vector3 startToPosition = position - startPos;
        float projectionLength = Vector3.Dot(startToPosition, railDirection);
        projectionLength = Mathf.Clamp(projectionLength, 0, railLength);
        
        return startPos + railDirection * projectionLength;
    }

    // Draw the grind path in the editor
    void OnDrawGizmos()
    {
        if (grind_start != null && grind_end != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(grind_start.position, grind_end.position);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grind_start.position, 0.2f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grind_end.position, 0.2f);
        }
    }
}