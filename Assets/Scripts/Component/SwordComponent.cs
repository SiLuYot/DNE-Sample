using Unity.Entities;
using Unity.Mathematics;

namespace Component
{
    public struct SwordComponent : IComponentData
    {
        public float Duration;
        public float ElapsedTime;
        public float RotationSpeed;
        public float CurrentAngle;
        public float3 OwnerPosition;
        public int Durability;
    }
}
