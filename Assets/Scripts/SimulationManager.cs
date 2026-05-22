using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Debug = UnityEngine.Debug;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Offspring Prefabs")]
    [SerializeField] private GameObject preyJuvenilePrefab;
    [SerializeField] private GameObject predatorJuvenilePrefab;

    [Header("Statistics")]
    [SerializeField] private float statisticsReportInterval = 5f;

    [Header("Data Export")]
    [SerializeField] private bool exportChartsOnStop = true;
    [SerializeField] private float dataSampleInterval = 1f;
    [SerializeField] private string outputFolderName = "SimulationOutputs";
    [SerializeField] private string pythonExecutable = "python";
    [SerializeField] private string chartGeneratorScript = "Assets/Scripts/Python/generate_simulation_charts.py";

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
    private float dataSampleTimer;
    private bool hasExportedResults = false;
    private bool isQuitting = false;
    private readonly List<SimulationStatsSample> statsHistory = new List<SimulationStatsSample>();

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
        dataSampleTimer = 0f;
    }

    private void Update()
    {
        simulationTime += Time.deltaTime;
        statisticsTimer -= Time.deltaTime;
        dataSampleTimer -= Time.deltaTime;

        if (statisticsTimer <= 0f)
        {
            statisticsTimer = statisticsReportInterval;
            PrintStatistics();
        }

        if (dataSampleTimer <= 0f)
        {
            dataSampleTimer = Mathf.Max(0.1f, dataSampleInterval);
            RecordStatsSample();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        ExportResultsIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (!isQuitting)
                ExportResultsIfNeeded();

            Instance = null;
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

        Vector3 offset = UnityEngine.Random.insideUnitSphere * 2f;
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

    private void RecordStatsSample()
    {
        statsHistory.Add(BuildStatsSample());
    }

    private SimulationStatsSample BuildStatsSample()
    {
        Agent[] preyAgents = prey.Where(a => a != null && a.IsAlive).ToArray();
        Agent[] predatorAgents = predators.Where(a => a != null && a.IsAlive).ToArray();
        GrassPatch[] grassPatches = FindObjectsByType<GrassPatch>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        ShelterZone[] shelters = FindObjectsByType<ShelterZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        float totalGrassFood = grassPatches.Sum(g => g != null ? g.CurrentFoodAmount : 0f);
        float maxGrassFood = grassPatches.Sum(g => g != null ? g.MaxFoodAmount : 0f);
        int shelterOccupancy = shelters.Sum(s => s != null ? s.GetOccupancy() : 0);
        int shelterCapacity = shelters.Sum(s => s != null ? s.GetCapacity() : 0);

        return new SimulationStatsSample
        {
            time = simulationTime,
            totalAgents = allAgents.Count,
            preyCount = preyAgents.Length,
            predatorCount = predatorAgents.Length,
            preyJuvenileCount = preyAgents.Count(a => a.AgentLifeStage == LifeStage.Juvenile),
            preyAdultCount = preyAgents.Count(a => a.AgentLifeStage == LifeStage.Adult),
            predatorJuvenileCount = predatorAgents.Count(a => a.AgentLifeStage == LifeStage.Juvenile),
            predatorAdultCount = predatorAgents.Count(a => a.AgentLifeStage == LifeStage.Adult),
            avgPreyEnergy = AverageEnergy(preyAgents),
            avgPredatorEnergy = AverageEnergy(predatorAgents),
            minPreyEnergy = MinEnergy(preyAgents),
            maxPreyEnergy = MaxEnergy(preyAgents),
            minPredatorEnergy = MinEnergy(predatorAgents),
            maxPredatorEnergy = MaxEnergy(predatorAgents),
            totalBirths = totalBirths,
            totalDeaths = totalDeaths,
            totalKills = totalKills,
            availableGrassPatches = grassPatches.Count(g => g != null && g.IsAvailable),
            totalGrassPatches = grassPatches.Length,
            totalGrassFood = totalGrassFood,
            maxGrassFood = maxGrassFood,
            shelterOccupancy = shelterOccupancy,
            shelterCapacity = shelterCapacity
        };
    }

    private static float AverageEnergy(Agent[] agents) => agents.Length > 0 ? agents.Average(a => a.CurrentEnergy) : 0f;
    private static float MinEnergy(Agent[] agents) => agents.Length > 0 ? agents.Min(a => a.CurrentEnergy) : 0f;
    private static float MaxEnergy(Agent[] agents) => agents.Length > 0 ? agents.Max(a => a.CurrentEnergy) : 0f;

    private void ExportResultsIfNeeded()
    {
        if (!Application.isPlaying)
            return;

        if (!exportChartsOnStop || hasExportedResults)
            return;

        hasExportedResults = true;

        if (statsHistory.Count == 0 || statsHistory[statsHistory.Count - 1].time < simulationTime)
            RecordStatsSample();

        if (statsHistory.Count == 0)
            return;

        try
        {
            string outputDirectory = CreateOutputDirectory();
            string csvPath = Path.Combine(outputDirectory, "simulation_stats.csv");
            string metadataPath = Path.Combine(outputDirectory, "simulation_metadata.json");

            WriteStatsCsv(csvPath);
            WriteMetadataJson(metadataPath);
            RunChartGenerator(csvPath, metadataPath, outputDirectory);

            UnityEngine.Debug.Log($"Simulation results exported to: {outputDirectory}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to export simulation results: {ex.Message}");
        }
    }

    private string CreateOutputDirectory()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string root = Path.Combine(projectRoot, outputFolderName);
        string sessionName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string outputDirectory = Path.Combine(root, sessionName);

        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    private void WriteStatsCsv(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("time,totalAgents,preyCount,predatorCount,preyJuvenileCount,preyAdultCount,predatorJuvenileCount,predatorAdultCount,avgPreyEnergy,avgPredatorEnergy,minPreyEnergy,maxPreyEnergy,minPredatorEnergy,maxPredatorEnergy,totalBirths,totalDeaths,totalKills,availableGrassPatches,totalGrassPatches,totalGrassFood,maxGrassFood,shelterOccupancy,shelterCapacity");

        foreach (SimulationStatsSample sample in statsHistory)
        {
            sb.AppendLine(string.Join(",",
                FormatFloat(sample.time),
                sample.totalAgents,
                sample.preyCount,
                sample.predatorCount,
                sample.preyJuvenileCount,
                sample.preyAdultCount,
                sample.predatorJuvenileCount,
                sample.predatorAdultCount,
                FormatFloat(sample.avgPreyEnergy),
                FormatFloat(sample.avgPredatorEnergy),
                FormatFloat(sample.minPreyEnergy),
                FormatFloat(sample.maxPreyEnergy),
                FormatFloat(sample.minPredatorEnergy),
                FormatFloat(sample.maxPredatorEnergy),
                sample.totalBirths,
                sample.totalDeaths,
                sample.totalKills,
                sample.availableGrassPatches,
                sample.totalGrassPatches,
                FormatFloat(sample.totalGrassFood),
                FormatFloat(sample.maxGrassFood),
                sample.shelterOccupancy,
                sample.shelterCapacity
            ));
        }

        File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
    }

    private void WriteMetadataJson(string metadataPath)
    {
        SimulationStatsSample first = statsHistory[0];
        SimulationStatsSample last = statsHistory[statsHistory.Count - 1];

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"startedAt\": \"{DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}\",");
        sb.AppendLine($"  \"durationSeconds\": {FormatFloat(simulationTime)},");
        sb.AppendLine($"  \"sampleIntervalSeconds\": {FormatFloat(dataSampleInterval)},");
        sb.AppendLine($"  \"samples\": {statsHistory.Count},");
        sb.AppendLine($"  \"initialPrey\": {first.preyCount},");
        sb.AppendLine($"  \"initialPredators\": {first.predatorCount},");
        sb.AppendLine($"  \"finalPrey\": {last.preyCount},");
        sb.AppendLine($"  \"finalPredators\": {last.predatorCount},");
        sb.AppendLine($"  \"totalBirths\": {totalBirths},");
        sb.AppendLine($"  \"totalDeaths\": {totalDeaths},");
        sb.AppendLine($"  \"totalKills\": {totalKills}");
        sb.AppendLine("}");

        File.WriteAllText(metadataPath, sb.ToString(), Encoding.UTF8);
    }

    private void RunChartGenerator(string csvPath, string metadataPath, string outputDirectory)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string scriptPath = Path.IsPathRooted(chartGeneratorScript)
            ? chartGeneratorScript
            : Path.Combine(projectRoot, chartGeneratorScript);

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogWarning($"Chart generator script not found: {scriptPath}");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            Arguments = $"\"{scriptPath}\" \"{csvPath}\" \"{metadataPath}\" \"{outputDirectory}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (Process process = Process.Start(startInfo))
        {
            if (process == null)
            {
                UnityEngine.Debug.LogWarning("Could not start Python chart generator.");
                return;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            bool finished = process.WaitForExit(30000);

            if (!finished)
            {
                process.Kill();
                UnityEngine.Debug.LogWarning("Chart generation timed out after 30 seconds.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(stdout))
                UnityEngine.Debug.Log(stdout);

            if (process.ExitCode != 0)
                UnityEngine.Debug.LogWarning($"Chart generation failed: {stderr}");
            else if (!string.IsNullOrWhiteSpace(stderr))
                UnityEngine.Debug.LogWarning(stderr);
        }
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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

    private struct SimulationStatsSample
    {
        public float time;
        public int totalAgents;
        public int preyCount;
        public int predatorCount;
        public int preyJuvenileCount;
        public int preyAdultCount;
        public int predatorJuvenileCount;
        public int predatorAdultCount;
        public float avgPreyEnergy;
        public float avgPredatorEnergy;
        public float minPreyEnergy;
        public float maxPreyEnergy;
        public float minPredatorEnergy;
        public float maxPredatorEnergy;
        public int totalBirths;
        public int totalDeaths;
        public int totalKills;
        public int availableGrassPatches;
        public int totalGrassPatches;
        public float totalGrassFood;
        public float maxGrassFood;
        public int shelterOccupancy;
        public int shelterCapacity;
    }
}
