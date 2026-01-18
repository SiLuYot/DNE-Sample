using Unity.Entities;

namespace Component.Sword
{
    public struct SwordSpawnerComponent : IComponentData
    {
        public Entity Prefab;
        public float AttackCooldown;
        public float Duration;
        public float OrbitRadius;
    }
}
