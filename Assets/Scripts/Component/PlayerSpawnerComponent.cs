using Unity.Entities;

namespace Component
{
    public struct PlayerSpawnerComponent : IComponentData
    {
        public Entity Player;
    }
}