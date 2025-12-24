using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody board_rb;
    public Transform player_transform;
    public Transform skater_mesh_transform;
    public Transform board_transform;
    public GameObject metarig;
    public TrickController trickController;
    
    [Header("Board Foot Positions")]
    public Transform frontFootBoardTarget;  // Front foot position on board
    public Transform backFootBoardTarget;   // Back foot position on board
    public Transform frontFootTarget_non_parent;  // Front foot position on ground
    public Transform backFootTarget_non_parent;  // Back foot position on ground
    
    [Header("IK Settings")]
    [Range(0f, 1f)]
    public float ikWeight = 1f;
    public bool debugIK = false;
    public bool enableIK = true;
    
    [Header("Trick Lift Settings")]
    public float trickLiftHeight = 0.58f;  // How high to lift feet during tricks
    public float liftTransitionSpeed = 8f; // How fast feet lift/lower
    
    private float currentLiftAmount = 0f;  // Current lift interpolation (0-1)
    
    // Track which foot goes to which target based on board rotation
    private bool isBoardFlipped = false;
    
    [Header("Rotation Settings")]
    private Quaternion originalRotation;
    private float max_rotation = 35f;
    private float targetYRotation = 0f;
    private float baseYRotation = 0f;
    private float facingYawOffset = 0f;
    
    [Header("180 Turn")]
    private bool isTurning180 = false;
    private float turn180StartTime = 0f;
    private float turn180Duration = 0.35f;
    private float turn180AngleRemaining = 0f;
    private float turn180Direction = 1f;
    
    private LegIK leftLegIK;
    private LegIK rightLegIK;
    
    private Vector3 leftFootTargetPos;
    private Vector3 rightFootTargetPos;
    
    private Vector3 leftFootOffset;
    private Vector3 rightFootOffset;
    private bool isIKMirrored = false;
    
    void Start()
    {
        originalRotation = transform.rotation;
        SetupIK();
        SetupDefaultFootTargets();
        SyncIKMirrorWithScale();
    }
    
    void SetupIK()
    {
        if (metarig == null)
        {
            Debug.LogError("Metarig not assigned!");
            return;
        }
        
        Transform leftThigh = FindBoneRecursive(metarig.transform, "thigh.L");
        Transform leftShin = FindBoneRecursive(metarig.transform, "shin.L");
        Transform leftFoot = FindBoneRecursive(metarig.transform, "foot.L");
        
        Transform rightThigh = FindBoneRecursive(metarig.transform, "thigh.R");
        Transform rightShin = FindBoneRecursive(metarig.transform, "shin.R");
        Transform rightFoot = FindBoneRecursive(metarig.transform, "foot.R");
        
        if (leftThigh && leftShin && leftFoot)
        {
            leftLegIK = new LegIK(leftThigh, leftShin, leftFoot);
            Debug.Log($"Left IK: Upper={leftLegIK.upperLength:F3} Lower={leftLegIK.lowerLength:F3}");
        }
        else
        {
            Debug.LogError($"Left leg bones missing! T:{leftThigh!=null} S:{leftShin!=null} F:{leftFoot!=null}");
        }
        
        if (rightThigh && rightShin && rightFoot)
        {
            rightLegIK = new LegIK(rightThigh, rightShin, rightFoot);
            Debug.Log($"Right IK: Upper={rightLegIK.upperLength:F3} Lower={rightLegIK.lowerLength:F3}");
        }
        else
        {
            Debug.LogError($"Right leg bones missing! T:{rightThigh!=null} S:{rightShin!=null} F:{rightFoot!=null}");
        }
    }
    
    void SetupDefaultFootTargets()
    {
        if (frontFootBoardTarget == null)
        {
            GameObject frontTarget = new GameObject("FrontFootTarget");
            frontTarget.transform.SetParent(board_transform);
            frontTarget.transform.localPosition = new Vector3(-0.15f, 0.05f, 0.15f);
            frontTarget.transform.localRotation = Quaternion.identity;
            frontFootBoardTarget = frontTarget.transform;
        }
        
        if (backFootBoardTarget == null)
        {
            GameObject backTarget = new GameObject("BackFootTarget");
            backTarget.transform.SetParent(board_transform);
            backTarget.transform.localPosition = new Vector3(0.15f, 0.05f, -0.15f);
            backTarget.transform.localRotation = Quaternion.identity;
            backFootBoardTarget = backTarget.transform;
        }
    }
    
    Transform FindBoneRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;
        
        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, name);
            if (found != null)
                return found;
        }
        
        return null;
    }

    void Update()
    {
        transform.rotation = originalRotation;

        if (skater_mesh_transform != null)
        {
            float signedYaw = GetSignedYaw(skater_mesh_transform);
            float offsetX = signedYaw >= 0f ? -0.2f : 0.2f;
            Vector3 lp = skater_mesh_transform.localPosition;
            skater_mesh_transform.localPosition = Vector3.Lerp(
                skater_mesh_transform.localPosition, 
                new Vector3(offsetX, lp.y, lp.z), 
                Time.deltaTime * 20f
            );
        }

        if (isTurning180)
        {
            Update180Turn();
            return;
        }
        
        CalculateBaseRotation();
        
        if (Keyboard.current.qKey.isPressed) {
            targetYRotation = baseYRotation - 60f;
        }
        else if (Keyboard.current.eKey.isPressed) {
            targetYRotation = baseYRotation + 60f;
        }
        else {
            targetYRotation = baseYRotation;
        }
        
        Vector3 currentEuler = player_transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(currentEuler.x, facingYawOffset + targetYRotation, currentEuler.z);
        player_transform.rotation = Quaternion.Slerp(player_transform.rotation, targetRotation, Time.deltaTime * 10f);

        // Keep IK pole directions in sync with mesh mirroring
        SyncIKMirrorWithScale();
    }
    
    void LateUpdate()
    {
        if (!enableIK)
            return;
        
        UpdateFootTargets();
        ApplyIK();
    }
    
    void UpdateFootTargets()
    {
        // Ensure we have the targets we need for the current state
        if (frontFootBoardTarget == null || backFootBoardTarget == null)
            return;
        
        bool performingTrick = trickController != null && trickController.isPerformingTrick;
        if (performingTrick && (frontFootTarget_non_parent == null || backFootTarget_non_parent == null))
            return;
        
        // Determine if board is flipped (roughly 180 degrees from start)
        float boardYRotation = board_transform.localEulerAngles.y;
        
        // Normalize to -180 to 180
        if (boardYRotation > 180f)
            boardYRotation -= 360f;
        
        isBoardFlipped = Mathf.Abs(boardYRotation) > 120f;
        
        // Smoothly interpolate lift amount based on trick state
        float targetLift = performingTrick ? 1f : 0f;
        currentLiftAmount = Mathf.Lerp(currentLiftAmount, targetLift, Time.deltaTime * liftTransitionSpeed);

        // Pick the base targets for the current state
        Transform leftTarget = performingTrick ? frontFootTarget_non_parent : frontFootBoardTarget;
        Transform rightTarget = performingTrick ? backFootTarget_non_parent : backFootBoardTarget;


        // Update IK weight and offsets per state
        if (performingTrick)
        {
            ikWeight = 0.8f;
            UpdateTrickFootOffsets();
        }
        else
        {
            ikWeight = 1f;
            leftFootOffset = Vector3.Lerp(leftFootOffset, Vector3.zero, Time.deltaTime * 10f);
            rightFootOffset = Vector3.Lerp(rightFootOffset, Vector3.zero, Time.deltaTime * 10f);
        }
        
        // Prepare offsets for swap logic (keep offset with its target)
        Vector3 leftOffset = leftFootOffset;
        Vector3 rightOffset = rightFootOffset;
        
        // Swap targets (and their offsets) when the board is flipped
        if (isBoardFlipped & !performingTrick)
        {
            (leftTarget, rightTarget) = (rightTarget, leftTarget);
            (leftOffset, rightOffset) = (rightOffset, leftOffset);
        }
        
        // Apply the trick-specific offsets
        Vector3 leftBasePos = leftTarget.position + leftTarget.TransformDirection(leftOffset);
        Vector3 rightBasePos = rightTarget.position + rightTarget.TransformDirection(rightOffset);
        
        // Add vertical lift during tricks (world space up direction)
        Vector3 liftOffset = Vector3.up * (trickLiftHeight * currentLiftAmount);
        
        leftFootTargetPos = leftBasePos + liftOffset;
        rightFootTargetPos = rightBasePos + liftOffset;
    }

    void SyncIKMirrorWithScale()
    {
        if (skater_mesh_transform == null)
            return;

        bool mirrored = skater_mesh_transform.localScale.x < 0f;
        if (mirrored == isIKMirrored)
            return;

        isIKMirrored = mirrored;
        SetIKMirror(mirrored);
    }

    public void SetIKMirror(bool mirrored)
    {
        isIKMirrored = mirrored;

        if (leftLegIK != null)
            leftLegIK.SetPoleFlip(mirrored);

        if (rightLegIK != null)
            rightLegIK.SetPoleFlip(mirrored);
    }
    
    void UpdateTrickFootOffsets()
    {
        if (trickController == null)
            return;
        
        int currentTrick = trickController.GetCurrentTrick();
        if (currentTrick == -1)
            return;
        
        TrickFootPattern pattern = GetTrickPattern(currentTrick);
        if (pattern == null)
            return;
        
        float trickProgress = trickController.GetTrickProgress();
        
        leftFootOffset = pattern.EvaluateLeftFoot(trickProgress);
        rightFootOffset = pattern.EvaluateRightFoot(trickProgress);
    }
    
    void ApplyIK()
    {
        if (leftLegIK != null)
        {
            leftLegIK.Solve(leftFootTargetPos, ikWeight);
        }
        
        if (rightLegIK != null)
        {
            rightLegIK.Solve(rightFootTargetPos, ikWeight);
        }
    }
    
    TrickFootPattern GetTrickPattern(int trickType)
    {
        switch (trickType)
        {
            case 0: return new KickflipPattern();
            case 1: return new ShuvitPattern();
            case 2: return new HeelflipPattern();
            case 3: return new VarialKickflipPattern();
            case 4: return new VarialHeelflipPattern();
            default: return null;
        }
    }
    
    public void AddFootOffset(Vector3 leftOffset, Vector3 rightOffset)
    {
        this.leftFootOffset = leftOffset;
        this.rightFootOffset = rightOffset;
    }
    
    float GetSignedYaw(Transform t)
    {
        return Mathf.DeltaAngle(0f, t.localEulerAngles.y);
    }

    public bool YawIsPositive() {
        return GetSignedYaw(skater_mesh_transform) >= 0f;
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
        
        turn180AngleRemaining -= step;

        if (normalized >= 1f || Mathf.Approximately(turn180AngleRemaining, 0f))
        {
            turn180AngleRemaining = 0f;
            isTurning180 = false;
        }
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
    
    void OnDrawGizmos()
    {
        
        // Draw board targets
        if (frontFootBoardTarget != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.7f); // Green for front
            Gizmos.DrawWireSphere(frontFootBoardTarget.position, 0.08f);
            Gizmos.DrawWireSphere(frontFootTarget_non_parent.position, 0.04f);
            Gizmos.DrawRay(frontFootBoardTarget.position, frontFootBoardTarget.forward * 0.08f);
        }
        
        if (backFootBoardTarget != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.7f); // Yellow for back
            Gizmos.DrawWireSphere(backFootBoardTarget.position, 0.08f);
            Gizmos.DrawWireSphere(backFootTarget_non_parent.position, 0.04f);
            Gizmos.DrawRay(backFootBoardTarget.position, backFootBoardTarget.forward * 0.08f);
        }
        
        // Draw actual foot targets (after swap logic)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftFootTargetPos, 0.05f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rightFootTargetPos, 0.05f);
        
        // Draw lines from board targets to actual targets
        if (isBoardFlipped)
        {
            // Show swapped connections
            if (backFootBoardTarget != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawLine(backFootBoardTarget.position, leftFootTargetPos);
            }
            if (frontFootBoardTarget != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                Gizmos.DrawLine(frontFootBoardTarget.position, rightFootTargetPos);
            }
        }
        else
        {
            // Show normal connections
            if (frontFootBoardTarget != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawLine(frontFootBoardTarget.position, leftFootTargetPos);
            }
            if (backFootBoardTarget != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                Gizmos.DrawLine(backFootBoardTarget.position, rightFootTargetPos);
            }
        }
        
        if (leftLegIK != null)
            leftLegIK.DrawGizmos(Color.red);
        
        if (rightLegIK != null)
            rightLegIK.DrawGizmos(Color.blue);
    }
}

[System.Serializable]
public class LegIK
{
    public Transform root;
    public Transform mid;
    public Transform tip;
    
    public float upperLength;
    public float lowerLength;
    
    private Vector3 rootAxis;
    private Vector3 midAxis;
    
    private Vector3 defaultPoleDirection;
    private float poleFlipSign = 1f;
    
    public LegIK(Transform root, Transform mid, Transform tip)
    {
        this.root = root;
        this.mid = mid;
        this.tip = tip;
        
        upperLength = Vector3.Distance(root.position, mid.position);
        lowerLength = Vector3.Distance(mid.position, tip.position);
        
        Vector3 rootToMid = mid.position - root.position;
        rootAxis = root.InverseTransformDirection(rootToMid).normalized;
        
        Vector3 midToTip = tip.position - mid.position;
        midAxis = mid.InverseTransformDirection(midToTip).normalized;
        
        Vector3 rootToTip = tip.position - root.position;
        Vector3 rootToMidNorm = rootToMid.normalized;
        Vector3 midProjected = root.position + Vector3.Project(rootToMid, rootToTip);
        defaultPoleDirection = (mid.position - midProjected).normalized;
        
        if (defaultPoleDirection.sqrMagnitude < 0.01f)
            defaultPoleDirection = Vector3.Cross(rootToMidNorm, Vector3.up).normalized;
    }

    public void SetPoleFlip(bool mirrored)
    {
        // mirrored true means the character mesh is flipped on X, so flip the pole
        poleFlipSign = mirrored ? -1f : 1f;
    }
    
    public void Solve(Vector3 targetPos, float weight)
    {
        if (weight <= 0f) return;
        
        Vector3 rootPos = root.position;
        targetPos = Vector3.Lerp(tip.position, targetPos, weight);
        
        Vector3 rootToTarget = targetPos - rootPos;
        float targetDist = rootToTarget.magnitude;
        
        float maxDist = upperLength + lowerLength - 0.001f;
        float minDist = Mathf.Abs(upperLength - lowerLength) + 0.001f;
        
        if (targetDist > maxDist)
        {
            targetDist = maxDist;
            targetPos = rootPos + rootToTarget.normalized * maxDist;
            rootToTarget = targetPos - rootPos;
        }
        else if (targetDist < minDist)
        {
            targetDist = minDist;
            targetPos = rootPos + rootToTarget.normalized * minDist;
            rootToTarget = targetPos - rootPos;
        }
        
        float a = upperLength;
        float b = lowerLength;
        float c = targetDist;
        
        float cosRootAngle = (c * c + a * a - b * b) / (2f * c * a);
        cosRootAngle = Mathf.Clamp(cosRootAngle, -1f, 1f);
        float rootAngle = Mathf.Acos(cosRootAngle);
        
        Vector3 poleDir = GetPoleDirection(rootPos, targetPos);
        Vector3 targetDir = rootToTarget.normalized;
        Vector3 axis = Vector3.Cross(targetDir, poleDir).normalized;
        
        Quaternion rootRotation = Quaternion.AngleAxis(rootAngle * Mathf.Rad2Deg, axis);
        Vector3 midDir = rootRotation * targetDir;
        Vector3 midPos = rootPos + midDir * upperLength;
        
        Vector3 currentRootDir = root.TransformDirection(rootAxis);
        Vector3 desiredRootDir = (midPos - rootPos).normalized;
        
        Quaternion rootDelta = Quaternion.FromToRotation(currentRootDir, desiredRootDir);
        root.rotation = rootDelta * root.rotation;
        
        Vector3 currentMidDir = mid.TransformDirection(midAxis);
        Vector3 desiredMidDir = (targetPos - midPos).normalized;
        
        Quaternion midDelta = Quaternion.FromToRotation(currentMidDir, desiredMidDir);
        mid.rotation = midDelta * mid.rotation;
    }
    
    Vector3 GetPoleDirection(Vector3 rootPos, Vector3 targetPos)
    {
        Vector3 toTarget = (targetPos - rootPos).normalized;
        Vector3 poleDir = defaultPoleDirection * poleFlipSign;
        Vector3 polePlane = Vector3.ProjectOnPlane(poleDir, toTarget);
        
        if (polePlane.sqrMagnitude < 0.01f)
        {
            polePlane = Vector3.Cross(toTarget, Vector3.up);
            if (polePlane.sqrMagnitude < 0.01f)
                polePlane = Vector3.Cross(toTarget, Vector3.right);
        }
        
        return polePlane.normalized;
    }
    
    public void DrawGizmos(Color color)
    {
        if (root == null || mid == null || tip == null)
            return;
        
        Gizmos.color = color;
        Gizmos.DrawLine(root.position, mid.position);
        Gizmos.DrawLine(mid.position, tip.position);
        
        Gizmos.DrawWireSphere(root.position, 0.02f);
        Gizmos.DrawWireSphere(mid.position, 0.025f);
        Gizmos.DrawWireSphere(tip.position, 0.02f);
        
        Gizmos.color = Color.yellow;
        Vector3 pole = GetPoleDirection(root.position, tip.position);
        Gizmos.DrawRay(mid.position, pole * 0.1f);
    }
}