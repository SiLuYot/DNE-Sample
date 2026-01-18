using Unity.Mathematics;
using Unity.NetCode;

namespace Component.Player
{
    public struct PlayerInputComponent : IInputComponentData
    {
        public float Horizontal;
        public float Vertical;
        public float3 AimDirection;
    }
}