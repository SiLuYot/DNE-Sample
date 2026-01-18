using Component.Enemy;
using Component.Experience;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System.Enemy
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct EnemyDeathServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var expOrbPrefab = Entity.Null;
            if (SystemAPI.TryGetSingleton<ExperienceOrbSpawnerComponent>(out var expSpawner))
            {
                expOrbPrefab = expSpawner.Prefab;
            }

            foreach (var (deadTag, transform, entity) in SystemAPI
                         .Query<RefRO<EnemyDeadTag>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (expOrbPrefab != Entity.Null)
                {
                    var expOrb = ecb.Instantiate(expOrbPrefab);
                    ecb.SetComponent(expOrb, new LocalTransform
                    {
                        Position = transform.ValueRO.Position,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                }

                ecb.DestroyEntity(entity);
            }
        }
    }
}
