using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Random decision-making brain for agents.
/// Chooses valid actions using weighted random selection.
/// Weights are higher for contextually relevant actions.
/// </summary>
public class RandomBrain : MonoBehaviour, IAgentBrain
{
    /// <summary>
    /// Decides which action the agent should take based on weighted random selection.
    /// </summary>
    public AgentAction DecideAction(Agent agent, List<AgentAction> validActions)
    {
        if (validActions == null || validActions.Count == 0)
        {
            return AgentAction.Idle;
        }

        // Filter valid actions based on agent state and perception
        List<(AgentAction action, float weight)> weightedActions = new List<(AgentAction, float)>();

        foreach (AgentAction action in validActions)
        {
            float weight = GetActionWeight(agent, action);
            if (weight > 0f)
            {
                weightedActions.Add((action, weight));
            }
        }

        // If no weighted actions found, use any valid action with default weight
        if (weightedActions.Count == 0)
        {
            return validActions[Random.Range(0, validActions.Count)];
        }

        // Weighted random selection
        return SelectWeightedAction(weightedActions);
    }

    /// <summary>
    /// Get the weight for a specific action based on agent state and perception.
    /// Higher weight = higher probability of selection.
    /// </summary>
    private float GetActionWeight(Agent agent, AgentAction action)
    {
        AgentPerception perception = agent.Perception;

        switch (action)
        {
            // Universal movement actions
            case AgentAction.MoveForward:
                return 40f;  // High: agents should move regularly

            case AgentAction.TurnLeft:
            case AgentAction.TurnRight:
                return 20f;  // Medium: agents should turn to explore

            case AgentAction.Idle:
                return 5f;   // Low: agents should not idle often

            // Prey-only actions
            case AgentAction.EatGrass:
                return GetEatGrassWeight(agent, perception);

            case AgentAction.EnterShelter:
                return GetEnterShelterWeight(agent, perception);

            case AgentAction.LeaveShelter:
                return GetLeaveShelterWeight(agent, perception);

            // Predator-only action
            case AgentAction.Attack:
                return GetAttackWeight(agent, perception);

            // Adult-only action
            case AgentAction.Mate:
                return GetMateWeight(agent, perception);

            default:
                return 0f;
        }
    }

    /// <summary>
    /// Weight for eating grass. Higher if hungry and near grass.
    /// </summary>
    private float GetEatGrassWeight(Agent agent, AgentPerception perception)
    {
        // Only valid for prey
        if (agent.AgentSpecies != Species.Prey)
            return 0f;

        // Cannot eat inside shelter
        if (agent.IsInShelter)
            return 0f;

        // No visible grass -> low probability
        if (perception.VisibleGrass.Count == 0)
            return 0f;

        // Check if agent is hungry (energy below 70% of max)
        float hungerFactor = agent.CurrentEnergy < agent.MaxEnergy * 0.7f ? 1.5f : 1f;

        return 50f * hungerFactor;  // Higher if hungry
    }

    /// <summary>
    /// Weight for entering shelter. Higher if predators nearby.
    /// </summary>
    private float GetEnterShelterWeight(Agent agent, AgentPerception perception)
    {
        // Only valid for prey
        if (agent.AgentSpecies != Species.Prey)
            return 0f;

        // Cannot enter if already inside
        if (agent.IsInShelter)
            return 0f;

        // No visible shelters -> cannot enter
        if (perception.VisibleShelters.Count == 0)
            return 0f;

        // Check if predators are nearby
        float threatFactor = perception.VisiblePredators.Count > 0 ? 2f : 0.5f;

        return 25f * threatFactor;  // Higher if predators nearby
    }

    /// <summary>
    /// Weight for leaving shelter. Lower weight to keep prey safer.
    /// </summary>
    private float GetLeaveShelterWeight(Agent agent, AgentPerception perception)
    {
        // Only valid for prey
        if (agent.AgentSpecies != Species.Prey)
            return 0f;

        // Can only leave if currently inside shelter
        if (!agent.IsInShelter)
            return 0f;

        // Check if predators are visible outside
        float threatFactor = perception.VisiblePredators.Count > 0 ? 0.5f : 1f;

        return 25f * threatFactor;  // Lower if predators visible
    }

    /// <summary>
    /// Weight for attacking. Higher if prey is nearby and not in shelter.
    /// </summary>
    private float GetAttackWeight(Agent agent, AgentPerception perception)
    {
        // Only valid for predators
        if (agent.AgentSpecies != Species.Predator)
            return 0f;

        // No visible prey -> cannot attack
        if (perception.VisiblePrey.Count == 0)
            return 0f;

        // The actual attack validation (shelter, range) is done in Agent.Attack()
        // RandomBrain just assigns weight if prey is visible

        return 70f;  // High: predators should actively hunt
    }

    /// <summary>
    /// Weight for mating. Higher if partner available and energy sufficient.
    /// </summary>
    private float GetMateWeight(Agent agent, AgentPerception perception)
    {
        // Only valid for adults
        if (agent.AgentLifeStage != LifeStage.Adult)
            return 0f;

        // Check if same-species partner is nearby
        List<PerceptionObject> potentialPartners = new List<PerceptionObject>();

        if (agent.AgentSpecies == Species.Prey)
        {
            potentialPartners = perception.VisiblePrey;
        }
        else if (agent.AgentSpecies == Species.Predator)
        {
            potentialPartners = perception.VisiblePredators;
        }

        // No partners nearby
        if (potentialPartners.Count == 0)
            return 0f;

        // The actual mate validation (energy, compatibility) is done in Agent.Mate()
        // RandomBrain just assigns weight if partner is visible

        return 30f;  // Medium: agents should reproduce but not constantly
    }

    /// <summary>
    /// Select an action from weighted list using weighted random selection.
    /// </summary>
    private AgentAction SelectWeightedAction(List<(AgentAction action, float weight)> weightedActions)
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var item in weightedActions)
        {
            totalWeight += item.weight;
        }

        // Random selection based on weights
        float random = Random.Range(0f, totalWeight);
        float accumulated = 0f;

        foreach (var item in weightedActions)
        {
            accumulated += item.weight;
            if (random <= accumulated)
            {
                return item.action;
            }
        }

        // Fallback (should rarely happen)
        return weightedActions[weightedActions.Count - 1].action;
    }
}
