using Unity.Entities;

namespace Component
{
    public struct HomingMissileSpawnerComponent : IComponentData
    {
        public Entity Prefab;
        public float AttackCooldown;
        public float Speed;
        public float MaxDistance;
        public float TurnSpeed;
        public float LaunchHeight;
    }
}