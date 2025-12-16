using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace System.Job
{
    [BurstCompile]
    public partial struct EnemyChaseJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> PlayerTransforms;
        public float Speed;

        private void Execute(ref LocalTransform transform, ref PhysicsVelocity velocity, in EnemyComponent enemy)
        {
            var min = float.MaxValue;
            var nearestPos = float3.zero;

            for (var i = 0; i < PlayerTransforms.Length; i++)
            {
                var playerPos = PlayerTransforms[i].Position;
                var distanceSq = math.distancesq(transform.Position, playerPos);

                if (distanceSq < min)
                {
                    min = distanceSq;
                    nearestPos = playerPos;
                }
            }

            if (math.distancesq(transform.Position, nearestPos) < 0.5f)
                return;

            var dir = math.normalizesafe(nearestPos - transform.Position);
            transform.Rotation = quaternion.LookRotationSafe(dir, math.up());

            var value = dir * Speed;
            velocity.Linear = new float3(value.x, 0, value.z);
            velocity.Angular = float3.zero;
        }
    }
}