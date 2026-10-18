using System;
using System.Collections;
using System.Collections.Generic;
using RuinsEcho.Combat;
using RuinsEcho.Core;
using RuinsEcho.Enemies;
using RuinsEcho.Player;
using RuinsEcho.Relics;
using RuinsEcho.Spawning;
using UnityEngine;

namespace RuinsEcho.Recording
{
    /// <summary>
    /// 影子分身（规格 4.2）。严格按录制时间戳回放 Transform，
    /// 不使用 Rigidbody2D 驱动位移；碰撞体只参与伤害查询。
    /// </summary>
    public sealed class ShadowEcho : MonoBehaviour, IDamageable
    {
        public static readonly List<ShadowEcho> Active = new List<ShadowEcho>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
        }

        public List<RecordFrame> playbackFrames = new List<RecordFrame>();
        public float playbackStartTime;
        public float HP;
        public bool isPlaying;

        /// <summary>Boss 扭曲力场可将其设为 0.5。</summary>
        public float PlaybackSpeedMultiplier = 1f;

        public bool IsAlive => HP > 0f;

        public float MaxHP => _maxHp;

        private readonly PlaybackCursor _cursor = new PlaybackCursor();
        private readonly List<RecordFrame> _dueFrames = new List<RecordFrame>(64);
        private readonly Collider2D[] _hitBuffer = new Collider2D[32];
        private readonly HashSet<EnemyBase> _hitEnemies = new HashSet<EnemyBase>();

        private float _maxHp;
        private float _inheritedDamage;
        private RelicManager _relics;
        private PlayerStatus _owner;
        private ShadowPool _pool;
        private CharacterData _character;
        private Vector2 _playbackOffset;
        private float _lastTickTime;
        private float _invulnerableUntil;
        private bool _returned;
        private SpriteRenderer _renderer;
        private Color _baseColor;

        public void Setup(
            List<RecordFrame> frames,
            float inheritedDamage,
            float maxHp,
            RelicManager relics,
            PlayerStatus owner,
            ShadowPool pool,
            Vector2 playbackOffset,
            CharacterData character)
        {
            playbackFrames.Clear();
            if (frames != null)
            {
                playbackFrames.AddRange(frames);
            }

            _relics = relics;
            _owner = owner;
            _pool = pool;
            _character = character;
            _playbackOffset = playbackOffset;
            _inheritedDamage = Mathf.Max(0f, inheritedDamage);
            _maxHp = Mathf.Max(1f, maxHp);
            HP = _maxHp;

            _returned = false;
            _invulnerableUntil = 0f;
            isPlaying = false;
            PlaybackSpeedMultiplier = 1f;

            _cursor.Load(playbackFrames);
            _cursor.Configure(false, relics != null && relics.Stats.LoopShadows, 1f);

            if (!Active.Contains(this))
            {
                Active.Add(this);
            }

            EnsureRenderer();
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
            }

            if (playbackFrames.Count > 0)
            {
                transform.position = playbackFrames[0].WorldPosition + _playbackOffset;
                transform.rotation = Quaternion.Euler(0f, 0f, playbackFrames[0].Rotation);
            }
            else
            {
                transform.position = (owner != null ? owner.transform.position : transform.position);
            }
        }

        public void StartPlayback()
        {
            if (playbackFrames.Count == 0)
            {
                ReturnToPool();
                return;
            }

            playbackStartTime = Time.unscaledTime;
            _lastTickTime = playbackStartTime;
            _cursor.Reset();
            isPlaying = true;

            _dueFrames.Clear();
            _cursor.AdvanceBy(0f, _dueFrames);
            ApplyFrames(_dueFrames);

            if (_cursor.IsFinished)
            {
                FinishPlayback();
            }
        }

        private void Update()
        {
            if (!isPlaying || !IsAlive)
            {
                return;
            }

            float now = Time.unscaledTime;
            float delta = Mathf.Max(0f, now - _lastTickTime);
            _lastTickTime = now;

            _cursor.Speed = Mathf.Max(0f, PlaybackSpeedMultiplier);
            _dueFrames.Clear();
            _cursor.AdvanceBy(delta, _dueFrames);
            ApplyFrames(_dueFrames);

            if (_cursor.IsFinished)
            {
                FinishPlayback();
            }
        }

        private void ApplyFrames(List<RecordFrame> frames)
        {
            if (frames == null)
            {
                return;
            }

            for (int i = 0; i < frames.Count; i++)
            {
                RecordFrame frame = frames[i];
                Vector2 position = frame.WorldPosition + _playbackOffset;

                float jitter = _relics != null ? _relics.Stats.ShadowPositionJitter : 0f;
                if (jitter > 0f)
                {
                    position += UnityEngine.Random.insideUnitCircle * jitter;
                }

                transform.position = position;
                transform.rotation = Quaternion.Euler(0f, 0f, frame.Rotation);

                if (frame.TriggerDodge)
                {
                    _invulnerableUntil = Mathf.Max(
                        _invulnerableUntil,
                        Time.unscaledTime + GameConfig.ShadowDodgeInvincibleTime);
                }

                if (frame.TriggerAttack)
                {
                    PerformAttack(frame.Rotation);
                }
            }
        }

        private void PerformAttack(float rotationDegrees)
        {
            Physics2D.SyncTransforms();

            float radians = rotationDegrees * Mathf.Deg2Rad;
            Vector2 aim = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 center = (Vector2)transform.position + (aim * GameConfig.ShadowAttackReach);
            float radius = GameConfig.ShadowAttackRadius
                           * (_relics != null ? _relics.Stats.ShadowAttackRadiusMultiplier : 1f);

            int count = Physics2D.OverlapCircleNonAlloc(center, radius, _hitBuffer);
            _hitEnemies.Clear();

            float critChance = _relics != null ? _relics.Stats.CritChance : GameConfig.BaseCritChance;

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hitBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive || !_hitEnemies.Add(enemy))
                {
                    continue;
                }

                bool critical = UnityEngine.Random.value < critChance;
                float damage = _inheritedDamage * (critical ? GameConfig.CritMultiplier : 1f);

                if (critical && _relics != null && _relics.Stats.ShadowCritMarks)
                {
                    enemy.ApplyMark(GameConfig.MarkDurationSeconds, _relics.Stats.MarkedDamageMultiplier);
                }

                enemy.TakeDamage(new DamageInfo(
                    damage,
                    gameObject,
                    critical,
                    DamageSource.Shadow,
                    enemy.transform.position));

                CombatEvents.RaiseShadowHitEnemy(this, enemy);
            }

            if (_relics != null && _relics.Stats.ShadowFriendlyFire && _owner != null && _owner.IsAlive)
            {
                float friendlyRadius = radius + 0.6f;
                if (Vector2.Distance(center, _owner.transform.position) <= friendlyRadius)
                {
                    _owner.TakeDamage(new DamageInfo(
                        _inheritedDamage,
                        gameObject,
                        false,
                        DamageSource.Shadow,
                        _owner.transform.position));
                }
            }
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0f)
            {
                return;
            }

            if (Time.unscaledTime < _invulnerableUntil)
            {
                return;
            }

            float multiplier = _character != null ? _character.shadowDamageTakenMultiplier : 1f;
            HP = Mathf.Max(0f, HP - (damage.Amount * Mathf.Max(0f, multiplier)));

            Flash();

            if (HP <= 0f)
            {
                isPlaying = false;
                CombatEvents.RaiseShadowDied(this);
                ReturnToPool();
            }
        }

        /// <summary>Boss 二阶段时序反转。</summary>
        public void SetPlaybackDirection(bool reverse)
        {
            _cursor.SetDirection(reverse);
        }

        public void ReturnToPool()
        {
            if (_returned)
            {
                return;
            }

            _returned = true;
            isPlaying = false;
            HP = 0f;
            Active.Remove(this);
            playbackFrames.Clear();

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void FinishPlayback()
        {
            if (_relics != null && _relics.Stats.LoopShadows && IsAlive)
            {
                _cursor.Reset();
                _lastTickTime = Time.unscaledTime;
                return;
            }

            ReturnToPool();
        }

        public static ShadowEcho GetNearest(Vector2 position)
        {
            ShadowEcho best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Active.Count; i++)
            {
                ShadowEcho shadow = Active[i];
                if (shadow == null || !shadow.IsAlive || !shadow.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = ((Vector2)shadow.transform.position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = shadow;
                }
            }

            return best;
        }

        public static int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Active.Count; i++)
                {
                    if (Active[i] != null && Active[i].IsAlive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void EnsureRenderer()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer != null)
                {
                    _baseColor = _renderer.color;
                }
            }
        }

        private void Flash()
        {
            EnsureRenderer();
            if (_renderer == null)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _renderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            if (_renderer != null)
            {
                _renderer.color = _baseColor;
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }
    }
}
