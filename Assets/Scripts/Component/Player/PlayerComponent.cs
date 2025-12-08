using Unity.Entities;

namespace Component.Player
{
    public struct PlayerComponent : IComponentData
    {
        public int NetworkId;
    }
}