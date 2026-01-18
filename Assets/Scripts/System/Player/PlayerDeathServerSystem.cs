using Component.Player;
using RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System.Player
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerDeathServerSystem : ISystem
    {
        private ComponentLookup<NetworkId> _networkIdLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _networkIdLookup = state.GetComponentLookup<NetworkId>(true);
            _linkedEntityLookup = state.GetBufferLookup<LinkedEntityGroup>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _networkIdLookup.Update(ref state);
            _linkedEntityLookup.Update(ref state);

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var connectionByNetworkId = new NativeHashMap<int, Entity>(16, Allocator.Temp);
            foreach (var (networkId, entity) in SystemAPI
                .Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                connectionByNetworkId[networkId.ValueRO.Value] = entity;
            }

            foreach (var (playerComp, deadTag, playerEntity) in SystemAPI
                .Query<RefRO<PlayerComponent>, RefRO<PlayerDeadTag>>()
                .WithEntityAccess())
            {
                if (connectionByNetworkId.TryGetValue(playerComp.ValueRO.NetworkId, out var connectionEntity))
                {
                    if (_linkedEntityLookup.HasBuffer(connectionEntity))
                    {
                        var buffer = _linkedEntityLookup[connectionEntity];
                        for (int i = buffer.Length - 1; i >= 0; i--)
                        {
                            if (buffer[i].Value == playerEntity)
                            {
                                buffer.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new PlayerDeathRequest());
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
                }

                ecb.DestroyEntity(playerEntity);
            }

            connectionByNetworkId.Dispose();
        }
    }
}
