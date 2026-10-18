using System;
using RuinsEcho.Core;

namespace RuinsEcho.Save
{
    /// <summary>局外进度服务：残片增减、内容解锁、自动保存。</summary>
    public sealed class MetaProgress
    {
        public SaveData Data { get; }
        public string SavePath { get; }

        public MetaProgress(SaveData data, string savePath = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            SavePath = savePath;
        }

        public static MetaProgress Load(string savePath = null)
        {
            return new MetaProgress(SaveSystem.Load(savePath), savePath);
        }

        public bool IsRelicUnlocked(string relicId)
        {
            return Data.HasRelic(relicId);
        }

        public bool IsCharacterUnlocked(string characterId)
        {
            return Data.HasCharacter(characterId);
        }

        public void AddFragments(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Data.fragments += amount;
            Save();
        }

        public bool TryUnlockRelic(string relicId, int cost)
        {
            if (string.IsNullOrEmpty(relicId) || Data.HasRelic(relicId) || Data.fragments < cost)
            {
                return false;
            }

            Data.fragments -= cost;
            Data.unlockedRelicIds.Add(relicId);
            Save();
            return true;
        }

        public bool TryUnlockCharacter(string characterId, int cost)
        {
            if (string.IsNullOrEmpty(characterId) || Data.HasCharacter(characterId) || Data.fragments < cost)
            {
                return false;
            }

            Data.fragments -= cost;
            Data.unlockedCharacterIds.Add(characterId);
            Save();
            return true;
        }

        public void Save()
        {
            SaveSystem.Save(Data, SavePath);
        }
    }
}
