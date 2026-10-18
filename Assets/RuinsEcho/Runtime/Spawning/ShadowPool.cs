using System;
using System.Collections.Generic;
using RuinsEcho.Core;
using RuinsEcho.Recording;
using UnityEngine;

namespace RuinsEcho.Spawning
{
    /// <summary>
    /// 影子对象池（规格 4.3 / 12）。预实例化上限 10，减少 GC。
    /// 工厂由 Bootstrap 注入，避免对象池依赖具体表现层。
    /// </summary>
    public sealed class ShadowPool : MonoBehaviour
    {
        public int MaxSize = GameConfig.ShadowPoolSize;

        [NonSerialized]
        public Func<ShadowEcho> Factory;

        private readonly Queue<ShadowEcho> _inactive = new Queue<ShadowEcho>(GameConfig.ShadowPoolSize);
        private readonly List<ShadowEcho> _all = new List<ShadowEcho>(GameConfig.ShadowPoolSize);

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _all.Count; i++)
                {
                    if (_all[i] != null && _all[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int TotalCount => _all.Count;

        public ShadowEcho Get()
        {
            while (_inactive.Count > 0)
            {
                ShadowEcho pooled = _inactive.Dequeue();
                if (pooled == null)
                {
                    continue;
                }

                pooled.gameObject.SetActive(true);
                return pooled;
            }

            if (_all.Count >= MaxSize)
            {
                return null;
            }

            ShadowEcho created = Factory != null ? Factory() : CreateFallback();
            if (created == null)
            {
                return null;
            }

            _all.Add(created);
            created.gameObject.SetActive(true);
            return created;
        }

        public void Release(ShadowEcho shadow)
        {
            if (shadow == null)
            {
                return;
            }

            shadow.gameObject.SetActive(false);

            if (!_inactive.Contains(shadow))
            {
                _inactive.Enqueue(shadow);
            }
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _all.Count; i++)
            {
                ShadowEcho shadow = _all[i];
                if (shadow == null)
                {
                    continue;
                }

                if (shadow.gameObject.activeSelf)
                {
                    // ReturnToPool 会从 ShadowEcho.Active 移除并清空帧，再回到本池。
                    shadow.ReturnToPool();
                }
                else
                {
                    Release(shadow);
                }
            }
        }

        private ShadowEcho CreateFallback()
        {
            GameObject go = new GameObject("ShadowEcho");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            return go.AddComponent<ShadowEcho>();
        }
    }
}
