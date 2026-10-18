using System;
using System.Collections.Generic;
using System.Linq;
using RuinsEcho.Core;
using RuinsEcho.Relics;
using UnityEngine;

namespace RuinsEcho.Dungeon
{
    /// <summary>
    /// 确定性地牢生成器（规格 7.3）。相同 seed + 相同遗物池 => 完全一致的布局、
    /// 敌人分布与遗物三选一。使用自研 DeterministicRandom，不依赖 Unity Random.State。
    /// </summary>
    public static class DungeonGenerator
    {
        public static DungeonLayout Generate(uint seed, IReadOnlyList<string> availableRelicIds = null)
        {
            var rng = new DeterministicRandom(seed);
            var layout = new DungeonLayout { Seed = seed };

            List<string> relicIds = (availableRelicIds ?? RelicCatalog.All.Select(relic => relic.relicId))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            for (int layerIndex = 0; layerIndex < GameConfig.TotalLayers; layerIndex++)
            {
                var layer = new LayerLayout
                {
                    Index = layerIndex,
                    Combat = CreateCombatRoom(rng, layerIndex, relicIds),
                    RestBranch = CreateBranchRoom(rng, layerIndex, RoomType.Rest, null),
                    EventBranch = CreateBranchRoom(
                        rng,
                        layerIndex,
                        RoomType.Event,
                        rng.Chance(0.5f) ? DungeonEventIds.SacrificeHp : DungeonEventIds.SacrificeRelic),
                    Boss = CreateBossRoom(rng, layerIndex)
                };

                layout.Layers.Add(layer);
            }

            return layout;
        }

        private static RoomNode CreateCombatRoom(
            DeterministicRandom rng,
            int layerIndex,
            IReadOnlyList<string> relicIds)
        {
            var room = new RoomNode
            {
                RoomId = $"L{layerIndex + 1}_Combat",
                Type = RoomType.Combat,
                LayerIndex = layerIndex,
                RoomSeed = rng.NextUInt()
            };

            int enemyCount = 3 + layerIndex + rng.Range(0, 2);
            for (int i = 0; i < enemyCount; i++)
            {
                float angle = ((Mathf.PI * 2f * i) / enemyCount) + rng.Range(-0.28f, 0.28f);
                float radiusX = rng.Range(2.2f, 5.0f);
                float radiusY = rng.Range(1.4f, 3.2f);
                Vector2 position = new Vector2(
                    Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY);

                room.EnemySpawns.Add(new EnemySpawn(RollEnemyKind(rng, layerIndex), position));
            }

            List<RelicData> choices = RelicCatalog.GetRandomChoices(
                rng,
                relicIds,
                Array.Empty<string>(),
                3);

            for (int i = 0; i < choices.Count; i++)
            {
                room.RewardRelicIds.Add(choices[i].relicId);
            }

            return room;
        }

        private static RoomNode CreateBranchRoom(
            DeterministicRandom rng,
            int layerIndex,
            RoomType type,
            string eventId)
        {
            return new RoomNode
            {
                RoomId = $"L{layerIndex + 1}_{type}",
                Type = type,
                LayerIndex = layerIndex,
                RoomSeed = rng.NextUInt(),
                EventId = eventId
            };
        }

        private static RoomNode CreateBossRoom(DeterministicRandom rng, int layerIndex)
        {
            bool finalBoss = layerIndex == GameConfig.TotalLayers - 1;

            return new RoomNode
            {
                RoomId = $"L{layerIndex + 1}_Boss",
                Type = RoomType.Boss,
                LayerIndex = layerIndex,
                RoomSeed = rng.NextUInt(),
                BossId = finalBoss ? "boss_echo_sovereign" : $"boss_layer_{layerIndex + 1}",
                BossMaxHP = 280f + (layerIndex * 160f) + (finalBoss ? 240f : 0f),
                BossDamage = 14f + (layerIndex * 3f),
                IsFinalBoss = finalBoss
            };
        }

        private static EnemyKind RollEnemyKind(DeterministicRandom rng, int layerIndex)
        {
            float roll = rng.NextFloat();
            float hunterChance = 0.10f + (layerIndex * 0.02f);
            float rangedChance = 0.28f;

            if (roll < hunterChance)
            {
                return EnemyKind.ShadowHunter;
            }

            if (roll < hunterChance + rangedChance)
            {
                return EnemyKind.Ranged;
            }

            return EnemyKind.Melee;
        }
    }
}
