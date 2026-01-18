using Component.HomingMissile;
using Component.Player;
using Component.Projectile;
using Component.Sword;
using RPC;
using Type;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System.GoInGame
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GoInGameServerSystem : ISystem
    {
        private ComponentLookup<NetworkId> _networkIdFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawnerComponent>();
            state.RequireForUpdate<ProjectileSpawnerComponent>();
            state.RequireForUpdate<HomingMissileSpawnerComponent>();
            state.RequireForUpdate<SwordSpawnerComponent>();

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest>()
                .WithAll<ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));

            _networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            var prefab = SystemAPI.GetSingleton<PlayerSpawnerComponent>().Player;
            var projectile = SystemAPI.GetSingleton<ProjectileSpawnerComponent>();
            var missile = SystemAPI.GetSingleton<HomingMissileSpawnerComponent>();
            var sword = SystemAPI.GetSingleton<SwordSpawnerComponent>();

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            _networkIdFromEntity.Update(ref state);

            foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                         .WithAll<GoInGameRequest>()
                         .WithEntityAccess())
            {
                var attackType = (AttackUpgradeType)random.NextInt(0, 3);

                commandBuffer.AddComponent<NetworkStreamInGame>(reqSrc.ValueRO.SourceConnection);

                var networkId = _networkIdFromEntity[reqSrc.ValueRO.SourceConnection];
                var player = commandBuffer.Instantiate(prefab);

                commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                commandBuffer.SetComponent(player, new PlayerComponent { NetworkId = networkId.Value });
                commandBuffer.SetComponent(player, new PlayerNameComponent { PlayerName = reqData.ValueRO.PlayerName });
                commandBuffer.SetComponent(player, new PlayerProjectileAttackComponent
                {
                    AttackCooldown = projectile.AttackCooldown,
                    CurrentCooldown = 0f,
                    AttackLevel = attackType == AttackUpgradeType.Projectile ? 1 : 0
                });
                commandBuffer.SetComponent(player, new PlayerMissileAttackComponent
                {
                    AttackCooldown = missile.AttackCooldown,
                    CurrentCooldown = 0f,
                    AttackLevel = attackType == AttackUpgradeType.Missile ? 1 : 0
                });
                commandBuffer.SetComponent(player, new PlayerSwordAttackComponent
                {
                    AttackCooldown = sword.AttackCooldown,
                    CurrentCooldown = 0f,
                    AttackLevel = attackType == AttackUpgradeType.Sword ? 1 : 0
                });

                commandBuffer.AppendToBuffer(reqSrc.ValueRO.SourceConnection, new LinkedEntityGroup { Value = player });
                commandBuffer.DestroyEntity(reqEntity);
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}