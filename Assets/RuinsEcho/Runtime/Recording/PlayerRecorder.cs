using System;
using System.Collections.Generic;
using RuinsEcho.Core;
using RuinsEcho.Player;
using UnityEngine;

namespace RuinsEcho.Recording
{
    /// <summary>
    /// 每帧记录玩家状态（规格 4.1）。在 LateUpdate 记录，确保拿到本帧移动后的最终位置，
    /// 也避免与 PlayerController 的 Update 顺序耦合。
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class PlayerRecorder : MonoBehaviour
    {
        private PlayerController _controller;
        private PlayerStatus _status;
        private RecordBuffer _buffer = new RecordBuffer(GameConfig.RecordWindowSeconds);

        public RecordBuffer Buffer => _buffer;

        public bool IsFrozen { get; private set; }

        public bool HasValidBuffer => _buffer != null && _buffer.HasValidBuffer;

        public event Action BufferCleared;

        public void Initialize(PlayerController controller, PlayerStatus status, CharacterData character)
        {
            _controller = controller;
            _status = status;

            float window = GameConfig.RecordWindowSeconds
                           + (character != null ? character.recordDurationBonus : 0f);

            _buffer.SetMaxDuration(window);
            IsFrozen = false;
        }

        /// <summary>休憩密室 / 奖励界面冻结录制；离开后恢复。</summary>
        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
        }

        public void ClearBuffer()
        {
            _buffer.Clear();
            BufferCleared?.Invoke();
        }

        public List<RecordFrame> GetBufferCopy()
        {
            return _buffer.GetCopy();
        }

        public void SetCharacter(CharacterData character)
        {
            float window = GameConfig.RecordWindowSeconds
                           + (character != null ? character.recordDurationBonus : 0f);
            _buffer.SetMaxDuration(window);
        }

        private void LateUpdate()
        {
            if (_controller == null || _status == null || !_status.IsAlive || IsFrozen)
            {
                return;
            }

            RecordFrame frame = new RecordFrame(
                Time.unscaledTime,
                transform.position,
                _controller.AimAngle,
                _controller.AttackTriggeredThisFrame,
                _controller.DodgeTriggeredThisFrame);

            _buffer.Append(frame);
        }

        private void OnDestroy()
        {
            _buffer.Clear();
        }
    }
}
