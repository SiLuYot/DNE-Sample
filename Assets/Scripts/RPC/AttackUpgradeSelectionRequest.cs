using Unity.NetCode;

namespace RPC
{
    public struct AttackUpgradeSelectionRequest : IRpcCommand
    {
        public AttackUpgradeType UpgradeType;
        public int TargetLevel;
    }

    public enum AttackUpgradeType : byte
    {
        Projectile = 0,
        Missile = 1,
        Sword = 2
    }
}
