using System.Collections.Generic;

/// <summary>
/// Interface for agent decision-making systems.
/// Implementations can be random, rule-based, or ML-based.
/// </summary>
public interface IAgentBrain
{
    /// <summary>
    /// Decides which action the agent should take.
    /// </summary>
    /// <param name="agent">The agent making the decision.</param>
    /// <param name="validActions">List of currently valid actions for this agent.</param>
    /// <returns>The chosen AgentAction.</returns>
    AgentAction DecideAction(Agent agent, List<AgentAction> validActions);
}
