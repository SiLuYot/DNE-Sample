using Component;
using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
        private ComponentLookup<SwordComponent> _swordLookup; // 읽기/쓰기용
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

            state.Dependency = new SwordTriggerJob
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

    [BurstCompile]
    public struct SwordTriggerJob : ITriggerEventsJob
    {
        [NativeDisableParallelForRestriction] public ComponentLookup<SwordComponent> SwordLookup;
        [ReadOnly] public ComponentLookup<SwordOwnerComponent> SwordOwnerLookup;
        [ReadOnly] public ComponentLookup<EnemyComponent> EnemyLookup;
        [ReadOnly] public ComponentLookup<EnemyDeadTag> EnemyDeadLookup;
        [ReadOnly] public ComponentLookup<EnemyKnockbackComponent> EnemyKnockbackLookup;
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

            // 넉백 적용 (내구도와 상관없이 항상 적용)
            if (!EnemyKnockbackLookup.HasComponent(enemyEntity) &&
                SwordOwnerLookup.HasComponent(swordEntity) &&
                TransformLookup.HasComponent(enemyEntity))
            {
                var swordOwner = SwordOwnerLookup[swordEntity];
                if (TransformLookup.HasComponent(swordOwner.Owner))
                {
                    var playerPos = TransformLookup[swordOwner.Owner].Position;
                    var enemyPos = TransformLookup[enemyEntity].Position;
                    var knockbackDir = math.normalizesafe(enemyPos - playerPos);

                    // Y축은 0으로 유지 (2D 평면)
                    knockbackDir.y = 0;
                    knockbackDir = math.normalizesafe(knockbackDir);

                    Ecb.AddComponent(enemyEntity, new EnemyKnockbackComponent
                    {
                        Direction = knockbackDir,
                        Speed = 10f,
                        RemainingTime = 0.2f
                    });
                }
            }

            // 내구도 체크 - 내구도가 없으면 적을 처치하지 않음
            var swordComp = SwordLookup[swordEntity];
            if (swordComp.Durability <= 0)
                return;

            // 내구도 감소
            swordComp.Durability -= 1;
            SwordLookup[swordEntity] = swordComp;

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
