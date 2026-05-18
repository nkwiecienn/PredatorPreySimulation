using System.Collections.Generic;

public interface IAgentBrain
{
    AgentAction DecideAction(Agent agent, List<AgentAction> validActions);
}
