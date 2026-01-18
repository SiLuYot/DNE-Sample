using Component.Projectile;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Projectile
{
    [DisallowMultipleComponent]
    public class ProjectileAuthoring : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _maxDistance = 20f;

        class Baker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ProjectileComponent
                {
                    Speed = authoring._speed,
                    MaxDistance = authoring._maxDistance,
                    TraveledDistance = 0,
                    Direction = default
                });
            }
        }
    }
}