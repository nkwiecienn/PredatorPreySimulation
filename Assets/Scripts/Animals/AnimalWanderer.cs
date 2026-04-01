using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AnimalBase))]
[RequireComponent(typeof(NavMeshAgent))]
public class AnimalWanderer : MonoBehaviour
{
    [SerializeField] private WanderArea wanderArea;
    [SerializeField] private int maxAttempts = 10;

    private AnimalBase animal;
    private NavMeshAgent agent;

    private float waitTimer;
    private bool waiting;

    private void Awake()
    {
        animal = GetComponent<AnimalBase>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        PickNextDestination();
    }

    private void Update()
    {
        if (animal == null || animal.SpeciesData == null || agent == null || wanderArea == null)
            return;

        if (agent.pathPending)
            return;

        if (!waiting && agent.remainingDistance <= agent.stoppingDistance)
        {
            waiting = true;
            waitTimer = Random.Range(
                animal.SpeciesData.MinWaitTime,
                animal.SpeciesData.MaxWaitTime
            );
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                PickNextDestination();
            }
        }
    }

    public void Initialize(WanderArea area)
    {
        wanderArea = area;
    }

    private void PickNextDestination()
    {
        if (wanderArea == null || animal == null || animal.SpeciesData == null)
            return;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPoint = wanderArea.GetRandomPoint();

            if (NavMesh.SamplePosition(
                randomPoint,
                out NavMeshHit hit,
                animal.SpeciesData.NavMeshSampleRadius,
                NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        Debug.LogWarning($"{name}: Could not find a valid wander destination.");
    }
}