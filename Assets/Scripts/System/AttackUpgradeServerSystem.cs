using Component.Player;
using RPC;
using Type;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct AttackUpgradeServerSystem : ISystem
    {
        private ComponentLookup<PlayerComponent> _playerLookup;
        private ComponentLookup<PlayerProjectileAttackComponent> _projectileAttackLookup;
        private ComponentLookup<PlayerMissileAttackComponent> _missileAttackLookup;
        private ComponentLookup<PlayerSwordAttackComponent> _swordAttackLookup;
        private ComponentLookup<PlayerExperienceComponent> _experienceLookup;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AttackUpgradeRequest>()
                .WithAll<ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));

            _playerLookup = state.GetComponentLookup<PlayerComponent>(true);
            _projectileAttackLookup = state.GetComponentLookup<PlayerProjectileAttackComponent>(false);
            _missileAttackLookup = state.GetComponentLookup<PlayerMissileAttackComponent>(false);
            _swordAttackLookup = state.GetComponentLookup<PlayerSwordAttackComponent>(false);
            _experienceLookup = state.GetComponentLookup<PlayerExperienceComponent>(false);
        }

        public void OnUpdate(ref SystemState state)
        {
            _playerLookup.Update(ref state);
            _projectileAttackLookup.Update(ref state);
            _missileAttackLookup.Update(ref state);
            _swordAttackLookup.Update(ref state);
            _experienceLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var playersByNetworkId = new NativeHashMap<int, Entity>(16, Allocator.Temp);

            foreach (var (player, entity) in SystemAPI
                         .Query<RefRO<PlayerComponent>>()
                         .WithEntityAccess())
            {
                playersByNetworkId[player.ValueRO.NetworkId] = entity;
            }

            foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<AttackUpgradeRequest>>()
                         .WithEntityAccess())
            {
                if (!SystemAPI.HasComponent<NetworkId>(reqSrc.ValueRO.SourceConnection))
                {
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                var networkId = SystemAPI.GetComponent<NetworkId>(reqSrc.ValueRO.SourceConnection);

                if (!playersByNetworkId.TryGetValue(networkId.Value, out var playerEntity))
                {
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                if (_experienceLookup.HasComponent(playerEntity))
                {
                    var exp = _experienceLookup[playerEntity];
                    var targetLevel = reqData.ValueRO.TargetLevel;

                    bool isMilestone = targetLevel >= 5 && (targetLevel % 5 == 0);
                    bool alreadyUpgraded = exp.LastUpgradedLevel >= targetLevel;
                    bool levelValid = exp.Level >= targetLevel;

                    if (!isMilestone || alreadyUpgraded || !levelValid)
                    {
                        ecb.DestroyEntity(reqEntity);
                        continue;
                    }

                    exp.LastUpgradedLevel = targetLevel;
                    _experienceLookup[playerEntity] = exp;
                }
                else
                {
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                if (reqData.ValueRO.UpgradeType == AttackUpgradeType.Projectile)
                {
                    if (_projectileAttackLookup.HasComponent(playerEntity))
                    {
                        var attack = _projectileAttackLookup[playerEntity];
                        attack.AttackLevel++;
                        _projectileAttackLookup[playerEntity] = attack;
                    }
                }
                else if (reqData.ValueRO.UpgradeType == AttackUpgradeType.Missile)
                {
                    if (_missileAttackLookup.HasComponent(playerEntity))
                    {
                        var attack = _missileAttackLookup[playerEntity];
                        attack.AttackLevel++;
                        _missileAttackLookup[playerEntity] = attack;
                    }
                }
                else if (reqData.ValueRO.UpgradeType == AttackUpgradeType.Sword)
                {
                    if (_swordAttackLookup.HasComponent(playerEntity))
                    {
                        var attack = _swordAttackLookup[playerEntity];
                        attack.AttackLevel++;
                        _swordAttackLookup[playerEntity] = attack;
                    }
                }

                ecb.DestroyEntity(reqEntity);
            }

            ecb.Playback(state.EntityManager);
            playersByNetworkId.Dispose();
        }
    }
}