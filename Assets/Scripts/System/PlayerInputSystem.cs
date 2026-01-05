using Component.Player;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
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

            // 마우스 위치를 월드 좌표로 변환
            var mousePos = Input.mousePosition;
            var camera = Camera.main;
            var aimDirection = float3.zero;

            if (camera != null)
            {
                // 카메라에서 마우스 위치로 레이 발사
                var ray = camera.ScreenPointToRay(mousePos);

                // Y=0 평면과의 교차점 계산 (2D 평면 게임이므로)
                if (math.abs(ray.direction.y) > 0.001f)
                {
                    var distance = -ray.origin.y / ray.direction.y;
                    var worldMousePos = ray.origin + ray.direction * distance;

                    // 플레이어 위치 가져오기
                    foreach (var (playerInput, transform) in SystemAPI
                        .Query<RefRW<PlayerInputComponent>, RefRO<LocalTransform>>()
                        .WithAll<GhostOwnerIsLocal>())
                    {
                        var playerPos = transform.ValueRO.Position;
                        var direction = new float3(worldMousePos.x, 0, worldMousePos.z) - playerPos;

                        // 방향 정규화 (거리가 너무 가까우면 기본 방향 사용)
                        if (math.lengthsq(direction) > 0.01f)
                        {
                            aimDirection = math.normalize(direction);
                        }
                        else
                        {
                            aimDirection = new float3(0, 0, 1); // 기본 전방 방향
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