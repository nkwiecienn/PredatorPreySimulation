using UnityEngine;
using UnityEngine.AI;

public class AnimalSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public AnimalSpecies Species;
        public GameObject Prefab;
        public AnimalSpeciesData SpeciesData;
        [Range(0f, 1f)] public float SpawnWeight = 0.5f;
    }

    [Header("Spawner Settings")]
    [SerializeField] private WanderArea wanderArea;
    [SerializeField] private int totalAnimalsToSpawn = 20;
    [SerializeField] private int maxSpawnAttempts = 20;
    [SerializeField] private SpawnEntry[] spawnEntries;

    private void Start()
    {
        SpawnAnimals();
    }

    public void SpawnAnimals()
    {
        if (wanderArea == null)
        {
            Debug.LogError("AnimalSpawner: WanderArea is missing.");
            return;
        }

        if (spawnEntries == null || spawnEntries.Length == 0)
        {
            Debug.LogError("AnimalSpawner: No spawn entries configured.");
            return;
        }

        for (int i = 0; i < totalAnimalsToSpawn; i++)
        {
            SpawnEntry chosenEntry = GetRandomEntry();
            if (chosenEntry == null || chosenEntry.Prefab == null)
                continue;

            Vector3 spawnPosition = FindValidSpawnPosition(chosenEntry);
            GameObject instance = Instantiate(chosenEntry.Prefab, spawnPosition, Quaternion.identity);

            AnimalBase animal = instance.GetComponent<AnimalBase>();
            if (animal != null && chosenEntry.SpeciesData != null)
            {
                animal.Initialize(chosenEntry.SpeciesData);
            }

            AnimalWanderer wanderer = instance.GetComponent<AnimalWanderer>();
            if (wanderer != null)
            {
                wanderer.Initialize(wanderArea);
            }
        }
    }

    private SpawnEntry GetRandomEntry()
    {
        float totalWeight = 0f;

        foreach (var entry in spawnEntries)
        {
            if (entry != null && entry.Prefab != null)
                totalWeight += Mathf.Max(0f, entry.SpawnWeight);
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (var entry in spawnEntries)
        {
            if (entry == null || entry.Prefab == null)
                continue;

            current += Mathf.Max(0f, entry.SpawnWeight);
            if (randomValue <= current)
                return entry;
        }

        return spawnEntries[spawnEntries.Length - 1];
    }

    private Vector3 FindValidSpawnPosition(SpawnEntry entry)
    {
        float sampleRadius = 8f;
        if (entry != null && entry.SpeciesData != null)
            sampleRadius = entry.SpeciesData.NavMeshSampleRadius;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 point = wanderArea.GetRandomPoint();

            if (NavMesh.SamplePosition(point, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                return hit.position;
        }

        Debug.LogWarning("AnimalSpawner: fallback spawn position used.");
        return wanderArea.transform.position;
    }
}