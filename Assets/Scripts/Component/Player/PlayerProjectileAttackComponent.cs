using Unity.Entities;
using Unity.NetCode;

namespace Component.Player
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerProjectileAttackComponent : IComponentData
    {
        public float AttackCooldown;
        public float CurrentCooldown;
        [GhostField] public int AttackLevel;
    }
}