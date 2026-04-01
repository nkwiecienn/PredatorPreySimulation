using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AnimalBase : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private AnimalSpeciesData speciesData;

    [Header("Runtime")]
    [SerializeField] private float currentEnergy;

    protected NavMeshAgent Agent { get; private set; }

    public AnimalSpeciesData SpeciesData => speciesData;
    public float CurrentEnergy => currentEnergy;
    public AnimalSpecies Species => speciesData != null ? speciesData.Species : default;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();

        if (speciesData == null)
        {
            Debug.LogError($"{name}: SpeciesData is missing.", this);
            return;
        }

        ApplySpeciesData();
        currentEnergy = speciesData.MaxEnergy;
    }

    protected virtual void Update()
    {
        TickAnimal();
    }

    protected virtual void TickAnimal()
    {
        // For later: hunger decay, sensing, decision making, etc.
    }

    public virtual void Initialize(AnimalSpeciesData data)
    {
        speciesData = data;

        if (Agent == null)
            Agent = GetComponent<NavMeshAgent>();

        ApplySpeciesData();
        currentEnergy = speciesData.MaxEnergy;
    }

    protected virtual void ApplySpeciesData()
    {
        if (speciesData == null || Agent == null)
            return;

        Agent.speed = speciesData.MoveSpeed;
        Agent.angularSpeed = speciesData.AngularSpeed;
        Agent.acceleration = speciesData.Acceleration;
        Agent.stoppingDistance = speciesData.StoppingDistance;
    }
}