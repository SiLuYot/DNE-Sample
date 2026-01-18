using UI;
using Unity.Entities;

namespace Component.UI
{
    public class UIDeathCleanupComponent : ICleanupComponentData
    {
        public DeathView View;
    }
}
