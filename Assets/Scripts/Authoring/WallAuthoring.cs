using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Authoring
{
    public class WallAuthoring : MonoBehaviour
    {
        class Baker : Baker<WallAuthoring>
        {
            public override void Bake(WallAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Renderable);

                AddComponent(entity, new PhysicsMass
                {
                    InverseMass = 0,
                    InverseInertia = new float3(0),
                });
            }
        }
    }
}