using Unity.Entities;
using Unity.NetCode;

namespace Component
{
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct ExperienceOrbComponent : IComponentData
    {
        [GhostField] public int ExperienceValue;  // 경험치 값
        [GhostField] public float MoveSpeed;      // 플레이어를 향해 이동하는 속도
    }
}
