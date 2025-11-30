using Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
    public partial struct LocalPlayerClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<PlayerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(isReadOnly: true);
            var localConnectionQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalConnectionTag, NetworkId>()
                .Build();

            var localConnectionEntity = localConnectionQuery.GetSingletonEntity();
            var localNetworkId = networkIdLookup[localConnectionEntity].Value;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (ghostOwner, entity) in SystemAPI.Query<RefRO<GhostOwner>>()
                         .WithAll<PlayerComponent>()
                         .WithNone<LocalPlayerTag>()
                         .WithEntityAccess())
            {
                if (ghostOwner.ValueRO.NetworkId != localNetworkId)
                    continue;

                ecb.AddComponent<LocalPlayerTag>(entity);
                break;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}