using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Agent))]
public class AgentPerception : MonoBehaviour
{
    [Header("Perception Settings")]
    [SerializeField] private int rayCount = 16;
    [SerializeField] private LayerMask detectableLayers;

    [Tooltip("Vertical offset from transform.position for ray origin. " +
             "Set to ~half the agent capsule height so rays aren't fired from ground level.")]
    [SerializeField] private float rayHeightOffset = 0.5f;

    private float viewRadius;
    private float viewAngle;

    private readonly List<PerceptionObject> visibleObjects = new List<PerceptionObject>();
    private readonly List<PerceptionObject> visibleGrass = new List<PerceptionObject>();
    private readonly List<PerceptionObject> visibleShelters = new List<PerceptionObject>();
    private readonly List<PerceptionObject> visiblePredators = new List<PerceptionObject>();
    private readonly List<PerceptionObject> visiblePrey = new List<PerceptionObject>();
    private readonly List<PerceptionObject> visibleObstacles = new List<PerceptionObject>();

    private readonly Dictionary<GameObject, PerceptionObject> dedupeBuffer
        = new Dictionary<GameObject, PerceptionObject>();

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    public void Initialize(float radius, float angle)
    {
        viewRadius = radius;
        viewAngle = angle;
    }

    // -------------------------------------------------------------------------
    // Public getters
    // -------------------------------------------------------------------------

    public List<PerceptionObject> VisibleObjects => visibleObjects;
    public List<PerceptionObject> VisibleGrass => visibleGrass;
    public List<PerceptionObject> VisibleShelters => visibleShelters;
    public List<PerceptionObject> VisiblePredators => visiblePredators;
    public List<PerceptionObject> VisiblePrey => visiblePrey;
    public List<PerceptionObject> VisibleObstacles => visibleObstacles;

    public List<GrassPatch> GetVisibleGrassPatches()
    {
        var result = new List<GrassPatch>(visibleGrass.Count);
        foreach (var p in visibleGrass)
        {
            var g = p.gameObject.GetComponent<GrassPatch>();
            if (g != null) result.Add(g);
        }
        return result;
    }

    public List<ShelterZone> GetVisibleShelterZones()
    {
        var result = new List<ShelterZone>(visibleShelters.Count);
        foreach (var p in visibleShelters)
        {
            var s = p.gameObject.GetComponent<ShelterZone>();
            if (s != null) result.Add(s);
        }
        return result;
    }

    public List<Agent> GetVisiblePreyAgents()
    {
        var result = new List<Agent>(visiblePrey.Count);
        foreach (var p in visiblePrey)
        {
            var a = p.gameObject.GetComponent<Agent>();
            if (a != null) result.Add(a);
        }
        return result;
    }

    public List<Agent> GetVisiblePredatorAgents()
    {
        var result = new List<Agent>(visiblePredators.Count);
        foreach (var p in visiblePredators)
        {
            var a = p.gameObject.GetComponent<Agent>();
            if (a != null) result.Add(a);
        }
        return result;
    }

    // -------------------------------------------------------------------------
    // Perception update — called every decision interval by Agent
    // -------------------------------------------------------------------------

    public void UpdatePerception()
    {
        visibleObjects.Clear();
        visibleGrass.Clear();
        visibleShelters.Clear();
        visiblePredators.Clear();
        visiblePrey.Clear();
        visibleObstacles.Clear();
        dedupeBuffer.Clear();

        if (viewRadius <= 0f) return;

        Vector3 rayOrigin = transform.position + Vector3.up * rayHeightOffset;
        int count = Mathf.Max(1, rayCount);

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float rayAngle = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, t);
            Vector3 dir = Quaternion.Euler(0f, rayAngle, 0f) * transform.forward;

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, dir, viewRadius, detectableLayers);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue;

                PerceptionObjectType type = ClassifyLayer(hit.collider.gameObject.layer);
                if (type == PerceptionObjectType.Unknown) continue;

                GameObject go = hit.collider.gameObject;

                if (!dedupeBuffer.TryGetValue(go, out PerceptionObject existing)
                    || hit.distance < existing.distance)
                {
                    dedupeBuffer[go] = new PerceptionObject
                    {
                        objectType = type,
                        gameObject = go,
                        distance = hit.distance,
                        direction = dir
                    };
                }
            }
        }

        foreach (PerceptionObject obj in dedupeBuffer.Values)
        {
            visibleObjects.Add(obj);
            CategorizeAndStore(obj);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static PerceptionObjectType ClassifyLayer(int layer)
    {
        string name = LayerMask.LayerToName(layer);
        switch (name)
        {
            case "Grass": return PerceptionObjectType.Grass;
            case "Shelter": return PerceptionObjectType.Shelter;
            case "Predator": return PerceptionObjectType.Predator;
            case "Prey": return PerceptionObjectType.Prey;
            case "Obstacle": return PerceptionObjectType.Obstacle;
            default: return PerceptionObjectType.Unknown;
        }
    }

    private void CategorizeAndStore(PerceptionObject obj)
    {
        switch (obj.objectType)
        {
            case PerceptionObjectType.Grass: visibleGrass.Add(obj); break;
            case PerceptionObjectType.Shelter: visibleShelters.Add(obj); break;
            case PerceptionObjectType.Predator: visiblePredators.Add(obj); break;
            case PerceptionObjectType.Prey: visiblePrey.Add(obj); break;
            case PerceptionObjectType.Obstacle: visibleObstacles.Add(obj); break;
        }
    }

    // -------------------------------------------------------------------------
    // Gizmos — visible in Scene view when the agent is selected
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (viewRadius <= 0f) return;

        Vector3 origin = transform.position + Vector3.up * rayHeightOffset;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(origin, viewRadius);

        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, left * viewRadius);
        Gizmos.DrawRay(origin, right * viewRadius);

        int count = Mathf.Max(1, rayCount);
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float ang = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, t);
            Vector3 d = Quaternion.Euler(0f, ang, 0f) * transform.forward;
            Gizmos.DrawLine(origin, origin + d * viewRadius);
        }

        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        foreach (var o in visibleGrass)
            Gizmos.DrawSphere(o.gameObject.transform.position, 0.2f);

        Gizmos.color = Color.blue;
        foreach (var o in visibleShelters)
            Gizmos.DrawSphere(o.gameObject.transform.position, 0.2f);

        Gizmos.color = Color.red;
        foreach (var o in visiblePredators)
            Gizmos.DrawSphere(o.gameObject.transform.position, 0.2f);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        foreach (var o in visiblePrey)
            Gizmos.DrawSphere(o.gameObject.transform.position, 0.2f);

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
        foreach (var o in visibleObstacles)
            Gizmos.DrawSphere(o.gameObject.transform.position, 0.2f);
    }
}

// =============================================================================
// Supporting types
// =============================================================================

public class PerceptionObject
{
    public PerceptionObjectType objectType;
    public GameObject gameObject;
    public float distance;
    public Vector3 direction;
}

public enum PerceptionObjectType { Grass, Shelter, Predator, Prey, Obstacle, Unknown }