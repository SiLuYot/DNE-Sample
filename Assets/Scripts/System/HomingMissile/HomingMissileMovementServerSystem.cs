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
    public partial struct HomingMissileMovementServerSystem : ISystem
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
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            NativeArray<Entity> enemyEntities = default;
            NativeArray<LocalTransform> enemyTransforms = default;

            if (!_enemyQuery.IsEmpty)
            {
                enemyEntities = _enemyQuery.ToEntityArray(Allocator.TempJob);
                enemyTransforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            }
            else
            {
                enemyEntities = new NativeArray<Entity>(0, Allocator.TempJob);
                enemyTransforms = new NativeArray<LocalTransform>(0, Allocator.TempJob);
            }

            var movementJob = new HomingMissileMovementServerJob
            {
                DeltaTime = deltaTime,
                EnemyEntities = enemyEntities,
                EnemyTransforms = enemyTransforms,
                Ecb = ecb
            };

            state.Dependency = movementJob.ScheduleParallel(state.Dependency);
            state.Dependency = enemyEntities.Dispose(state.Dependency);
            state.Dependency = enemyTransforms.Dispose(state.Dependency);
        }
    }
}