using UnityEngine;

namespace RuinsEcho.Core
{
    /// <summary>
    /// 角色模板配置（规格 11.2）。角色只改变录制 / 影子 / 能量的运行参数，
    /// 不提供任何直接伤害数值堆叠。
    /// </summary>
    [CreateAssetMenu(menuName = "Ruins Echo/Character", fileName = "CharacterData")]
    public sealed class CharacterData : ScriptableObject
    {
        [Header("身份")]
        public string characterId = "";
        public string displayName = "";

        [TextArea(2, 4)]
        public string description = "";

        [Header("录制")]
        [Tooltip("在 3 秒基础上增加的录制窗口。追猎者 +0.5 秒。")]
        public float recordDurationBonus;

        [Header("影子")]
        [Tooltip("影子最大生命倍率。追猎者 0.8。")]
        public float shadowHpMultiplier = 1f;

        [Tooltip("影子受到伤害的倍率；0.8 表示承伤效率提升 25%。守御者 0.8。")]
        public float shadowDamageTakenMultiplier = 1f;

        [Header("能量")]
        [Tooltip("回响能量恢复倍率。守御者 0.8。")]
        public float energyRegenMultiplier = 1f;
    }
}
