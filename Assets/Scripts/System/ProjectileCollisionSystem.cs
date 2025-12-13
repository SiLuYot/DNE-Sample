using Component;
using Component.Enemy;
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
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct ProjectileCollisionSystem : ISystem
    {
        private ComponentLookup<ProjectileComponent> _projectileLookup;
        private ComponentLookup<EnemyComponent> _enemyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<SimulationSingleton>();

            _projectileLookup = state.GetComponentLookup<ProjectileComponent>(true);
            _enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            
            _projectileLookup.Update(ref state);
            _enemyLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            var triggerEvents = simulation.AsSimulation().TriggerEvents;
            
            // Trigger Events 처리
            foreach (var triggerEvent in triggerEvents)
            {
                var entityA = triggerEvent.EntityA;
                var entityB = triggerEvent.EntityB;

                var isAProjectile = _projectileLookup.HasComponent(entityA);
                var isBProjectile = _projectileLookup.HasComponent(entityB);

                var isAEnemy = _enemyLookup.HasComponent(entityA);
                var isBEnemy = _enemyLookup.HasComponent(entityB);

                // 발사체와 적이 충돌했는지 확인
                if ((isAProjectile && isBEnemy) || (isBProjectile && isAEnemy))
                {
                    ecb.DestroyEntity(entityA);
                    ecb.DestroyEntity(entityB);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}