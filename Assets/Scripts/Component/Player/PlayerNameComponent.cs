using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Component.Player
{
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct PlayerNameComponent : IComponentData
    {
        [GhostField] public FixedString128Bytes PlayerName;
    }
}