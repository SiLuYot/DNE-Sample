using Component.Enemy;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    /// <summary>
    /// EnemyDeadTag가 있는 Enemy를 정리하는 시스템
    /// 충돌 시스템들이 실행된 후에 실행되어 사망한 적들을 삭제함
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileCollisionSystem))]
    [UpdateAfter(typeof(HomingMissileCollisionSystem))]
    public partial struct EnemyCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // EnemyDeadTag가 있는 모든 Enemy를 삭제
            foreach (var (deadTag, entity) in SystemAPI
                .Query<RefRO<EnemyDeadTag>>()
                .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
