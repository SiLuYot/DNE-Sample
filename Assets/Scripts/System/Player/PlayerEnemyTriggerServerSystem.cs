using Component.Enemy;
using Component.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;

namespace System.Player
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial struct PlayerEnemyTriggerServerSystem : ISystem
    {
        private ComponentLookup<PlayerComponent> _playerLookup;
        private ComponentLookup<EnemyComponent> _enemyLookup;
        private ComponentLookup<EnemyDeadTag> _enemyDeadLookup;
        private ComponentLookup<PlayerDeadTag> _playerDeadLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<EnemyComponent>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _playerLookup = state.GetComponentLookup<PlayerComponent>(true);
            _enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
            _enemyDeadLookup = state.GetComponentLookup<EnemyDeadTag>(true);
            _playerDeadLookup = state.GetComponentLookup<PlayerDeadTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _playerLookup.Update(ref state);
            _enemyLookup.Update(ref state);
            _enemyDeadLookup.Update(ref state);
            _playerDeadLookup.Update(ref state);

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new PlayerEnemyTriggerServerJob
            {
                PlayerLookup = _playerLookup,
                EnemyLookup = _enemyLookup,
                EnemyDeadLookup = _enemyDeadLookup,
                PlayerDeadLookup = _playerDeadLookup,
                Ecb = ecb
            }.Schedule(simulation, state.Dependency);
        }
    }
}
