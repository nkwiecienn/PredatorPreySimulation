using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ShelterZone : MonoBehaviour
{
    [Header("Shelter Settings")]
    [SerializeField] private int shelterCapacity = 10;

    private readonly List<Agent> preyInside = new List<Agent>();
    private Collider shelterCollider;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        shelterCollider = GetComponent<Collider>();
        if (shelterCollider == null)
        {
            Debug.LogError($"ShelterZone '{gameObject.name}' has no Collider.");
            return;
        }

        shelterCollider.isTrigger = true;

        int shelterLayer = LayerMask.NameToLayer("Shelter");
        if (shelterLayer >= 0 && gameObject.layer != shelterLayer)
            gameObject.layer = shelterLayer;
    }

    // -------------------------------------------------------------------------
    // Trigger events — these are the single source of truth for shelter state
    // -------------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        Agent agent = other.GetComponent<Agent>();
        if (agent == null || agent.AgentSpecies != Species.Prey) return;
        if (preyInside.Contains(agent)) return;

        preyInside.Add(agent);
        agent.SetInShelter(true);
    }

    private void OnTriggerExit(Collider other)
    {
        Agent agent = other.GetComponent<Agent>();
        if (agent == null) return;

        preyInside.Remove(agent);
        agent.SetInShelter(false);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public bool IsAgentInside(Agent agent) => agent != null && preyInside.Contains(agent);

    public bool HasCapacity()
    {
        preyInside.RemoveAll(p => p == null);
        return preyInside.Count < shelterCapacity;
    }

    public List<Agent> GetPreyInside()
    {
        preyInside.RemoveAll(p => p == null);
        return new List<Agent>(preyInside);
    }

    public int GetOccupancy() => GetPreyInside().Count;
    public int GetCapacity() => shelterCapacity;

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (shelterCollider != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
            Gizmos.DrawCube(shelterCollider.bounds.center, shelterCollider.bounds.size);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(shelterCollider.bounds.center, shelterCollider.bounds.size);
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Shelter: {GetOccupancy()}/{shelterCapacity}");
        }
#endif
    }
}