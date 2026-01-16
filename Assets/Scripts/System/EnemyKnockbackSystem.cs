using Component.Enemy;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyKnockbackSystem : ISystem
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
                // 넉백 방향으로 이동
                transform.ValueRW.Position += knockback.ValueRO.Direction * knockback.ValueRO.Speed * deltaTime;

                // 남은 시간 감소
                knockback.ValueRW.RemainingTime -= deltaTime;

                // 넉백 종료
                if (knockback.ValueRO.RemainingTime <= 0)
                {
                    ecb.RemoveComponent<EnemyKnockbackComponent>(entity);
                }
            }
        }
    }
}
