using Unity.Entities;

namespace Component.Enemy
{
    /// <summary>
    /// 사망 판정이 된 Enemy를 마킹하는 태그 컴포넌트
    /// 이 태그가 있는 Enemy는 더 이상 충돌 판정에서 제외됨
    /// </summary>
    public struct EnemyDeadTag : IComponentData
    {
    }
}
