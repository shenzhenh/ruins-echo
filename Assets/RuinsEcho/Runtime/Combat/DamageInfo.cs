using UnityEngine;

namespace RuinsEcho.Combat
{
    public enum DamageSource
    {
        Player = 0,
        Shadow = 1,
        Enemy = 2,
        Environment = 3,
        Redirect = 4
    }

    /// <summary>一次伤害事件的完整上下文，供遗物 / UI / 音效监听。</summary>
    public struct DamageInfo
    {
        public float Amount;
        public GameObject Source;
        public bool IsCritical;
        public DamageSource SourceType;
        public Vector2 HitPoint;

        public DamageInfo(
            float amount,
            GameObject source,
            bool isCritical,
            DamageSource sourceType,
            Vector2 hitPoint)
        {
            Amount = amount;
            Source = source;
            IsCritical = isCritical;
            SourceType = sourceType;
            HitPoint = hitPoint;
        }
    }
}
