using Component.Player;
using RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerCleanupSystem : ISystem
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

            // NetworkId -> 연결 엔티티 매핑 생성
            var connectionByNetworkId = new NativeHashMap<int, Entity>(16, Allocator.Temp);
            foreach (var (networkId, entity) in SystemAPI
                .Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                connectionByNetworkId[networkId.ValueRO.Value] = entity;
            }

            // PlayerDeadTag가 있는 플레이어 처리
            foreach (var (playerComp, deadTag, playerEntity) in SystemAPI
                .Query<RefRO<PlayerComponent>, RefRO<PlayerDeadTag>>()
                .WithEntityAccess())
            {
                // 연결 엔티티 찾기
                if (connectionByNetworkId.TryGetValue(playerComp.ValueRO.NetworkId, out var connectionEntity))
                {
                    // LinkedEntityGroup에서 플레이어 엔티티 제거
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

                    // 클라이언트에 사망 알림 RPC 전송
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new PlayerDeathRpc());
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
                }

                // 플레이어 엔티티 삭제
                ecb.DestroyEntity(playerEntity);
            }

            connectionByNetworkId.Dispose();
        }
    }
}
