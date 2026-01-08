using Component.UI;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerUIAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _namePrefab;
        [SerializeField] private GameObject _upgradeSelectPrefab;

        class Baker : Baker<PlayerUIAuthoring>
        {
            public override void Bake(PlayerUIAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new UIConfigComponent
                {
                    NamePrefab = authoring._namePrefab,
                    UpgradeViewPrefab = authoring._upgradeSelectPrefab
                });
            }
        }
    }
}