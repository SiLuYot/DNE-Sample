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
        private Unity.Mathematics.Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawnerComponent>();
            state.RequireForUpdate<ProjectileSpawnerComponent>();
            state.RequireForUpdate<HomingMissileSpawnerComponent>();
            state.RequireForUpdate<SwordSpawnerComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest>()
                .WithAll<ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));

            _networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
            _random = Unity.Mathematics.Random.CreateFromIndex((uint)state.WorldUnmanaged.Time.ElapsedTime + 1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var prefab = SystemAPI.GetSingleton<PlayerSpawnerComponent>().Player;
            var projectile = SystemAPI.GetSingleton<ProjectileSpawnerComponent>();
            var missile = SystemAPI.GetSingleton<HomingMissileSpawnerComponent>();
            var sword = SystemAPI.GetSingleton<SwordSpawnerComponent>();

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            _networkIdFromEntity.Update(ref state);

            foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                         .WithAll<GoInGameRequest>()
                         .WithEntityAccess())
            {
                var attackType = (AttackUpgradeType)_random.NextInt(0, 3);

                ecb.AddComponent<NetworkStreamInGame>(reqSrc.ValueRO.SourceConnection);

                var networkId = _networkIdFromEntity[reqSrc.ValueRO.SourceConnection];
                var player = ecb.Instantiate(prefab);

                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                ecb.SetComponent(player, new PlayerComponent { NetworkId = networkId.Value });
                ecb.SetComponent(player, new PlayerNameComponent { PlayerName = reqData.ValueRO.PlayerName });
                ecb.SetComponent(player, new PlayerProjectileAttackComponent
                {
                    AttackCooldown = projectile.AttackCooldown,
                    CurrentCooldown = 0f,
                    AttackLevel = attackType == AttackUpgradeType.Projectile ? 1 : 0
                });
                ecb.SetComponent(player, new PlayerMissileAttackComponent
                {
                    AttackCooldown = missile.AttackCooldown,
                    CurrentCooldown = 0f,
                    AttackLevel = attackType == AttackUpgradeType.Missile ? 1 : 0
                });
                ecb.SetComponent(player, new PlayerSwordAttackComponent
                {
                    AttackCooldown = sword.AttackCooldown,
                    CurrentCooldown = 0f,
                    AttackLevel = attackType == AttackUpgradeType.Sword ? 1 : 0
                });

                ecb.AppendToBuffer(reqSrc.ValueRO.SourceConnection, new LinkedEntityGroup { Value = player });
                ecb.DestroyEntity(reqEntity);
            }
        }
    }
}