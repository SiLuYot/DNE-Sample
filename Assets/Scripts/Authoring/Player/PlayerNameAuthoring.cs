using Component.UI;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerNameAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _namePrefab;

        class Baker : Baker<PlayerNameAuthoring>
        {
            public override void Bake(PlayerNameAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new UIConfigComponent
                {
                    NamePrefab = authoring._namePrefab
                });
            }
        }
    }
}