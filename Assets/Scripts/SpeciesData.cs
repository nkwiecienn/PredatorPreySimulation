using UnityEngine;

[CreateAssetMenu(fileName = "SpeciesData", menuName = "PredatorPrey/SpeciesData")]
public class SpeciesData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private Species species;
    [SerializeField] private LifeStage lifeStage;

    [Header("Physical Properties")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float bodyMass = 1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxAngularVelocity = 180f;

    [Header("Perception")]
    [SerializeField] private float viewRadius = 8f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 120f;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float startEnergy = 80f;
    [SerializeField] private float passiveEnergyLossPerSecond = 0.8f;
    [SerializeField] private float starvationThreshold = 0f;

    [Header("Feeding (Prey Only)")]
    [SerializeField] private float energyRestoredByEating = 25f;
    [SerializeField] private float feedingRange = 1.5f;

    [Header("Attack (Predator Only)")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackEnergyGain = 50f;

    [Header("Reproduction (Adult Only)")]
    [SerializeField] private float matingRange = 2f;
    [SerializeField] private float minEnergyToReproduce = 60f;
    [SerializeField] private float reproductionEnergyCost = 25f;
    [SerializeField] private int offspringCount = 1;
    [SerializeField] private float offspringBirthEnergy = 50f;
    [SerializeField] private float reproductionCooldown = 15f;

    [Header("Lifecycle (Juvenile Only)")]
    [SerializeField] private float maturityAge = 30f;

    // Properties for read-only access
    public Species Species => species;
    public LifeStage LifeStage => lifeStage;
    public float Size => size;
    public float BodyMass => bodyMass;
    public float MoveSpeed => moveSpeed;
    public float MaxAngularVelocity => maxAngularVelocity;
    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public float MaxEnergy => maxEnergy;
    public float StartEnergy => startEnergy;
    public float PassiveEnergyLossPerSecond => passiveEnergyLossPerSecond;
    public float StarvationThreshold => starvationThreshold;
    public float EnergyRestoredByEating => energyRestoredByEating;
    public float FeedingRange => feedingRange;
    public float AttackRange => attackRange;
    public float AttackEnergyGain => attackEnergyGain;
    public float MatingRange => matingRange;
    public float MinEnergyToReproduce => minEnergyToReproduce;
    public float ReproductionEnergyCost => reproductionEnergyCost;
    public int OffspringCount => offspringCount;
    public float OffspringBirthEnergy => offspringBirthEnergy;
    public float ReproductionCooldown => reproductionCooldown;
    public float MaturityAge => maturityAge;
}
