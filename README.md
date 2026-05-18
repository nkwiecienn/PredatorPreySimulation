# Predator-Prey Simulation

A reinforcement learning driven Unity ecosystem simulation of Predator and Prey dynamics.

## Quick Navigation

| Folder                                  | Purpose                                         |
| --------------------------------------- | ----------------------------------------------- |
| `Assets/Scripts/`                       | core systems                                    |
| `Assets/ScriptableObjects/SpeciesData/` | tunable configuration assets                    |
| `Assets/Prefabs/`                       | agent prefabs (adult/juvenile prey & predators) |

## Core Systems at a Glance

| System         | File                   | Role                               |
| -------------- | ---------------------- | ---------------------------------- |
| **Agent**      | `Agent.cs`             | State, lifecycle, action execution |
| **Movement**   | `AgentMovement.cs`     | Physics-based movement             |
| **Perception** | `AgentPerception.cs`   | Raycast cone detection             |
| **Decision**   | `RandomBrain.cs`       | Weighted random action selection   |
| **Manager**    | `SimulationManager.cs` | Global tracking & statistics       |
| **Spawner**    | `AgentSpawner.cs`      | Initial population generation      |
| **Food**       | `GrassPatch.cs`        | Renewable grass resource           |
| **Shelter**    | `ShelterZone.cs`       | Safe zones with protection logic   |

## Agent Actions

```
Movement (all agents):  TurnLeft, TurnRight, MoveForward, Idle
Prey Only:              EatGrass, EnterShelter, LeaveShelter
Predators Only:         Attack
Adults Only:            Mate
```

Each action validates range, energy, perception, and agent state before execution.

## Game Loop (Every Frame)

```
Decision Cycle (every 0.5s):
├─ Raycast to perceive nearby objects
├─ Get available actions (filtered by species/age)
├─ Weighted random selection via RandomBrain (for now)
└─ Execute chosen action

Lifecycle Updates:
├─ Passive energy loss
├─ Age tracking
├─ Juvenile -> Adult maturity transition
└─ Starvation death check
```
