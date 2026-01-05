using Component;
using Component.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace System.Job
{
    [BurstCompile]
    public partial struct HomingMissileCollisionJob : IJobEntity
    {
        public float CollisionRadius;
        [ReadOnly] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public NativeArray<LocalTransform> EnemyTransforms;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public Entity ExperienceOrbPrefab;

        private void Execute(
            Entity missileEntity,
            [ChunkIndexInQuery] int sortKey,
            in HomingMissileComponent missile,
            in LocalTransform missileTransform)
        {
            if (!missile.HasLaunched)
                return;

            var missilePos = missileTransform.Position;
            var collisionRadiusSq = CollisionRadius * CollisionRadius;

            for (int i = 0; i < EnemyEntities.Length; i++)
            {
                var enemyEntity = EnemyEntities[i];
                var enemyPos = EnemyTransforms[i].Position;

                var distanceSq = math.distancesq(missilePos, enemyPos);

                if (distanceSq <= collisionRadiusSq)
                {
                    // 경험치 오브 생성 (프리팹이 유효한 경우에만)
                    if (ExperienceOrbPrefab != Entity.Null)
                    {
                        var expOrb = Ecb.Instantiate(sortKey, ExperienceOrbPrefab);
                        Ecb.SetComponent(sortKey, expOrb, new LocalTransform
                        {
                            Position = enemyPos,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });
                    }

                    // Enemy를 즉시 삭제하지 않고 Dead 태그만 추가
                    // 이렇게 하면 같은 프레임에 다른 발사체가 이 적과 충돌하지 않음
                    Ecb.AddComponent<EnemyDeadTag>(sortKey, enemyEntity);

                    Ecb.DestroyEntity(sortKey, missileEntity);
                    return;
                }
            }
        }
    }
}