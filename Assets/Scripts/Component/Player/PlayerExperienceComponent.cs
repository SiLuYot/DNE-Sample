using Unity.Entities;
using Unity.NetCode;

namespace Component.Player
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerExperienceComponent : IComponentData
    {
        [GhostField] public int CurrentExperience;  // 현재 누적된 경험치
        [GhostField] public int Level;              // 현재 레벨 (경험치/100 + 1)
        public float CollectionRadius;  // 경험치를 획득하는 반경 (서버 전용, 복제 불필요)
    }
}
