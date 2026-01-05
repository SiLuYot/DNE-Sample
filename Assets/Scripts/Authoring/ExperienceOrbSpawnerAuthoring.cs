using Component;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class ExperienceOrbSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        class Baker : Baker<ExperienceOrbSpawnerAuthoring>
        {
            public override void Bake(ExperienceOrbSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ExperienceOrbSpawnerComponent
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
