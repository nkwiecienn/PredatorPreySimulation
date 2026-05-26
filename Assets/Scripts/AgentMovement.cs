using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentMovement : MonoBehaviour
{
    [Header("Avoidance")]
    [SerializeField] private Vector3 worldCenter = Vector3.zero;
    [SerializeField] private Vector3 worldSize = new Vector3(96f, 0f, 96f);
    [SerializeField] private float boundaryMargin = 8f;
    [SerializeField] private float obstacleAvoidanceDistance = 2.5f;
    [SerializeField] private float obstacleProbeRadius = 0.45f;
    [SerializeField] private LayerMask obstacleLayers = 1;

    private Rigidbody rb;
    private float moveSpeed;
    private float maxAngularVelocity;
    private Vector3 targetVelocity = Vector3.zero;
    private float targetAngularVelocity = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(float speed, float angularVelocity)
    {
        moveSpeed = speed;
        maxAngularVelocity = angularVelocity;
    }

    public void TurnLeft()
    {
        targetAngularVelocity = maxAngularVelocity;
        DebugLogger.LogMovement(gameObject.name, AgentAction.TurnLeft);
    }

    public void TurnRight()
    {
        targetAngularVelocity = -maxAngularVelocity;
        DebugLogger.LogMovement(gameObject.name, AgentAction.TurnRight);
    }

    public void MoveForward()
    {
        targetVelocity = transform.forward * moveSpeed;
        DebugLogger.LogMovement(gameObject.name, AgentAction.MoveForward);
    }

    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            Idle();
            return;
        }

        Vector3 normalizedDirection = direction.normalized;
        targetVelocity = normalizedDirection * moveSpeed;
        targetAngularVelocity = 0f;
        transform.rotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
    }

    public void MoveAwayFrom(Vector3 targetPosition)
    {
        Vector3 direction = transform.position - targetPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            direction = transform.forward;

        MoveTowards(transform.position + direction.normalized);
    }

    public void Idle()
    {
        targetVelocity = Vector3.zero;
        targetAngularVelocity = 0f;
    }

    public void ApplyMovement()
    {
        if (rb == null)
            return;

        Vector3 steeredVelocity = ApplyAvoidance(targetVelocity);
        rb.linearVelocity = steeredVelocity;

        rb.angularVelocity = Vector3.up * (targetAngularVelocity * Mathf.Deg2Rad);
    }

    public void ResetMovement()
    {
        targetVelocity = Vector3.zero;
        targetAngularVelocity = 0f;
    }

    private Vector3 ApplyAvoidance(Vector3 desiredVelocity)
    {
        Vector3 correction = Vector3.zero;
        Vector3 position = transform.position;

        float halfX = worldSize.x * 0.5f;
        float halfZ = worldSize.z * 0.5f;
        float minX = worldCenter.x - halfX + boundaryMargin;
        float maxX = worldCenter.x + halfX - boundaryMargin;
        float minZ = worldCenter.z - halfZ + boundaryMargin;
        float maxZ = worldCenter.z + halfZ - boundaryMargin;

        if (position.x < minX) correction += Vector3.right * (minX - position.x);
        else if (position.x > maxX) correction += Vector3.left * (position.x - maxX);

        if (position.z < minZ) correction += Vector3.forward * (minZ - position.z);
        else if (position.z > maxZ) correction += Vector3.back * (position.z - maxZ);

        Vector3 flatVelocity = desiredVelocity;
        flatVelocity.y = 0f;

        if (flatVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 origin = position + Vector3.up * 0.5f;
            Vector3 direction = flatVelocity.normalized;

            if (Physics.SphereCast(origin, obstacleProbeRadius, direction, out RaycastHit hit,
                    obstacleAvoidanceDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 away = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
                if (away.sqrMagnitude <= 0.01f)
                    away = hit.normal;

                correction += (away + hit.normal).normalized * moveSpeed;
            }
        }

        if (correction.sqrMagnitude <= 0.01f)
            return desiredVelocity;

        Vector3 correctedDirection = (flatVelocity + correction * moveSpeed).normalized;
        if (correctedDirection.sqrMagnitude <= 0.01f)
            correctedDirection = correction.normalized;

        transform.rotation = Quaternion.LookRotation(correctedDirection, Vector3.up);
        targetAngularVelocity = 0f;
        return correctedDirection * moveSpeed;
    }
}
