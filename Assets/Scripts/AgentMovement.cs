using UnityEngine;

/// <summary>
/// Handles Rigidbody-based movement for agents.
/// Responsible for: TurnLeft, TurnRight, MoveForward, Idle.
/// </summary>
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

    /// <summary>
    /// Initialize movement parameters from SpeciesData.
    /// </summary>
    public void Initialize(float speed, float angularVelocity)
    {
        moveSpeed = speed;
        maxAngularVelocity = angularVelocity;
    }

    /// <summary>
    /// Rotate the agent left (counter-clockwise around Y axis).
    /// </summary>
    public void TurnLeft()
    {
        targetAngularVelocity = maxAngularVelocity;
    }

    /// <summary>
    /// Rotate the agent right (clockwise around Y axis).
    /// </summary>
    public void TurnRight()
    {
        targetAngularVelocity = -maxAngularVelocity;
    }

    /// <summary>
    /// Move the agent forward in its facing direction.
    /// </summary>
    public void MoveForward()
    {
        targetVelocity = transform.forward * moveSpeed;
    }

    /// <summary>
    /// Stop all movement and rotation.
    /// </summary>
    public void Idle()
    {
        targetVelocity = Vector3.zero;
        targetAngularVelocity = 0f;
    }

    /// <summary>
    /// Apply movement physics in FixedUpdate.
    /// Called by Agent to ensure proper Rigidbody integration.
    /// </summary>
    public void ApplyMovement()
    {
        if (rb == null)
            return;

        // Apply velocity
        rb.velocity = targetVelocity;

        // Apply angular velocity (rotation around Y axis only)
        rb.angularVelocity = Vector3.up * (targetAngularVelocity * Mathf.Deg2Rad);
    }

    /// <summary>
    /// Clear movement intent each frame (actions must be re-applied).
    /// </summary>
    public void ResetMovement()
    {
        targetVelocity = Vector3.zero;
        targetAngularVelocity = 0f;
    }
}
