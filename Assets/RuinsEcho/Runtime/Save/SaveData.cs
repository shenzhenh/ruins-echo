using System;
using System.Collections.Generic;

namespace RuinsEcho.Save
{
    /// <summary>
    /// 局外永久进度（规格 11.1）。只允许保存残片与“已解锁选项”，
    /// 任何局内数值（HP / 能量 / 遗物组合 / 种子）都不得写入这里。
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int fragments;
        public List<string> unlockedRelicIds = new List<string>();
        public List<string> unlockedCharacterIds = new List<string>();

        public bool HasRelic(string relicId)
        {
            return unlockedRelicIds != null && unlockedRelicIds.Contains(relicId);
        }

        public bool HasCharacter(string characterId)
        {
            return unlockedCharacterIds != null && unlockedCharacterIds.Contains(characterId);
        }

        public void EnsureDefaults(
            IEnumerable<string> defaultRelicIds,
            IEnumerable<string> defaultCharacterIds)
        {
            if (version <= 0)
            {
                version = 1;
            }

            if (unlockedRelicIds == null)
            {
                unlockedRelicIds = new List<string>();
            }

            if (unlockedCharacterIds == null)
            {
                unlockedCharacterIds = new List<string>();
            }

            if (defaultRelicIds != null)
            {
                foreach (string id in defaultRelicIds)
                {
                    if (!string.IsNullOrEmpty(id) && !unlockedRelicIds.Contains(id))
                    {
                        unlockedRelicIds.Add(id);
                    }
                }
            }

            if (defaultCharacterIds != null)
            {
                foreach (string id in defaultCharacterIds)
                {
                    if (!string.IsNullOrEmpty(id) && !unlockedCharacterIds.Contains(id))
                    {
                        unlockedCharacterIds.Add(id);
                    }
                }
            }
        }
    }
}
