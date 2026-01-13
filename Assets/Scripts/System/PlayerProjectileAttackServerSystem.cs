using Component;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerProjectileAttackServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<ProjectileSpawnerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var spawner = SystemAPI.GetSingleton<ProjectileSpawnerComponent>();

            foreach (var (attack, input, transform) in SystemAPI
                         .Query<RefRW<PlayerProjectileAttackComponent>, RefRO<PlayerInputComponent>, RefRO<LocalTransform>>())
            {
                if (attack.ValueRO.AttackLevel <= 0)
                    continue;

                attack.ValueRW.CurrentCooldown -= deltaTime;

                if (attack.ValueRO.CurrentCooldown > 0)
                    continue;

                var dir = input.ValueRO.AimDirection;
                if (dir.Equals(float3.zero))
                {
                    dir = new float3(0, 0, 1);
                }
                
                var projectile = ecb.Instantiate(spawner.Prefab);

                ecb.SetComponent(projectile, new LocalTransform
                {
                    Position = transform.ValueRO.Position,
                    Rotation = quaternion.LookRotationSafe(dir, math.up()),
                    Scale = 1f
                });

                ecb.SetComponent(projectile, new ProjectileComponent
                {
                    Speed = spawner.Speed,
                    MaxDistance = spawner.MaxDistance,
                    Direction = dir
                });
                
                var attackSpeedMultiplier = 1.0f + (attack.ValueRO.AttackLevel - 1) * 0.5f;
                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown / attackSpeedMultiplier;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}