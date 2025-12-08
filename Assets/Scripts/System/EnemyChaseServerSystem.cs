using System.Job;
using Component.Enemy;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EnemyChaseServerSystem : ISystem
    {
        private const float MoveSpeed = 4f;
        private EntityQuery _playerQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<EnemyComponent>();
            state.RequireForUpdate<PlayerComponent>();

            var queryDescription = new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PlayerComponent>(),
                    ComponentType.ReadOnly<LocalTransform>()
                }
            };

            _playerQuery = state.GetEntityQuery(queryDescription);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty)
                return;

            var playerTransforms = _playerQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var job = new EnemyChaseJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                MoveSpeed = MoveSpeed,
                PlayerTransforms = playerTransforms
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency = playerTransforms.Dispose(state.Dependency);
        }
    }
}