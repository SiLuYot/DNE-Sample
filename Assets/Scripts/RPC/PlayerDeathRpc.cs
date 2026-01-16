using Unity.NetCode;

namespace RPC
{
    /// <summary>
    /// 서버에서 클라이언트로 플레이어 사망을 알리는 RPC
    /// </summary>
    public struct PlayerDeathRpc : IRpcCommand
    {
    }
}
