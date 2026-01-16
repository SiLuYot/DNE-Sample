using Component;
using Component.Player;
using RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    /// <summary>
    /// 리스폰 RPC를 수신하고 새 플레이어를 스폰하는 시스템
    /// GoInGameServerSystem의 플레이어 스폰 로직을 재사용
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct RespawnServerSystem : ISystem
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
                .WithAll<RespawnRequest>()
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
                .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RespawnRequest>>()
                .WithEntityAccess())
            {
                var connectionEntity = reqSrc.ValueRO.SourceConnection;

                // 이미 플레이어가 있는지 확인 (중복 리스폰 방지)
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

                // 랜덤 공격 타입 선택
                var attackType = (AttackUpgradeType)random.NextInt(0, 3);

                // 새 플레이어 엔티티 생성
                var player = commandBuffer.Instantiate(prefab);

                commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                commandBuffer.SetComponent(player, new PlayerComponent { NetworkId = networkId.Value });
                commandBuffer.SetComponent(player, new PlayerNameComponent { PlayerName = reqData.ValueRO.PlayerName });

                // 공격 시스템 초기화 (레벨 1로)
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

                // 경험치 초기화 (레벨 1, 경험치 0)
                commandBuffer.SetComponent(player, new PlayerExperienceComponent
                {
                    CurrentExperience = 0,
                    Level = 1,
                    LastUpgradedLevel = 0,
                    CollectionRadius = 2.0f
                });

                // LinkedEntityGroup에 추가
                commandBuffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = player });

                commandBuffer.DestroyEntity(reqEntity);
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
