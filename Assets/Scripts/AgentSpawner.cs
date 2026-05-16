using UnityEngine;

/// <summary>
/// Responsible for spawning the initial population of agents at the start of the simulation.
/// This component should be added to a GameObject in the scene and configured with prefab references.
/// </summary>
public class AgentSpawner : MonoBehaviour
{
    [Header("Agent Prefabs")]
    [SerializeField] private GameObject preyAdultPrefab;
    [SerializeField] private GameObject preyJuvenilePrefab;
    [SerializeField] private GameObject predatorAdultPrefab;
    [SerializeField] private GameObject predatorJuvenilePrefab;

    [Header("Initial Population")]
    [SerializeField] private int initialPreyAdults = 20;
    [SerializeField] private int initialPreyJuveniles = 10;
    [SerializeField] private int initialPredatorAdults = 4;
    [SerializeField] private int initialPredatorJuveniles = 2;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(50f, 50f, 50f);

    [Header("Terrain Detection")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float raycastHeight = 100f;

    private void Start()
    {
        // Spawn all initial agents
        SpawnInitialPopulation();
    }

    /// <summary>
    /// Spawn all initial agents based on configured population sizes.
    /// </summary>
    private void SpawnInitialPopulation()
    {
        Debug.Log("Starting to spawn initial population...");

        // Spawn prey adults
        for (int i = 0; i < initialPreyAdults; i++)
        {
            SpawnAgent(preyAdultPrefab, $"PreyAdult_{i}");
        }

        // Spawn prey juveniles
        for (int i = 0; i < initialPreyJuveniles; i++)
        {
            SpawnAgent(preyJuvenilePrefab, $"PreyJuvenile_{i}");
        }

        // Spawn predator adults
        for (int i = 0; i < initialPredatorAdults; i++)
        {
            SpawnAgent(predatorAdultPrefab, $"PredatorAdult_{i}");
        }

        // Spawn predator juveniles
        for (int i = 0; i < initialPredatorJuveniles; i++)
        {
            SpawnAgent(predatorJuvenilePrefab, $"PredatorJuvenile_{i}");
        }

        int totalSpawned = initialPreyAdults + initialPreyJuveniles + initialPredatorAdults + initialPredatorJuveniles;
        Debug.Log($"Spawned {totalSpawned} initial agents");
    }

    /// <summary>
    /// Spawn a single agent at a random position in the spawn area.
    /// </summary>
    private void SpawnAgent(GameObject prefab, string agentId)
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot spawn agent: prefab is null");
            return;
        }

        // Get random position in spawn area
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // Raycast down to find terrain
        if (!RaycastToTerrain(spawnPosition, out Vector3 terrainPosition))
        {
            Debug.LogWarning($"Failed to find terrain for agent {agentId}. Using spawn position with y=0");
            spawnPosition.y = 0f;
        }
        else
        {
            spawnPosition = terrainPosition;
        }

        // Instantiate the agent prefab
        GameObject agentInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        agentInstance.name = agentId;

        // Set the agent ID in the Agent component
        Agent agentComponent = agentInstance.GetComponent<Agent>();
        if (agentComponent != null)
        {
            // Access the private agentId field via reflection or set it through a public method
            // For now, we'll rely on the Inspector-set agentId being overridden by name
            agentInstance.name = agentId;
        }
        else
        {
            Debug.LogError($"Prefab {prefab.name} does not have an Agent component");
            Destroy(agentInstance);
        }
    }

    /// <summary>
    /// Get a random position within the spawn area.
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(
            spawnAreaCenter.x - spawnAreaSize.x * 0.5f,
            spawnAreaCenter.x + spawnAreaSize.x * 0.5f
        );

        float randomZ = Random.Range(
            spawnAreaCenter.z - spawnAreaSize.z * 0.5f,
            spawnAreaCenter.z + spawnAreaSize.z * 0.5f
        );

        // Start high for raycast
        return new Vector3(randomX, raycastHeight, randomZ);
    }

    /// <summary>
    /// Raycast downward from the given position to find the terrain.
    /// Returns true if terrain was found, and sets terrainPosition to the hit point.
    /// </summary>
    private bool RaycastToTerrain(Vector3 position, out Vector3 terrainPosition)
    {
        terrainPosition = position;

        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(position.x, raycastHeight, position.z);
        Vector3 rayDirection = Vector3.down;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastHeight * 2f, terrainLayer))
        {
            terrainPosition = hit.point;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Visualize the spawn area in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw spawn area bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);

        // Draw spawn area outline
        Gizmos.color = Color.green;
        Vector3 halfSize = spawnAreaSize * 0.5f;
        Vector3 min = spawnAreaCenter - halfSize;
        Vector3 max = spawnAreaCenter + halfSize;

        // Bottom rectangle
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z));
        Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z));

        // Top rectangle
        Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z));
        Gizmos.DrawLine(new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z));
        Gizmos.DrawLine(new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z));

        // Vertical lines
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z));
        Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z));
    }
}
