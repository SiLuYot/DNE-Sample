using System.Job;
using Component;
using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(HomingMissileMovementSystem))]
    public partial struct HomingMissileCollisionSystem : ISystem
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
                .WithNone<EnemyDeadTag>()  // 이미 사망 판정된 적은 제외
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

            // 경험치 오브 스포너가 있으면 프리팹 사용, 없으면 Entity.Null
            var expOrbPrefab = Entity.Null;
            if (SystemAPI.TryGetSingleton<ExperienceOrbSpawnerComponent>(out var expSpawner))
            {
                expOrbPrefab = expSpawner.Prefab;
            }

            var job = new HomingMissileCollisionJob
            {
                CollisionRadius = 0.5f,
                EnemyEntities = enemyEntities,
                EnemyTransforms = enemyTransforms,
                Ecb = ecb,
                ExperienceOrbPrefab = expOrbPrefab
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency = enemyEntities.Dispose(state.Dependency);
            state.Dependency = enemyTransforms.Dispose(state.Dependency);
        }
    }
}