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
    public partial struct PlayerMissileAttackServerSystem : ISystem
    {
        private Unity.Mathematics.Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<HomingMissileSpawnerComponent>();
            
            _random = Unity.Mathematics.Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var spawner = SystemAPI.GetSingleton<HomingMissileSpawnerComponent>();

            foreach (var (attack, transform) in SystemAPI
                         .Query<RefRW<PlayerMissileAttackComponent>, RefRO<LocalTransform>>())
            {
                attack.ValueRW.CurrentCooldown -= deltaTime;

                if (attack.ValueRO.CurrentCooldown > 0)
                    continue;

                // 원형으로 미사일 배치
                var angleStep = 360f / spawner.MissileCount;
                
                for (int i = 0; i < spawner.MissileCount; i++)
                {
                    var angle = math.radians(angleStep * i);
                    var offset = new float3(
                        math.cos(angle) * 0.3f,
                        0,
                        math.sin(angle) * 0.3f
                    );

                    // 랜덤 높이 목표
                    var randomHeight = _random.NextFloat(0.5f, 1.5f);
                    var targetHeight = transform.ValueRO.Position.y + spawner.LaunchHeight + randomHeight;

                    // 퍼지는 방향 (방사형)
                    var spreadDirection = new float3(
                        math.cos(angle),
                        0,
                        math.sin(angle)
                    );

                    // 초기 속도: 위쪽 + 퍼지는 방향
                    var spreadStrength = _random.NextFloat(3f, 6f); // 퍼지는 강도
                    var upwardStrength = spawner.Speed * 1.5f; // 위로 올라가는 강도
                    
                    var launchVelocity = new float3(
                        spreadDirection.x * spreadStrength,
                        upwardStrength,
                        spreadDirection.z * spreadStrength
                    );

                    var missile = ecb.Instantiate(spawner.Prefab);

                    ecb.SetComponent(missile, new LocalTransform
                    {
                        Position = transform.ValueRO.Position + offset,
                        Rotation = quaternion.LookRotationSafe(launchVelocity, math.up()),
                        Scale = 1f
                    });

                    ecb.SetComponent(missile, new HomingMissileComponent
                    {
                        Speed = spawner.Speed,
                        MaxDistance = spawner.MaxDistance,
                        TurnSpeed = spawner.TurnSpeed,
                        LaunchHeight = targetHeight,
                        LaunchVelocity = launchVelocity,
                        TraveledDistance = 0,
                        TargetEnemy = Entity.Null,
                        HasLaunched = false
                    });
                }

                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}