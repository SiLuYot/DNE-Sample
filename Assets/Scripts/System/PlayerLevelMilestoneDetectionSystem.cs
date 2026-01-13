using Component.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace System
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerLevelMilestoneDetectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerExperienceComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (expComp, entity) in SystemAPI
                .Query<RefRO<PlayerExperienceComponent>>()
                .WithAll<GhostOwnerIsLocal>()
                .WithEntityAccess())
            {
                var currentLevel = expComp.ValueRO.Level;
                var lastUpgradedLevel = expComp.ValueRO.LastUpgradedLevel;

                if (SystemAPI.HasComponent<PendingAttackUpgradeComponent>(entity))
                    continue;

                bool isMilestone = currentLevel >= 5 && (currentLevel % 5 == 0);
                bool alreadyUpgraded = lastUpgradedLevel >= currentLevel;

                if (isMilestone && !alreadyUpgraded)
                {
                    ecb.AddComponent(entity, new PendingAttackUpgradeComponent
                    {
                        LastUpgradedLevel = currentLevel
                    });
                }
            }
        }
    }
}
