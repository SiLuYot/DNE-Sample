using Component.Enemy;
using Component.Sword;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace System.Sword
{
    [BurstCompile]
    public struct SwordTriggerServerJob : ITriggerEventsJob
    {
        [NativeDisableParallelForRestriction] public ComponentLookup<SwordComponent> SwordLookup;
        [ReadOnly] public ComponentLookup<SwordOwnerComponent> SwordOwnerLookup;
        [ReadOnly] public ComponentLookup<EnemyComponent> EnemyLookup;
        [ReadOnly] public ComponentLookup<EnemyDeadTag> EnemyDeadLookup;
        [ReadOnly] public ComponentLookup<EnemyKnockbackComponent> EnemyKnockbackLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer Ecb;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            Entity swordEntity = Entity.Null;
            Entity enemyEntity = Entity.Null;

            if (SwordLookup.HasComponent(entityA) && EnemyLookup.HasComponent(entityB))
            {
                swordEntity = entityA;
                enemyEntity = entityB;
            }
            else if (SwordLookup.HasComponent(entityB) && EnemyLookup.HasComponent(entityA))
            {
                swordEntity = entityB;
                enemyEntity = entityA;
            }

            if (swordEntity == Entity.Null || enemyEntity == Entity.Null)
                return;

            if (EnemyDeadLookup.HasComponent(enemyEntity))
                return;

            if (!EnemyKnockbackLookup.HasComponent(enemyEntity) &&
                SwordOwnerLookup.HasComponent(swordEntity) &&
                TransformLookup.HasComponent(enemyEntity))
            {
                var swordOwner = SwordOwnerLookup[swordEntity];
                if (TransformLookup.HasComponent(swordOwner.Owner))
                {
                    var playerPos = TransformLookup[swordOwner.Owner].Position;
                    var enemyPos = TransformLookup[enemyEntity].Position;
                    var knockbackDir = math.normalizesafe(enemyPos - playerPos);

                    knockbackDir.y = 0;
                    knockbackDir = math.normalizesafe(knockbackDir);

                    Ecb.AddComponent(enemyEntity, new EnemyKnockbackComponent
                    {
                        Direction = knockbackDir,
                        Speed = 10f,
                        RemainingTime = 0.2f
                    });
                }
            }

            var swordComp = SwordLookup[swordEntity];
            if (swordComp.Durability <= 0)
                return;

            swordComp.Durability -= 1;
            SwordLookup[swordEntity] = swordComp;

            Ecb.AddComponent<EnemyDeadTag>(enemyEntity);
        }
    }
}
