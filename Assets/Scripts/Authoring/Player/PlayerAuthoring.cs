using Component.Player;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour
    {
        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerComponent>(entity);
                AddComponent<PlayerNameComponent>(entity);
                AddComponent<PlayerProjectileAttackComponent>(entity);
                AddComponent<PlayerMissileAttackComponent>(entity);
            }
        }
    }
}