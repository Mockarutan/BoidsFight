using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class Boid : UnityEngine.MonoBehaviour
{
    public float Speed = 1f;
    public float Separation = 1f;
    public float Alignment = 1f;
    public float Coherence = 1f;
    public BoidTrigger Trigger;

    class Baker : Baker<Boid>
    {
        public override void Bake(Boid authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var trigEntity = GetEntity(authoring.Trigger, TransformUsageFlags.Dynamic);

            AddComponent(entity, new BoidData
            {
                TriggerEntity = trigEntity,
                Speed = authoring.Speed,
                Separation = authoring.Separation,
                Alignment = authoring.Alignment,
                Coherence = authoring.Coherence,
            });
        }
    }
}

//[Knackelibang.EasySettingsLocal]
public static class BoidDebugValues
{
    public static float Speed = 1f;
    public static float Separation = 1f;
    public static float Alignment = 1f;
    public static float Coherence = 1f;
}

struct BoidData : IComponentData
{
    public Entity TriggerEntity;
    public float Speed;
    public float Separation;
    public float Alignment;
    public float Coherence;
}

struct BoidSphereData : IComponentData
{
    public quaternion Rotation;
    public quaternion DeltaRotation;
    public float Radius;
    public float RotationSpeed;
}

struct BoidGroupData : ISharedComponentData
{
    public int Index;
    public float Speed;
    public float Separation;
    public float Alignment;
    public float Coherence;

    static readonly float PHI = math.PI * (3.0f - math.sqrt(5.0f)); // golden angle in radians

    public static float3 GetSpherePosition(quaternion rot, int index, int total, float radius)
    {
        var groupPos = GetUnitSpherePosition(index, total);
        return math.mul(rot, groupPos) * radius;
    }

    public static float3 GetUnitSpherePosition(int index, int total)
    {
        var y = 1 - (index / (float)(total - 1)) * 2; // y goes from 1 to -1
        var radius = math.sqrt(1 - y * y); // radius at y
        var theta = PHI * index; // golden angle increment

        var x = math.cos(theta) * radius;
        var z = math.sin(theta) * radius;

        return math.normalize(new float3(x, y, z));
    }
}

partial struct BoidsSystem : ISystem
{
    const int GROUP_COUNT = 64;

    private bool _Simulate;
    private EntityQuery _BoidsQuery;

    private NativeArray<int> _BoidCount;
    private NativeArray<float3> _AccDirections;
    private NativeArray<float3> _AccPositions;
    private NativeArray<float3> _AvgDirections;
    private NativeArray<float3> _AvgPositions;

    private UnsafeList<NativeList<LocalTransform>> _NewLocalTransforms;

    private BufferLookup<BoidNeigour> _BoidDataLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();

        _BoidsQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, BoidData, BoidGroupData>().Build();

        _BoidCount = new NativeArray<int>(GROUP_COUNT, Allocator.Persistent);
        _AccDirections = new NativeArray<float3>(GROUP_COUNT, Allocator.Persistent);
        _AccPositions = new NativeArray<float3>(GROUP_COUNT, Allocator.Persistent);
        _AvgDirections = new NativeArray<float3>(GROUP_COUNT, Allocator.Persistent);
        _AvgPositions = new NativeArray<float3>(GROUP_COUNT, Allocator.Persistent);
        _NewLocalTransforms = new UnsafeList<NativeList<LocalTransform>>(GROUP_COUNT, Allocator.Persistent);

        _BoidDataLookup = state.GetBufferLookup<BoidNeigour>(true);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        _BoidCount.Dispose();
        _AccDirections.Dispose();
        _AccPositions.Dispose();
        _AvgDirections.Dispose();
        _AvgPositions.Dispose();
        _NewLocalTransforms.Dispose();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<BoidsSettingsData>();
        state.EntityManager.GetAllUniqueSharedComponents<BoidGroupData>(out var groupData, Allocator.TempJob);
        _NewLocalTransforms.Clear();

        _BoidDataLookup.Update(ref state);

        var deltaTime = 0f;
        if (UnityEngine.InputSystem.Keyboard.current.xKey.wasPressedThisFrame)
            _Simulate = _Simulate == false;

        if (_Simulate || UnityEngine.InputSystem.Keyboard.current.cKey.isPressed)
            deltaTime = SystemAPI.Time.DeltaTime;

        var spawnerData = SystemAPI.GetSingleton<BoidSpawnerData>();
        var sphereData = SystemAPI.GetSingleton<BoidSphereData>();
        var combDep = default(JobHandle);

        for (byte i = 1; i < _BoidCount.Length; i++)
        {
            _BoidCount[i] = 0;
            _AccDirections[i] = default;
            _AccPositions[i] = default;
        }

        for (byte i = 1; i < groupData.Length; i++)
        {
            // First one is default
            var index = i - 1;

            _BoidsQuery.ResetFilter();
            _BoidsQuery.SetSharedComponentFilter(groupData[i]);

            var entityCount = _BoidsQuery.CalculateEntityCount();
            var newTransforms = new NativeList<LocalTransform>(entityCount, Allocator.TempJob);
            newTransforms.Length = entityCount;
            _NewLocalTransforms.Add(newTransforms);

            var dep = new SimulateBoids()
            {
                Speed = BoidDebugValues.Speed,
                Separation = BoidDebugValues.Separation,
                Alignment = BoidDebugValues.Alignment,
                Coherence = BoidDebugValues.Coherence,

                DeltaTime = deltaTime,
                Settings = settings,
                GroupIndex = index,
                GroupPosition = BoidGroupData.GetSpherePosition(quaternion.identity, index, spawnerData.GroupCount, sphereData.Radius),
                GroupDatas = groupData.AsArray(),

                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,

                AvgDirections = _AvgDirections,
                AvgPositions = _AvgPositions,

                BoidNeigourLookup = _BoidDataLookup,

                NewTransforms = newTransforms.AsArray(),

                BoidCounts = _BoidCount,
                AccDirections = _AccDirections,
                AccPositions = _AccPositions,

            }.Schedule(_BoidsQuery, state.Dependency);

            combDep = JobHandle.CombineDependencies(dep, combDep);
        }

        state.Dependency = JobHandle.CombineDependencies(state.Dependency, combDep);
        state.Dependency = new ClearBoidNeigourJob().ScheduleParallel(state.Dependency);

        for (byte i = 1; i < groupData.Length; i++)
        {
            // First one is default
            var index = i - 1;

            _AvgDirections[index] = _AccDirections[index] / _BoidCount[index];
            _AvgPositions[index] = _AccPositions[index] / _BoidCount[index];

            _BoidsQuery.ResetFilter();
            _BoidsQuery.SetSharedComponentFilter(groupData[i]);
            _BoidsQuery.CopyFromComponentDataListAsync(_NewLocalTransforms[index], state.Dependency, out JobHandle depHandle);
            state.Dependency = JobHandle.CombineDependencies(state.Dependency, depHandle);
        }

        state.CompleteDependency();

        groupData.Dispose();

        for (int i = 0; i < _NewLocalTransforms.Length; i++)
            _NewLocalTransforms[i].Dispose();
    }

    [BurstCompile]
    partial struct ClearBoidNeigourJob : IJobEntity
    {
        public void Execute(ref DynamicBuffer<BoidNeigour> neigours)
        {
            neigours.Clear();
        }
    }

    [BurstCompile]
    partial struct SimulateBoids : IJobEntity
    {
        public float Speed;
        public float Separation;
        public float Alignment;
        public float Coherence;

        public float DeltaTime;
        public BoidsSettingsData Settings;
        public int GroupIndex;
        public float3 GroupPosition;

        [ReadOnly] public CollisionWorld CollisionWorld;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BoidGroupData> GroupDatas;
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> AvgDirections;
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> AvgPositions;

        [ReadOnly] public BufferLookup<BoidNeigour> BoidNeigourLookup;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<LocalTransform> NewTransforms;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> BoidCounts;
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> AccDirections;
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> AccPositions;

        public void Execute([EntityIndexInQuery] int index, in LocalTransform trans, in BoidData boidData)
        {
            if (AvgDirections[GroupIndex].Equals(default))
            {
                NewTransforms[index] = trans;
            }
            else
            {
                var groupData = GroupDatas[GroupIndex + 1];
                var separStrength = 0f;
                var separationDiff = float3.zero;

                var hitCache = new NativeList<DistanceHit>(Allocator.Temp);
                var neigours = CollisionWorld.OverlapSphere(trans.Position, Settings.SeparationRadius, ref hitCache, Settings.SeparationFilter, QueryInteraction.IgnoreTriggers);

                //var neigours = BoidNeigourLookup[boidData.TriggerEntity];
                if (hitCache.Length > 0)
                {
                    var avgNeigourVec = float3.zero;
                    for (int i = 0; i < hitCache.Length; i++)
                    {
                        var sepVec = hitCache[i].Position - trans.Position;
                        var sepDir = math.normalize(sepVec);
                        var sepFactor = Settings.SeparationRadius - math.length(sepVec);
                        avgNeigourVec += sepDir * sepFactor;
                    }

                    separationDiff = math.cross(trans.Forward(), avgNeigourVec);
                    separStrength = Settings.Separation * groupData.Separation * boidData.Separation * Separation;
                }

                var speed = Settings.Speed * groupData.Speed * boidData.Speed * Speed;

                var targetPosition = GroupPosition; //AvgPositions[GroupIndex]
                var targetDir = math.normalize(targetPosition - trans.Position);
                var alignmentDiff = math.cross(trans.Forward(), AvgDirections[GroupIndex]);
                var coherenceDiff = math.cross(trans.Forward(), targetDir);

                var alignStrength = Settings.Alignment * groupData.Alignment * boidData.Alignment * Alignment;
                var coherStrength = Settings.Coherence * groupData.Coherence * boidData.Coherence * Coherence;
                var combTorque = (alignStrength * alignmentDiff) + (coherStrength * coherenceDiff) + (separStrength * -separationDiff);

                //var combDir = math.normalize(AvgDirections[GroupIndex] * alignStrength + targetDir * coherStrength);
                //UnityEngine.Debug.DrawLine(trans.Position, trans.Position + trans.Forward() * 0.2f, UnityEngine.Color.blue);
                //UnityEngine.Debug.DrawLine(trans.Position, trans.Position + combDir * 0.2f, UnityEngine.Color.red);
                //UnityEngine.Debug.DrawLine(trans.Position, trans.Position + combTorque * 0.2f, UnityEngine.Color.green);

                var rot = quaternion.AxisAngle(math.normalize(combTorque), math.length(combTorque) * DeltaTime);
                var newTrans = LocalTransform.FromPositionRotation(trans.Position, math.mul(rot, trans.Rotation));
                NewTransforms[index] = newTrans.Translate(newTrans.Forward() * DeltaTime * speed);
            }

            BoidCounts[GroupIndex]++;
            AccDirections[GroupIndex] += trans.Forward();
            AccPositions[GroupIndex] += trans.Position;
        }
    }
}
