using Unity.Entities;
using Unity.NetCode;

namespace Component.Experience
{
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct ExperienceOrbComponent : IComponentData
    {
        [GhostField] public int ExperienceValue;
        [GhostField] public float MoveSpeed;
    }
}
