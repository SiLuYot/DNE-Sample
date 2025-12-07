using Component;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    [DisallowMultipleComponent]
    public class PlayerInputAuthoring : MonoBehaviour
    {
        class PlayerInputBaking : Baker<PlayerInputAuthoring>
        {
            public override void Bake(PlayerInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerInputComponent>(entity);
            }
        }
    }
}