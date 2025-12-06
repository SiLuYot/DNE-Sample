using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Component
{
    public struct PlayerNameComponent : IComponentData
    {
        [GhostField]
        public FixedString128Bytes PlayerName;
    }
}