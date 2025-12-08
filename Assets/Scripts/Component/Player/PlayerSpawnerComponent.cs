using Unity.Entities;

namespace Component.Player
{
    public struct PlayerSpawnerComponent : IComponentData
    {
        public Entity Player;
    }
}