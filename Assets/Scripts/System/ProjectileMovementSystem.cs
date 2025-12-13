using Component;
using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ProjectileMovementSystem : ISystem
    {
        private ComponentLookup<EnemyComponent> _enemyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            _enemyLookup.Update(ref state);

            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            foreach (var (projectile, transform, entity) in SystemAPI
                         .Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var currentPos = transform.ValueRO.Position;
                var movement = projectile.ValueRO.Direction * projectile.ValueRO.Speed * deltaTime;
                var nextPos = currentPos + movement;

                // Raycast로 이동 경로상의 충돌 체크
                var rayInput = new RaycastInput
                {
                    Start = currentPos,
                    End = nextPos,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = ~0u,
                        GroupIndex = 0
                    }
                };

                //physicsWorld.OverlapBox()
                
                var hitEnemy = false;
                if (physicsWorld.CastRay(rayInput, out var hit))
                {
                    if (_enemyLookup.HasComponent(hit.Entity))
                    {
                        // 적과 충돌
                        ecb.DestroyEntity(hit.Entity);
                        ecb.DestroyEntity(entity);
                        hitEnemy = true;
                    }
                }

                if (!hitEnemy)
                {
                    // 충돌하지 않았으면 이동
                    transform.ValueRW.Position = nextPos;
                    projectile.ValueRW.TraveledDistance += projectile.ValueRO.Speed * deltaTime;

                    // 최대 거리 도달 시 제거
                    if (projectile.ValueRO.TraveledDistance >= projectile.ValueRO.MaxDistance)
                    {
                        ecb.DestroyEntity(entity);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}