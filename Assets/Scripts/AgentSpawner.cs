using UnityEngine;

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
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(50f, 0f, 50f);  // Y unused

    [Header("Terrain Detection")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float raycastHeight = 100f;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        ValidatePrefabs();
        SpawnInitialPopulation();
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    private void SpawnInitialPopulation()
    {
        Debug.Log("Starting to spawn initial population...");

        SpawnGroup(preyAdultPrefab, "PreyAdult", initialPreyAdults);
        SpawnGroup(preyJuvenilePrefab, "PreyJuvenile", initialPreyJuveniles);
        SpawnGroup(predatorAdultPrefab, "PredatorAdult", initialPredatorAdults);
        SpawnGroup(predatorJuvenilePrefab, "PredatorJuvenile", initialPredatorJuveniles);

        int total = initialPreyAdults + initialPreyJuveniles
                  + initialPredatorAdults + initialPredatorJuveniles;
        Debug.Log($"Spawned {total} initial agents.");
    }

    private void SpawnGroup(GameObject prefab, string namePrefix, int count)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            string id = $"{namePrefix}_{i:D2}";
            SpawnAgent(prefab, id);
        }
    }

    private void SpawnAgent(GameObject prefab, string agentId)
    {
        Vector3 spawnPos = GetRandomSpawnPosition();

        if (RaycastToTerrain(spawnPos, out Vector3 groundPos))
            spawnPos = groundPos;
        else
        {
            Debug.LogWarning($"No terrain found for {agentId} — spawning at y=0");
            spawnPos.y = 0f;
        }

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        obj.name = agentId;

        Agent agent = obj.GetComponent<Agent>();
        if (agent == null)
        {
            Debug.LogError($"Prefab '{prefab.name}' is missing an Agent component.");
            Destroy(obj);
            return;
        }

        agent.SetAgentId(agentId);
    }

    // -------------------------------------------------------------------------
    // Position helpers
    // -------------------------------------------------------------------------

    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnAreaCenter.x - spawnAreaSize.x * 0.5f,
                               spawnAreaCenter.x + spawnAreaSize.x * 0.5f);
        float z = Random.Range(spawnAreaCenter.z - spawnAreaSize.z * 0.5f,
                               spawnAreaCenter.z + spawnAreaSize.z * 0.5f);
        return new Vector3(x, raycastHeight, z);
    }

    private bool RaycastToTerrain(Vector3 origin, out Vector3 hitPoint)
    {
        hitPoint = origin;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, terrainLayer))
        {
            hitPoint = hit.point;
            return true;
        }
        return false;
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private void ValidatePrefabs()
    {
        CheckPrefab(preyAdultPrefab, "Prey Adult");
        CheckPrefab(preyJuvenilePrefab, "Prey Juvenile");
        CheckPrefab(predatorAdultPrefab, "Predator Adult");
        CheckPrefab(predatorJuvenilePrefab, "Predator Juvenile");

        if (terrainLayer == 0)
            Debug.LogWarning("AgentSpawner: terrainLayer is not set. Agents will spawn at y=0.");
    }

    private void CheckPrefab(GameObject prefab, string label)
    {
        if (prefab == null)
            Debug.LogError($"AgentSpawner: {label} prefab is not assigned.");
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Vector3 displaySize = new Vector3(spawnAreaSize.x, 2f, spawnAreaSize.z);
        Gizmos.DrawCube(spawnAreaCenter, displaySize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, displaySize);
    }
}