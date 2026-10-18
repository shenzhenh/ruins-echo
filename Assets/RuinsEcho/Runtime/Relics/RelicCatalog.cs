using System;
using System.Collections.Generic;
using System.Linq;
using RuinsEcho.Core;
using RuinsEcho.Relics.Effects;
using UnityEngine;

namespace RuinsEcho.Relics
{
    /// <summary>
    /// MVP 9 件遗物的代码内置目录（5 增益 + 4 扭曲，规格 6.2）。
    /// 用 ScriptableObject 承载数据，用效果类注入行为；后续可直接替换为 .asset。
    /// </summary>
    public static class RelicCatalog
    {
        private static List<RelicData> _all;
        private static Dictionary<string, RelicData> _byId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _all = null;
            _byId = null;
        }

        public static IReadOnlyList<RelicData> All
        {
            get
            {
                EnsureCreated();
                return _all;
            }
        }

        public static RelicData Get(string relicId)
        {
            EnsureCreated();
            return !string.IsNullOrEmpty(relicId) && _byId.TryGetValue(relicId, out RelicData data)
                ? data
                : null;
        }

        /// <summary>
        /// 从解锁池中抽取 count 个不重复、未持有的遗物。
        /// 候选先按 ID 排序再洗牌，保证同种子结果与输入集合顺序无关。
        /// </summary>
        public static List<RelicData> GetRandomChoices(
            DeterministicRandom rng,
            IEnumerable<string> availableIds,
            IEnumerable<string> ownedIds,
            int count,
            bool? twisted = null)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (count <= 0)
            {
                return new List<RelicData>();
            }

            EnsureCreated();

            var owned = new HashSet<string>(ownedIds ?? Array.Empty<string>());
            IEnumerable<string> ids = availableIds ?? _all.Select(relic => relic.relicId);

            var candidates = new List<RelicData>();
            foreach (string id in ids.Distinct().OrderBy(value => value, StringComparer.Ordinal))
            {
                RelicData relic = Get(id);
                if (relic == null || owned.Contains(id))
                {
                    continue;
                }

                if (twisted.HasValue && relic.isTwisted != twisted.Value)
                {
                    continue;
                }

                candidates.Add(relic);
            }

            rng.Shuffle(candidates);

            if (candidates.Count > count)
            {
                candidates.RemoveRange(count, candidates.Count - count);
            }

            return candidates;
        }

        private static void EnsureCreated()
        {
            if (_all != null)
            {
                return;
            }

            _all = new List<RelicData>
            {
                Create(
                    MetaIds.RelicAfterimagePersistence,
                    "残影存续",
                    "影子回放结束后不销毁，循环重复动作序列，直到影子血量耗尽。",
                    twisted: false,
                    rarity: RelicRarity.Common,
                    new PersistentEchoEffect()),
                Create(
                    MetaIds.RelicTwinEcho,
                    "双生回响",
                    "召唤一次生成 2 个影子；召唤能量消耗 ×2。",
                    twisted: false,
                    rarity: RelicRarity.Common,
                    new TwinEchoEffect()),
                Create(
                    MetaIds.RelicShadowSacrifice,
                    "替影承伤",
                    "敌人优先锁定影子；本体受到伤害的 25% 转移给一个存活影子。",
                    twisted: false,
                    rarity: RelicRarity.Common,
                    new ShadowSacrificeEffect()),
                Create(
                    MetaIds.RelicEchoConduction,
                    "回响传导",
                    "影子命中敌人时，玩家普攻冷却缩短 0.3 秒。",
                    twisted: false,
                    rarity: RelicRarity.Common,
                    new EchoConductionEffect()),
                Create(
                    MetaIds.RelicShadowStrike,
                    "掠影打击",
                    "影子暴击命中会给目标挂标记；本体对被标记目标伤害 +30%。",
                    twisted: false,
                    rarity: RelicRarity.Common,
                    new ShadowStrikeEffect()),
                Create(
                    MetaIds.RelicSymbioticBacklash,
                    "共生反噬",
                    "影子攻击判定会误伤本体；影子伤害 ×2。",
                    twisted: true,
                    rarity: RelicRarity.Rare,
                    new SymbioticBacklashEffect()),
                Create(
                    MetaIds.RelicEnergySurge,
                    "能量狂潮",
                    "回响能量恢复速度 ×1.8；本体受到伤害 ×1.3。",
                    twisted: true,
                    rarity: RelicRarity.Rare,
                    new EnergySurgeEffect()),
                Create(
                    MetaIds.RelicWillErosion,
                    "意志侵蚀",
                    "影子最大生命 ×1.8；玩家最大生命 -40%。",
                    twisted: true,
                    rarity: RelicRarity.Rare,
                    new WillErosionEffect()),
                Create(
                    MetaIds.RelicActionTurbulence,
                    "动作乱流",
                    "影子回放位置每帧随机偏移；影子攻击范围 +50%。",
                    twisted: true,
                    rarity: RelicRarity.Rare,
                    new ActionTurbulenceEffect())
            };

            _byId = new Dictionary<string, RelicData>(_all.Count);
            foreach (RelicData relic in _all)
            {
                _byId[relic.relicId] = relic;
            }
        }

        private static RelicData Create(
            string id,
            string displayName,
            string description,
            bool twisted,
            RelicRarity rarity,
            IRelicEffect effect)
        {
            RelicData data = ScriptableObject.CreateInstance<RelicData>();
            data.hideFlags = HideFlags.DontSave;
            data.name = id;
            data.relicId = id;
            data.displayName = displayName;
            data.description = description;
            data.isTwisted = twisted;
            data.rarity = rarity;
            data.effects = new List<IRelicEffect> { effect };
            return data;
        }
    }
}
