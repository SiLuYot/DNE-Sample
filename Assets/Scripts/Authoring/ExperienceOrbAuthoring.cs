using Component;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class ExperienceOrbAuthoring : MonoBehaviour
    {
        public int ExperienceValue = 10;
        public float MoveSpeed = 8f;

        class Baker : Baker<ExperienceOrbAuthoring>
        {
            public override void Bake(ExperienceOrbAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExperienceOrbComponent
                {
                    ExperienceValue = authoring.ExperienceValue,
                    MoveSpeed = authoring.MoveSpeed
                });
            }
        }
    }
}
