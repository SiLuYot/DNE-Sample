using Component.Enemy;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Enemy
{
    [DisallowMultipleComponent]
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _enemyPrefab;

        class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var comp = default(EnemySpawnerComponent);
                comp.Enemy = GetEntity(authoring._enemyPrefab, TransformUsageFlags.Dynamic);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, comp);
            }
        }
    }
}