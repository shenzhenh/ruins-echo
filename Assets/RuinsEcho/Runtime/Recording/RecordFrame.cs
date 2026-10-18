using System;
using UnityEngine;

namespace RuinsEcho.Recording
{
    /// <summary>
    /// 单个录制快照。字段与规格 4.1 一致，使用 UnscaledTime 以保证
    /// Boss 时间扭曲 / 慢动作不会破坏回放时序。
    /// </summary>
    [Serializable]
    public struct RecordFrame
    {
        public float UnscaledTimestamp;
        public Vector2 WorldPosition;
        public float Rotation;
        public bool TriggerAttack;
        public bool TriggerDodge;

        public RecordFrame(
            float unscaledTimestamp,
            Vector2 worldPosition,
            float rotation,
            bool triggerAttack = false,
            bool triggerDodge = false)
        {
            UnscaledTimestamp = unscaledTimestamp;
            WorldPosition = worldPosition;
            Rotation = rotation;
            TriggerAttack = triggerAttack;
            TriggerDodge = triggerDodge;
        }
    }
}
