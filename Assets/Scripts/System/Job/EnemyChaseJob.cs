using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace System.Job
{
    [BurstCompile]
    public partial struct EnemyChaseJob : IJobEntity
    {
        [ReadOnly] 
        public NativeArray<LocalTransform> PlayerTransforms;
        public float DeltaTime;
        public float MoveSpeed;

        private void Execute(ref LocalTransform transform, in EnemyComponent enemy)
        {
            var enemyPosition = transform.Position;

            float minDistanceSq = float.MaxValue;
            float3 nearestPlayerPosition = float3.zero;

            for (int i = 0; i < PlayerTransforms.Length; i++)
            {
                var playerPos = PlayerTransforms[i].Position;
                var distanceSq = math.distancesq(enemyPosition, playerPos);

                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    nearestPlayerPosition = playerPos;
                }
            }

            var direction = math.normalize(nearestPlayerPosition - enemyPosition);
            transform.Position += direction * MoveSpeed * DeltaTime;
            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
        }
    }
}