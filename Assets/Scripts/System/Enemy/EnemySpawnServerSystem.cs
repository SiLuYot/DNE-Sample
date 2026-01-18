using Component.Enemy;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System.Enemy
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EnemySpawnServerSystem : ISystem
    {
        private float _spawnTimer;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<EnemySpawnerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _spawnTimer += SystemAPI.Time.DeltaTime;

            if (_spawnTimer < 3f)
                return;

            _spawnTimer = 0;

            var prefab = SystemAPI.GetSingleton<EnemySpawnerComponent>().Enemy;
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var transform in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<EnemySpawnPointComponent>())
            {
                var enemy = commandBuffer.Instantiate(prefab);
                commandBuffer.SetComponent(enemy, new EnemyComponent());
                commandBuffer.SetComponent(enemy, new LocalTransform
                {
                    Position = transform.ValueRO.Position,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}