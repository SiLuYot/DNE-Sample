using Component;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System
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

            var enemy = commandBuffer.Instantiate(prefab);
            commandBuffer.SetComponent(enemy, new EnemyComponent());
            commandBuffer.Playback(state.EntityManager);
        }
    }
}