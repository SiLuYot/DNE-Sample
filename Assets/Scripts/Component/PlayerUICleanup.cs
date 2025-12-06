using UI;
using Unity.Entities;

namespace Component
{
    public class PlayerUICleanup : ICleanupComponentData
    {
        public PlayerNameView View;
    }
}