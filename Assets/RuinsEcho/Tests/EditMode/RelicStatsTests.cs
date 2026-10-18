using NUnit.Framework;
using RuinsEcho.Core;
using RuinsEcho.Relics;
using UnityEngine;

namespace RuinsEcho.Tests
{
    public sealed class RelicStatsTests
    {
        private GameObject _go;
        private RelicManager _manager;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RelicManagerTest");
            _manager = _go.AddComponent<RelicManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private void Add(string relicId)
        {
            Assert.IsTrue(_manager.AddRelic(RelicCatalog.Get(relicId)), relicId);
        }

        [Test]
        public void TwinEcho_DoublesShadowsAndCost()
        {
            Add(MetaIds.RelicTwinEcho);

            Assert.AreEqual(2, _manager.Stats.ShadowsPerSummon);
            Assert.AreEqual(2f, _manager.Stats.SummonCostMultiplier, 0.0001f);
        }

        [Test]
        public void EnergySurge_TradesDefenseForRegen()
        {
            Add(MetaIds.RelicEnergySurge);

            Assert.AreEqual(1.8f, _manager.Stats.EnergyRegenMultiplier, 0.0001f);
            Assert.AreEqual(1.3f, _manager.Stats.PlayerDamageTakenMultiplier, 0.0001f);
        }

        [Test]
        public void WillErosion_ScalesShadowUpAndPlayerDown()
        {
            Add(MetaIds.RelicWillErosion);

            Assert.AreEqual(1.8f, _manager.Stats.ShadowMaxHpMultiplier, 0.0001f);
            Assert.AreEqual(0.6f, _manager.Stats.PlayerMaxHpMultiplier, 0.0001f);
        }

        [Test]
        public void SymbioticBacklash_DoublesShadowDamageAndEnablesFriendlyFire()
        {
            Add(MetaIds.RelicSymbioticBacklash);

            Assert.AreEqual(2f, _manager.Stats.ShadowDamageMultiplier, 0.0001f);
            Assert.IsTrue(_manager.Stats.ShadowFriendlyFire);
        }

        [Test]
        public void ShadowSacrifice_PrefersShadowsAndRedirectsDamage()
        {
            Add(MetaIds.RelicShadowSacrifice);

            Assert.IsTrue(_manager.Stats.EnemiesPreferShadows);
            Assert.AreEqual(0.25f, _manager.Stats.DamageRedirectToShadows, 0.0001f);
        }

        [Test]
        public void EchoConduction_RefundsAttackCooldown()
        {
            Add(MetaIds.RelicEchoConduction);
            Assert.AreEqual(0.3f, _manager.Stats.AttackCooldownRefund, 0.0001f);
        }

        [Test]
        public void ShadowStrike_MarksOnCrit()
        {
            Add(MetaIds.RelicShadowStrike);

            Assert.IsTrue(_manager.Stats.ShadowCritMarks);
            Assert.AreEqual(1.3f, _manager.Stats.MarkedDamageMultiplier, 0.0001f);
        }

        [Test]
        public void ActionTurbulence_AddsJitterAndRadius()
        {
            Add(MetaIds.RelicActionTurbulence);

            Assert.AreEqual(0.15f, _manager.Stats.ShadowPositionJitter, 0.0001f);
            Assert.AreEqual(1.5f, _manager.Stats.ShadowAttackRadiusMultiplier, 0.0001f);
        }

        [Test]
        public void AfterimagePersistence_LoopsShadows()
        {
            Add(MetaIds.RelicAfterimagePersistence);
            Assert.IsTrue(_manager.Stats.LoopShadows);
        }

        [Test]
        public void AddRelic_RejectsDuplicatesAndRespectsCapacity()
        {
            foreach (RelicData relic in RelicCatalog.All)
            {
                _manager.AddRelic(relic);
            }

            Assert.AreEqual(GameConfig.MaxRelics, _manager.ActiveRelics.Count);
            Assert.IsFalse(_manager.AddRelic(RelicCatalog.Get(MetaIds.RelicTwinEcho)));
        }

        [Test]
        public void RemoveAndClear_RebuildStats()
        {
            Add(MetaIds.RelicTwinEcho);
            Assert.AreEqual(2, _manager.Stats.ShadowsPerSummon);

            Assert.IsTrue(_manager.RemoveRelic(MetaIds.RelicTwinEcho));
            Assert.AreEqual(1, _manager.Stats.ShadowsPerSummon);
            Assert.AreEqual(1f, _manager.Stats.SummonCostMultiplier, 0.0001f);

            Add(MetaIds.RelicEnergySurge);
            _manager.ClearAllRelics();

            Assert.AreEqual(0, _manager.ActiveRelics.Count);
            Assert.AreEqual(1f, _manager.Stats.EnergyRegenMultiplier, 0.0001f);
        }
    }
}
