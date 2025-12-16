using Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace System.Job
{
    [BurstCompile]
    public partial struct HomingMissileMovementJob : IJobEntity
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
            // 발사 단계 처리
            if (!missile.HasLaunched)
            {
                // 초기 속도로 퍼지면서 올라감
                transform.Position += missile.LaunchVelocity * DeltaTime;
                
                if (transform.Position.y >= missile.LaunchHeight)
                {
                    missile.HasLaunched = true;
                    
                    // 발사 완료 시 가장 가까운 타겟 찾기
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

            // 타겟이 유효한지 확인
            var targetPos = float3.zero;
            var hasValidTarget = false;

            if (missile.TargetEnemy != Entity.Null)
            {
                // 타겟이 아직 살아있는지 확인
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
                // 타겟 추적
                var currentPos = transform.Position;
                var directionToTarget = math.normalize(targetPos - currentPos);
                var currentForward = math.forward(transform.Rotation);

                // 부드러운 회전
                var newForward = math.normalize(
                    math.lerp(currentForward, directionToTarget, missile.TurnSpeed * DeltaTime)
                );

                transform.Rotation = quaternion.LookRotationSafe(newForward, math.up());
                transform.Position += newForward * missile.Speed * DeltaTime;
                missile.TraveledDistance += missile.Speed * DeltaTime;
            }
            else
            {
                // 타겟이 없으면 직진
                var forward = math.forward(transform.Rotation);
                transform.Position += forward * missile.Speed * DeltaTime;
                missile.TraveledDistance += missile.Speed * DeltaTime;
            }

            // 최대 거리 도달 시 제거
            if (missile.TraveledDistance >= missile.MaxDistance)
            {
                Ecb.DestroyEntity(sortKey, entity);
            }
        }
    }
}