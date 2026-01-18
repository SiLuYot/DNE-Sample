using Component.Enemy;
using Component.Experience;
using Component.Sword;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace System.Sword
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct SwordTriggerServerSystem : ISystem
    {
        private ComponentLookup<SwordComponent> _swordLookup;
        private ComponentLookup<SwordOwnerComponent> _swordOwnerLookup;
        private ComponentLookup<EnemyComponent> _enemyLookup;
        private ComponentLookup<EnemyDeadTag> _enemyDeadLookup;
        private ComponentLookup<EnemyKnockbackComponent> _enemyKnockbackLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<SwordComponent>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _swordLookup = state.GetComponentLookup<SwordComponent>(false);
            _swordOwnerLookup = state.GetComponentLookup<SwordOwnerComponent>(true);
            _enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
            _enemyDeadLookup = state.GetComponentLookup<EnemyDeadTag>(true);
            _enemyKnockbackLookup = state.GetComponentLookup<EnemyKnockbackComponent>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _swordLookup.Update(ref state);
            _swordOwnerLookup.Update(ref state);
            _enemyLookup.Update(ref state);
            _enemyDeadLookup.Update(ref state);
            _enemyKnockbackLookup.Update(ref state);
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

            state.Dependency = new SwordTriggerServerJob
            {
                SwordLookup = _swordLookup,
                SwordOwnerLookup = _swordOwnerLookup,
                EnemyLookup = _enemyLookup,
                EnemyDeadLookup = _enemyDeadLookup,
                EnemyKnockbackLookup = _enemyKnockbackLookup,
                TransformLookup = _transformLookup,
                Ecb = ecb,
                ExperienceOrbPrefab = expOrbPrefab
            }.Schedule(simulation, state.Dependency);
        }
    }
}