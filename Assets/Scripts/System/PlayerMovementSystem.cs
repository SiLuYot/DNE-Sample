using Component.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

namespace System
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var speed = 6f;

            foreach (var (input, player, velocity) in SystemAPI
                         .Query<RefRO<PlayerInputComponent>, RefRW<PlayerComponent>, RefRW<PhysicsVelocity>>()
                         .WithAll<Simulate, PlayerComponent>())
            {
                var inputValue = new float3(input.ValueRO.Horizontal, 0, input.ValueRO.Vertical);

                if (math.lengthsq(inputValue) < math.EPSILON)
                {
                    velocity.ValueRW.Linear = float3.zero;
                    velocity.ValueRW.Angular = float3.zero;
                    continue;
                }

                var dir = math.normalizesafe(inputValue);
                var value = dir * speed;

                velocity.ValueRW.Linear = new float3(value.x, 0, value.z);
                velocity.ValueRW.Angular = float3.zero;

                player.ValueRW.Direction = dir;
            }
        }
    }
}