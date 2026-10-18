using System;
using RuinsEcho.Combat;
using RuinsEcho.Core;
using RuinsEcho.Recording;
using RuinsEcho.Relics;
using UnityEngine;

namespace RuinsEcho.Player
{
    /// <summary>
    /// 玩家局内生命与属性。规格 3.2 / 3.3。
    /// 只持有局内数据；死亡后由外层流程销毁，不写入局外存档。
    /// </summary>
    public sealed class PlayerStatus : MonoBehaviour, IDamageable
    {
        public float BaseMaxHP = GameConfig.BaseMaxHP;
        public float MaxHP { get; private set; }
        public float HP { get; private set; }

        public bool IsAlive => HP > 0f;

        public RelicManager Relics { get; private set; }
        public PlayerController Controller { get; set; }

        public event Action<float, float> HealthChanged;
        public event Action<DamageInfo> Damaged;
        public event Action Died;

        private bool _initialized;
        private bool _deathRaised;

        public void Initialize(RelicManager relics)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Relics = relics;

            if (Relics != null)
            {
                Relics.RelicsChanged += ApplyRelicStats;
            }

            MaxHP = Mathf.Max(1f, BaseMaxHP);
            HP = MaxHP;
            _deathRaised = false;
            HealthChanged?.Invoke(HP, MaxHP);
        }

        private void OnDestroy()
        {
            if (Relics != null)
            {
                Relics.RelicsChanged -= ApplyRelicStats;
            }
        }

        /// <summary>遗物改变最大生命时调用（意志侵蚀等）。</summary>
        public void ApplyRelicStats()
        {
            float multiplier = Relics != null ? Relics.Stats.PlayerMaxHpMultiplier : 1f;
            float newMax = Mathf.Max(1f, BaseMaxHP * multiplier);

            if (Mathf.Approximately(newMax, MaxHP))
            {
                return;
            }

            MaxHP = newMax;
            HP = Mathf.Clamp(HP, 0f, MaxHP);
            HealthChanged?.Invoke(HP, MaxHP);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0f)
            {
                return;
            }

            if (Controller != null && Controller.IsInvulnerable)
            {
                return;
            }

            float multiplier = Relics != null ? Relics.Stats.PlayerDamageTakenMultiplier : 1f;
            float remaining = damage.Amount * Mathf.Max(0f, multiplier);

            float redirect = Relics != null ? Relics.Stats.DamageRedirectToShadows : 0f;
            if (redirect > 0f && remaining > 0f)
            {
                ShadowEcho shadow = ShadowEcho.GetNearest(transform.position);
                if (shadow != null && shadow.IsAlive)
                {
                    float transferred = remaining * Mathf.Clamp01(redirect);
                    shadow.TakeDamage(new DamageInfo(
                        transferred,
                        damage.Source,
                        damage.IsCritical,
                        DamageSource.Redirect,
                        shadow.transform.position));
                    remaining -= transferred;
                }
            }

            if (remaining <= 0f)
            {
                return;
            }

            HP = Mathf.Max(0f, HP - remaining);
            Damaged?.Invoke(damage);
            HealthChanged?.Invoke(HP, MaxHP);

            if (HP <= 0f && !_deathRaised)
            {
                _deathRaised = true;
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            HP = Mathf.Min(MaxHP, HP + amount);
            HealthChanged?.Invoke(HP, MaxHP);
        }

        public void HealPercent(float normalizedAmount)
        {
            Heal(MaxHP * Mathf.Clamp01(normalizedAmount));
        }

        /// <summary>新一局 / 重开时重置局内生命。</summary>
        public void ResetForNewRun()
        {
            float multiplier = Relics != null ? Relics.Stats.PlayerMaxHpMultiplier : 1f;
            MaxHP = Mathf.Max(1f, BaseMaxHP * multiplier);
            HP = MaxHP;
            _deathRaised = false;
            HealthChanged?.Invoke(HP, MaxHP);
        }

        public void KillImmediately()
        {
            if (!IsAlive)
            {
                return;
            }

            HP = 0f;
            HealthChanged?.Invoke(HP, MaxHP);
            if (!_deathRaised)
            {
                _deathRaised = true;
                Died?.Invoke();
            }
        }
    }
}
