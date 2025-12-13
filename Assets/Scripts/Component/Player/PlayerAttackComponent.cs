using Unity.Entities;

namespace Component.Player
{
    public struct PlayerAttackComponent : IComponentData
    {
        public float AttackCooldown;
        public float CurrentCooldown;
    }
}