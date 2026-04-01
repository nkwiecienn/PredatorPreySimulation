using UnityEngine;

[CreateAssetMenu(fileName = "AnimalSpeciesData", menuName = "PredatorPrey/Animal Species Data")]
public class AnimalSpeciesData : ScriptableObject
{
    [Header("Identity")]
    public AnimalSpecies Species;
    public string DisplayName;

    [Header("Movement")]
    public float MoveSpeed = 2f;
    public float AngularSpeed = 180f;
    public float Acceleration = 6f;
    public float StoppingDistance = 0.2f;

    [Header("Wandering")]
    public float MinWaitTime = 1f;
    public float MaxWaitTime = 3f;
    public float NavMeshSampleRadius = 8f;

    [Header("Runtime Properties")]
    public float MaxEnergy = 100f;
    public float ViewDistance = 10f;
    public int RayCount = 5;

    [Header("Diet")]
    public bool EatsGrass = false;
    public bool EatsPrey = false;
}