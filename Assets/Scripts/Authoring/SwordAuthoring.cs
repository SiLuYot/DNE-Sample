using Component;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    [DisallowMultipleComponent]
    public class SwordAuthoring : MonoBehaviour
    {
        class Baker : Baker<SwordAuthoring>
        {
            public override void Bake(SwordAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SwordComponent
                {
                    Duration = 0,
                    ElapsedTime = 0,
                    RotationSpeed = 0,
                    CurrentAngle = 0,
                    OwnerPosition = float3.zero,
                });
            }
        }
    }
}
