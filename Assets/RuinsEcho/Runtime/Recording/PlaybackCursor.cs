using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuinsEcho.Recording
{
    /// <summary>
    /// 把绝对时间戳的录制帧转换为可播放时间轴。纯逻辑，可在 EditMode 中测试。
    ///
    /// 约定：
    /// - 帧时间相对于录制序列第一帧；
    /// - AdvanceBy 返回“本次推进跨过的所有帧”，重复时间戳的帧也会全部执行；
    /// - Speed 用于 Boss 扭曲力场（0.5 = 半速）；
    /// - Reverse 用于 Boss 二阶段时序反转；
    /// - Loop 用于遗物【残影存续】。
    /// </summary>
    public sealed class PlaybackCursor
    {
        private const float Epsilon = 1e-5f;

        private readonly List<RecordFrame> _frames = new List<RecordFrame>(512);
        private readonly List<float> _relativeTimes = new List<float>(512);

        private int _nextIndex;
        private float _playhead;
        private bool _hasAdvanced;

        public float Duration { get; private set; }

        public float Playhead => _playhead;

        public int FrameCount => _frames.Count;

        public float Speed { get; set; } = 1f;

        public bool Loop { get; set; }

        public bool Reverse { get; private set; }

        public bool IsFinished { get; private set; } = true;

        public void Load(IReadOnlyList<RecordFrame> frames)
        {
            _frames.Clear();
            _relativeTimes.Clear();

            if (frames != null && frames.Count > 0)
            {
                float origin = frames[0].UnscaledTimestamp;
                float last = 0f;

                for (int i = 0; i < frames.Count; i++)
                {
                    RecordFrame frame = frames[i];
                    float relative = frame.UnscaledTimestamp - origin;

                    // 防御性处理：缓存理论上单调，但外部传入的列表可能乱序。
                    if (relative < last)
                    {
                        relative = last;
                    }

                    _frames.Add(frame);
                    _relativeTimes.Add(relative);
                    last = relative;
                }

                Duration = last;
            }
            else
            {
                Duration = 0f;
            }

            Reset();
        }

        public void Configure(bool reverse, bool loop, float speed)
        {
            Reverse = reverse;
            Loop = loop;
            Speed = Mathf.Max(0f, speed);
            Reset();
        }

        public void Reset()
        {
            IsFinished = _frames.Count == 0;
            _hasAdvanced = false;
            _playhead = Reverse ? Duration : 0f;
            _nextIndex = Reverse ? _frames.Count - 1 : 0;
        }

        /// <summary>
        /// 运行中切换方向（Boss 二阶段）。会跳过当前 time 已执行过的帧，避免重复触发。
        /// </summary>
        public void SetDirection(bool reverse)
        {
            if (Reverse == reverse)
            {
                return;
            }

            Reverse = reverse;

            if (_frames.Count == 0)
            {
                return;
            }

            if (!_hasAdvanced)
            {
                Reset();
                return;
            }

            IsFinished = false;

            if (Reverse)
            {
                _nextIndex = _frames.Count - 1;
                while (_nextIndex >= 0 && _relativeTimes[_nextIndex] >= _playhead)
                {
                    _nextIndex--;
                }
            }
            else
            {
                _nextIndex = 0;
                while (_nextIndex < _frames.Count && _relativeTimes[_nextIndex] <= _playhead)
                {
                    _nextIndex++;
                }
            }

            if (!Loop)
            {
                if (!Reverse && _playhead >= Duration - Epsilon)
                {
                    IsFinished = true;
                }
                else if (Reverse && _playhead <= Epsilon)
                {
                    IsFinished = true;
                }
            }
        }

        /// <summary>
        /// 推进时间轴，把所有跨过的帧写入 output。
        /// output 会被清空；返回本次写入的帧数。
        /// </summary>
        public int AdvanceBy(float deltaSeconds, List<RecordFrame> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Clear();

            if (_frames.Count == 0)
            {
                IsFinished = true;
                return 0;
            }

            if (IsFinished)
            {
                return 0;
            }

            float delta = Mathf.Max(0f, deltaSeconds) * Mathf.Max(0f, Speed);
            _hasAdvanced = true;

            if (Reverse)
            {
                _playhead -= delta;
            }
            else
            {
                _playhead += delta;
            }

            bool wrapped = false;

            if (Duration <= 0f)
            {
                // 只有一帧：执行一次后结束，循环时也不做无意义的高速重放。
                _playhead = 0f;
            }
            else if (Loop)
            {
                if (_playhead > Duration || _playhead < 0f)
                {
                    _playhead = Mathf.Repeat(_playhead, Duration);
                    wrapped = true;
                }
            }
            else
            {
                _playhead = Mathf.Clamp(_playhead, 0f, Duration);
            }

            if (wrapped)
            {
                _nextIndex = Reverse ? _frames.Count - 1 : 0;
            }

            if (Reverse)
            {
                while (_nextIndex >= 0 && _relativeTimes[_nextIndex] >= _playhead - Epsilon)
                {
                    output.Add(_frames[_nextIndex]);
                    _nextIndex--;
                }
            }
            else
            {
                while (_nextIndex < _frames.Count && _relativeTimes[_nextIndex] <= _playhead + Epsilon)
                {
                    output.Add(_frames[_nextIndex]);
                    _nextIndex++;
                }
            }

            if (Duration <= 0f)
            {
                IsFinished = true;
            }
            else if (!Loop)
            {
                if (!Reverse && _playhead >= Duration - Epsilon)
                {
                    IsFinished = true;
                }
                else if (Reverse && _playhead <= Epsilon)
                {
                    IsFinished = true;
                }
            }

            return output.Count;
        }
    }
}
