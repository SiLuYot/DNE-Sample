using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Component.Player
{
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct PlayerComponent : IComponentData
    {
        [GhostField] public int NetworkId;
        [GhostField] public float3 Direction;
    }
}