using Unity.Entities;

namespace Component
{
    public struct PlayerComponent : IComponentData
    {
        public int NetworkId;
    }
}