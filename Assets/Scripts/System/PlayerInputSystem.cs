using Component;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace System
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerSpawnerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInputComponent>>().WithAll<GhostOwnerIsLocal>())
            {
                playerInput.ValueRW = default;
                playerInput.ValueRW.Horizontal = Input.GetAxis("Horizontal");
                playerInput.ValueRW.Vertical = Input.GetAxis("Vertical");
            }
        }
    }
}