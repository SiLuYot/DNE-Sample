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
    public partial struct PlayerAttackServerSystem : ISystem
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

            foreach (var (attack, player, transform) in SystemAPI
                         .Query<RefRW<PlayerAttackComponent>, RefRO<PlayerComponent>, RefRO<LocalTransform>>())
            {
                attack.ValueRW.CurrentCooldown -= deltaTime;

                if (attack.ValueRO.CurrentCooldown > 0)
                    continue;

                var dir = player.ValueRO.Direction;
                if (dir.Equals(float3.zero))
                {
                    dir = new float3(0, 0, 1);
                }

                var dir1 = math.rotate(quaternion.AxisAngle(math.up(), math.radians(10f)), dir);
                var dir2 = math.rotate(quaternion.AxisAngle(math.up(), math.radians(-10f)), dir);

                var directions = new float3[]
                {
                    dir, dir1, dir2
                };

                for (int i = 0; i < directions.Length; i++)
                {
                    var projectile = ecb.Instantiate(spawner.Prefab);

                    ecb.SetComponent(projectile, new LocalTransform
                    {
                        Position = transform.ValueRO.Position,
                        Rotation = quaternion.LookRotationSafe(directions[i], math.up()),
                        Scale = 1f
                    });

                    ecb.SetComponent(projectile, new ProjectileComponent
                    {
                        Speed = spawner.Speed,
                        MaxDistance = spawner.MaxDistance,
                        Direction = directions[i]
                    });
                }

                attack.ValueRW.CurrentCooldown = attack.ValueRO.AttackCooldown;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}