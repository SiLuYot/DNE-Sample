using Component.Player;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour
    {
        public float ExperienceCollectionRadius = 2f;

        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerComponent>(entity);
                AddComponent<PlayerNameComponent>(entity);
                AddComponent<PlayerProjectileAttackComponent>(entity);
                AddComponent<PlayerMissileAttackComponent>(entity);
                AddComponent(entity, new PlayerExperienceComponent
                {
                    CurrentExperience = 0,
                    Level = 1,  // 초기 레벨은 1
                    CollectionRadius = authoring.ExperienceCollectionRadius
                });
            }
        }
    }
}