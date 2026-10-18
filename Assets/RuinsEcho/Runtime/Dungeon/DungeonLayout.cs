using System;
using System.Collections.Generic;
using RuinsEcho.Core;
using UnityEngine;

namespace RuinsEcho.Dungeon
{
    public enum RoomType
    {
        Combat = 0,
        Rest = 1,
        Event = 2,
        Boss = 3
    }

    public static class DungeonEventIds
    {
        public const string SacrificeHp = "event_sacrifice_hp";
        public const string SacrificeRelic = "event_sacrifice_relic";
    }

    [Serializable]
    public struct EnemySpawn
    {
        public EnemyKind Kind;
        public Vector2 Position;

        public EnemySpawn(EnemyKind kind, Vector2 position)
        {
            Kind = kind;
            Position = position;
        }
    }

    [Serializable]
    public sealed class RoomNode
    {
        public string RoomId;
        public RoomType Type;
        public int LayerIndex;
        public uint RoomSeed;

        public List<EnemySpawn> EnemySpawns = new List<EnemySpawn>();
        public List<string> RewardRelicIds = new List<string>();

        public string EventId;
        public string BossId;
        public float BossMaxHP;
        public float BossDamage;
        public bool IsFinalBoss;
    }

    [Serializable]
    public sealed class LayerLayout
    {
        public int Index;
        public RoomNode Combat;
        public RoomNode RestBranch;
        public RoomNode EventBranch;
        public RoomNode Boss;
    }

    /// <summary>
    /// 种子驱动的 5 层地牢数据（规格 7）。只描述房间、敌人位置与掉落，
    /// 不依赖场景，便于测试“同种子完全一致”。
    /// </summary>
    [Serializable]
    public sealed class DungeonLayout
    {
        public uint Seed;
        public List<LayerLayout> Layers = new List<LayerLayout>();

        public int RoomCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Layers.Count; i++)
                {
                    LayerLayout layer = Layers[i];
                    if (layer.Combat != null) count++;
                    if (layer.RestBranch != null) count++;
                    if (layer.EventBranch != null) count++;
                    if (layer.Boss != null) count++;
                }

                return count;
            }
        }

        public LayerLayout GetLayer(int index)
        {
            if (index < 0 || index >= Layers.Count)
            {
                return null;
            }

            return Layers[index];
        }
    }
}
