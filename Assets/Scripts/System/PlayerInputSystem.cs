using Component.Player;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics; // float2 사용을 위해 추가
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
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");

            foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInputComponent>>().WithAll<GhostOwnerIsLocal>())
            {
                playerInput.ValueRW.Horizontal = horizontal;
                playerInput.ValueRW.Vertical = vertical;
            }
        }
    }
}