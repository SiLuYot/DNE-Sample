using Component.Experience;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System.Experience
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExperienceMovementServerSystem))]
    public partial struct ExperienceCollectionServerSystem : ISystem
    {
        private EntityQuery _orbQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerExperienceComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _orbQuery = SystemAPI.QueryBuilder()
                .WithAll<ExperienceOrbComponent, LocalTransform>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_orbQuery.IsEmpty)
                return;

            var orbEntities = _orbQuery.ToEntityArray(Allocator.TempJob);
            var orbTransforms = _orbQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var orbComponents = _orbQuery.ToComponentDataArray<ExperienceOrbComponent>(Allocator.TempJob);

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (playerExp, playerTrans) in SystemAPI
                         .Query<RefRW<PlayerExperienceComponent>, RefRO<LocalTransform>>())
            {
                var playerPos = playerTrans.ValueRO.Position;
                var collectionRadiusSq = 0.5f * 0.5f;

                for (int i = 0; i < orbEntities.Length; i++)
                {
                    var orbPos = orbTransforms[i].Position;
                    var distanceSq = math.distancesq(playerPos, orbPos);

                    if (distanceSq <= collectionRadiusSq)
                    {
                        playerExp.ValueRW.CurrentExperience += orbComponents[i].ExperienceValue;
                        playerExp.ValueRW.Level = playerExp.ValueRO.CurrentExperience / 100 + 1;

                        ecb.DestroyEntity(orbEntities[i]);
                    }
                }
            }

            orbEntities.Dispose();
            orbTransforms.Dispose();
            orbComponents.Dispose();
        }
    }
}