using Component.Player;
using Component.Sword;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System.Sword
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct SwordAttackServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<SwordSpawnerComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var spawner = SystemAPI.GetSingleton<SwordSpawnerComponent>();

            foreach (var (attack, input, transform, playerEntity) in SystemAPI
                         .Query<RefRW<PlayerSwordAttackComponent>, RefRO<PlayerInputComponent>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
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

                var baseAngle = math.atan2(aimDir.x, aimDir.z);
                var swingRange = math.radians(180f);
                var startAngle = baseAngle - math.radians(90f);
                var rotationSpeed = swingRange / spawner.Duration;

                var sword = ecb.Instantiate(spawner.Prefab);

                ecb.SetComponent(sword, new LocalTransform
                {
                    Position = transform.ValueRO.Position,
                    Rotation = quaternion.AxisAngle(math.up(), startAngle),
                    Scale = 1f
                });

                ecb.SetComponent(sword, new SwordComponent
                {
                    Duration = spawner.Duration,
                    ElapsedTime = 0,
                    RotationSpeed = rotationSpeed,
                    CurrentAngle = startAngle,
                    OwnerPosition = transform.ValueRO.Position,
                    Durability = attack.ValueRO.AttackLevel,
                });

                ecb.AddComponent(sword, new SwordOwnerComponent
                {
                    Owner = playerEntity
                });

                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown;
            }
        }
    }
}