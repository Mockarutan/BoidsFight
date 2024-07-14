using Unity.Entities;
using UnityEngine;

public class BoidTrigger : MonoBehaviour
{
    class Baker : Baker<BoidTrigger>
    {
        public override void Bake(BoidTrigger authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<BoidNeigour>(entity);
        }
    }
}
