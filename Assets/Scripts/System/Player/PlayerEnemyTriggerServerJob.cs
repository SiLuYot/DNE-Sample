using Component.Enemy;
using Component.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace System.Player
{
    [BurstCompile]
    public struct PlayerEnemyTriggerServerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<PlayerComponent> PlayerLookup;
        [ReadOnly] public ComponentLookup<EnemyComponent> EnemyLookup;
        [ReadOnly] public ComponentLookup<EnemyDeadTag> EnemyDeadLookup;
        [ReadOnly] public ComponentLookup<PlayerDeadTag> PlayerDeadLookup;
        public EntityCommandBuffer Ecb;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            Entity playerEntity = Entity.Null;
            Entity enemyEntity = Entity.Null;

            if (PlayerLookup.HasComponent(entityA) && EnemyLookup.HasComponent(entityB))
            {
                playerEntity = entityA;
                enemyEntity = entityB;
            }
            else if (PlayerLookup.HasComponent(entityB) && EnemyLookup.HasComponent(entityA))
            {
                playerEntity = entityB;
                enemyEntity = entityA;
            }

            if (playerEntity == Entity.Null || enemyEntity == Entity.Null)
                return;

            if (EnemyDeadLookup.HasComponent(enemyEntity))
                return;

            if (PlayerDeadLookup.HasComponent(playerEntity))
                return;

            Ecb.AddComponent<PlayerDeadTag>(playerEntity);
        }
    }
}
