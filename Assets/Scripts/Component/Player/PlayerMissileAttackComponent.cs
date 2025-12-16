using Unity.Entities;

namespace Component.Player
{
    public struct PlayerMissileAttackComponent : IComponentData
    {
        public float AttackCooldown;
        public float CurrentCooldown;
    }
}