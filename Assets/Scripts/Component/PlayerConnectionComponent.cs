using Unity.Entities;

namespace Component
{
    public struct PlayerConnectionComponent : IComponentData
    {
        public bool IsConnected;
        public bool Updated;
    }
}