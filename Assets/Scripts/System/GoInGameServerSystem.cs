using Component;
using RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace System
{
    // When server receives go in game request, go in game and delete request
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GoInGameServerSystem : ISystem
    {
        private ComponentLookup<NetworkId> _networkIdFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawnerComponent>();

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest>()
                .WithAll<ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));

            _networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get the prefab to instantiate
            var prefab = SystemAPI.GetSingleton<PlayerSpawnerComponent>().Player;

            // Ge the name of the prefab being instantiated
            state.EntityManager.GetName(prefab, out var prefabName);
            var worldName = new FixedString32Bytes(state.WorldUnmanaged.Name);

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _networkIdFromEntity.Update(ref state);

            foreach (var (reqSrc, reqEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequest>().WithEntityAccess())
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(reqSrc.ValueRO.SourceConnection);
                // Get the NetworkId for the requesting client
                var networkId = _networkIdFromEntity[reqSrc.ValueRO.SourceConnection];

                // Log information about the connection request that includes the client's assigned NetworkId and the name of the prefab spawned.
                Debug.Log($"'{worldName}' setting connection '{networkId.Value}' to in game, spawning a Ghost '{prefabName}' for them!");

                // Instantiate the prefab
                var player = commandBuffer.Instantiate(prefab);
                // Associate the instantiated prefab with the connected client's assigned NetworkId
                commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });

                // Add the player to the linked entity group so it is destroyed automatically on disconnect
                commandBuffer.AppendToBuffer(reqSrc.ValueRO.SourceConnection, new LinkedEntityGroup { Value = player });
                commandBuffer.DestroyEntity(reqEntity);
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}