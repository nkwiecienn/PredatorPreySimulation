using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton manager for the entire predator-prey simulation.
/// Tracks all active agents, statistics, and handles lifecycle events.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Offspring Prefabs")]
    [SerializeField] private GameObject preyJuvenilePrefab;
    [SerializeField] private GameObject predatorJuvenilePrefab;

    // All active agents
    private List<Agent> allAgents = new List<Agent>();

    // Agents by species (for fast lookup)
    private List<Agent> predators = new List<Agent>();
    private List<Agent> prey = new List<Agent>();

    // Statistics tracking
    private int totalBirths = 0;
    private int totalDeaths = 0;
    private int totalKills = 0;
    private float simulationTime = 0f;
    private float statisticsTimer = 0f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SimulationManager instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        statisticsTimer = statisticsReportInterval;
    }

    private void Update()
    {
        simulationTime += Time.deltaTime;

        // Print statistics periodically
        statisticsTimer -= Time.deltaTime;
        if (statisticsTimer <= 0f)
        {
            statisticsTimer = statisticsReportInterval;
            PrintStatistics();
        }
    }

    /// <summary>
    /// Register a new agent in the simulation.
    /// Called when an agent is spawned.
    /// </summary>
    public void RegisterAgent(Agent agent)
    {
        if (agent == null) return;

        // Add to global list
        if (!allAgents.Contains(agent))
        {
            allAgents.Add(agent);
        }

        // Add to species-specific list
        if (agent.AgentSpecies == Species.Predator)
        {
            if (!predators.Contains(agent))
                predators.Add(agent);
        }
        else if (agent.AgentSpecies == Species.Prey)
        {
            if (!prey.Contains(agent))
                prey.Add(agent);
        }
    }

    /// <summary>
    /// Unregister an agent from the simulation.
    /// Called when an agent dies.
    /// </summary>
    public void UnregisterAgent(Agent agent, DeathCause cause)
    {
        if (agent == null) return;

        // Remove from global list
        allAgents.Remove(agent);

        // Remove from species-specific list
        if (agent.AgentSpecies == Species.Predator)
        {
            predators.Remove(agent);
        }
        else if (agent.AgentSpecies == Species.Prey)
        {
            prey.Remove(agent);
        }

        // Update statistics
        totalDeaths++;
        if (cause == DeathCause.Predation)
        {
            totalKills++;
        }
    }

    /// <summary>
    /// Spawn an offspring from two parent agents.
    /// Called when adults successfully mate.
    /// </summary>
    public Agent SpawnOffspring(Agent parent1, Agent parent2)
    {
        if (parent1 == null || parent2 == null)
        {
            Debug.LogError("Cannot spawn offspring: one or both parents are null");
            return null;
        }

        if (parent1.AgentSpecies != parent2.AgentSpecies)
        {
            Debug.LogError("Cannot spawn offspring: parents are different species");
            return null;
        }

        // Subtract reproduction cost from both parents
        float reproductionCost = parent1.SpeciesData != null ? parent1.SpeciesData.ReproductionEnergyCost : 25f;
        parent1.ConsumeEnergy(reproductionCost);
        parent2.ConsumeEnergy(reproductionCost);

        // Determine offspring prefab based on parent species
        GameObject offspringPrefab = null;
        if (parent1.AgentSpecies == Species.Prey)
        {
            offspringPrefab = preyJuvenilePrefab;
        }
        else if (parent1.AgentSpecies == Species.Predator)
        {
            offspringPrefab = predatorJuvenilePrefab;
        }

        if (offspringPrefab == null)
        {
            Debug.LogError("Offspring prefab not assigned in SimulationManager");
            return null;
        }

        // Spawn offspring near parent1
        Vector3 spawnOffset = Random.insideUnitSphere * 2f;  // Random offset within 2 units
        spawnOffset.y = 0f;  // Keep on same height as parent
        Vector3 spawnPosition = parent1.transform.position + spawnOffset;

        // Instantiate offspring
        GameObject offspringInstance = Instantiate(offspringPrefab, spawnPosition, Quaternion.identity);
        offspringInstance.name = $"{(parent1.AgentSpecies == Species.Prey ? "PreyJuvenile" : "PredatorJuvenile")}_Offspring_{totalBirths}";

        // The offspring will auto-register in its Start() method
        Agent offspringAgent = offspringInstance.GetComponent<Agent>();
        if (offspringAgent == null)
        {
            Debug.LogError("Offspring prefab does not have Agent component");
            Destroy(offspringInstance);
            return null;
        }

        // Increment birth counter
        totalBirths++;

        Debug.Log($"Offspring spawned: {offspringInstance.name}. Total births: {totalBirths}");

        return offspringAgent;
    }

    /// <summary>
    /// Print current simulation statistics to console.
    /// </summary>
    private void PrintStatistics()
    {
        int predatorCount = predators.Count;
        int preyCount = prey.Count;

        float avgPredatorEnergy = predatorCount > 0
            ? predators.Average(p => p.CurrentEnergy)
            : 0f;

        float avgPreyEnergy = preyCount > 0
            ? prey.Average(p => p.CurrentEnergy)
            : 0f;

        string stats = $"[{simulationTime:F1}s] " +
                      $"Prey: {preyCount} (Avg Energy: {avgPreyEnergy:F1}) | " +
                      $"Predators: {predatorCount} (Avg Energy: {avgPredatorEnergy:F1}) | " +
                      $"Births: {totalBirths} | " +
                      $"Deaths: {totalDeaths} | " +
                      $"Kills: {totalKills}";

        Debug.Log(stats);
    }

    // Getter methods for debugging and other systems
    public int GetPredatorCount() => predators.Count;
    public int GetPreyCount() => prey.Count;
    public int GetTotalAgentCount() => allAgents.Count;
    public int GetTotalBirths() => totalBirths;
    public int GetTotalDeaths() => totalDeaths;
    public int GetTotalKills() => totalKills;
    public float GetSimulationTime() => simulationTime;

    public List<Agent> GetAllAgents() => new List<Agent>(allAgents);
    public List<Agent> GetPredators() => new List<Agent>(predators);
    public List<Agent> GetPrey() => new List<Agent>(prey);
}
