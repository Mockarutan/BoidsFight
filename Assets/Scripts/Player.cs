using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Physics.Extensions;

class Player : MonoBehaviour
{
    public float MaxSpeed;
    public float Acceleration;
    public float RotationSpeed;

    class Baker : Baker<Player>
    {
        public override void Bake(Player authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerInputData>(entity);
            AddComponent(entity, new PlayerData
            {
                MaxSpeed = authoring.MaxSpeed,
                Acceleration = authoring.Acceleration,
                RotationSpeed = authoring.RotationSpeed,
            });
        }
    }
}

struct PlayerData : IComponentData
{
    public float MaxSpeed;
    public float Acceleration;
    public float RotationSpeed;
    public quaternion TargetRotation;
}

struct PlayerInputData : IComponentData
{
    public float3 Accelerate;
    public float2 MouseDelta;
    public bool Fire;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct InputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var input in SystemAPI.Query<RefRW<PlayerInputData>>())
        {
            var forward = Keyboard.current.wKey.isPressed ? 1 : 0;
            var left = Keyboard.current.aKey.isPressed ? -1 : 0;
            var right = Keyboard.current.dKey.isPressed ? 1 : 0;

            input.ValueRW.MouseDelta = Mouse.current.delta.value;
            input.ValueRW.Accelerate.x = left + right;
            input.ValueRW.Accelerate.z = forward;
            input.ValueRW.Fire = Keyboard.current.spaceKey.isPressed;
        }
    }
}

[UpdateBefore(typeof(TransformSystemGroup))]
partial struct PlayerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, velocity, massData, playerData, input) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, PhysicsMass, PlayerData, PlayerInputData>())
        {
            if (math.lengthsq(input.Accelerate) > 0)
            {
                var localAcc = playerData.Acceleration * deltaTime * input.Accelerate;
                var worldAcc = transform.ValueRO.TransformDirection(localAcc);
                velocity.ValueRW.ApplyLinearImpulse(massData, worldAcc);

                if (math.length(velocity.ValueRW.Linear) > playerData.MaxSpeed)
                {
                    velocity.ValueRW.Linear = math.normalize(velocity.ValueRW.Linear) * playerData.MaxSpeed;
                }
            }

            transform.ValueRW = transform.ValueRW.WithRotation(math.slerp(transform.ValueRW.Rotation, playerData.TargetRotation, playerData.RotationSpeed));
        }
    }
}

[UpdateAfter(typeof(TransformSystemGroup))]
partial class CameraUpate : SystemBase
{
    private CameraTarget _CameraTarget;

    protected override void OnUpdate()
    {
        if (_CameraTarget == null)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _CameraTarget = GameObject.FindFirstObjectByType<CameraTarget>();
        }

        if (_CameraTarget != null)
        {
            Entities.ForEach((ref PlayerData playerData, in LocalTransform trans) =>
            {
                if (_CameraTarget.Configurated == false)
                {
                    _CameraTarget.Configurated = true;
                    _CameraTarget.Flattened.rotation = trans.Rotation;
                }

                playerData.TargetRotation = _CameraTarget.Flattened.rotation;

                _CameraTarget.transform.position = trans.Position;
                _CameraTarget.transform.rotation = trans.Rotation;

            }).WithoutBurst().Run();
        }
    }
}