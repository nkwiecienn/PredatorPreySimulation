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
        [Header("Shelters")]
    [SerializeField] private bool useSceneBuildingsAsShelters = true;
    [SerializeField] private string shelterNamePrefix = "rpgpp_lt_building";
    [SerializeField] private Vector3 fallbackShelterSize = new Vector3(6f, 3f, 6f);

    [Header("Terrain Detection")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float raycastHeight = 100f;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        ValidatePrefabs();
        ConfigureSceneShelters();
        SpawnInitialPopulation();
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    private void SpawnInitialPopulation()
    {
        DebugLogger.LogSpawnerInit(initialPreyAdults, initialPreyJuveniles, initialPredatorAdults, initialPredatorJuveniles);

        SpawnGroup(preyAdultPrefab, "PreyAdult", initialPreyAdults);
        SpawnGroup(preyJuvenilePrefab, "PreyJuvenile", initialPreyJuveniles);
        SpawnGroup(predatorAdultPrefab, "PredatorAdult", initialPredatorAdults);
        SpawnGroup(predatorJuvenilePrefab, "PredatorJuvenile", initialPredatorJuveniles);

        int total = initialPreyAdults + initialPreyJuveniles
                  + initialPredatorAdults + initialPredatorJuveniles;
    }

     private void ConfigureSceneShelters()
    {
        if (!useSceneBuildingsAsShelters) return;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            GameObject candidate = sceneTransform.gameObject;
            if (!candidate.name.StartsWith(shelterNamePrefix)) continue;

            EnsureShelterCollider(candidate);

            if (candidate.GetComponent<ShelterZone>() == null)
                candidate.AddComponent<ShelterZone>();
        }
    }

    private void EnsureShelterCollider(GameObject shelter)
    {
        if (shelter.GetComponent<Collider>() != null)
            return;

        BoxCollider collider = shelter.AddComponent<BoxCollider>();

        if (TryGetLocalRendererBounds(shelter, out Bounds localBounds))
        {
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }
        else
        {
            collider.center = Vector3.up * (fallbackShelterSize.y * 0.5f);
            collider.size = fallbackShelterSize;
        }
    }

    private bool TryGetLocalRendererBounds(GameObject root, out Bounds localBounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        localBounds = new Bounds(Vector3.zero, fallbackShelterSize);

        if (renderers.Length == 0)
            return false;

        bool hasBounds = false;
        Bounds worldBounds = new Bounds(root.transform.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            if (!hasBounds)
            {
                worldBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return false;

        localBounds = new Bounds(
            root.transform.InverseTransformPoint(worldBounds.center),
            new Vector3(
                worldBounds.size.x / Mathf.Max(root.transform.lossyScale.x, 0.001f),
                worldBounds.size.y / Mathf.Max(root.transform.lossyScale.y, 0.001f),
                worldBounds.size.z / Mathf.Max(root.transform.lossyScale.z, 0.001f)
            )
        );
        return true;
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
            DebugLogger.LogSpawnerError($"Prefab '{prefab.name}' is missing an Agent component.");
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