using Unity.Entities;
using Unity.Mathematics;

namespace Component
{
    public struct HomingMissileComponent : IComponentData
    {
        public float Speed;
        public float MaxDistance;
        public float TraveledDistance;
        public float TurnSpeed;
        public float LaunchHeight;
        public float3 LaunchVelocity;
        public Entity TargetEnemy;
        public bool HasLaunched;
    }
}