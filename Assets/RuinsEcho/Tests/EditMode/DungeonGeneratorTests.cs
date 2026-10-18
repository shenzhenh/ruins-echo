using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RuinsEcho.Core;
using RuinsEcho.Dungeon;
using RuinsEcho.Relics;

namespace RuinsEcho.Tests
{
    public sealed class DungeonGeneratorTests
    {
        private static List<string> AllRelicIds()
        {
            return RelicCatalog.All.Select(relic => relic.relicId).ToList();
        }

        private static string Signature(DungeonLayout layout)
        {
            var builder = new StringBuilder();

            for (int layerIndex = 0; layerIndex < layout.Layers.Count; layerIndex++)
            {
                LayerLayout layer = layout.Layers[layerIndex];
                builder.Append('L').Append(layer.Index).Append('|');

                for (int i = 0; i < layer.Combat.EnemySpawns.Count; i++)
                {
                    EnemySpawn spawn = layer.Combat.EnemySpawns[i];
                    builder.Append((int)spawn.Kind)
                        .Append(':')
                        .Append(spawn.Position.x.ToString("F4"))
                        .Append(',')
                        .Append(spawn.Position.y.ToString("F4"))
                        .Append(';');
                }

                builder.Append('|');
                for (int i = 0; i < layer.Combat.RewardRelicIds.Count; i++)
                {
                    builder.Append(layer.Combat.RewardRelicIds[i]).Append(';');
                }

                builder.Append('|')
                    .Append(layer.EventBranch.EventId)
                    .Append('|')
                    .Append(layer.Boss.BossMaxHP.ToString("F1"))
                    .Append('\n');
            }

            return builder.ToString();
        }

        [Test]
        public void Generate_ProducesFiveLayersWithCombatBranchAndBoss()
        {
            DungeonLayout layout = DungeonGenerator.Generate(12345u, AllRelicIds());

            Assert.AreEqual(GameConfig.TotalLayers, layout.Layers.Count);
            Assert.AreEqual(20, layout.RoomCount);

            for (int i = 0; i < layout.Layers.Count; i++)
            {
                LayerLayout layer = layout.Layers[i];
                Assert.AreEqual(i, layer.Index);
                Assert.AreEqual(RoomType.Combat, layer.Combat.Type);
                Assert.AreEqual(RoomType.Rest, layer.RestBranch.Type);
                Assert.AreEqual(RoomType.Event, layer.EventBranch.Type);
                Assert.AreEqual(RoomType.Boss, layer.Boss.Type);
                Assert.GreaterOrEqual(layer.Combat.EnemySpawns.Count, 3);
            }
        }

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            DungeonLayout first = DungeonGenerator.Generate(987654321u, AllRelicIds());
            DungeonLayout second = DungeonGenerator.Generate(987654321u, AllRelicIds());

            Assert.AreEqual(Signature(first), Signature(second));
        }

        [Test]
        public void Generate_DifferentSeedsProduceDifferentLayouts()
        {
            DungeonLayout first = DungeonGenerator.Generate(1u, AllRelicIds());
            DungeonLayout second = DungeonGenerator.Generate(2u, AllRelicIds());

            Assert.AreNotEqual(Signature(first), Signature(second));
        }

        [Test]
        public void Generate_IsIndependentOfAvailableRelicOrder()
        {
            List<string> ids = AllRelicIds();
            List<string> reversed = Enumerable.Reverse(ids).ToList();

            DungeonLayout first = DungeonGenerator.Generate(4242u, ids);
            DungeonLayout second = DungeonGenerator.Generate(4242u, reversed);

            for (int i = 0; i < first.Layers.Count; i++)
            {
                CollectionAssert.AreEqual(
                    first.Layers[i].Combat.RewardRelicIds,
                    second.Layers[i].Combat.RewardRelicIds);
            }
        }

        [Test]
        public void Generate_LastBossIsFinalBoss()
        {
            DungeonLayout layout = DungeonGenerator.Generate(7u, AllRelicIds());

            for (int i = 0; i < layout.Layers.Count; i++)
            {
                Assert.AreEqual(i == layout.Layers.Count - 1, layout.Layers[i].Boss.IsFinalBoss);
            }
        }

        [Test]
        public void Generate_RewardsComeFromAvailablePool()
        {
            List<string> available = new List<string>
            {
                MetaIds.RelicAfterimagePersistence,
                MetaIds.RelicTwinEcho,
                MetaIds.RelicEchoConduction,
                MetaIds.RelicShadowStrike
            };

            DungeonLayout layout = DungeonGenerator.Generate(99u, available);

            foreach (LayerLayout layer in layout.Layers)
            {
                Assert.LessOrEqual(layer.Combat.RewardRelicIds.Count, 3);
                foreach (string id in layer.Combat.RewardRelicIds)
                {
                    CollectionAssert.Contains(available, id);
                }
            }
        }
    }
}
