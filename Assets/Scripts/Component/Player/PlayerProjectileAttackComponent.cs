using Unity.Entities;

namespace Component.Player
{
    public struct PlayerProjectileAttackComponent : IComponentData
    {
        public float AttackCooldown;
        public float CurrentCooldown;
    }
}