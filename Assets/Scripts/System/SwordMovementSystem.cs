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
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct SwordMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<SwordComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (sword, owner, transform, entity) in SystemAPI
                         .Query<RefRW<SwordComponent>, RefRO<SwordOwnerComponent>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                sword.ValueRW.ElapsedTime += deltaTime;

                if (sword.ValueRO.ElapsedTime >= sword.ValueRO.Duration)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // 플레이어 위치 따라가기
                var ownerPosition = sword.ValueRO.OwnerPosition;
                if (SystemAPI.HasComponent<LocalTransform>(owner.ValueRO.Owner))
                {
                    ownerPosition = SystemAPI.GetComponent<LocalTransform>(owner.ValueRO.Owner).Position;
                    sword.ValueRW.OwnerPosition = ownerPosition;
                }

                // Y축 기준 회전 각도 업데이트
                sword.ValueRW.CurrentAngle += sword.ValueRO.RotationSpeed * deltaTime;

                // 위치는 플레이어 위치에 고정, Y축 기준으로 회전만 적용
                transform.ValueRW.Position = ownerPosition;
                transform.ValueRW.Rotation = quaternion.AxisAngle(math.up(), sword.ValueRO.CurrentAngle);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
