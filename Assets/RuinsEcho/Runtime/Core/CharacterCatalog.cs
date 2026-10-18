using System.Collections.Generic;
using UnityEngine;

namespace RuinsEcho.Core
{
    /// <summary>
    /// MVP 两个角色模板的代码内置目录。正式制作时可替换为 CharacterData 资产，
    /// 但 ID 必须保持不变以兼容局外存档。
    /// </summary>
    public static class CharacterCatalog
    {
        private static List<CharacterData> _all;
        private static Dictionary<string, CharacterData> _byId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _all = null;
            _byId = null;
        }

        public static IReadOnlyList<CharacterData> All
        {
            get
            {
                EnsureCreated();
                return _all;
            }
        }

        public static CharacterData Get(string characterId)
        {
            EnsureCreated();
            return _byId.TryGetValue(characterId, out CharacterData data) ? data : null;
        }

        public static CharacterData Hunter => Get(MetaIds.CharacterHunter);
        public static CharacterData Guardian => Get(MetaIds.CharacterGuardian);

        private static void EnsureCreated()
        {
            if (_all != null)
            {
                return;
            }

            _all = new List<CharacterData>
            {
                Create(
                    MetaIds.CharacterHunter,
                    "追猎者",
                    "录制缓存 +0.5 秒；影子基础生命 -20%。擅长更长的动作编排。",
                    recordDurationBonus: GameConfig.PlayerRecordWindowBonusHunter,
                    shadowHpMultiplier: 0.8f,
                    shadowDamageTakenMultiplier: 1f,
                    energyRegenMultiplier: 1f),
                Create(
                    MetaIds.CharacterGuardian,
                    "守御者",
                    "影子承伤效率 +25%；回响能量恢复 -20%。更适合用影子抗压。",
                    recordDurationBonus: 0f,
                    shadowHpMultiplier: 1f,
                    shadowDamageTakenMultiplier: 0.8f,
                    energyRegenMultiplier: 0.8f)
            };

            _byId = new Dictionary<string, CharacterData>(_all.Count);
            foreach (CharacterData character in _all)
            {
                _byId[character.characterId] = character;
            }
        }

        private static CharacterData Create(
            string id,
            string displayName,
            string description,
            float recordDurationBonus,
            float shadowHpMultiplier,
            float shadowDamageTakenMultiplier,
            float energyRegenMultiplier)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.hideFlags = HideFlags.DontSave;
            data.name = id;
            data.characterId = id;
            data.displayName = displayName;
            data.description = description;
            data.recordDurationBonus = recordDurationBonus;
            data.shadowHpMultiplier = shadowHpMultiplier;
            data.shadowDamageTakenMultiplier = shadowDamageTakenMultiplier;
            data.energyRegenMultiplier = energyRegenMultiplier;
            return data;
        }
    }
}
