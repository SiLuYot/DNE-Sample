using Component;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct NetCodeConnectionEventListener : ISystem
    {
        private bool _wasConnected;

        public void OnCreate(ref SystemState state)
        {
            _wasConnected = SystemAPI.HasSingleton<NetworkStreamInGame>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var isConnected = SystemAPI.HasSingleton<NetworkStreamInGame>();
            if (isConnected == _wasConnected)
                return;

            state.EntityManager.CreateEntity(isConnected
                ? typeof(PlayerConnectedEvent)
                : typeof(PlayerDisconnectedEvent));

            _wasConnected = isConnected;
        }
    }
}