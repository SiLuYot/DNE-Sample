using Component;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    [DisallowMultipleComponent]
    public class HomingMissileAuthoring : MonoBehaviour
    {
        class Baker : Baker<HomingMissileAuthoring>
        {
            public override void Bake(HomingMissileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HomingMissileComponent
                {
                    Speed = 0,
                    MaxDistance = 0,
                    TurnSpeed = 0,
                    TraveledDistance = 0,
                    LaunchHeight = 0,
                    LaunchVelocity = float3.zero,
                    TargetEnemy = Entity.Null,
                    HasLaunched = false
                });
            }
        }
    }
}