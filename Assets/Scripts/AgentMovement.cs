using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentMovement : MonoBehaviour
{
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
    }

    public void TurnRight()
    {
        targetAngularVelocity = -maxAngularVelocity;
    }

    public void MoveForward()
    {
        targetVelocity = transform.forward * moveSpeed;
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

        rb.linearVelocity = targetVelocity;

        rb.angularVelocity = Vector3.up * (targetAngularVelocity * Mathf.Deg2Rad);
    }

    public void ResetMovement()
    {
        targetVelocity = Vector3.zero;
        targetAngularVelocity = 0f;
    }
}
