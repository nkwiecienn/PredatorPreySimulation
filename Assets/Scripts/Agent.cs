using UnityEngine;
using System.Collections.Generic;

public enum Species { Predator, Prey }
public enum LifeStage { Juvenile, Adult }
public enum DeathCause { Starvation, Predation, Other }

public enum AgentAction
{
    TurnRight, TurnLeft, MoveForward, Idle,
    EatGrass, EnterShelter, LeaveShelter,
    Attack, Mate
}

[RequireComponent(typeof(Rigidbody))]
public class Agent : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Identity")]
    [SerializeField] private string agentId = "Agent_01";
    [SerializeField] private SpeciesData speciesData;

    [Header("Lifecycle — assign adult data so Mature() works without Resources.Load")]
    [SerializeField] private SpeciesData adultSpeciesData;

    [Header("Decision Making")]
    [SerializeField] private float decisionInterval = 0.5f;

    // -------------------------------------------------------------------------
    // Runtime state (initialised from SpeciesData in Awake)
    // -------------------------------------------------------------------------

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
    private bool isAlive = true;
    private float reproductionCooldownTimer = 0f;

    // -------------------------------------------------------------------------
    // Component references
    // -------------------------------------------------------------------------

    private Rigidbody rb;
    private AgentMovement agentMovement;
    private AgentPerception agentPerception;
    private IAgentBrain brain;
    private AgentAction currentAction = AgentAction.Idle;
    private float decisionTimer = 0f;

    // -------------------------------------------------------------------------
    // Public read-only properties
    // -------------------------------------------------------------------------

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
    public float PassiveEnergyLossPerSecond => passiveEnergyLossPerSecond;
    public float StarvationThreshold => starvationThreshold;
    public float Age => age;
    public bool IsInShelter => isInShelter;
    public AgentAction CurrentAction => currentAction;
    public bool IsAlive => isAlive;
    public bool IsReproductionReady => reproductionCooldownTimer <= 0f;
    public AgentPerception Perception => agentPerception;
    public SpeciesData SpeciesData => speciesData;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agentMovement = GetComponent<AgentMovement>();
        agentPerception = GetComponent<AgentPerception>();

        if (speciesData != null)
            ApplySpeciesData(speciesData, setEnergy: true);

        ApplyPhysicalSettings();

        DebugLogger.LogAgentInit(agentId, speciesData?.Species.ToString() ?? "Unknown",
                                speciesData?.LifeStage.ToString() ?? "Unknown");
    }

    private void Start()
    {
        if (agentMovement != null)
            agentMovement.Initialize(moveSpeed, maxAngularVelocity);

        if (agentPerception != null)
            agentPerception.Initialize(viewRadius, viewAngle);

        brain = GetComponent<IAgentBrain>();
        decisionTimer = decisionInterval;

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.RegisterAgent(this);
    }

    private void Update()
    {
        if (!isAlive) return;

        decisionTimer -= Time.deltaTime;
        reproductionCooldownTimer = Mathf.Max(0f, reproductionCooldownTimer - Time.deltaTime);

        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            MakeDecision();
        }

        TickLifecycle(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;
        agentMovement?.ApplyMovement();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, left * viewRadius);
        Gizmos.DrawRay(transform.position, right * viewRadius);
    }

    // -------------------------------------------------------------------------
    // Decision loop
    // -------------------------------------------------------------------------

    private void MakeDecision()
    {
        agentPerception?.UpdatePerception();

        List<AgentAction> validActions = GetAvailableActions();

        currentAction = (brain != null && validActions.Count > 0)
            ? brain.DecideAction(this, validActions)
            : AgentAction.Idle;

        ExecuteAction(currentAction);
    }

    private List<AgentAction> GetAvailableActions()
    {
        var actions = isInShelter
            ? new List<AgentAction> { AgentAction.Idle }
            : new List<AgentAction>
        {
            AgentAction.TurnRight,
            AgentAction.TurnLeft,
            AgentAction.MoveForward,
            AgentAction.Idle
        };

        if (species == Species.Prey)
        {
            if (!isInShelter) actions.Add(AgentAction.EatGrass);
            if (!isInShelter) actions.Add(AgentAction.EnterShelter);
            if (isInShelter) actions.Add(AgentAction.LeaveShelter);
        }

        if (species == Species.Predator)
            actions.Add(AgentAction.Attack);

        if (lifeStage == LifeStage.Adult && !isInShelter)
            actions.Add(AgentAction.Mate);

        return actions;
    }

    private void ExecuteAction(AgentAction action)
    {
        DebugLogger.LogAgentAction(agentId, action);

        switch (action)
        {
            case AgentAction.TurnLeft: TurnLeft(); break;
            case AgentAction.TurnRight: TurnRight(); break;
            case AgentAction.MoveForward: MoveForward(); break;
            case AgentAction.Idle: Idle(); break;
            case AgentAction.EatGrass: EatGrass(); break;
            case AgentAction.EnterShelter: EnterShelter(); break;
            case AgentAction.LeaveShelter: LeaveShelter(); break;
            case AgentAction.Attack: Attack(); break;
            case AgentAction.Mate: Mate(); break;
        }
    }

    // -------------------------------------------------------------------------
    // Movement delegates
    // -------------------------------------------------------------------------

    private void TurnLeft() => agentMovement?.TurnLeft();
    private void TurnRight() => agentMovement?.TurnRight();
    private void MoveForward() => agentMovement?.MoveForward();
    private void Idle() => agentMovement?.Idle();

    // -------------------------------------------------------------------------
    // Actions
    // -------------------------------------------------------------------------

    private void EatGrass()
    {
        if (agentPerception == null) return;

        List<GrassPatch> visible = agentPerception.GetVisibleGrassPatches();
        if (visible.Count == 0) return;

        float feedRange = speciesData != null ? speciesData.FeedingRange : 2f;
        GrassPatch best = null;
        float bestDist = float.MaxValue;

        foreach (GrassPatch g in visible)
        {
            if (g == null) continue;
            float d = Vector3.Distance(transform.position, g.transform.position);
            if (d <= feedRange && d < bestDist) { bestDist = d; best = g; }
        }

        best?.Eat(this);
    }

    private void EnterShelter()
    {
        if (agentPerception == null || agentMovement == null) return;

        List<ShelterZone> visible = agentPerception.GetVisibleShelterZones();
        if (visible.Count == 0) return;

        ShelterZone target = null;
        float bestDist = float.MaxValue;

        foreach (ShelterZone shelter in visible)
        {
            if (shelter == null || !shelter.HasCapacity()) continue;

            float d = Vector3.Distance(transform.position, shelter.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                target = shelter;
            }
        }

        if (target == null) return;

        if (target.IsAgentInside(this))
        {
            Idle();
            return;
        }

        agentMovement.MoveTowards(target.transform.position);
    }

    private void LeaveShelter()
    {
        if (agentMovement == null)
        {
            SetInShelter(false);
            return;
        }

        ShelterZone currentShelter = FindCurrentShelter();
        if (currentShelter != null)
            agentMovement.MoveAwayFrom(currentShelter.transform.position);
        else
            SetInShelter(false);
    }

    private void Attack()
    {
        if (agentPerception == null || agentMovement == null) return;

        List<Agent> visible = agentPerception.GetVisiblePreyAgents();
        if (visible.Count == 0) return;

        float attackRange = speciesData != null ? speciesData.AttackRange : 1.5f;
        Agent target = null;
        float best = float.MaxValue;

        foreach (Agent prey in visible)
        {
            if (prey == null || !prey.IsAlive || prey.IsInShelter) continue;
            float d = Vector3.Distance(transform.position, prey.transform.position);
            if (d < best) { best = d; target = prey; }
        }

        if (target == null) return;

        if (best > attackRange)
        {
            agentMovement.MoveTowards(target.transform.position);
            return;
        }

        if (target != null)
        {
            float gain = speciesData != null ? speciesData.AttackEnergyGain : 50f;
            target.Die(DeathCause.Predation);
            RestoreEnergy(gain);
            DebugLogger.LogAgentKill(agentId, target.AgentId);
            DebugLogger.LogAgentEnergy(agentId, currentEnergy, maxEnergy, "after kill");
        }
    }

    private void Mate()
    {
        if (agentPerception == null || agentMovement == null) return;

        float minEnergy = speciesData != null ? speciesData.MinEnergyToReproduce : 60f;
        if (currentEnergy < minEnergy) return;
        if (!CanReproduce()) return;

        List<Agent> visible = species == Species.Prey
            ? agentPerception.GetVisiblePreyAgents()
            : agentPerception.GetVisiblePredatorAgents();

        if (visible.Count == 0) return;

        float matingRange = speciesData != null ? speciesData.MatingRange : 2f;
        Agent partner = null;
        float best = float.MaxValue;

        foreach (Agent candidate in visible)
        {
            if (candidate == null || candidate == this) continue;
            if (!candidate.IsAlive) continue;
            if (candidate.AgentSpecies != species) continue;
            if (candidate.AgentLifeStage != LifeStage.Adult) continue;
            if (!candidate.CanReproduce()) continue;
            if (candidate.IsInShelter) continue;

            float d = Vector3.Distance(transform.position, candidate.transform.position);
            if (d < best) { best = d; partner = candidate; }
        }

        if (partner == null) return;

        if (best > matingRange)
        {
            agentMovement.MoveTowards(partner.transform.position);
            return;
        }

        Agent offspring = SimulationManager.Instance != null
            ? SimulationManager.Instance.SpawnOffspring(this, partner)
            : null;

        if (offspring != null)
        {
            BeginReproductionCooldown();
            partner.BeginReproductionCooldown();
        }
    }

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void TickLifecycle(float deltaTime)
    {
        currentEnergy -= passiveEnergyLossPerSecond * deltaTime;
        age += deltaTime;

        if (lifeStage == LifeStage.Juvenile && speciesData != null)
        {
            if (age >= speciesData.MaturityAge)
                Mature();
        }

        if (currentEnergy <= starvationThreshold)
            Die(DeathCause.Starvation);
    }

    public void Mature()
    {
        if (lifeStage == LifeStage.Adult) return;

        if (adultSpeciesData == null)
        {
            DebugLogger.LogAgentError(agentId, "adultSpeciesData not assigned — cannot mature. Assign it in the Juvenile prefab Inspector.");
            return;
        }

        lifeStage = LifeStage.Adult;
        ApplySpeciesData(adultSpeciesData, setEnergy: false);
        speciesData = adultSpeciesData;

        DebugLogger.LogAgentMatured(agentId);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void ConsumeEnergy(float amount)
    {
        currentEnergy = Mathf.Max(0f, currentEnergy - amount);
        DebugLogger.LogAgentEnergy(agentId, currentEnergy, maxEnergy, "energy consumed");
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        DebugLogger.LogAgentEnergy(agentId, currentEnergy, maxEnergy, "energy restored");
    }

    public void Die(DeathCause cause)
    {
        if (!isAlive) return;

        isAlive = false;

        SimulationManager.Instance?.UnregisterAgent(this, cause);
        DebugLogger.LogAgentDeath(agentId, cause);
        Destroy(gameObject);
    }

    public void SetInShelter(bool value) => isInShelter = value;

    public void SetAgentId(string id) => agentId = id;

    public void BeginReproductionCooldown()
    {
        reproductionCooldownTimer = speciesData != null ? speciesData.ReproductionCooldown : 15f;
    }

    public bool CanReproduce()
    {
        return lifeStage == LifeStage.Adult
            && speciesData != null
            && isAlive
            && !isInShelter
            && reproductionCooldownTimer <= 0f
            && currentEnergy >= speciesData.MinEnergyToReproduce;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void ApplySpeciesData(SpeciesData data, bool setEnergy)
    {
        species = data.Species;
        lifeStage = data.LifeStage;
        size = data.Size;
        bodyMass = data.BodyMass;
        moveSpeed = data.MoveSpeed;
        maxAngularVelocity = data.MaxAngularVelocity;
        viewRadius = data.ViewRadius;
        viewAngle = data.ViewAngle;
        maxEnergy = data.MaxEnergy;
        passiveEnergyLossPerSecond = data.PassiveEnergyLossPerSecond;
        starvationThreshold = data.StarvationThreshold;

        if (setEnergy)
            currentEnergy = data.StartEnergy;
        else
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);

        if (agentMovement != null) agentMovement.Initialize(moveSpeed, maxAngularVelocity);
        if (agentPerception != null) agentPerception.Initialize(viewRadius, viewAngle);
        ApplyPhysicalSettings();
    }

    private void ApplyPhysicalSettings()
    {
        transform.localScale = Vector3.one * size;

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = bodyMass;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private ShelterZone FindCurrentShelter()
    {
        ShelterZone[] shelters = FindObjectsByType<ShelterZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (ShelterZone shelter in shelters)
        {
            if (shelter != null && shelter.IsAgentInside(this))
                return shelter;
        }

        return null;
    }

}
