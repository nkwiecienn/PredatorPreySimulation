using UnityEngine;

public static class DebugLogger
{
    public enum LogLevel { Verbose, Info, Warning, Error }
    public static LogLevel CurrentLogLevel = LogLevel.Verbose;
    private static bool enableAgentLogs = true;
    private static bool enablePerceptionLogs = true;
    private static bool enableSimulationLogs = true;
    private static bool enableSpawnerLogs = true;
    private static bool enableMovementLogs = false;
    private static bool enableGrassLogs = false;

    // =========================================================================
    // Agent Logging
    // =========================================================================

    public static void LogAgentInit(string agentId, string species, string lifeStage)
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Info)) return;
        Debug.Log($"[AGENT] {agentId} initialized as {species} {lifeStage}");
    }

    public static void LogAgentAction(string agentId, AgentAction action)
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Verbose)) return;
        Debug.Log($"[AGENT] {agentId} performing action: {action}");
    }

    public static void LogAgentEnergy(string agentId, float energy, float maxEnergy, string reason = "")
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Verbose)) return;
        string msg = $"[AGENT] {agentId} energy: {energy:F1}/{maxEnergy:F1}";
        if (!string.IsNullOrEmpty(reason)) msg += $" ({reason})";
        Debug.Log(msg);
    }

    public static void LogAgentMatured(string agentId)
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Info)) return;
        Debug.Log($"[AGENT] {agentId} matured to Adult");
    }

    public static void LogAgentDeath(string agentId, DeathCause cause)
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Info)) return;
        Debug.Log($"[AGENT] {agentId} died from {cause}");
    }

    public static void LogAgentKill(string killerId, string victimId)
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Info)) return;
        Debug.Log($"[AGENT] {killerId} killed {victimId}");
    }

    public static void LogAgentReproduction(string parent1Id, string parent2Id, string offspringId)
    {
        if (!enableAgentLogs || !ShouldLog(LogLevel.Info)) return;
        Debug.Log($"[AGENT] {parent1Id} and {parent2Id} spawned offspring: {offspringId}");
    }

    public static void LogAgentError(string agentId, string error)
    {
        if (!enableAgentLogs) return;
        Debug.LogError($"[AGENT] {agentId}: {error}");
    }

    // =========================================================================
    // Perception Logging
    // =========================================================================

    public static void LogPerceptionUpdate(string agentId, int visibleCount, int grassCount, int predatorCount, int preyCount)
    {
        if (!enablePerceptionLogs || !ShouldLog(LogLevel.Verbose)) return;
        Debug.Log($"[PERCEPTION] {agentId} sees: {visibleCount} objects " +
                  $"({grassCount} grass, {predatorCount} predators, {preyCount} prey)");
    }

    public static void LogPerceptionError(string agentId, string error)
    {
        if (!enablePerceptionLogs) return;
        Debug.LogError($"[PERCEPTION] {agentId}: {error}");
    }

    // =========================================================================
    // Movement Logging
    // =========================================================================

    public static void LogMovement(string agentId, AgentAction action)
    {
        if (!enableMovementLogs || !ShouldLog(LogLevel.Verbose)) return;
        Debug.Log($"[MOVEMENT] {agentId}: {action}");
    }

    public static void LogMovementError(string agentId, string error)
    {
        if (!enableMovementLogs) return;
        Debug.LogError($"[MOVEMENT] {agentId}: {error}");
    }

    // =========================================================================
    // Grass Logging
    // =========================================================================

    public static void LogGrassEaten(string grassId, string agentId, float energyGained)
    {
        if (!enableGrassLogs || !ShouldLog(LogLevel.Verbose)) return;
        Debug.Log($"[GRASS] {agentId} ate from {grassId}, gained {energyGained:F1} energy");
    }

    public static void LogGrassRespawn(string grassId, float foodAmount)
    {
        if (!enableGrassLogs || !ShouldLog(LogLevel.Verbose)) return;
        Debug.Log($"[GRASS] {grassId} regrew to {foodAmount:F1} food");
    }

    // =========================================================================
    // Spawner Logging
    // =========================================================================

    public static void LogSpawnerInit(int preyAdults, int preyJuveniles, int predAdults, int predJuveniles)
    {
        if (!enableSpawnerLogs || !ShouldLog(LogLevel.Info)) return;
        int total = preyAdults + preyJuveniles + predAdults + predJuveniles;
        Debug.Log($"[SPAWNER] Spawning {total} agents " +
                  $"(Prey: {preyAdults}A+{preyJuveniles}J, Pred: {predAdults}A+{predJuveniles}J)");
    }

    public static void LogSpawnedAgent(string agentId, string species, string lifeStage, Vector3 position)
    {
        if (!enableSpawnerLogs || !ShouldLog(LogLevel.Verbose)) return;
        Debug.Log($"[SPAWNER] Spawned {agentId} ({species} {lifeStage}) at {position}");
    }

    public static void LogSpawnerError(string error)
    {
        if (!enableSpawnerLogs) return;
        Debug.LogError($"[SPAWNER] {error}");
    }

    // =========================================================================
    // Simulation Logging
    // =========================================================================

    public static void LogSimulationStats(int totalAgents, int predators, int prey, int births, int deaths, float time)
    {
        if (!enableSimulationLogs || !ShouldLog(LogLevel.Info)) return;
        Debug.Log($"[SIM] Time: {time:F1}s | Agents: {totalAgents} (P:{predators} | Pr:{prey}) | " +
                  $"Births: {births} | Deaths: {deaths}");
    }

    public static void LogSimulationWarning(string warning)
    {
        if (!enableSimulationLogs) return;
        Debug.LogWarning($"[SIM] {warning}");
    }

    public static void LogSimulationError(string error)
    {
        if (!enableSimulationLogs) return;
        Debug.LogError($"[SIM] {error}");
    }

    // =========================================================================
    // Control Methods
    // =========================================================================

    public static void SetLogLevel(LogLevel level)
    {
        CurrentLogLevel = level;
        Debug.Log($"[LOGGER] Log level set to {level}");
    }

    public static void EnableCategory(string category, bool enable)
    {
        switch (category.ToLower())
        {
            case "agent": enableAgentLogs = enable; break;
            case "perception": enablePerceptionLogs = enable; break;
            case "movement": enableMovementLogs = enable; break;
            case "grass": enableGrassLogs = enable; break;
            case "spawner": enableSpawnerLogs = enable; break;
            case "simulation": enableSimulationLogs = enable; break;
            case "all":
                enableAgentLogs = enablePerceptionLogs = enableMovementLogs =
                enableGrassLogs = enableSpawnerLogs = enableSimulationLogs = enable;
                break;
            default:
                Debug.LogWarning($"Unknown log category: {category}");
                return;
        }
        Debug.Log($"[LOGGER] {category} logging {(enable ? "enabled" : "disabled")}");
    }

    public static void LogSummary()
    {
        Debug.Log("[LOGGER] ========== LOGGER ENABLED CATEGORIES ==========");
        Debug.Log($"  Agent:      {(enableAgentLogs ? "✓" : "✗")}");
        Debug.Log($"  Perception: {(enablePerceptionLogs ? "✓" : "✗")}");
        Debug.Log($"  Movement:   {(enableMovementLogs ? "✓" : "✗")}");
        Debug.Log($"  Grass:      {(enableGrassLogs ? "✓" : "✗")}");
        Debug.Log($"  Spawner:    {(enableSpawnerLogs ? "✓" : "✗")}");
        Debug.Log($"  Simulation: {(enableSimulationLogs ? "✓" : "✗")}");
        Debug.Log($"  Log Level:  {CurrentLogLevel}");
        Debug.Log("[LOGGER] ==============================================");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static bool ShouldLog(LogLevel level)
    {
        return level >= CurrentLogLevel;
    }
}
