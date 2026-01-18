using Component.Enemy;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System.Enemy
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EnemySpawnServerSystem : ISystem
    {
        private float _spawnTimer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<EnemySpawnerComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _spawnTimer += SystemAPI.Time.DeltaTime;

            if (_spawnTimer < 3f)
                return;

            _spawnTimer = 0;

            var prefab = SystemAPI.GetSingleton<EnemySpawnerComponent>().Enemy;
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var transform in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<EnemySpawnPointComponent>())
            {
                var enemy = ecb.Instantiate(prefab);
                ecb.SetComponent(enemy, new EnemyComponent());
                ecb.SetComponent(enemy, new LocalTransform
                {
                    Position = transform.ValueRO.Position,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }
        }
    }
}