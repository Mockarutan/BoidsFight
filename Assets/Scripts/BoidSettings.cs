using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Entities;
using Unity.Physics;

public class BoidSettings : UnityEngine.MonoBehaviour
{
    public float Speed;
    public float Separation;
    public float Alignment;
    public float Coherence;
    public float SeparationRadius;
    public UnityEngine.LayerMask SeparationLayer;

    public UnityEngine.AnimationCurve SeparationCurve;
    public UnityEngine.AnimationCurve AlignmentCurve;
    public UnityEngine.AnimationCurve CoherenceCurve;

    class Baker : Baker<BoidSettings>
    {
        public override void Bake(BoidSettings authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BoidsSettingsData
            {
                Speed = authoring.Speed,
                Separation = authoring.Separation,
                Alignment = authoring.Alignment,
                Coherence = authoring.Coherence,
                SeparationRadius = authoring.SeparationRadius,
                SeparationFilter = new CollisionFilter { CollidesWith = (uint)authoring.SeparationLayer.value },

                SeparationCurve = authoring.SeparationCurve.ToDotsAnimationCurve(),
                AlignmentCurve = authoring.AlignmentCurve.ToDotsAnimationCurve(),
                CoherenceCurve = authoring.CoherenceCurve.ToDotsAnimationCurve(),
            });
        }
    }
}

struct BoidsSettingsData : IComponentData
{
    public float Speed;
    public float Separation;
    public float Alignment;
    public float Coherence;
    public float SeparationRadius;

    public CollisionFilter SeparationFilter;

    public AnimationCurve SeparationCurve;
    public AnimationCurve AlignmentCurve;
    public AnimationCurve CoherenceCurve;
}