using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

struct BoidNeigour : IBufferElementData
{
    public float3 Position;
    public float DistanceSq;
}

//[UpdateInGroup(typeof(PhysicsSystemGroup))]
//[UpdateAfter(typeof(PhysicsSimulationGroup))]
//partial struct BoidSeparation : ISystem
//{
//    public BufferLookup<BoidNeigour> _BoidNeigourLookup;
//    public ComponentLookup<BoidData> _BoidDataLookup;
//    public ComponentLookup<LocalToWorld> _LocalToWorldLookup;

//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        _BoidNeigourLookup = state.GetBufferLookup<BoidNeigour>();
//        _BoidDataLookup = state.GetComponentLookup<BoidData>();
//        _LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>();

//        state.RequireForUpdate<SimulationSingleton>();
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        return;

//        _BoidNeigourLookup.Update(ref state);
//        _BoidDataLookup.Update(ref state);
//        _LocalToWorldLookup.Update(ref state);

//        state.Dependency = new TriggerJob
//        {
//            BoidNeigourLookup = _BoidNeigourLookup,
//            BoidDataLookup = _BoidDataLookup,
//            LocalToWorldLookup = _LocalToWorldLookup,

//        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
//    }

//    [BurstCompile]
//    struct TriggerJob : ITriggerEventsJob
//    {
//        public BufferLookup<BoidNeigour> BoidNeigourLookup;

//        [ReadOnly] public ComponentLookup<BoidData> BoidDataLookup;
//        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;

//        public void Execute(TriggerEvent triggerEvent)
//        {
//            var posA = LocalToWorldLookup[triggerEvent.EntityA].Position;
//            var posB = LocalToWorldLookup[triggerEvent.EntityB].Position;
//            var distSq = math.distancesq(posA, posB);

//            var buffA = BoidNeigourLookup[BoidDataLookup[triggerEvent.EntityA].TriggerEntity];
//            var buffB = BoidNeigourLookup[BoidDataLookup[triggerEvent.EntityB].TriggerEntity];

//            var index = 0;
//            for (; index < buffA.Length; index++)
//            {
//                if (distSq < buffA[index].DistanceSq)
//                    break;
//            }
//            buffA.Insert(index, new BoidNeigour { Position = posB, DistanceSq = distSq });

//            index = 0;
//            for (; index < buffB.Length; index++)
//            {
//                if (distSq < buffB[index].DistanceSq)
//                    break;
//            }
//            buffB.Insert(index, new BoidNeigour { Position = posA, DistanceSq = distSq });
//        }
//    }
//}
