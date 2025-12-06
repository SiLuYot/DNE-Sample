using Unity.Collections;
using Unity.NetCode;

namespace RPC
{
    public struct GoInGameRequest : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
    }
}