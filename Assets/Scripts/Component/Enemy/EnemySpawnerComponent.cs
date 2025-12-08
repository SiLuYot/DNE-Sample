using Unity.Entities;

namespace Component.Enemy
{
    public struct EnemySpawnerComponent : IComponentData
    {
        public Entity Enemy;
    }
}