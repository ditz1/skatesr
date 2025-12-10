using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float rotationSpeed = 10f;

    private Vector3 last_position;
    private Vector3 movementDirection;

    private float initialX;
    private float initialZ;

    void Start()
    {
        last_position = transform.position;

        // Cache the initial X/Z rotation so we only ever change Y.
        Vector3 euler = transform.rotation.eulerAngles;
        initialX = euler.x;
        initialZ = euler.z;
    }

    void LateUpdate()
    {
        Vector3 current_position = transform.position;
        movementDirection = current_position - last_position;
        last_position = current_position;

        // Ignore vertical movement, only care about XZ.
        Vector3 flatDir = new Vector3(movementDirection.x, 0f, movementDirection.z);

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            // Get the yaw we want from the movement direction.
            Quaternion targetRotation = Quaternion.LookRotation(flatDir, Vector3.up);
            float targetY = targetRotation.eulerAngles.y;

            // Smoothly rotate only the Y angle.
            float currentY = transform.rotation.eulerAngles.y;
            float newY = Mathf.LerpAngle(currentY, targetY, rotationSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(initialX, newY, initialZ);
        }
    }

    public Vector3 GetMovementDirection()
    {
        // Return the flat movement direction (same as what we use for rotation)
        return new Vector3(movementDirection.x, 0f, movementDirection.z).normalized;
    }

    void OnDrawGizmos()
    {
        if (movementDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.red;
            Vector3 flatDir = new Vector3(movementDirection.x, 0f, movementDirection.z).normalized;
            Gizmos.DrawRay(transform.position, flatDir * 2f);
        }
    }
}
