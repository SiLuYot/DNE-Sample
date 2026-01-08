using Unity.Entities;
using Unity.NetCode;

namespace Component.Player
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerExperienceComponent : IComponentData
    {
        [GhostField] public int CurrentExperience;
        [GhostField] public int Level;
        [GhostField] public int LastUpgradedLevel;
        public float CollectionRadius;
    }
}
