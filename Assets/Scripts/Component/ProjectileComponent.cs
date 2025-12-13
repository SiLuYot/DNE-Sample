using Unity.Entities;
using Unity.Mathematics;

namespace Component
{
    public struct ProjectileComponent : IComponentData
    {
        public float Speed;
        public float MaxDistance;
        public float TraveledDistance;
        public float3 Direction;
    }
}