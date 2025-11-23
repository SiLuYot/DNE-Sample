using Component;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    public partial struct NetCodeConnectionEventListener : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var entity = state.EntityManager.CreateEntity(typeof(PlayerConnectionComponent));
            state.EntityManager.SetComponentData(entity, new PlayerConnectionComponent());

            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<NetworkStreamDriver>();
            state.RequireForUpdate<PlayerConnectionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var localNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;
            var driver = SystemAPI.GetSingleton<NetworkStreamDriver>();

            var entity = SystemAPI.GetSingletonEntity<PlayerConnectionComponent>();
            var connection = state.EntityManager.GetComponentData<PlayerConnectionComponent>(entity);

            foreach (var evt in driver.ConnectionEventsForTick)
            {
                if (evt.Id.Value != localNetworkId)
                    continue;

                connection.IsConnected = evt.State == ConnectionState.State.Connected;
                connection.Updated = true;
                
                state.EntityManager.SetComponentData(entity, connection);
                break;
            }
        }
    }
}