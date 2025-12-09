using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace System
{
    namespace System
    {
        [UpdateInGroup(typeof(SimulationSystemGroup))]
        [BurstCompile]
        public partial struct PhysicsConstraintSystem : ISystem
        {
            private const float FixedYPosition = 0f;

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                foreach (var transform in SystemAPI
                             .Query<RefRW<LocalTransform>>()
                             .WithAll<PhysicsMass, Simulate>())
                {
                    var currentForward = math.forward(transform.ValueRO.Rotation);
                    var newForward = math.normalize(new float3(currentForward.x, 0, currentForward.z));

                    transform.ValueRW.Rotation = quaternion.LookRotation(newForward, math.up());
                    transform.ValueRW.Position.y = FixedYPosition;
                }
            }
        }
    }
}