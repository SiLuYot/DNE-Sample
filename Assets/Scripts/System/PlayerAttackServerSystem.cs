using Component;
using Component.Enemy;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerAttackServerSystem : ISystem
    {
        //private ComponentLookup<EnemyComponent> _enemyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<ProjectileSpawnerComponent>();

            //_enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //_enemyLookup.Update(ref state);

            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var spawner = SystemAPI.GetSingleton<ProjectileSpawnerComponent>();

            foreach (var (attack, player, transform) in SystemAPI
                         .Query<RefRW<PlayerAttackComponent>, RefRO<PlayerComponent>, RefRO<LocalTransform>>()) 
            {
                attack.ValueRW.CurrentCooldown -= deltaTime;

                if (attack.ValueRO.CurrentCooldown > 0)
                    continue;

                var projectile = ecb.Instantiate(spawner.Prefab);

                var direction = player.ValueRO.Direction;
                if (direction.Equals(float3.zero))
                {
                    direction = new float3(0, 0, 1);
                }

                ecb.SetComponent(projectile, new LocalTransform
                {
                    Position = transform.ValueRO.Position,
                    Rotation = quaternion.LookRotationSafe(direction, math.up()),
                    Scale = 1f
                });

                ecb.SetComponent(projectile, new ProjectileComponent
                {
                    Speed = spawner.Speed,
                    MaxDistance = spawner.MaxDistance,
                    Direction = direction
                });

                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}