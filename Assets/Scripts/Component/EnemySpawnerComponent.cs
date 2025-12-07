using Unity.Entities;

namespace Component
{
    public struct EnemySpawnerComponent : IComponentData
    {
        public Entity Enemy;
    }
}