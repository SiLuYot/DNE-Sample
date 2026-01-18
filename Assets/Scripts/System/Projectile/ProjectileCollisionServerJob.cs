using Component.Enemy;
using Component.Projectile;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace System.Projectile
{
    [BurstCompile]
    public partial struct ProjectileCollisionServerJob : IJobEntity
    {
        public float CollisionRadius;
        [ReadOnly] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public NativeArray<LocalTransform> EnemyTransforms;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public Entity ExperienceOrbPrefab;

        private void Execute(
            Entity projectileEntity,
            [ChunkIndexInQuery] int sortKey,
            in ProjectileComponent projectile,
            in LocalTransform projectileTransform)
        {
            var projectilePos = projectileTransform.Position;

            for (int i = 0; i < EnemyEntities.Length; i++)
            {
                var enemyEntity = EnemyEntities[i];
                var enemyPos = EnemyTransforms[i].Position;

                var distanceSq = math.distancesq(projectilePos, enemyPos);
                var collisionRadiusSq = CollisionRadius * CollisionRadius;

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

                    Ecb.DestroyEntity(sortKey, projectileEntity);
                    return;
                }
            }
        }
    }
}