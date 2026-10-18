using System;
using System.Collections.Generic;
using RuinsEcho.Core;
using UnityEngine;

namespace RuinsEcho.Relics
{
    /// <summary>
    /// 本局遗物持有者。持有最多 8 件遗物，并把所有效果聚合为 RelicStats。
    /// 局内数据：一局结束必须 ClearAllRelics，严禁写入存档。
    /// </summary>
    public sealed class RelicManager : MonoBehaviour
    {
        public List<RelicData> activeRelics = new List<RelicData>();
        public int MaxRelicCount = GameConfig.MaxRelics;

        public RelicStats Stats { get; } = new RelicStats();

        public IReadOnlyList<RelicData> ActiveRelics => activeRelics;

        public bool CanAddRelic => activeRelics.Count < MaxRelicCount;

        public event Action<RelicData> RelicAdded;
        public event Action<RelicData> RelicRemoved;
        public event Action RelicsChanged;

        public bool HasRelic(string relicId)
        {
            if (string.IsNullOrEmpty(relicId))
            {
                return false;
            }

            for (int i = 0; i < activeRelics.Count; i++)
            {
                if (activeRelics[i] != null && activeRelics[i].relicId == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AddRelic(RelicData relic)
        {
            if (relic == null || !relic.IsValid)
            {
                return false;
            }

            if (!CanAddRelic || HasRelic(relic.relicId))
            {
                return false;
            }

            activeRelics.Add(relic);
            RebuildStats();
            RelicAdded?.Invoke(relic);
            return true;
        }

        public bool RemoveRelic(RelicData relic)
        {
            if (relic == null || !activeRelics.Remove(relic))
            {
                return false;
            }

            InvokeRemove(relic);
            RebuildStats();
            RelicRemoved?.Invoke(relic);
            return true;
        }

        public bool RemoveRelic(string relicId)
        {
            for (int i = 0; i < activeRelics.Count; i++)
            {
                if (activeRelics[i] != null && activeRelics[i].relicId == relicId)
                {
                    return RemoveRelic(activeRelics[i]);
                }
            }

            return false;
        }

        public void ClearAllRelics()
        {
            if (activeRelics.Count == 0)
            {
                return;
            }

            RelicData[] removed = activeRelics.ToArray();
            activeRelics.Clear();

            for (int i = 0; i < removed.Length; i++)
            {
                InvokeRemove(removed[i]);
            }

            RebuildStats();

            for (int i = 0; i < removed.Length; i++)
            {
                RelicRemoved?.Invoke(removed[i]);
            }
        }

        /// <summary>
        /// 从零重算聚合值。幂等，可安全地在任意遗物变化后调用。
        /// </summary>
        public void RebuildStats()
        {
            Stats.Reset();

            for (int i = 0; i < activeRelics.Count; i++)
            {
                RelicData relic = activeRelics[i];
                if (relic == null || relic.effects == null)
                {
                    continue;
                }

                for (int e = 0; e < relic.effects.Count; e++)
                {
                    relic.effects[e]?.OnApply(this);
                }
            }

            Stats.Clamp();
            RelicsChanged?.Invoke();
        }

        private void InvokeRemove(RelicData relic)
        {
            if (relic?.effects == null)
            {
                return;
            }

            for (int e = 0; e < relic.effects.Count; e++)
            {
                relic.effects[e]?.OnRemove(this);
            }
        }
    }
}
