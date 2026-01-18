using Component.Enemy;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace System.Enemy
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct EnemyChaseServerSystem : ISystem
    {
        private float _moveSpeed;
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _moveSpeed = 3f;

            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<EnemyComponent>();
            state.RequireForUpdate<PlayerComponent>();

            _playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerComponent, LocalTransform>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty)
                return;

            var playerTransforms = _playerQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var job = new EnemyChaseServerJob
            {
                PlayerTransforms = playerTransforms,
                Speed = _moveSpeed,
            };

            var query = SystemAPI.QueryBuilder()
                .WithAll<EnemyComponent, LocalTransform, PhysicsVelocity>()
                .WithNone<EnemyDeadTag>()
                .Build();

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
            state.Dependency = playerTransforms.Dispose(state.Dependency);
        }
    }
}