using Component.Projectile;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace System.Projectile
{
    [BurstCompile]
    public partial struct ProjectileMovementServerJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
            Entity entity,
            [ChunkIndexInQuery] int sortKey,
            ref ProjectileComponent projectile,
            ref LocalTransform transform)
        {
            transform.Position += projectile.Direction * projectile.Speed * DeltaTime;
            projectile.TraveledDistance += projectile.Speed * DeltaTime;

            if (projectile.TraveledDistance >= projectile.MaxDistance)
            {
                Ecb.DestroyEntity(sortKey, entity);
            }
        }
    }
}