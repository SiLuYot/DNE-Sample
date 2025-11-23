using Unity.NetCode;

namespace Component
{
    public struct PlayerInputComponent : IInputComponentData
    {
        public float Horizontal;
        public float Vertical;
    }
}