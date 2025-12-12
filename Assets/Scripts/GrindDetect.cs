using UnityEngine;
using System.Collections.Generic;

public class GrindDetect : MonoBehaviour
{
    public Transform grind_start;
    
    [Header("Grind Path")]
    [Tooltip("List of points that define the grind path (in order)")]
    public List<Transform> grindPoints = new List<Transform>();
    
    [Tooltip("How close to the rail the player needs to be to start grinding")]
    float grindSnapDistance = 2.5f;
    
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
            
            // Check if we have valid grind points
            if (grindPoints == null || grindPoints.Count < 2)
            {
                Debug.LogWarning("Need at least 2 grind points!");
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
                    boardController.StartGrind(grindPoints);
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
                Debug.Log("Collision exit - ending grind");
                boardController.EndGrind();
            }
        }
    }
    
    // Helper function to find closest point on the entire grind rail path
    Vector3 GetClosestPointOnGrindRail(Vector3 position)
    {
        if (grindPoints == null || grindPoints.Count < 2) return position;
        
        Vector3 closestPoint = grindPoints[0].position;
        float closestDistance = float.MaxValue;
        
        // Check each segment
        for (int i = 0; i < grindPoints.Count - 1; i++)
        {
            Vector3 segmentStart = grindPoints[i].position;
            Vector3 segmentEnd = grindPoints[i + 1].position;
            Vector3 segmentDirection = segmentEnd - segmentStart;
            float segmentLength = segmentDirection.magnitude;
            segmentDirection.Normalize();
            
            Vector3 startToPosition = position - segmentStart;
            float projectionLength = Vector3.Dot(startToPosition, segmentDirection);
            projectionLength = Mathf.Clamp(projectionLength, 0, segmentLength);
            
            Vector3 pointOnSegment = segmentStart + segmentDirection * projectionLength;
            float distance = Vector3.Distance(position, pointOnSegment);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = pointOnSegment;
            }
        }
        
        return closestPoint;
    }

    // Draw the grind path in the editor
    void OnDrawGizmos()
    {
        if (grindPoints != null && grindPoints.Count >= 2)
        {
            Gizmos.color = Color.yellow;
            
            // Draw lines between points
            for (int i = 0; i < grindPoints.Count - 1; i++)
            {
                if (grindPoints[i] != null && grindPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(grindPoints[i].position, grindPoints[i + 1].position);
                }
            }
            
            // Draw spheres at each point
            for (int i = 0; i < grindPoints.Count; i++)
            {
                if (grindPoints[i] != null)
                {
                    Gizmos.color = i == 0 ? Color.green : (i == grindPoints.Count - 1 ? Color.red : Color.yellow);
                    Gizmos.DrawWireSphere(grindPoints[i].position, 0.2f);
                }
            }
        }
        
        if (grind_start != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grind_start.position, 0.3f);
        }
    }
}