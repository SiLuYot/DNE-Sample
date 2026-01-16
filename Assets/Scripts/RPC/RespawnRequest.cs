using Unity.Collections;
using Unity.NetCode;

namespace RPC
{
    /// <summary>
    /// 클라이언트가 서버에 리스폰을 요청하는 RPC
    /// </summary>
    public struct RespawnRequest : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
    }
}
