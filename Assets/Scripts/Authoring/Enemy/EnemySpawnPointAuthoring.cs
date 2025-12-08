using Component.Enemy;
using Unity.Entities;
using UnityEngine;

namespace Authoring.Enemy
{
    public class EnemySpawnPointAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnemySpawnPointAuthoring>
        {
            public override void Bake(EnemySpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemySpawnPointComponent>(entity);
            }
        }
    }
}