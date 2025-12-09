using Component.Player;
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
            var isHorizontal = Input.GetKey(KeyCode.A) ||
                               Input.GetKey(KeyCode.D) ||
                               Input.GetKey(KeyCode.LeftArrow) ||
                               Input.GetKey(KeyCode.RightArrow);

            var isVertical = Input.GetKey(KeyCode.W) ||
                             Input.GetKey(KeyCode.S) ||
                             Input.GetKey(KeyCode.UpArrow) ||
                             Input.GetKey(KeyCode.DownArrow);

            foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInputComponent>>().WithAll<GhostOwnerIsLocal>())
            {
                playerInput.ValueRW = default;

                if (isHorizontal)
                    playerInput.ValueRW.Horizontal = Input.GetAxis("Horizontal");

                if (isVertical)
                    playerInput.ValueRW.Vertical = Input.GetAxis("Vertical");
            }
        }
    }
}