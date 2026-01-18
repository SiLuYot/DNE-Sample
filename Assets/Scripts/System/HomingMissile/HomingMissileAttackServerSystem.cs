using Component.HomingMissile;
using Component.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System.HomingMissile
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct HomingMissileAttackServerSystem : ISystem
    {
        private Unity.Mathematics.Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<HomingMissileSpawnerComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _random = Unity.Mathematics.Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var spawner = SystemAPI.GetSingleton<HomingMissileSpawnerComponent>();

            foreach (var (attack, input, transform) in SystemAPI
                         .Query<RefRW<PlayerMissileAttackComponent>, RefRO<PlayerInputComponent>,
                             RefRO<LocalTransform>>())
            {
                if (attack.ValueRO.AttackLevel <= 0)
                    continue;

                attack.ValueRW.CurrentCooldown -= deltaTime;

                if (attack.ValueRO.CurrentCooldown > 0)
                    continue;

                var aimDir = input.ValueRO.AimDirection;
                if (aimDir.Equals(float3.zero))
                {
                    aimDir = new float3(0, 0, 1);
                }

                var baseAngle = math.atan2(aimDir.z, aimDir.x);

                var attackLevel = math.max(1, attack.ValueRO.AttackLevel);
                var missileCount = attackLevel;

                var angleStep = 360f / missileCount;

                for (int i = 0; i < missileCount; i++)
                {
                    var angle = math.radians(angleStep * i) + baseAngle;
                    var offset = new float3(
                        math.cos(angle) * 0.3f,
                        0,
                        math.sin(angle) * 0.3f
                    );

                    var randomHeight = _random.NextFloat(0.5f, 1.5f);
                    var targetHeight = transform.ValueRO.Position.y + spawner.LaunchHeight + randomHeight;

                    var spreadDirection = new float3(
                        math.cos(angle),
                        0,
                        math.sin(angle)
                    );

                    var spreadStrength = _random.NextFloat(3f, 6f);
                    var upwardStrength = spawner.Speed * 1.5f;

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
        }
    }
}