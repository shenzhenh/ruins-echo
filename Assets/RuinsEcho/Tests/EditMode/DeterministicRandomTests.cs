using NUnit.Framework;
using RuinsEcho.Core;

namespace RuinsEcho.Tests
{
    public sealed class DeterministicRandomTests
    {
        [Test]
        public void KnownSequence_IsStableAcrossVersions()
        {
            // 这些值锁定 SplitMix64 实现；改动算法会导致同种子旧存档 / 旧地图失效。
            var rng = new DeterministicRandom(12345u);

            Assert.AreEqual(513431484u, rng.NextUInt());
            Assert.AreEqual(756420222u, rng.NextUInt());
            Assert.AreEqual(2177033948u, rng.NextUInt());
            Assert.AreEqual(1447552346u, rng.NextUInt());
            Assert.AreEqual(526787252u, rng.NextUInt());
        }

        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var first = new DeterministicRandom(42u);
            var second = new DeterministicRandom(42u);

            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(first.NextUInt(), second.NextUInt());
            }
        }

        [Test]
        public void Range_StaysWithinBounds()
        {
            var rng = new DeterministicRandom(7u);

            for (int i = 0; i < 1000; i++)
            {
                int value = rng.Range(3, 9);
                Assert.GreaterOrEqual(value, 3);
                Assert.Less(value, 9);
            }
        }

        [Test]
        public void NextFloat_IsInUnitInterval()
        {
            var rng = new DeterministicRandom(9u);

            for (int i = 0; i < 1000; i++)
            {
                float value = rng.NextFloat();
                Assert.GreaterOrEqual(value, 0f);
                Assert.Less(value, 1f);
            }
        }

        [Test]
        public void Shuffle_IsDeterministicForSameSeed()
        {
            int[] first = { 1, 2, 3, 4, 5, 6, 7 };
            int[] second = { 1, 2, 3, 4, 5, 6, 7 };

            new DeterministicRandom(123u).Shuffle(first);
            new DeterministicRandom(123u).Shuffle(second);

            CollectionAssert.AreEqual(first, second);
        }
    }
}
