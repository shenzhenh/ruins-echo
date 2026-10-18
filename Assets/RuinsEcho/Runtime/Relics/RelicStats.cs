using System;
using UnityEngine;

namespace RuinsEcho.Relics
{
    /// <summary>
    /// 一件遗物对全系统的聚合修正。所有系统只读这个快照，
    /// 不直接遍历遗物列表，避免 if-else 扩散。
    /// </summary>
    [Serializable]
    public sealed class RelicStats
    {
        public int ShadowsPerSummon;
        public bool LoopShadows;
        public bool EnemiesPreferShadows;
        public float DamageRedirectToShadows;

        public float ShadowDamageMultiplier;
        public float ShadowMaxHpMultiplier;
        public float ShadowDamageTakenMultiplier;
        public float ShadowAttackRadiusMultiplier;
        public float ShadowPositionJitter;
        public bool ShadowFriendlyFire;
        public bool ShadowCritMarks;
        public float MarkedDamageMultiplier;
        public float AttackCooldownRefund;

        public float PlayerDamageTakenMultiplier;
        public float PlayerMaxHpMultiplier;
        public float EnergyRegenMultiplier;
        public float SummonCostMultiplier;
        public float CritChance;

        public RelicStats()
        {
            Reset();
        }

        public void Reset()
        {
            ShadowsPerSummon = 1;
            LoopShadows = false;
            EnemiesPreferShadows = false;
            DamageRedirectToShadows = 0f;

            ShadowDamageMultiplier = 1f;
            ShadowMaxHpMultiplier = 1f;
            ShadowDamageTakenMultiplier = 1f;
            ShadowAttackRadiusMultiplier = 1f;
            ShadowPositionJitter = 0f;
            ShadowFriendlyFire = false;
            ShadowCritMarks = false;
            MarkedDamageMultiplier = 1f;
            AttackCooldownRefund = 0f;

            PlayerDamageTakenMultiplier = 1f;
            PlayerMaxHpMultiplier = 1f;
            EnergyRegenMultiplier = 1f;
            SummonCostMultiplier = 1f;
            CritChance = Core.GameConfig.BaseCritChance;
        }

        public void Clamp()
        {
            ShadowsPerSummon = Mathf.Max(1, ShadowsPerSummon);
            DamageRedirectToShadows = Mathf.Clamp01(DamageRedirectToShadows);

            ShadowDamageMultiplier = Mathf.Max(0f, ShadowDamageMultiplier);
            ShadowMaxHpMultiplier = Mathf.Max(0.01f, ShadowMaxHpMultiplier);
            ShadowDamageTakenMultiplier = Mathf.Max(0.01f, ShadowDamageTakenMultiplier);
            ShadowAttackRadiusMultiplier = Mathf.Max(0.01f, ShadowAttackRadiusMultiplier);
            ShadowPositionJitter = Mathf.Clamp(ShadowPositionJitter, 0f, 2f);
            MarkedDamageMultiplier = Mathf.Max(1f, MarkedDamageMultiplier);
            AttackCooldownRefund = Mathf.Max(0f, AttackCooldownRefund);

            PlayerDamageTakenMultiplier = Mathf.Max(0f, PlayerDamageTakenMultiplier);
            PlayerMaxHpMultiplier = Mathf.Max(0.01f, PlayerMaxHpMultiplier);
            EnergyRegenMultiplier = Mathf.Max(0f, EnergyRegenMultiplier);
            SummonCostMultiplier = Mathf.Max(0f, SummonCostMultiplier);
            CritChance = Mathf.Clamp01(CritChance);
        }
    }
}
