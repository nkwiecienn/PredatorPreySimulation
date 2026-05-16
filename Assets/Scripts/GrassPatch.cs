using UnityEngine;

/// <summary>
/// Represents a grass patch that can be eaten by prey to restore energy.
/// Grass depletes when eaten and regrows over time.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GrassPatch : MonoBehaviour
{
    [Header("Grass Resources")]
    [SerializeField] private float maxFoodAmount = 100f;
    [SerializeField] private float foodPerBite = 10f;
    [SerializeField] private float regrowAmountPerSecond = 2f;

    [Header("Visual Feedback")]
    [SerializeField] private Material fullGrassMaterial;
    [SerializeField] private Material emptyGrassMaterial;

    // Runtime state
    private float currentFoodAmount;
    private Renderer grassRenderer;
    private bool isAvailable = true;

    private void Awake()
    {
        currentFoodAmount = maxFoodAmount;

        // Get the renderer for visual feedback
        grassRenderer = GetComponent<Renderer>();
        if (grassRenderer == null)
        {
            grassRenderer = GetComponentInChildren<Renderer>();
        }

        // Set initial layer to Grass if not already set
        if (gameObject.layer != LayerMask.NameToLayer("Grass"))
        {
            gameObject.layer = LayerMask.NameToLayer("Grass");
        }

        // Ensure collider is set as trigger for detection, not physics
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;  // Keep as regular collider for raycast detection
        }

        UpdateVisualFeedback();
    }

    private void Update()
    {
        // Regrow grass over time
        Regrow(Time.deltaTime);
    }

    /// <summary>
    /// Attempt to eat from this grass patch.
    /// Returns the amount of energy restored.
    /// </summary>
    public float Eat(Agent agent)
    {
        if (agent == null)
        {
            Debug.LogError("Cannot eat grass: agent is null");
            return 0f;
        }

        // Only prey can eat grass
        if (agent.AgentSpecies != Species.Prey)
        {
            Debug.LogWarning($"Agent {agent.AgentId} is not prey and cannot eat grass");
            return 0f;
        }

        // Check if there's food available
        if (currentFoodAmount <= 0f)
        {
            return 0f;
        }

        // Check if agent is close enough to eat
        float distanceToAgent = Vector3.Distance(transform.position, agent.transform.position);
        float feedingRange = agent.SpeciesData != null ? agent.SpeciesData.FeedingRange : 1.5f;

        if (distanceToAgent > feedingRange)
        {
            return 0f;
        }

        // Consume grass
        float amountEaten = Mathf.Min(foodPerBite, currentFoodAmount);
        currentFoodAmount -= amountEaten;

        // Restore agent energy based on amount eaten
        float energyRestored = (amountEaten / foodPerBite) *
                              (agent.SpeciesData != null ? agent.SpeciesData.EnergyRestoredByEating : 25f);
        agent.RestoreEnergy(energyRestored);

        UpdateVisualFeedback();

        return energyRestored;
    }

    /// <summary>
    /// Regrow grass over time.
    /// </summary>
    public void Regrow(float deltaTime)
    {
        if (currentFoodAmount < maxFoodAmount)
        {
            currentFoodAmount += regrowAmountPerSecond * deltaTime;
            currentFoodAmount = Mathf.Min(currentFoodAmount, maxFoodAmount);
            UpdateVisualFeedback();
        }
    }

    /// <summary>
    /// Get the food availability state.
    /// </summary>
    public bool IsAvailable => currentFoodAmount > 0f;

    /// <summary>
    /// Get current food amount (for debugging).
    /// </summary>
    public float CurrentFoodAmount => currentFoodAmount;

    /// <summary>
    /// Get max food amount (for debugging).
    /// </summary>
    public float MaxFoodAmount => maxFoodAmount;

    /// <summary>
    /// Update visual feedback based on grass state.
    /// Changes color/material based on fullness.
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (grassRenderer == null)
            return;

        // Scale visual size based on food amount
        float fillRatio = currentFoodAmount / maxFoodAmount;

        // Adjust local scale to show depletion
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Max(0.1f, fillRatio);  // Keep minimum height for visibility
        transform.localScale = scale;

        // Change material based on fullness
        if (fillRatio > 0.5f)
        {
            // Full grass - use full material if available
            if (fullGrassMaterial != null)
                grassRenderer.material = fullGrassMaterial;
        }
        else if (fillRatio > 0f)
        {
            // Partially depleted - use a blend or intermediate material
            // For now, adjust color alpha/brightness
            Color color = grassRenderer.material.color;
            color.a = fillRatio;
            grassRenderer.material.color = color;
        }
        else
        {
            // Empty grass - use empty material if available
            if (emptyGrassMaterial != null)
                grassRenderer.material = emptyGrassMaterial;
        }
    }

    /// <summary>
    /// Helper: Get energy restored value for this agent.
    /// </summary>
    private float GetAgentEnergyRestored(Agent agent)
    {
        // Access from SpeciesData via Agent if available
        if (agent.SpeciesData != null)
        {
            return agent.SpeciesData.EnergyRestoredByEating;
        }
        return 25f;  // Default
    }

    /// <summary>
    /// Debug visualization for grass patch in editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw grass patch bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);

        // Draw feeding range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
