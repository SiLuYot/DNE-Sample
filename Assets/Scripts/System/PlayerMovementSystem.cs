using Component.Player;
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
            var speed = 6f;
            float min = -15, max = 15f;

            foreach (var (input, trans, player) in SystemAPI
                         .Query<RefRO<PlayerInputComponent>, RefRW<LocalTransform>, RefRW<PlayerComponent>>()
                         .WithAll<Simulate>())
            {
                var inputValue = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);

                if (math.lengthsq(inputValue) < 0.01f)
                    continue;

                var pos = trans.ValueRO.Position;
                var dir = math.normalizesafe(inputValue);
                var value = dir * speed * SystemAPI.Time.DeltaTime;

                var x = math.clamp(pos.x + value.x, min, max);
                var z = math.clamp(pos.z + value.y, min, max);
                
                player.ValueRW.Direction = new float3(dir.x, 0, dir.y);
                trans.ValueRW.Position = new float3(x, 0, z);
            }
        }
    }
}