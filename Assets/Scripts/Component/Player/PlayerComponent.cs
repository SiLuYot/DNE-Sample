using Unity.Entities;
using Unity.Mathematics;

namespace Component.Player
{
    public struct PlayerComponent : IComponentData
    {
        public int NetworkId;
        public float3 Direction;
    }
}