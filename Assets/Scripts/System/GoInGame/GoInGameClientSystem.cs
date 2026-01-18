using Component.Player;
using RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Authentication;
using EntityQueryBuilder = Unity.Entities.EntityQueryBuilder;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;
using WorldSystemFilterFlags = Unity.Entities.WorldSystemFilterFlags;

namespace System.GoInGame
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GoInGameClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawnerComponent>();

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();

            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (id, entity) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithEntityAccess()
                         .WithNone<NetworkStreamInGame>())
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(entity);

                var req = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(req, new GoInGameRequest() { PlayerName = AuthenticationService.Instance.PlayerName });
                commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}