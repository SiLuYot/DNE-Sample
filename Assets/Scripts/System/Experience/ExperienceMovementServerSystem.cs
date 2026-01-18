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
    public partial struct ExperienceMovementServerSystem : ISystem
    {
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<ExperienceOrbComponent>();

            _playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerComponent, PlayerExperienceComponent, LocalTransform>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty)
                return;

            var playerEntities = _playerQuery.ToEntityArray(Allocator.Temp);
            var playerTransforms = _playerQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var playerExperiences = _playerQuery.ToComponentDataArray<PlayerExperienceComponent>(Allocator.Temp);

            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (orb, trans) in SystemAPI
                         .Query<RefRO<ExperienceOrbComponent>, RefRW<LocalTransform>>())
            {
                var orbPos = trans.ValueRO.Position;
                var closestPlayer = Entity.Null;
                var closestDistanceSq = float.MaxValue;

                for (int i = 0; i < playerEntities.Length; i++)
                {
                    var playerPos = playerTransforms[i].Position;
                    var distanceSq = math.distancesq(orbPos, playerPos);
                    var attractionRadiusSq =
                        playerExperiences[i].CollectionRadius * playerExperiences[i].CollectionRadius;

                    if (distanceSq <= attractionRadiusSq && distanceSq < closestDistanceSq)
                    {
                        closestDistanceSq = distanceSq;
                        closestPlayer = playerEntities[i];
                    }
                }

                if (closestPlayer != Entity.Null)
                {
                    var playerIndex = -1;
                    for (int i = 0; i < playerEntities.Length; i++)
                    {
                        if (playerEntities[i] == closestPlayer)
                        {
                            playerIndex = i;
                            break;
                        }
                    }

                    if (playerIndex >= 0)
                    {
                        var playerPos = playerTransforms[playerIndex].Position;
                        var dirVector = playerPos - orbPos;

                        if (math.lengthsq(dirVector) > 0.0001f)
                        {
                            var direction = math.normalize(dirVector);
                            var newPos = orbPos + direction * orb.ValueRO.MoveSpeed * deltaTime;
                            trans.ValueRW.Position = newPos;
                        }
                    }
                }
            }

            playerEntities.Dispose();
            playerTransforms.Dispose();
            playerExperiences.Dispose();
        }
    }
}