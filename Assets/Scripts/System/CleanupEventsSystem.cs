using Component;
using Unity.Entities;

namespace System
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct CleanupEventsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var (evt, entity) in SystemAPI.Query<RefRO<PlayerConnectedEvent>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
            
            foreach (var (evt, entity) in SystemAPI.Query<RefRO<PlayerDisconnectedEvent>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}