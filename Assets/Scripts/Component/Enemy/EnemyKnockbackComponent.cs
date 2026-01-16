using Unity.Entities;
using Unity.Mathematics;

namespace Component.Enemy
{
    public struct EnemyKnockbackComponent : IComponentData
    {
        public float3 Direction;
        public float Speed;
        public float RemainingTime;
    }
}
