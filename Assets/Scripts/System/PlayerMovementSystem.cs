using Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace System
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var speed = SystemAPI.Time.DeltaTime * 6;
            foreach (var (input, trans) in SystemAPI.Query<RefRO<PlayerInputComponent>, RefRW<LocalTransform>>().WithAll<Simulate>())
            {
                var moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);
                moveInput = math.normalizesafe(moveInput) * speed;
                trans.ValueRW.Position += new float3(moveInput.x, 0, moveInput.y);
            }
        }
    }
}