using Component;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExperienceOrbMovementSystem))]
    public partial struct ExperienceCollectionSystem : ISystem
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
                var collectionRadiusSq = 0.5f * 0.5f; // 실제 획득 반경 (0.5 유닛)

                for (int i = 0; i < orbEntities.Length; i++)
                {
                    var orbPos = orbTransforms[i].Position;
                    var distanceSq = math.distancesq(playerPos, orbPos);

                    // 충분히 가까우면 경험치 획득
                    if (distanceSq <= collectionRadiusSq)
                    {
                        playerExp.ValueRW.CurrentExperience += orbComponents[i].ExperienceValue;

                        // 레벨 계산: [경험치/100 + 1]
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
