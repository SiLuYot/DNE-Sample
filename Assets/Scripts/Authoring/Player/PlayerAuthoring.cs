using Component.Player;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private float _experienceCollectionRadius = 2f;

        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerComponent>(entity);
                AddComponent<PlayerNameComponent>(entity);
                AddComponent(entity, new PlayerProjectileAttackComponent());
                AddComponent(entity, new PlayerMissileAttackComponent());
                AddComponent(entity, new PlayerExperienceComponent
                {
                    CurrentExperience = 0,
                    Level = 1,
                    LastUpgradedLevel = 0,
                    CollectionRadius = authoring._experienceCollectionRadius
                });
            }
        }
    }
}