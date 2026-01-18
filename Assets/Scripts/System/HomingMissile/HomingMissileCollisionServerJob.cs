using Component.Enemy;
using Component.HomingMissile;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace System.HomingMissile
{
    [BurstCompile]
    public partial struct HomingMissileCollisionServerJob : IJobEntity
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

                    Ecb.AddComponent<EnemyDeadTag>(sortKey, enemyEntity);

                    Ecb.DestroyEntity(sortKey, missileEntity);
                    return;
                }
            }
        }
    }
}