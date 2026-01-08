using Unity.Entities;

namespace Component.Player
{
    public struct PendingAttackUpgradeComponent : IComponentData
    {
        public int LastUpgradedLevel;
    }
}
