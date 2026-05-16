using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a shelter zone where prey can hide from predators.
/// Shelter zones are trigger volumes that protect prey from attacks.
/// Prey inside cannot be attacked by predators, but also cannot eat grass.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShelterZone : MonoBehaviour
{
    [Header("Shelter Settings")]
    [SerializeField] private float shelterCapacity = 10f;  // Max prey that can hide simultaneously

    [Header("Protection Settings")]
    [SerializeField] private bool blocksPredatorAttacks = true;  // Predators cannot attack prey inside
    [SerializeField] private float protectionRadius = 5f;  // Radius of shelter protection

    // Runtime state
    private List<Agent> preyInside = new List<Agent>();
    private Collider shelterCollider;

    private void Awake()
    {
        shelterCollider = GetComponent<Collider>();
        if (shelterCollider == null)
        {
            Debug.LogError($"ShelterZone {gameObject.name} requires a Collider component");
            return;
        }

        // Shelter zones should be triggers for detection
        shelterCollider.isTrigger = true;

        // Set layer to Shelter if not already set
        if (gameObject.layer != LayerMask.NameToLayer("Shelter"))
        {
            gameObject.layer = LayerMask.NameToLayer("Shelter");
        }
    }

    /// <summary>
    /// Called when a collider enters the shelter trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Agent agent = other.GetComponent<Agent>();
        if (agent != null && agent.AgentSpecies == Species.Prey)
        {
            // Only add if not already inside (prevent duplicates)
            if (!preyInside.Contains(agent))
            {
                preyInside.Add(agent);
            }
        }
    }

    /// <summary>
    /// Called when a collider exits the shelter trigger.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        Agent agent = other.GetComponent<Agent>();
        if (agent != null)
        {
            preyInside.Remove(agent);
        }
    }

    /// <summary>
    /// Check if an agent is currently inside this shelter.
    /// </summary>
    public bool IsAgentInside(Agent agent)
    {
        return agent != null && preyInside.Contains(agent);
    }

    /// <summary>
    /// Get all prey currently inside this shelter.
    /// </summary>
    public List<Agent> GetPreyInside()
    {
        // Clean up null entries (dead agents)
        preyInside.RemoveAll(prey => prey == null);
        return new List<Agent>(preyInside);
    }

    /// <summary>
    /// Check if shelter has capacity for more prey.
    /// </summary>
    public bool HasCapacity()
    {
        return preyInside.Count < shelterCapacity;
    }

    /// <summary>
    /// Get current occupancy (for debugging).
    /// </summary>
    public int GetOccupancy()
    {
        preyInside.RemoveAll(prey => prey == null);
        return preyInside.Count;
    }

    /// <summary>
    /// Get shelter capacity (for debugging).
    /// </summary>
    public float GetCapacity()
    {
        return shelterCapacity;
    }

    /// <summary>
    /// Check if a prey agent at a position is protected from a predator attack.
    /// </summary>
    public bool ProtectsFromAttack(Agent preyAgent, Agent predatorAgent)
    {
        if (!blocksPredatorAttacks)
            return false;

        if (preyAgent == null || predatorAgent == null)
            return false;

        // Prey must be inside this shelter
        if (!IsAgentInside(preyAgent))
            return false;

        return true;
    }

    /// <summary>
    /// Debug visualization for shelter zone in editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw shelter bounds
        Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
        if (shelterCollider != null)
        {
            Gizmos.DrawCube(transform.position + shelterCollider.bounds.center - transform.position,
                           shelterCollider.bounds.size);
        }

        // Draw protection radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, protectionRadius);

        // Draw capacity indicator
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            string label = $"Shelter: {GetOccupancy()}/{shelterCapacity}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }
    }
}
