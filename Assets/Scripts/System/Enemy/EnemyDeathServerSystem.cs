using Component.Enemy;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

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

            foreach (var (deadTag, entity) in SystemAPI
                         .Query<RefRO<EnemyDeadTag>>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}