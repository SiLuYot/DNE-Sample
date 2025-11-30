using Component;
using Unity.Entities;
using Unity.Services.Authentication;
using UnityEngine;

namespace Authoring
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

                var nameComp = default(PlayerNameComponent);
                nameComp.Name = AuthenticationService.Instance.PlayerName;
                AddComponent(entity, nameComp);
            }
        }
    }
}