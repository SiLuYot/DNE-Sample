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

namespace System.Player
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlayerRespawnServerSystem : ISystem
    {
        private ComponentLookup<NetworkId> _networkIdLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawnerComponent>();
            state.RequireForUpdate<ProjectileSpawnerComponent>();
            state.RequireForUpdate<HomingMissileSpawnerComponent>();
            state.RequireForUpdate<SwordSpawnerComponent>();

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerRespawnRequest>()
                .WithAll<ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));

            _networkIdLookup = state.GetComponentLookup<NetworkId>(true);
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

            _networkIdLookup.Update(ref state);

            foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<PlayerRespawnRequest>>()
                         .WithEntityAccess())
            {
                var connectionEntity = reqSrc.ValueRO.SourceConnection;

                var hasExistingPlayer = false;
                var networkId = _networkIdLookup[connectionEntity];
                foreach (var playerComp in SystemAPI.Query<RefRO<PlayerComponent>>())
                {
                    if (playerComp.ValueRO.NetworkId == networkId.Value)
                    {
                        hasExistingPlayer = true;
                        break;
                    }
                }

                if (hasExistingPlayer)
                {
                    commandBuffer.DestroyEntity(reqEntity);
                    continue;
                }

                var attackType = (AttackUpgradeType)random.NextInt(0, 3);

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

                commandBuffer.SetComponent(player, new PlayerExperienceComponent
                {
                    CurrentExperience = 0,
                    Level = 1,
                    LastUpgradedLevel = 0,
                    CollectionRadius = 2.0f
                });

                commandBuffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = player });

                commandBuffer.DestroyEntity(reqEntity);
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}