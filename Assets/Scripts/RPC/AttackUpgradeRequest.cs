using Type;
using Unity.NetCode;

namespace RPC
{
    public struct AttackUpgradeRequest : IRpcCommand
    {
        public AttackUpgradeType UpgradeType;
        public int TargetLevel;
    }
}