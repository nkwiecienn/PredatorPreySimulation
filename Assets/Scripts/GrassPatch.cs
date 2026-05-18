using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GrassPatch : MonoBehaviour
{
    [Header("Food Settings")]
    [SerializeField] private float maxFoodAmount = 100f;
    [SerializeField] private float foodPerBite = 10f;
    [SerializeField] private float regrowAmountPerSecond = 2f;

    [Header("Visual Feedback (optional)")]
    [SerializeField] private Material fullGrassMaterial;
    [SerializeField] private Material emptyGrassMaterial;

    private float currentFoodAmount;
    private Renderer grassRenderer;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        currentFoodAmount = maxFoodAmount;
        grassRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        int grassLayer = LayerMask.NameToLayer("Grass");
        if (grassLayer >= 0 && gameObject.layer != grassLayer)
            gameObject.layer = grassLayer;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        UpdateVisuals();
    }

    private void Update()
    {
        if (currentFoodAmount < maxFoodAmount)
        {
            currentFoodAmount = Mathf.Min(maxFoodAmount,
                currentFoodAmount + regrowAmountPerSecond * Time.deltaTime);
            UpdateVisuals();
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public float Eat(Agent agent)
    {
        if (agent == null || agent.AgentSpecies != Species.Prey) return 0f;
        if (currentFoodAmount <= 0f) return 0f;

        float range = agent.SpeciesData != null ? agent.SpeciesData.FeedingRange : 1.5f;
        if (Vector3.Distance(transform.position, agent.transform.position) > range) return 0f;

        float amountEaten = Mathf.Min(foodPerBite, currentFoodAmount);
        currentFoodAmount -= amountEaten;

        float energyRestored = (amountEaten / foodPerBite) *
                               (agent.SpeciesData != null ? agent.SpeciesData.EnergyRestoredByEating : 25f);
        agent.RestoreEnergy(energyRestored);
        UpdateVisuals();
        return energyRestored;
    }

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    public bool IsAvailable => currentFoodAmount > 0f;
    public float CurrentFoodAmount => currentFoodAmount;
    public float MaxFoodAmount => maxFoodAmount;

    // -------------------------------------------------------------------------
    // Visuals
    // -------------------------------------------------------------------------

    private void UpdateVisuals()
    {
        if (grassRenderer == null) return;

        float fill = maxFoodAmount > 0f ? currentFoodAmount / maxFoodAmount : 0f;

        Vector3 s = transform.localScale;
        s.y = Mathf.Max(0.1f, fill);
        transform.localScale = s;

        if (fullGrassMaterial != null && emptyGrassMaterial != null)
        {
            grassRenderer.sharedMaterial = fill > 0.5f ? fullGrassMaterial : emptyGrassMaterial;
        }
        else
        {
            Color tint = Color.Lerp(new Color(0.35f, 0.2f, 0.05f), new Color(0.15f, 0.6f, 0.15f), fill);
            grassRenderer.material.color = tint;
        }
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, transform.localScale);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}