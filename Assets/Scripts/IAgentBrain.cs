using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public interface IAgentBrain
{
    AgentAction DecideAction(Agent agent, List<AgentAction> validActions);
}
