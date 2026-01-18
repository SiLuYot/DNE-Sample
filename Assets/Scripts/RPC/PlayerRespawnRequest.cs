using Unity.Collections;
using Unity.NetCode;

namespace RPC
{
    public struct PlayerRespawnRequest : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
    }
}
