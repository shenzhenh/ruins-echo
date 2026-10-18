using System;
using System.Collections.Generic;

namespace RuinsEcho.Core
{
    /// <summary>
    /// 跨平台、跨 Unity 版本稳定的确定性随机数发生器（SplitMix64）。
    /// 规格要求“同种子结果完全一致”；Unity 的 Random.State 会随版本变化，
    /// 因此这里使用固定算法，详见 docs/decisions/0001-deterministic-rng.md。
    /// </summary>
    public sealed class DeterministicRandom
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;
        private ulong _state;

        public uint Seed { get; }

        public DeterministicRandom(uint seed)
        {
            Seed = seed;
            _state = seed + Gamma;
            if (_state == 0UL)
            {
                _state = Gamma;
            }

            // 预热一次，避免小种子产生高度相关的第一个值。
            NextUInt();
        }

        public uint NextUInt()
        {
            _state += Gamma;

            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;

            return (uint)(z >> 32);
        }

        /// <summary>[minInclusive, maxExclusive)</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        public float NextFloat()
        {
            // 24 位精度，结果在 [0, 1)。
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            return minInclusive + ((maxInclusive - minInclusive) * NextFloat());
        }

        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("Cannot pick from an empty collection.", nameof(items));
            }

            return items[Range(0, items.Count)];
        }

        /// <summary>Fisher-Yates，原地打乱。</summary>
        public void Shuffle<T>(IList<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
