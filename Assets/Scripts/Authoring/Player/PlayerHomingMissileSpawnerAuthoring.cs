using Component.HomingMissile;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Player
{
    [DisallowMultipleComponent]
    public class PlayerHomingMissileSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _missilePrefab;
        [SerializeField] private float _attackCooldown = 3f;
        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _maxDistance = 100f;
        [SerializeField] private float _turnSpeed = 20f;
        [SerializeField] private float _launchHeight = 6f;

        class Baker : Baker<PlayerHomingMissileSpawnerAuthoring>
        {
            public override void Bake(PlayerHomingMissileSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new HomingMissileSpawnerComponent
                {
                    Prefab = GetEntity(authoring._missilePrefab, TransformUsageFlags.Dynamic),
                    AttackCooldown = authoring._attackCooldown,
                    Speed = authoring._speed,
                    MaxDistance = authoring._maxDistance,
                    TurnSpeed = authoring._turnSpeed,
                    LaunchHeight = authoring._launchHeight,
                });
            }
        }
    }
}