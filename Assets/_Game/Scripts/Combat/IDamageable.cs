// [KILL OR DEAD] Combat Core
namespace KillOrDead.Combat
{
    /// <summary>
    /// 데미지를 받을 수 있는 모든 것. Hitbox가 부모에서 이 인터페이스를 찾아 전달한다.
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void ApplyDamage(DamageInfo info);
    }
}
