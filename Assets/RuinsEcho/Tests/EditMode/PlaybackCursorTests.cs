using System.Collections.Generic;
using NUnit.Framework;
using RuinsEcho.Recording;
using UnityEngine;

namespace RuinsEcho.Tests
{
    public sealed class PlaybackCursorTests
    {
        private readonly List<RecordFrame> _due = new List<RecordFrame>();

        private static List<RecordFrame> BuildFrames()
        {
            return new List<RecordFrame>
            {
                new RecordFrame(100f, Vector2.zero, 0f),
                new RecordFrame(100.5f, Vector2.right, 0f),
                new RecordFrame(101f, Vector2.up, 0f)
            };
        }

        private static PlaybackCursor CreateCursor(bool reverse = false, bool loop = false, float speed = 1f)
        {
            var cursor = new PlaybackCursor();
            cursor.Load(BuildFrames());
            cursor.Configure(reverse, loop, speed);
            return cursor;
        }

        [Test]
        public void Forward_EmitsFramesWhenPlayheadCrossesThem()
        {
            PlaybackCursor cursor = CreateCursor();

            Assert.AreEqual(1, cursor.AdvanceBy(0f, _due));
            Assert.AreEqual(0f, cursor.Playhead, 0.0001f);

            Assert.AreEqual(1, cursor.AdvanceBy(0.6f, _due));
            Assert.AreEqual(100.5f, _due[0].UnscaledTimestamp, 0.0001f);

            Assert.AreEqual(1, cursor.AdvanceBy(0.4f, _due));
            Assert.AreEqual(101f, _due[0].UnscaledTimestamp, 0.0001f);
            Assert.IsTrue(cursor.IsFinished);
        }

        [Test]
        public void Speed_HalvesPlaybackRate()
        {
            PlaybackCursor cursor = CreateCursor(speed: 0.5f);

            Assert.AreEqual(2, cursor.AdvanceBy(1f, _due));
            Assert.AreEqual(0.5f, cursor.Playhead, 0.0001f);
            Assert.IsFalse(cursor.IsFinished);
        }

        [Test]
        public void Loop_WrapsAndReplaysFromStart()
        {
            PlaybackCursor cursor = CreateCursor(loop: true);

            Assert.AreEqual(1, cursor.AdvanceBy(0f, _due));
            Assert.AreEqual(1, cursor.AdvanceBy(1.1f, _due));

            Assert.AreEqual(0f, _due[0].UnscaledTimestamp, 0.0001f);
            Assert.IsFalse(cursor.IsFinished);
        }

        [Test]
        public void Reverse_EmitsFramesFromEndToStart()
        {
            PlaybackCursor cursor = CreateCursor(reverse: true);

            Assert.AreEqual(1, cursor.AdvanceBy(0f, _due));
            Assert.AreEqual(101f, _due[0].UnscaledTimestamp, 0.0001f);

            Assert.AreEqual(1, cursor.AdvanceBy(0.6f, _due));
            Assert.AreEqual(100.5f, _due[0].UnscaledTimestamp, 0.0001f);

            Assert.AreEqual(1, cursor.AdvanceBy(0.5f, _due));
            Assert.AreEqual(100f, _due[0].UnscaledTimestamp, 0.0001f);
            Assert.IsTrue(cursor.IsFinished);
        }

        [Test]
        public void EmptyBuffer_FinishesImmediately()
        {
            var cursor = new PlaybackCursor();
            cursor.Load(new List<RecordFrame>());
            cursor.Configure(false, false, 1f);

            Assert.IsTrue(cursor.IsFinished);
            Assert.AreEqual(0, cursor.AdvanceBy(1f, _due));
        }
    }
}
