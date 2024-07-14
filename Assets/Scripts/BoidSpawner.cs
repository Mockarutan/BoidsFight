using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class BoidSpawner : UnityEngine.MonoBehaviour
{
    public int TargetTotalCount;
    public int GroupCount;
    public float Radius;
    public float GroupRadius;
    public float SphereRotationSpeed;
    public Boid Prefab;

    class Baker : Baker<BoidSpawner>
    {
        public override void Bake(BoidSpawner authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BoidSpawnerData
            {
                TargetTotalCount = authoring.TargetTotalCount,
                GroupCount = authoring.GroupCount,
                GroupRadius = authoring.GroupRadius,
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            });
            AddComponent(entity, new BoidSphereData
            {
                RotationSpeed = authoring.SphereRotationSpeed,
                Radius = authoring.Radius,
            });
        }
    }
}

struct BoidSpawnerData : IComponentData
{
    public int TargetTotalCount;
    public int GroupCount;
    public float GroupRadius;
    public float SphereRotationSpeed;
    public Entity Prefab;

    public int EntitiesPerGroup => TargetTotalCount / GroupCount;
    public int TotalCount => EntitiesPerGroup * GroupCount;
}

partial struct BoidSpawnerSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            var groupData = new BoidGroupData
            {
                Speed = 1,
                Separation = 1,
                Alignment = 1,
                Coherence = 1,
            };

            var spawner = SystemAPI.GetSingletonEntity<BoidSpawnerData>();
            var origin = float3.zero;// SystemAPI.GetComponent<LocalTransform>(spawner).Position;
            var data = SystemAPI.GetComponent<BoidSpawnerData>(spawner);
            var sphereData = SystemAPI.GetComponent<BoidSphereData>(spawner);
            var entities = new NativeArray<Entity>(data.TotalCount, Allocator.Temp);
            state.EntityManager.Instantiate(data.Prefab, entities);

            var rand = new Random(1234);// (uint)UnityEngine.Random.Range(0, int.MaxValue));
            var randStartRot = quaternion.identity;// rand.NextQuaternionRotation();

            for (int i = 0; i < data.GroupCount; i++)
            {
                var groupPos = origin + BoidGroupData.GetSpherePosition(randStartRot, i, data.GroupCount, sphereData.Radius);
                groupData.Index = i;

                for (int k = 0; k < data.EntitiesPerGroup; k++)
                {
                    var entityIndex = i * data.EntitiesPerGroup + k;
                    var randRot = rand.NextQuaternionRotation();
                    var randDir = math.mul(rand.NextQuaternionRotation(), math.forward());
                    var randDist = rand.NextFloat(0, data.GroupRadius);
                    var pos = groupPos + (randDir * randDist);

                    SystemAPI.SetComponent(entities[entityIndex], LocalTransform.FromPositionRotation(pos, randRot));
                    state.EntityManager.AddSharedComponentManaged(entities[entityIndex], groupData);
                }
            }
        }
    }
}