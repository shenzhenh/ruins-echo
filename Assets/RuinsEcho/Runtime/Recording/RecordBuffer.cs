using System;
using System.Collections.Generic;

namespace RuinsEcho.Recording
{
    /// <summary>
    /// 3 秒滑动窗口录制缓存。纯 C#，不依赖场景对象，便于 EditMode 测试。
    /// 规格 4.1：每帧追加；最新帧 - 最旧帧 &gt; 窗口时从头部移除。
    /// </summary>
    public sealed class RecordBuffer
    {
        private readonly List<RecordFrame> _frames = new List<RecordFrame>(512);
        private float _maxDuration;

        public RecordBuffer(float maxDuration)
        {
            if (maxDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDuration), "Record window must be positive.");
            }

            _maxDuration = maxDuration;
        }

        public float MaxDuration => _maxDuration;

        public int Count => _frames.Count;

        public bool HasValidBuffer => _frames.Count > 0;

        public IReadOnlyList<RecordFrame> Frames => _frames;

        /// <summary>最后一帧相对第一帧的时长；少于两帧时为 0。</summary>
        public float Duration
        {
            get
            {
                if (_frames.Count < 2)
                {
                    return 0f;
                }

                return _frames[_frames.Count - 1].UnscaledTimestamp - _frames[0].UnscaledTimestamp;
            }
        }

        /// <summary>
        /// 追加一帧并立即修剪。乱序帧（时间戳倒退）会被拒绝，避免回放倒跳。
        /// </summary>
        public bool Append(in RecordFrame frame)
        {
            if (_frames.Count > 0 &&
                frame.UnscaledTimestamp < _frames[_frames.Count - 1].UnscaledTimestamp)
            {
                return false;
            }

            _frames.Add(frame);
            Trim(frame.UnscaledTimestamp);
            return true;
        }

        /// <summary>
        /// 删除超过窗口的旧帧。至少保留一帧，保证“刚清空后立即录制”不会出现空缓存。
        /// </summary>
        public int Trim(float newestTimestamp)
        {
            int removed = 0;
            while (_frames.Count > 1 &&
                   newestTimestamp - _frames[0].UnscaledTimestamp > _maxDuration)
            {
                _frames.RemoveAt(0);
                removed++;
            }

            return removed;
        }

        public void Clear()
        {
            _frames.Clear();
        }

        public List<RecordFrame> GetCopy()
        {
            return new List<RecordFrame>(_frames);
        }

        /// <summary>角色模板可能改变窗口（追猎者 +0.5 秒）。</summary>
        public void SetMaxDuration(float maxDuration)
        {
            if (maxDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDuration));
            }

            _maxDuration = maxDuration;
            if (_frames.Count > 1)
            {
                Trim(_frames[_frames.Count - 1].UnscaledTimestamp);
            }
        }
    }
}
