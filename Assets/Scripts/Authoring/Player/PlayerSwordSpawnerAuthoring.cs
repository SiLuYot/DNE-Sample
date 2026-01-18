using Component.Sword;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerSwordSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _swordPrefab;
        [SerializeField] private float _attackCooldown = 3f;
        [SerializeField] private float _duration = 0.25f;

        class Baker : Baker<PlayerSwordSpawnerAuthoring>
        {
            public override void Bake(PlayerSwordSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new SwordSpawnerComponent
                {
                    Prefab = GetEntity(authoring._swordPrefab, TransformUsageFlags.Dynamic),
                    AttackCooldown = authoring._attackCooldown,
                    Duration = authoring._duration,
                });
            }
        }
    }
}