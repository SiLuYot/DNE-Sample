using Unity.Entities;

namespace Component
{
    public struct SwordOwnerComponent : IComponentData
    {
        public Entity Owner;
    }
}
