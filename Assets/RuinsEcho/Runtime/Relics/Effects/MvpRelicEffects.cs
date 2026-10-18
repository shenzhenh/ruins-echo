using System;

namespace RuinsEcho.Relics.Effects
{
    /// <summary>残影存续：影子回放结束后循环重播，直到生命耗尽。</summary>
    [Serializable]
    public sealed class PersistentEchoEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.LoopShadows = true;
        }
    }

    /// <summary>双生回响：一次召唤 2 个影子，能量消耗 ×2。</summary>
    [Serializable]
    public sealed class TwinEchoEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.ShadowsPerSummon += 1;
            manager.Stats.SummonCostMultiplier *= 2f;
        }
    }

    /// <summary>替影承伤：敌人优先锁定影子；本体受到的部分伤害转移给影子。</summary>
    [Serializable]
    public sealed class ShadowSacrificeEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.EnemiesPreferShadows = true;
            manager.Stats.DamageRedirectToShadows += 0.25f;
        }
    }

    /// <summary>回响传导：影子命中敌人时，玩家普攻冷却缩短 0.3 秒。</summary>
    [Serializable]
    public sealed class EchoConductionEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.AttackCooldownRefund += 0.3f;
        }
    }

    /// <summary>掠影打击：影子暴击时标记目标，本体对被标记目标伤害 +30%。</summary>
    [Serializable]
    public sealed class ShadowStrikeEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.ShadowCritMarks = true;
            manager.Stats.MarkedDamageMultiplier *= 1.3f;
        }
    }

    /// <summary>共生反噬：影子攻击会误伤本体；影子伤害 ×2。</summary>
    [Serializable]
    public sealed class SymbioticBacklashEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.ShadowFriendlyFire = true;
            manager.Stats.ShadowDamageMultiplier *= 2f;
        }
    }

    /// <summary>能量狂潮：回响能量恢复 ×1.8；本体受到伤害 ×1.3。</summary>
    [Serializable]
    public sealed class EnergySurgeEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.EnergyRegenMultiplier *= 1.8f;
            manager.Stats.PlayerDamageTakenMultiplier *= 1.3f;
        }
    }

    /// <summary>意志侵蚀：影子最大生命大幅提升；玩家最大生命 -40%。</summary>
    [Serializable]
    public sealed class WillErosionEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.ShadowMaxHpMultiplier *= 1.8f;
            manager.Stats.PlayerMaxHpMultiplier *= 0.6f;
        }
    }

    /// <summary>动作乱流：影子回放位置随机抖动；攻击范围 +50%。</summary>
    [Serializable]
    public sealed class ActionTurbulenceEffect : RelicEffectBase
    {
        public override void OnApply(RelicManager manager)
        {
            manager.Stats.ShadowPositionJitter += 0.15f;
            manager.Stats.ShadowAttackRadiusMultiplier *= 1.5f;
        }
    }
}
