using UI;
using Unity.Entities;

namespace Component.UI
{
    /// <summary>
    /// Death UI의 수명을 연결 엔티티에 연결
    /// 연결이 끊기면 자동 정리
    /// </summary>
    public class UIDeathCleanupComponent : ICleanupComponentData
    {
        public DeathView View;
    }
}
