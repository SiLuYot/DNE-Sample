using Component.Enemy;
using Component.HomingMissile;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace System.HomingMissile
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(HomingMissileMovementServerSystem))]
    public partial struct HomingMissileCollisionServerSystem : ISystem
    {
        private EntityQuery _enemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<HomingMissileComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyComponent, LocalTransform>()
                .WithNone<EnemyDeadTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_enemyQuery.IsEmpty)
                return;

            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyTransforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            var job = new HomingMissileCollisionServerJob
            {
                CollisionRadius = 0.5f,
                EnemyEntities = enemyEntities,
                EnemyTransforms = enemyTransforms,
                Ecb = ecb
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency = enemyEntities.Dispose(state.Dependency);
            state.Dependency = enemyTransforms.Dispose(state.Dependency);
        }
    }
}
