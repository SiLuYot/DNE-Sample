using Component;
using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct SwordCollisionSystem : ISystem
    {
        private ComponentLookup<SwordComponent> _swordLookup;
        private ComponentLookup<EnemyComponent> _enemyLookup;
        private ComponentLookup<EnemyDeadTag> _enemyDeadLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<SwordComponent>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _swordLookup = state.GetComponentLookup<SwordComponent>(true);
            _enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
            _enemyDeadLookup = state.GetComponentLookup<EnemyDeadTag>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _swordLookup.Update(ref state);
            _enemyLookup.Update(ref state);
            _enemyDeadLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var expOrbPrefab = Entity.Null;
            if (SystemAPI.TryGetSingleton<ExperienceOrbSpawnerComponent>(out var expSpawner))
            {
                expOrbPrefab = expSpawner.Prefab;
            }

            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new SwordTriggerJob
            {
                SwordLookup = _swordLookup,
                EnemyLookup = _enemyLookup,
                EnemyDeadLookup = _enemyDeadLookup,
                TransformLookup = _transformLookup,
                Ecb = ecb,
                ExperienceOrbPrefab = expOrbPrefab
            }.Schedule(simulation, state.Dependency);
        }
    }

    [BurstCompile]
    public struct SwordTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<SwordComponent> SwordLookup;
        [ReadOnly] public ComponentLookup<EnemyComponent> EnemyLookup;
        [ReadOnly] public ComponentLookup<EnemyDeadTag> EnemyDeadLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer Ecb;
        public Entity ExperienceOrbPrefab;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            Entity swordEntity = Entity.Null;
            Entity enemyEntity = Entity.Null;

            if (SwordLookup.HasComponent(entityA) && EnemyLookup.HasComponent(entityB))
            {
                swordEntity = entityA;
                enemyEntity = entityB;
            }
            else if (SwordLookup.HasComponent(entityB) && EnemyLookup.HasComponent(entityA))
            {
                swordEntity = entityB;
                enemyEntity = entityA;
            }

            if (swordEntity == Entity.Null || enemyEntity == Entity.Null)
                return;

            if (EnemyDeadLookup.HasComponent(enemyEntity))
                return;

            if (ExperienceOrbPrefab != Entity.Null && TransformLookup.HasComponent(enemyEntity))
            {
                var enemyPos = TransformLookup[enemyEntity].Position;
                var expOrb = Ecb.Instantiate(ExperienceOrbPrefab);
                Ecb.SetComponent(expOrb, new LocalTransform
                {
                    Position = enemyPos,
                    Rotation = Unity.Mathematics.quaternion.identity,
                    Scale = 1f
                });
            }

            Ecb.AddComponent<EnemyDeadTag>(enemyEntity);
        }
    }
}
