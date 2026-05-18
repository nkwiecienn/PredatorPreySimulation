using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Offspring Prefabs")]
    [SerializeField] private GameObject preyJuvenilePrefab;
    [SerializeField] private GameObject predatorJuvenilePrefab;

    [Header("Statistics")]
    [SerializeField] private float statisticsReportInterval = 5f;

    // -------------------------------------------------------------------------
    // Tracking
    // -------------------------------------------------------------------------

    private readonly List<Agent> allAgents = new List<Agent>();
    private readonly List<Agent> predators = new List<Agent>();
    private readonly List<Agent> prey = new List<Agent>();

    private int totalBirths = 0;
    private int totalDeaths = 0;
    private int totalKills = 0;
    private float simulationTime = 0f;
    private float statisticsTimer;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
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
        statisticsTimer -= Time.deltaTime;

        if (statisticsTimer <= 0f)
        {
            statisticsTimer = statisticsReportInterval;
            PrintStatistics();
        }
    }

    // -------------------------------------------------------------------------
    // Agent registration
    // -------------------------------------------------------------------------

    public void RegisterAgent(Agent agent)
    {
        if (agent == null) return;

        if (!allAgents.Contains(agent)) allAgents.Add(agent);

        if (agent.AgentSpecies == Species.Predator && !predators.Contains(agent)) predators.Add(agent);
        else if (agent.AgentSpecies == Species.Prey && !prey.Contains(agent)) prey.Add(agent);

        DebugLogger.LogAgentInit(agent.AgentId, agent.AgentSpecies.ToString(), agent.AgentLifeStage.ToString());
    }

    public void UnregisterAgent(Agent agent, DeathCause cause)
    {
        if (agent == null) return;

        allAgents.Remove(agent);

        if (agent.AgentSpecies == Species.Predator) predators.Remove(agent);
        else if (agent.AgentSpecies == Species.Prey) prey.Remove(agent);

        totalDeaths++;
        if (cause == DeathCause.Predation) totalKills++;
    }

    // -------------------------------------------------------------------------
    // Reproduction
    // -------------------------------------------------------------------------

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

        float cost = parent1.SpeciesData != null ? parent1.SpeciesData.ReproductionEnergyCost : 25f;
        parent1.ConsumeEnergy(cost);
        parent2.ConsumeEnergy(cost);

        GameObject prefab = parent1.AgentSpecies == Species.Prey
            ? preyJuvenilePrefab
            : predatorJuvenilePrefab;

        if (prefab == null)
        {
            Debug.LogError("Offspring prefab not assigned in SimulationManager");
            return null;
        }

        Vector3 offset = Random.insideUnitSphere * 2f;
        offset.y = 0f;
        Vector3 spawnPos = parent1.transform.position + offset;

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        obj.name = $"{(parent1.AgentSpecies == Species.Prey ? "PreyJuvenile" : "PredatorJuvenile")}_Offspring_{totalBirths}";

        Agent offspring = obj.GetComponent<Agent>();
        if (offspring == null)
        {
            Debug.LogError("Offspring prefab is missing Agent component");
            Destroy(obj);
            return null;
        }

        totalBirths++;
        DebugLogger.LogAgentReproduction(parent1.AgentId, parent2.AgentId, obj.name);
        return offspring;
    }

    // -------------------------------------------------------------------------
    // Statistics
    // -------------------------------------------------------------------------

    private void PrintStatistics()
    {
        float avgPredEnergy = predators.Count > 0 ? predators.Average(p => p.CurrentEnergy) : 0f;
        float avgPreyEnergy = prey.Count > 0 ? prey.Average(p => p.CurrentEnergy) : 0f;

        Debug.Log(
            $"[{simulationTime:F1}s] " +
            $"Prey: {prey.Count} (Avg Energy: {avgPreyEnergy:F1}) | " +
            $"Predators: {predators.Count} (Avg Energy: {avgPredEnergy:F1}) | " +
            $"Births: {totalBirths} | Deaths: {totalDeaths} | Kills: {totalKills}"
        );
    }

    // -------------------------------------------------------------------------
    // Public getters
    // -------------------------------------------------------------------------

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