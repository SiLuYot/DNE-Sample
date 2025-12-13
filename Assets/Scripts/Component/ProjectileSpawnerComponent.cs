using Unity.Entities;

namespace Component
{
    public struct ProjectileSpawnerComponent : IComponentData
    {
        public Entity Prefab;
        public float AttackCooldown;
        public float Speed;
        public float MaxDistance;
    }
}