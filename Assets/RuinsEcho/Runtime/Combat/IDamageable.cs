namespace RuinsEcho.Combat
{
    /// <summary>玩家、影子、敌人、Boss 共同实现的伤害接收接口。</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(DamageInfo damage);
    }
}
