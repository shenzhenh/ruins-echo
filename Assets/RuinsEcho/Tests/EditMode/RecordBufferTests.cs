using NUnit.Framework;
using RuinsEcho.Recording;
using UnityEngine;

namespace RuinsEcho.Tests
{
    public sealed class RecordBufferTests
    {
        private static RecordFrame Frame(float timestamp, float x = 0f)
        {
            return new RecordFrame(timestamp, new Vector2(x, 0f), 0f);
        }

        [Test]
        public void Append_KeepsOnlyThreeSecondWindow()
        {
            var buffer = new RecordBuffer(3f);
            buffer.Append(Frame(0f));
            buffer.Append(Frame(1f));
            buffer.Append(Frame(3.01f));

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1f, buffer.Frames[0].UnscaledTimestamp);
            Assert.AreEqual(2.01f, buffer.Duration, 0.0001f);
        }

        [Test]
        public void Append_RejectsRewindingTimestamps()
        {
            var buffer = new RecordBuffer(3f);
            Assert.IsTrue(buffer.Append(Frame(10f)));
            Assert.IsFalse(buffer.Append(Frame(9.99f)));
            Assert.AreEqual(1, buffer.Count);
        }

        [Test]
        public void GetCopy_IsIndependentFromBuffer()
        {
            var buffer = new RecordBuffer(3f);
            buffer.Append(Frame(0f, 1f));

            var copy = buffer.GetCopy();
            copy.Clear();

            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(0, copy.Count);
        }

        [Test]
        public void SetMaxDuration_ReTrimsExistingFrames()
        {
            var buffer = new RecordBuffer(3f);
            buffer.Append(Frame(0f));
            buffer.Append(Frame(1f));
            buffer.Append(Frame(2f));

            buffer.SetMaxDuration(1.5f);

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1f, buffer.Frames[0].UnscaledTimestamp);
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            var buffer = new RecordBuffer(3f);
            buffer.Append(Frame(0f));
            buffer.Clear();

            Assert.IsFalse(buffer.HasValidBuffer);
            Assert.AreEqual(0, buffer.Count);
        }
    }
}
