using Unity.NetCode;

namespace RPC
{
    // RPC request from client to server for game to go "in game" and send snapshots / inputs
    public struct GoInGameRequest : IRpcCommand
    {
    }
}