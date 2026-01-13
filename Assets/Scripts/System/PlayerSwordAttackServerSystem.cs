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
    public partial struct PlayerSwordAttackServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<SwordSpawnerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

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

                // 마우스 방향 기준 시작 각도 (Y축 회전)
                var baseAngle = math.atan2(aimDir.x, aimDir.z);
                // -45도에서 시작해서 +45도까지 (총 90도 회전)
                var swingRange = math.radians(90f);
                var startAngle = baseAngle - math.radians(45f);
                var rotationSpeed = swingRange / spawner.Duration;

                var sword = ecb.Instantiate(spawner.Prefab);

                // Sword 위치는 플레이어 위치에 고정
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
                });

                ecb.AddComponent(sword, new SwordOwnerComponent
                {
                    Owner = playerEntity
                });

                var attackSpeedMultiplier = 1.0f + (attack.ValueRO.AttackLevel - 1) * 0.2f;
                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown / attackSpeedMultiplier;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
