using Component;
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
                    Ecb.DestroyEntity(sortKey, missileEntity);
                    Ecb.DestroyEntity(sortKey, enemyEntity);
                    return;
                }
            }
        }
    }
}