namespace RuinsEcho.Core
{
    /// <summary>
    /// 全局设计常量。数值来自 MVP 规格文档 3.2 / 4.1 / 5 / 11。
    /// 只放跨系统共享的常量；单个实体的可调参数放在各自组件上。
    /// </summary>
    public static class GameConfig
    {
        // 局外
        public const int TotalLayers = 5;
        public const int MaxRelics = 8;

        // 录制 / 回放
        public const float RecordWindowSeconds = 3f;
        public const float PlayerRecordWindowBonusHunter = 0.5f;

        // 玩家
        public const float BaseMaxHP = 100f;
        public const float BaseMoveSpeed = 4.5f;
        public const float BaseAttackDamage = 12f;
        public const float AttackWindup = 0.15f;
        public const float AttackRecovery = 0.2f;
        public const float AttackCooldown = AttackWindup + AttackRecovery;
        public const float AttackRadius = 1.0f;
        public const float AttackReach = 1.05f;

        public const float DodgeDistance = 2.2f;
        public const float DodgeDuration = 0.2f;
        public const float DodgeInvincibleTime = 0.35f;
        public const float DodgeCooldown = 1.8f;

        // 回响能量
        public const float BaseMaxEnergy = 100f;
        public const float BaseEnergyRegenPerSecond = 8f;
        public const float BaseSummonCost = 20f;
        public const float ShadowHunterEnergyPenalty = 15f;

        // 影子
        public const float BaseShadowHP = 40f;
        public const float ShadowAttackRadius = 1.0f;
        public const float ShadowAttackReach = 1.05f;
        public const float ShadowDodgeInvincibleTime = 0.3f;
        public const int ShadowPoolSize = 10;

        // 暴击 / 标记（规格未给数值，MVP 补充的最小规则）
        public const float BaseCritChance = 0.15f;
        public const float CritMultiplier = 1.5f;
        public const float MarkDurationSeconds = 3f;

        // 场地
        public const float ArenaHalfWidth = 11.5f;
        public const float ArenaHalfHeight = 6.5f;
    }
}
