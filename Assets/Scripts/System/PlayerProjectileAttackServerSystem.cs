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

        //[BurstCompile]
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

                var projectileCount = attack.ValueRO.AttackLevel;

                // 발사체 방향 생성 (중앙 기준으로 좌우 분산)
                // 레벨 1: 0도
                // 레벨 2: 0도, +10도
                // 레벨 3: -10도, 0도, +10도
                // 레벨 4: -10도, 0도, +10도, +20도
                // 레벨 5: -20도, -10도, 0도, +10도, +20도
                for (int i = 0; i < projectileCount; i++)
                {
                    float3 projectileDir;

                    if (projectileCount == 1)
                    {
                        // 레벨 1: 중앙만
                        projectileDir = dir;
                    }
                    else if (projectileCount == 2)
                    {
                        // 레벨 2: 0도, +10도
                        var angle = i * 10f;
                        projectileDir = math.rotate(quaternion.AxisAngle(math.up(), math.radians(angle)), dir);
                    }
                    else
                    {
                        // 레벨 3+: 좌우 균등 분산
                        // 중앙을 기준으로 좌우 번갈아가며 배치
                        var halfCount = projectileCount / 2;
                        var index = i - halfCount;
                        var angle = index * 10f;
                        projectileDir = math.rotate(quaternion.AxisAngle(math.up(), math.radians(angle)), dir);
                    }

                    var projectile = ecb.Instantiate(spawner.Prefab);

                    ecb.SetComponent(projectile, new LocalTransform
                    {
                        Position = transform.ValueRO.Position,
                        Rotation = quaternion.LookRotationSafe(projectileDir, math.up()),
                        Scale = 1f
                    });

                    ecb.SetComponent(projectile, new ProjectileComponent
                    {
                        Speed = spawner.Speed,
                        MaxDistance = spawner.MaxDistance,
                        Direction = projectileDir
                    });
                }

                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}