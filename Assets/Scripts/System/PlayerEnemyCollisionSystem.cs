using Component.Enemy;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial struct PlayerEnemyCollisionSystem : ISystem
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

            state.Dependency = new PlayerEnemyTriggerJob
            {
                PlayerLookup = _playerLookup,
                EnemyLookup = _enemyLookup,
                EnemyDeadLookup = _enemyDeadLookup,
                PlayerDeadLookup = _playerDeadLookup,
                Ecb = ecb
            }.Schedule(simulation, state.Dependency);
        }
    }

    [BurstCompile]
    public struct PlayerEnemyTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<PlayerComponent> PlayerLookup;
        [ReadOnly] public ComponentLookup<EnemyComponent> EnemyLookup;
        [ReadOnly] public ComponentLookup<EnemyDeadTag> EnemyDeadLookup;
        [ReadOnly] public ComponentLookup<PlayerDeadTag> PlayerDeadLookup;
        public EntityCommandBuffer Ecb;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            Entity playerEntity = Entity.Null;
            Entity enemyEntity = Entity.Null;

            // 플레이어와 적 엔티티 식별
            if (PlayerLookup.HasComponent(entityA) && EnemyLookup.HasComponent(entityB))
            {
                playerEntity = entityA;
                enemyEntity = entityB;
            }
            else if (PlayerLookup.HasComponent(entityB) && EnemyLookup.HasComponent(entityA))
            {
                playerEntity = entityB;
                enemyEntity = entityA;
            }

            if (playerEntity == Entity.Null || enemyEntity == Entity.Null)
                return;

            // 이미 죽은 적은 무시
            if (EnemyDeadLookup.HasComponent(enemyEntity))
                return;

            // 이미 죽은 플레이어는 무시
            if (PlayerDeadLookup.HasComponent(playerEntity))
                return;

            // 플레이어에게 사망 태그 추가
            Ecb.AddComponent<PlayerDeadTag>(playerEntity);
        }
    }
}
