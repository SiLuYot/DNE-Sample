using Component.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace System.Player
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

            var mousePos = Input.mousePosition;
            var camera = Camera.main;
            var aimDirection = float3.zero;

            if (camera != null)
            {
                var ray = camera.ScreenPointToRay(mousePos);

                if (math.abs(ray.direction.y) > 0.001f)
                {
                    var distance = -ray.origin.y / ray.direction.y;
                    var worldMousePos = ray.origin + ray.direction * distance;

                    foreach (var (playerInput, transform) in SystemAPI
                        .Query<RefRW<PlayerInputComponent>, RefRO<LocalTransform>>()
                        .WithAll<GhostOwnerIsLocal>())
                    {
                        var playerPos = transform.ValueRO.Position;
                        var direction = new float3(worldMousePos.x, 0, worldMousePos.z) - playerPos;

                        if (math.lengthsq(direction) > 0.01f)
                        {
                            aimDirection = math.normalize(direction);
                        }
                        else
                        {
                            aimDirection = new float3(0, 0, 1);
                        }

                        playerInput.ValueRW.Horizontal = horizontal;
                        playerInput.ValueRW.Vertical = vertical;
                        playerInput.ValueRW.AimDirection = aimDirection;
                    }
                }
            }
        }
    }
}