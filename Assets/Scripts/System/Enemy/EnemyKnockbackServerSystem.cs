using Component.Enemy;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace System.Enemy
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyKnockbackServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyKnockbackComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (knockback, transform, entity) in SystemAPI
                         .Query<RefRW<EnemyKnockbackComponent>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                transform.ValueRW.Position += knockback.ValueRO.Direction * knockback.ValueRO.Speed * deltaTime;

                knockback.ValueRW.RemainingTime -= deltaTime;

                if (knockback.ValueRO.RemainingTime <= 0)
                {
                    ecb.RemoveComponent<EnemyKnockbackComponent>(entity);
                }
            }
        }
    }
}
