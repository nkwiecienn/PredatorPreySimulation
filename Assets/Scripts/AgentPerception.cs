using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles local raycast perception for agents.
/// Agents only perceive nearby objects within their view radius and view angle.
/// Detects: Grass, Shelter, Predator, Prey, Obstacles.
/// </summary>
[RequireComponent(typeof(Agent))]
public class AgentPerception : MonoBehaviour
{
    [Header("Perception Settings")]
    [SerializeField] private int rayCount = 16;  // Number of rays in the cone
    [SerializeField] private LayerMask detectableLayers;

    [Header("Detected Objects")]
    private List<PerceptionObject> visibleObjects = new List<PerceptionObject>();
    private List<PerceptionObject> visibleGrass = new List<PerceptionObject>();
    private List<PerceptionObject> visibleShelters = new List<PerceptionObject>();
    private List<PerceptionObject> visiblePredators = new List<PerceptionObject>();
    private List<PerceptionObject> visiblePrey = new List<PerceptionObject>();
    private List<PerceptionObject> visibleObstacles = new List<PerceptionObject>();

    private Agent agent;
    private float viewRadius;
    private float viewAngle;

    private void Awake()
    {
        agent = GetComponent<Agent>();
    }

    /// <summary>
    /// Initialize perception parameters from agent's SpeciesData.
    /// </summary>
    public void Initialize(float radius, float angle)
    {
        viewRadius = radius;
        viewAngle = angle;
    }

    /// <summary>
    /// Update perception: raycast cone and collect visible objects.
    /// Called each decision interval by Agent.MakeDecision().
    /// </summary>
    public void UpdatePerception()
    {
        // Clear previous perception
        visibleObjects.Clear();
        visibleGrass.Clear();
        visibleShelters.Clear();
        visiblePredators.Clear();
        visiblePrey.Clear();
        visibleObstacles.Clear();

        // Cast rays in a cone pattern
        int raysTocast = Mathf.Max(1, rayCount);
        for (int i = 0; i < raysTocast; i++)
        {
            float angle = Mathf.Lerp(
                -viewAngle * 0.5f,
                viewAngle * 0.5f,
                raysTocast > 1 ? (float)i / (raysTocast - 1) : 0.5f
            );

            Vector3 rayDirection = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Ray ray = new Ray(transform.position, rayDirection);

            // Raycast and collect all hits
            RaycastHit[] hits = Physics.RaycastAll(ray, viewRadius, detectableLayers);

            foreach (RaycastHit hit in hits)
            {
                // Skip self
                if (hit.collider.gameObject == gameObject)
                    continue;

                // Categorize the detected object
                PerceptionObjectType objectType = CategorizeObject(hit.collider);
                if (objectType != PerceptionObjectType.Unknown)
                {
                    PerceptionObject perceptionObj = new PerceptionObject
                    {
                        objectType = objectType,
                        gameObject = hit.collider.gameObject,
                        distance = hit.distance,
                        direction = rayDirection,
                        component = hit.collider.GetComponent<MonoBehaviour>()
                    };

                    // Add to general list and specific category list
                    visibleObjects.Add(perceptionObj);
                    CategorizeAndStore(perceptionObj);
                }
            }
        }

        // Remove duplicates (same object detected by multiple rays)
        RemoveDuplicates();
    }

    /// <summary>
    /// Categorize a detected object by its layer or component type.
    /// </summary>
    private PerceptionObjectType CategorizeObject(Collider collider)
    {
        int layer = collider.gameObject.layer;
        string layerName = LayerMask.LayerToName(layer);

        // Layer-based detection
        if (layerName == "Grass")
            return PerceptionObjectType.Grass;
        if (layerName == "Shelter")
            return PerceptionObjectType.Shelter;
        if (layerName == "Predator")
            return PerceptionObjectType.Predator;
        if (layerName == "Prey")
            return PerceptionObjectType.Prey;
        if (layerName == "Obstacle")
            return PerceptionObjectType.Obstacle;

        return PerceptionObjectType.Unknown;
    }

    /// <summary>
    /// Store the perception object in the appropriate category list.
    /// </summary>
    private void CategorizeAndStore(PerceptionObject obj)
    {
        switch (obj.objectType)
        {
            case PerceptionObjectType.Grass:
                visibleGrass.Add(obj);
                break;
            case PerceptionObjectType.Shelter:
                visibleShelters.Add(obj);
                break;
            case PerceptionObjectType.Predator:
                visiblePredators.Add(obj);
                break;
            case PerceptionObjectType.Prey:
                visiblePrey.Add(obj);
                break;
            case PerceptionObjectType.Obstacle:
                visibleObstacles.Add(obj);
                break;
        }
    }

    /// <summary>
    /// Remove duplicate detections of the same object.
    /// (Same object may be hit by multiple rays in the cone.)
    /// </summary>
    private void RemoveDuplicates()
    {
        // Simple deduplication: if same GameObject detected multiple times,
        // keep only the closest instance
        Dictionary<GameObject, PerceptionObject> closestPerObject = new Dictionary<GameObject, PerceptionObject>();

        foreach (PerceptionObject obj in visibleObjects)
        {
            if (!closestPerObject.ContainsKey(obj.gameObject) ||
                obj.distance < closestPerObject[obj.gameObject].distance)
            {
                closestPerObject[obj.gameObject] = obj;
            }
        }

        // Rebuild lists with deduplicated objects
        visibleObjects.Clear();
        visibleGrass.Clear();
        visibleShelters.Clear();
        visiblePredators.Clear();
        visiblePrey.Clear();
        visibleObstacles.Clear();

        foreach (PerceptionObject obj in closestPerObject.Values)
        {
            visibleObjects.Add(obj);
            CategorizeAndStore(obj);
        }
    }

    /// <summary>
    /// Getters for perception data.
    /// </summary>
    public List<PerceptionObject> VisibleObjects => visibleObjects;
    public List<PerceptionObject> VisibleGrass => visibleGrass;
    public List<PerceptionObject> VisibleShelters => visibleShelters;
    public List<PerceptionObject> VisiblePredators => visiblePredators;
    public List<PerceptionObject> VisiblePrey => visiblePrey;
    public List<PerceptionObject> VisibleObstacles => visibleObstacles;

    /// <summary>
    /// Get visible grass patches as GrassPatch components.
    /// Converts PerceptionObjects to actual GrassPatch references for easier use.
    /// </summary>
    public List<GrassPatch> GetVisibleGrassPatches()
    {
        List<GrassPatch> grassPatches = new List<GrassPatch>();
        foreach (PerceptionObject perceptionGrass in visibleGrass)
        {
            GrassPatch grass = perceptionGrass.gameObject.GetComponent<GrassPatch>();
            if (grass != null)
            {
                grassPatches.Add(grass);
            }
        }
        return grassPatches;
    }

    /// <summary>
    /// Get visible shelter zones as ShelterZone components.
    /// Converts PerceptionObjects to actual ShelterZone references for easier use.
    /// </summary>
    public List<ShelterZone> GetVisibleShelterZones()
    {
        List<ShelterZone> shelterZones = new List<ShelterZone>();
        foreach (PerceptionObject perceptionShelter in visibleShelters)
        {
            ShelterZone shelter = perceptionShelter.gameObject.GetComponent<ShelterZone>();
            if (shelter != null)
            {
                shelterZones.Add(shelter);
            }
        }
        return shelterZones;
    }

    /// <summary>
    /// Get visible prey agents as Agent components.
    /// Converts PerceptionObjects to actual Agent references for easier use.
    /// </summary>
    public List<Agent> GetVisiblePreyAgents()
    {
        List<Agent> preyAgents = new List<Agent>();
        foreach (PerceptionObject perceptionPrey in visiblePrey)
        {
            Agent prey = perceptionPrey.gameObject.GetComponent<Agent>();
            if (prey != null)
            {
                preyAgents.Add(prey);
            }
        }
        return preyAgents;
    }

    /// <summary>
    /// Get visible predator agents as Agent components.
    /// Converts PerceptionObjects to actual Agent references for easier use.
    /// </summary>
    public List<Agent> GetVisiblePredatorAgents()
    {
        List<Agent> predatorAgents = new List<Agent>();
        foreach (PerceptionObject perceptionPredator in visiblePredators)
        {
            Agent predator = perceptionPredator.gameObject.GetComponent<Agent>();
            if (predator != null)
            {
                predatorAgents.Add(predator);
            }
        }
        return predatorAgents;
    }

    /// <summary>
    /// Draw debug visualization in Scene view when agent is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        if (agent == null)
            agent = GetComponent<Agent>();

        if (agent == null || viewRadius <= 0)
            return;

        // Draw all detected objects
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, viewRadius * 0.95f);

        // Draw rays (colored by detection type)
        int raysTocast = Mathf.Max(1, rayCount);
        for (int i = 0; i < raysTocast; i++)
        {
            float angle = Mathf.Lerp(
                -viewAngle * 0.5f,
                viewAngle * 0.5f,
                raysTocast > 1 ? (float)i / (raysTocast - 1) : 0.5f
            );

            Vector3 rayDirection = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            // Draw ray line (thin gray)
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(transform.position, transform.position + rayDirection * viewRadius);
        }

        // Draw colored spheres at detected object positions
        Gizmos.color = Color.green;
        foreach (PerceptionObject grass in visibleGrass)
        {
            Gizmos.DrawSphere(grass.gameObject.transform.position, 0.2f);
        }

        Gizmos.color = Color.blue;
        foreach (PerceptionObject shelter in visibleShelters)
        {
            Gizmos.DrawSphere(shelter.gameObject.transform.position, 0.2f);
        }

        Gizmos.color = Color.red;
        foreach (PerceptionObject predator in visiblePredators)
        {
            Gizmos.DrawSphere(predator.gameObject.transform.position, 0.2f);
        }

        Gizmos.color = new Color(1f, 0.5f, 0f);  // Orange for prey
        foreach (PerceptionObject prey in visiblePrey)
        {
            Gizmos.DrawSphere(prey.gameObject.transform.position, 0.2f);
        }

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f);  // Dark gray for obstacles
        foreach (PerceptionObject obstacle in visibleObstacles)
        {
            Gizmos.DrawSphere(obstacle.gameObject.transform.position, 0.2f);
        }
    }
}

/// <summary>
/// Data structure for a perceived object.
/// </summary>
public class PerceptionObject
{
    public PerceptionObjectType objectType;
    public GameObject gameObject;
    public float distance;
    public Vector3 direction;
    public MonoBehaviour component;  // Reference to GrassPatch, ShelterZone, Agent, etc.
}

/// <summary>
/// Enum for categorizing detected objects.
/// </summary>
public enum PerceptionObjectType
{
    Grass,
    Shelter,
    Predator,
    Prey,
    Obstacle,
    Unknown
}
