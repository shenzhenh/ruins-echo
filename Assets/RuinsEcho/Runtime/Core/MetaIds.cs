namespace RuinsEcho.Core
{
    /// <summary>
    /// 局外存档使用的稳定 ID。ID 一旦发布不应修改，否则会丢失玩家解锁进度。
    /// </summary>
    public static class MetaIds
    {
        public const string CharacterHunter = "char_hunter";
        public const string CharacterGuardian = "char_guardian";

        public const string RelicAfterimagePersistence = "relic_afterimage_persistence";
        public const string RelicTwinEcho = "relic_twin_echo";
        public const string RelicShadowSacrifice = "relic_shadow_sacrifice";
        public const string RelicEchoConduction = "relic_echo_conduction";
        public const string RelicShadowStrike = "relic_shadow_strike";
        public const string RelicSymbioticBacklash = "relic_symbiotic_backlash";
        public const string RelicEnergySurge = "relic_energy_surge";
        public const string RelicWillErosion = "relic_will_erosion";
        public const string RelicActionTurbulence = "relic_action_turbulence";

        /// <summary>新存档默认解锁的遗物池。只解锁选项，不提供局内数值加成。</summary>
        public static readonly string[] DefaultRelicIds =
        {
            RelicAfterimagePersistence,
            RelicTwinEcho,
            RelicEchoConduction
        };

        public static readonly string[] DefaultCharacterIds =
        {
            CharacterHunter
        };
    }
}
