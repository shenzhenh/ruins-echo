using RuinsEcho.Core;
using UnityEngine;

namespace RuinsEcho.Player
{
    /// <summary>
    /// 回响能量（规格 5）。纯 C# 数据对象，由 PlayerController 每帧 Tick。
    /// 能量恢复、召唤消耗的遗物修正都由外部乘数注入。
    /// </summary>
    public sealed class EchoEnergy
    {
        public float MaxEnergy;
        public float BaseRegenPerSecond;
        public float RegenMultiplier = 1f;
        public float CostMultiplier = 1f;

        public float CurrentEnergy { get; private set; }

        public float SummonCost => GameConfig.BaseSummonCost * Mathf.Max(0f, CostMultiplier);

        public float Normalized => MaxEnergy <= 0f ? 0f : Mathf.Clamp01(CurrentEnergy / MaxEnergy);

        public EchoEnergy(
            float maxEnergy = GameConfig.BaseMaxEnergy,
            float currentEnergy = GameConfig.BaseMaxEnergy,
            float baseRegenPerSecond = GameConfig.BaseEnergyRegenPerSecond)
        {
            MaxEnergy = Mathf.Max(1f, maxEnergy);
            BaseRegenPerSecond = Mathf.Max(0f, baseRegenPerSecond);
            CurrentEnergy = Mathf.Clamp(currentEnergy, 0f, MaxEnergy);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float rate = BaseRegenPerSecond * Mathf.Max(0f, RegenMultiplier);
            Add(rate * deltaTime);
        }

        public bool TryConsume(float amount)
        {
            amount = Mathf.Max(0f, amount);

            if (CurrentEnergy + 1e-4f < amount)
            {
                return false;
            }

            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
            return true;
        }

        public void Add(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
        }

        public void Drain(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
        }

        public void Refill()
        {
            CurrentEnergy = MaxEnergy;
        }

        public void ResetForNewRun()
        {
            CurrentEnergy = MaxEnergy;
        }
    }
}
