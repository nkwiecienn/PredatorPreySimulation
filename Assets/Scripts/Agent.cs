using UnityEngine;
using System.Collections.Generic;

public enum Species
{
    Predator,
    Prey
}

public enum LifeStage
{
    Juvenile,
    Adult
}

public enum AgentAction
{
    TurnRight,
    TurnLeft,
    MoveForward,
    Idle,
    EatGrass,
    EnterShelter,
    LeaveShelter,
    Attack,
    Mate
}

[RequireComponent(typeof(Rigidbody))]
public class Agent : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string agentId = "Agent_01";
    [SerializeField] private SpeciesData speciesData;

    [Header("Decision Making")]
    [SerializeField] private float decisionInterval = 0.5f;

    // Runtime state (initialized from SpeciesData)
    private Species species;
    private LifeStage lifeStage;
    private float size;
    private float bodyMass;
    private float moveSpeed;
    private float maxAngularVelocity;
    private float viewRadius;
    private float viewAngle;
    private float maxEnergy;
    private float currentEnergy;
    private float passiveEnergyLossPerSecond;
    private float starvationThreshold;
    private float age = 0f;
    private bool isInShelter = false;

    private Rigidbody rb;
    private AgentMovement agentMovement;
    private AgentPerception agentPerception;
    private AgentAction currentAction = AgentAction.Idle;
    private float decisionTimer = 0f;
    private IAgentBrain brain;

    // Properties for external access
    public string AgentId => agentId;
    public Species AgentSpecies => species;
    public LifeStage AgentLifeStage => lifeStage;
    public float Size => size;
    public float BodyMass => bodyMass;
    public float MoveSpeed => moveSpeed;
    public float MaxAngularVelocity => maxAngularVelocity;
    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;
    public float PassiveEnergyLossPerStep => passiveEnergyLossPerSecond;
    public float StarvationThreshold => starvationThreshold;
    public float Age => age;
    public bool IsInShelter => isInShelter;
    public AgentAction CurrentAction => currentAction;
    public bool IsAlive => gameObject.activeSelf;
    public AgentPerception Perception => agentPerception;
    public SpeciesData SpeciesData => speciesData;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agentMovement = GetComponent<AgentMovement>();
        agentPerception = GetComponent<AgentPerception>();

        // Initialize from SpeciesData
        if (speciesData != null)
        {
            species = speciesData.Species;
            lifeStage = speciesData.LifeStage;
            size = speciesData.Size;
            bodyMass = speciesData.BodyMass;
            moveSpeed = speciesData.MoveSpeed;
            maxAngularVelocity = speciesData.MaxAngularVelocity;
            viewRadius = speciesData.ViewRadius;
            viewAngle = speciesData.ViewAngle;
            maxEnergy = speciesData.MaxEnergy;
            currentEnergy = speciesData.StartEnergy;
            passiveEnergyLossPerSecond = speciesData.PassiveEnergyLossPerSecond;
            starvationThreshold = speciesData.StarvationThreshold;
        }

        ApplyPhysicalSettings();
    }

    private void Start()
    {
        // Initialize movement component
        if (agentMovement != null)
        {
            agentMovement.Initialize(moveSpeed, maxAngularVelocity);
        }

        // Initialize perception component
        if (agentPerception != null)
        {
            agentPerception.Initialize(viewRadius, viewAngle);
        }

        brain = GetComponent<IAgentBrain>();
        decisionTimer = decisionInterval;

        // Register with SimulationManager when fully initialized
        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.RegisterAgent(this);
        }
    }

    private void Update()
    {
        // Decision loop: every decision interval
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            MakeDecision();
        }

        // Lifecycle update: every frame
        TickLifecycle(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        // Apply movement physics in FixedUpdate for proper Rigidbody integration
        if (agentMovement != null)
        {
            agentMovement.ApplyMovement();
        }
    }

    private void MakeDecision()
    {
        // Update perception
        if (agentPerception != null)
        {
            agentPerception.UpdatePerception();
        }

        // Get valid actions
        List<AgentAction> validActions = GetAvailableActions();

        // Ask brain to choose action
        if (brain != null && validActions.Count > 0)
        {
            currentAction = brain.DecideAction(this, validActions);
        }
        else
        {
            currentAction = AgentAction.Idle;
        }

        // Execute chosen action
        ExecuteAction(currentAction);
    }

    private void TickLifecycle(float deltaTime)
    {
        // Passive energy loss
        currentEnergy -= passiveEnergyLossPerSecond * deltaTime;

        // Aging
        age += deltaTime;

        // Check maturity transition (juvenile to adult)
        if (lifeStage == LifeStage.Juvenile && speciesData != null)
        {
            float maturityAge = speciesData.MaturityAge;
            if (age >= maturityAge)
            {
                Mature();
            }
        }

        // Starvation check
        if (currentEnergy <= starvationThreshold)
        {
            Die(DeathCause.Starvation);
        }
    }

    private void OnValidate()
    {
        size = Mathf.Max(0.1f, size);
        bodyMass = Mathf.Max(0.1f, bodyMass);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        maxAngularVelocity = Mathf.Max(0f, maxAngularVelocity);
        viewRadius = Mathf.Max(0f, viewRadius);
        currentEnergy = Mathf.Max(0f, currentEnergy);
        passiveEnergyLossPerSecond = Mathf.Max(0f, passiveEnergyLossPerSecond);
        decisionInterval = Mathf.Max(0.1f, decisionInterval);

        ApplyPhysicalSettings();
    }

    private void ApplyPhysicalSettings()
    {
        transform.localScale = Vector3.one * size;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.mass = bodyMass;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // View radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // View angle boundaries
        Vector3 leftDir = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftDir * viewRadius);
        Gizmos.DrawRay(transform.position, rightDir * viewRadius);
    }

    private List<AgentAction> GetAvailableActions()
    {
        List<AgentAction> actions = new List<AgentAction>();

        // Universal actions (all agents)
        actions.Add(AgentAction.TurnRight);
        actions.Add(AgentAction.TurnLeft);
        actions.Add(AgentAction.MoveForward);
        actions.Add(AgentAction.Idle);

        // Prey-only actions
        if (species == Species.Prey)
        {
            actions.Add(AgentAction.EatGrass);
            actions.Add(AgentAction.EnterShelter);
            actions.Add(AgentAction.LeaveShelter);
        }

        // Predator-only actions
        if (species == Species.Predator)
        {
            actions.Add(AgentAction.Attack);
        }

        // Adult-only actions
        if (lifeStage == LifeStage.Adult)
        {
            actions.Add(AgentAction.Mate);
        }

        return actions;
    }

    private void TurnLeft()
    {
        if (agentMovement != null)
            agentMovement.TurnLeft();
    }

    private void TurnRight()
    {
        if (agentMovement != null)
            agentMovement.TurnRight();
    }

    private void MoveForward()
    {
        if (agentMovement != null)
            agentMovement.MoveForward();
    }

    private void Idle()
    {
        if (agentMovement != null)
            agentMovement.Idle();
    }

    private void EatGrass()
    {
        // Only prey can eat grass
        if (species != Species.Prey)
        {
            return;
        }

        // Cannot eat while inside shelter
        if (isInShelter)
        {
            return;
        }

        // Find nearest visible grass from perception
        if (agentPerception == null)
        {
            return;
        }

        List<GrassPatch> visibleGrasses = agentPerception.GetVisibleGrassPatches();
        if (visibleGrasses.Count == 0)
        {
            return;
        }

        // Get the closest grass patch
        GrassPatch closestGrass = null;
        float closestDistance = float.MaxValue;

        foreach (GrassPatch grass in visibleGrasses)
        {
            if (grass == null) continue;

            float distance = Vector3.Distance(transform.position, grass.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGrass = grass;
            }
        }

        // Attempt to eat from the closest grass
        if (closestGrass != null)
        {
            closestGrass.Eat(this);
        }
    }

    private void EnterShelter()
    {
        // Only prey can enter shelter
        if (species != Species.Prey)
        {
            return;
        }

        // Cannot enter if already inside
        if (isInShelter)
        {
            return;
        }

        // Find nearest visible shelter from perception
        if (agentPerception == null)
        {
            return;
        }

        List<ShelterZone> visibleShelters = agentPerception.GetVisibleShelterZones();
        if (visibleShelters.Count == 0)
        {
            return;
        }

        // Get the closest shelter
        ShelterZone closestShelter = null;
        float closestDistance = float.MaxValue;

        foreach (ShelterZone shelter in visibleShelters)
        {
            if (shelter == null) continue;

            float distance = Vector3.Distance(transform.position, shelter.transform.position);
            if (distance < closestDistance && shelter.HasCapacity())
            {
                closestDistance = distance;
                closestShelter = shelter;
            }
        }

        // Mark as inside shelter
        if (closestShelter != null)
        {
            isInShelter = true;
        }
    }

    private void LeaveShelter()
    {
        // Only prey can leave shelter
        if (species != Species.Prey)
        {
            return;
        }

        // Can only leave if currently inside
        if (!isInShelter)
        {
            return;
        }

        // Exit shelter
        isInShelter = false;
    }

    private void Attack()
    {
        // Only predators can attack
        if (species != Species.Predator)
        {
            return;
        }

        // Find nearest visible prey from perception
        if (agentPerception == null)
        {
            return;
        }

        List<Agent> visiblePrey = agentPerception.GetVisiblePreyAgents();
        if (visiblePrey.Count == 0)
        {
            return;
        }

        // Get the closest prey
        Agent closestPrey = null;
        float closestDistance = float.MaxValue;

        foreach (Agent prey in visiblePrey)
        {
            if (prey == null || !prey.IsAlive) continue;

            // Cannot attack prey inside shelter
            if (prey.IsInShelter)
                continue;

            float distance = Vector3.Distance(transform.position, prey.transform.position);
            float attackRange = speciesData != null ? speciesData.AttackRange : 1.5f;

            if (distance < attackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestPrey = prey;
            }
        }

        // Attack the closest prey if in range
        if (closestPrey != null)
        {
            float energyGain = speciesData != null ? speciesData.AttackEnergyGain : 50f;

            // Kill the prey
            closestPrey.Die(DeathCause.Predation);

            // Restore predator energy
            RestoreEnergy(energyGain);

            Debug.Log($"{agentId} killed prey. Energy: {currentEnergy:F1}");
        }
    }

    private void Mate()
    {
        // Only adults can mate
        if (lifeStage != LifeStage.Adult)
        {
            return;
        }

        // Must have enough energy to reproduce
        float minEnergyToReproduce = speciesData != null ? speciesData.MinEnergyToReproduce : 60f;
        if (currentEnergy < minEnergyToReproduce)
        {
            return;
        }

        // Find nearby potential mates
        if (agentPerception == null)
        {
            return;
        }

        List<Agent> visibleAgents = species == Species.Prey ?
            agentPerception.GetVisiblePreyAgents() :
            agentPerception.GetVisiblePredatorAgents();

        if (visibleAgents.Count == 0)
        {
            return;
        }

        // Find a compatible adult partner of same species
        Agent partner = null;
        float closestDistance = float.MaxValue;
        float matingRange = speciesData != null ? speciesData.MatingRange : 2f;

        foreach (Agent potentialPartner in visibleAgents)
        {
            if (potentialPartner == null || potentialPartner == this) continue;
            if (!potentialPartner.IsAlive) continue;
            if (potentialPartner.AgentSpecies != species) continue;
            if (potentialPartner.AgentLifeStage != LifeStage.Adult) continue;
            if (potentialPartner.CurrentEnergy < minEnergyToReproduce) continue;

            float distance = Vector3.Distance(transform.position, potentialPartner.transform.position);
            if (distance < matingRange && distance < closestDistance)
            {
                closestDistance = distance;
                partner = potentialPartner;
            }
        }

        // If found a partner, reproduce
        if (partner != null)
        {
            SimulationManager.Instance.SpawnOffspring(this, partner);
        }
    }

    private void ExecuteAction(AgentAction action)
    {
        switch (action)
        {
            case AgentAction.TurnLeft:
                TurnLeft();
                break;
            case AgentAction.TurnRight:
                TurnRight();
                break;
            case AgentAction.MoveForward:
                MoveForward();
                break;
            case AgentAction.Idle:
                Idle();
                break;
            case AgentAction.EatGrass:
                EatGrass();
                break;
            case AgentAction.EnterShelter:
                EnterShelter();
                break;
            case AgentAction.LeaveShelter:
                LeaveShelter();
                break;
            case AgentAction.Attack:
                Attack();
                break;
            case AgentAction.Mate:
                Mate();
                break;
        }
    }

    public void ConsumeEnergy(float amount)
    {
        currentEnergy -= amount;
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;
    }

    public void Die(DeathCause cause)
    {
        if (!IsAlive)
            return;  // Already dead

        // Notify SimulationManager
        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.UnregisterAgent(this, cause);
        }

        Debug.Log($"{agentId} died from {cause}");
        Destroy(gameObject);
    }

    public bool CanReproduce()
    {
        if (lifeStage != LifeStage.Adult)
            return false;

        if (speciesData == null)
            return false;

        return currentEnergy >= speciesData.MinEnergyToReproduce;
    }

    public void Mature()
    {
        if (lifeStage == LifeStage.Adult)
            return;  // Already adult

        // Transition from juvenile to adult
        lifeStage = LifeStage.Adult;

        // Load and switch to adult SpeciesData
        // This assumes adult data is available via resources or a reference
        // For a cleaner future version, consider using a manager or asset reference

        if (species == Species.Prey)
        {
            // Try to load PreyAdultData
            SpeciesData adultData = Resources.Load<SpeciesData>("SpeciesData/PreyAdultData");
            if (adultData != null)
            {
                UpdateFromSpeciesData(adultData);
                speciesData = adultData;
            }
        }
        else if (species == Species.Predator)
        {
            // Try to load PredatorAdultData
            SpeciesData adultData = Resources.Load<SpeciesData>("SpeciesData/PredatorAdultData");
            if (adultData != null)
            {
                UpdateFromSpeciesData(adultData);
                speciesData = adultData;
            }
        }

        Debug.Log($"{agentId} matured to Adult");
    }

    /// <summary>
    /// Update agent properties from new SpeciesData.
    /// Used when transitioning from juvenile to adult.
    /// </summary>
    private void UpdateFromSpeciesData(SpeciesData newData)
    {
        if (newData == null)
            return;

        size = newData.Size;
        bodyMass = newData.BodyMass;
        moveSpeed = newData.MoveSpeed;
        maxAngularVelocity = newData.MaxAngularVelocity;
        viewRadius = newData.ViewRadius;
        viewAngle = newData.ViewAngle;
        maxEnergy = newData.MaxEnergy;
        // Don't reset energy - keep accumulated energy
        passiveEnergyLossPerSecond = newData.PassiveEnergyLossPerSecond;
        starvationThreshold = newData.StarvationThreshold;

        // Update movement and perception components if they exist
        if (agentMovement != null)
        {
            agentMovement.Initialize(moveSpeed, maxAngularVelocity);
        }

        if (agentPerception != null)
        {
            agentPerception.Initialize(viewRadius, viewAngle);
        }

        ApplyPhysicalSettings();
    }
}

public enum DeathCause
{
    Starvation,
    Predation,
    Other
}