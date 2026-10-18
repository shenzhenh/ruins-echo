using System;
using RuinsEcho.Enemies;
using RuinsEcho.Recording;
using UnityEngine;

namespace RuinsEcho.Combat
{
    /// <summary>
    /// 跨实体的轻量战斗事件总线。遗物监听这些事件，而不是让影子直接依赖玩家。
    /// 静态事件在进入 Play Mode 时显式清空，避免 Domain Reload 关闭时残留。
    /// </summary>
    public static class CombatEvents
    {
        public static event Action<ShadowEcho, EnemyBase> ShadowHitEnemy;
        public static event Action<ShadowEcho> ShadowDied;

        public static void RaiseShadowHitEnemy(ShadowEcho shadow, EnemyBase enemy)
        {
            ShadowHitEnemy?.Invoke(shadow, enemy);
        }

        public static void RaiseShadowDied(ShadowEcho shadow)
        {
            ShadowDied?.Invoke(shadow);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ShadowHitEnemy = null;
            ShadowDied = null;
        }
    }
}
