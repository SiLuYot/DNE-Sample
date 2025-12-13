using Component;
using Component.Player;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerProjectileSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _maxDistance = 100f;

        class Baker : Baker<PlayerProjectileSpawnerAuthoring>
        {
            public override void Bake(PlayerProjectileSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ProjectileSpawnerComponent
                {
                    Prefab = GetEntity(authoring._prefab, TransformUsageFlags.Dynamic),
                    AttackCooldown = authoring._attackCooldown,
                    Speed = authoring._speed,
                    MaxDistance = authoring._maxDistance,
                });
            }
        }
    }
}