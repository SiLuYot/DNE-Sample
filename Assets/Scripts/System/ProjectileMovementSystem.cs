using Component;
using Component.Enemy;
using Unity.Burst;
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
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _enemyLookup = state.GetComponentLookup<EnemyComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _enemyLookup.Update(ref state);

            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            foreach (var (projectile, transform, entity) in SystemAPI
                         .Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var currentPos = transform.ValueRO.Position;
                var movement = projectile.ValueRO.Direction * projectile.ValueRO.Speed * deltaTime;
                var nextPos = currentPos + movement;

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

                var hitEnemy = false;
                if (physicsWorld.CastRay(rayInput, out var hit))
                {
                    if (_enemyLookup.HasComponent(hit.Entity))
                    {
                        ecb.DestroyEntity(hit.Entity);
                        ecb.DestroyEntity(entity);
                        hitEnemy = true;
                    }
                }

                if (!hitEnemy)
                {
                    transform.ValueRW.Position = nextPos;
                    projectile.ValueRW.TraveledDistance += projectile.ValueRO.Speed * deltaTime;

                    if (projectile.ValueRO.TraveledDistance >= projectile.ValueRO.MaxDistance)
                    {
                        ecb.DestroyEntity(entity);
                    }
                }
            }
        }
    }
}