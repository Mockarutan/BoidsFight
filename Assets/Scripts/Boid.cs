using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public class Boid : MonoBehaviour
{
    public float Separation;
    public float Alignment;
    public float Coherence;

    class Baker : Baker<Boid>
    {
        public override void Bake(Boid authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BoidData
            {
                Separation = authoring.Separation,
                Alignment = authoring.Alignment,
                Coherence = authoring.Coherence,
            });
        }
    }
}

struct BoidData : IComponentData
{
    public float Separation;
    public float Alignment;
    public float Coherence;
}

partial struct BoidsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
