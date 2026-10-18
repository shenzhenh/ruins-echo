using System.Collections.Generic;
using UnityEngine;

namespace RuinsEcho.Relics
{
    public enum RelicRarity
    {
        Common = 0,
        Rare = 1
    }

    /// <summary>
    /// 遗物配置资产（规格 6.1）。局内运行时由 RelicCatalog 创建，
    /// 也可在编辑器中创建 .asset 并配置效果。
    /// </summary>
    [CreateAssetMenu(menuName = "Ruins Echo/Relic", fileName = "RelicData")]
    public sealed class RelicData : ScriptableObject
    {
        [Header("身份")]
        public string relicId = "";
        public string displayName = "";

        [TextArea(2, 5)]
        public string description = "";

        [Header("分类")]
        public bool isTwisted;
        public RelicRarity rarity = RelicRarity.Common;

        [Header("效果")]
        [SerializeReference]
        public List<IRelicEffect> effects = new List<IRelicEffect>();

        public bool IsValid => !string.IsNullOrEmpty(relicId);
    }
}
