using System;
using System.IO;
using NUnit.Framework;
using RuinsEcho.Core;
using RuinsEcho.Save;
using UnityEngine;

namespace RuinsEcho.Tests
{
    public sealed class SaveDataTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(
                Path.GetTempPath(),
                "ruins-echo-test-" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            string directory = Path.GetDirectoryName(_path);
            string pattern = Path.GetFileName(_path) + "*";
            foreach (string file in Directory.GetFiles(directory, pattern))
            {
                File.Delete(file);
            }
        }

        [Test]
        public void EnsureDefaults_AddsDefaultsOnlyOnce()
        {
            var data = new SaveData();
            data.EnsureDefaults(MetaIds.DefaultRelicIds, MetaIds.DefaultCharacterIds);
            data.EnsureDefaults(MetaIds.DefaultRelicIds, MetaIds.DefaultCharacterIds);

            Assert.AreEqual(MetaIds.DefaultRelicIds.Length, data.unlockedRelicIds.Count);
            Assert.AreEqual(MetaIds.DefaultCharacterIds.Length, data.unlockedCharacterIds.Count);
            Assert.IsTrue(data.HasRelic(MetaIds.RelicTwinEcho));
            Assert.IsTrue(data.HasCharacter(MetaIds.CharacterHunter));
        }

        [Test]
        public void JsonUtility_RoundTripsPermanentFields()
        {
            var data = new SaveData
            {
                fragments = 42
            };
            data.unlockedRelicIds.Add(MetaIds.RelicTwinEcho);
            data.unlockedCharacterIds.Add(MetaIds.CharacterGuardian);

            string json = JsonUtility.ToJson(data, true);
            SaveData restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(42, restored.fragments);
            CollectionAssert.Contains(restored.unlockedRelicIds, MetaIds.RelicTwinEcho);
            CollectionAssert.Contains(restored.unlockedCharacterIds, MetaIds.CharacterGuardian);
        }

        [Test]
        public void SaveAndLoad_PersistsFragmentsAndUnlocks()
        {
            SaveData data = SaveSystem.CreateDefault();
            data.fragments = 77;
            data.unlockedRelicIds.Add(MetaIds.RelicShadowStrike);

            SaveSystem.Save(data, _path);
            SaveData loaded = SaveSystem.Load(_path);

            Assert.AreEqual(77, loaded.fragments);
            Assert.IsTrue(loaded.HasRelic(MetaIds.RelicShadowStrike));
            Assert.IsTrue(loaded.HasCharacter(MetaIds.CharacterHunter));
        }

        [Test]
        public void Load_CorruptJsonFallsBackToDefaults()
        {
            File.WriteAllText(_path, "{ this is not valid json");

            SaveData loaded = SaveSystem.Load(_path);

            Assert.AreEqual(0, loaded.fragments);
            Assert.IsTrue(loaded.HasRelic(MetaIds.RelicTwinEcho));
        }
    }
}
