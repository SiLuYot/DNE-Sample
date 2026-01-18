using Component.HomingMissile;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace System.HomingMissile
{
    [BurstCompile]
    public partial struct HomingMissileMovementServerJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public NativeArray<LocalTransform> EnemyTransforms;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
            Entity entity,
            [ChunkIndexInQuery] int sortKey,
            ref HomingMissileComponent missile,
            ref LocalTransform transform)
        {
            if (!missile.HasLaunched)
            {
                transform.Position += missile.LaunchVelocity * DeltaTime;

                if (transform.Position.y >= missile.LaunchHeight)
                {
                    missile.HasLaunched = true;

                    if (EnemyEntities.Length > 0)
                    {
                        var minDistSq = float.MaxValue;
                        var nearestEnemy = Entity.Null;
                        var missilePos = transform.Position;

                        for (var i = 0; i < EnemyEntities.Length; i++)
                        {
                            var enemyPos = EnemyTransforms[i].Position;
                            var distSq = math.distancesq(missilePos, enemyPos);

                            if (distSq < minDistSq)
                            {
                                minDistSq = distSq;
                                nearestEnemy = EnemyEntities[i];
                            }
                        }

                        missile.TargetEnemy = nearestEnemy;
                    }
                }

                return;
            }

            var targetPos = float3.zero;
            var hasValidTarget = false;

            if (missile.TargetEnemy != Entity.Null)
            {
                for (var i = 0; i < EnemyEntities.Length; i++)
                {
                    if (EnemyEntities[i] == missile.TargetEnemy)
                    {
                        targetPos = EnemyTransforms[i].Position;
                        hasValidTarget = true;
                        break;
                    }
                }
            }

            if (hasValidTarget)
            {
                var currentPos = transform.Position;
                var directionToTarget = math.normalize(targetPos - currentPos);
                var currentForward = math.forward(transform.Rotation);

                var newForward = math.normalize(
                    math.lerp(currentForward, directionToTarget, missile.TurnSpeed * DeltaTime)
                );

                transform.Rotation = quaternion.LookRotationSafe(newForward, math.up());
                transform.Position += newForward * missile.Speed * DeltaTime;
                missile.TraveledDistance += missile.Speed * DeltaTime;
            }
            else
            {
                var forward = math.forward(transform.Rotation);
                transform.Position += forward * missile.Speed * DeltaTime;
                missile.TraveledDistance += missile.Speed * DeltaTime;
            }

            if (missile.TraveledDistance >= missile.MaxDistance)
            {
                Ecb.DestroyEntity(sortKey, entity);
            }
        }
    }
}