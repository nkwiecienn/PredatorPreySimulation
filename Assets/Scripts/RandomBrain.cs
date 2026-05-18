using System.Collections.Generic;
using UnityEngine;

public class RandomBrain : MonoBehaviour, IAgentBrain
{
    public AgentAction DecideAction(Agent agent, List<AgentAction> validActions)
    {
        if (validActions == null || validActions.Count == 0)
            return AgentAction.Idle;

        var weighted = new List<(AgentAction action, float weight)>(validActions.Count);

        foreach (AgentAction action in validActions)
        {
            float w = GetActionWeight(agent, action);
            if (w > 0f) weighted.Add((action, w));
        }

        if (weighted.Count == 0)
            return validActions[Random.Range(0, validActions.Count)];

        return SelectWeightedAction(weighted);
    }

    private float GetActionWeight(Agent agent, AgentAction action)
    {
        AgentPerception p = agent.Perception;

        switch (action)
        {
            case AgentAction.MoveForward: return 40f;
            case AgentAction.TurnLeft:
            case AgentAction.TurnRight: return 20f;
            case AgentAction.Idle: return 5f;

            case AgentAction.EatGrass: return EatGrassWeight(agent, p);
            case AgentAction.EnterShelter: return EnterShelterWeight(agent, p);
            case AgentAction.LeaveShelter: return LeaveShelterWeight(agent, p);
            case AgentAction.Attack: return AttackWeight(agent, p);
            case AgentAction.Mate: return MateWeight(agent, p);

            default: return 0f;
        }
    }

    // -------------------------------------------------------------------------
    // Per-action weight helpers
    // -------------------------------------------------------------------------

    private float EatGrassWeight(Agent agent, AgentPerception p)
    {
        if (p.VisibleGrass.Count == 0) return 0f;

        float hungerBoost = agent.CurrentEnergy < agent.MaxEnergy * 0.7f ? 1.5f : 1f;
        return 50f * hungerBoost;
    }

    private float EnterShelterWeight(Agent agent, AgentPerception p)
    {
        if (p.VisibleShelters.Count == 0) return 0f;

        float threatBoost = p.VisiblePredators.Count > 0 ? 2f : 0.5f;
        return 25f * threatBoost;
    }

    private float LeaveShelterWeight(Agent agent, AgentPerception p)
    {
        float threatPenalty = p.VisiblePredators.Count > 0 ? 0.5f : 1f;
        return 25f * threatPenalty;
    }

    private float AttackWeight(Agent agent, AgentPerception p)
    {
        return p.VisiblePrey.Count > 0 ? 70f : 0f;
    }

    private float MateWeight(Agent agent, AgentPerception p)
    {
        int partnerCount = agent.AgentSpecies == Species.Prey
            ? p.VisiblePrey.Count
            : p.VisiblePredators.Count;

        return partnerCount > 0 ? 30f : 0f;
    }

    // -------------------------------------------------------------------------
    // Weighted random selection
    // -------------------------------------------------------------------------

    private AgentAction SelectWeightedAction(List<(AgentAction action, float weight)> weighted)
    {
        float total = 0f;
        foreach (var item in weighted) total += item.weight;

        float roll = Random.Range(0f, total);
        float accumulated = 0f;

        foreach (var item in weighted)
        {
            accumulated += item.weight;
            if (roll <= accumulated) return item.action;
        }

        return weighted[weighted.Count - 1].action;
    }
}