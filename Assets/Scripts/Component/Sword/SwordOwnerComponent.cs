using Unity.Entities;

namespace Component.Sword
{
    public struct SwordOwnerComponent : IComponentData
    {
        public Entity Owner;
    }
}
