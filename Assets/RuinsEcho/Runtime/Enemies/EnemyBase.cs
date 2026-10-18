using System;
using System.Collections;
using System.Collections.Generic;
using RuinsEcho.Combat;
using RuinsEcho.Core;
using RuinsEcho.Player;
using RuinsEcho.Recording;
using RuinsEcho.Relics;
using UnityEngine;

namespace RuinsEcho.Enemies
{
    /// <summary>
    /// 基础敌人（规格 8.1）。MVP 技术切片实现近战追击与蚀影怪目标偏好；
    /// 远程杂兵 / Boss 在后续里程碑接入同一基类。
    /// </summary>
    public sealed class EnemyBase : MonoBehaviour, IDamageable
    {
        public static readonly List<EnemyBase> Active = new List<EnemyBase>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
        }

        [Header("身份")]
        public EnemyKind Kind = EnemyKind.Melee;

        [Header("属性")]
        public float MaxHP = 40f;
        public float HP = 40f;
        public float Damage = 8f;
        public float MoveSpeed = 2.2f;
        public float AttackRange = 1.1f;
        public float AttackCooldown = 1.2f;

        public bool IsAlive => HP > 0f;

        public float MarkDamageMultiplier => Time.time < _markUntil ? _markMultiplier : 1f;

        public event Action<EnemyBase> Died;

        private PlayerStatus _player;
        private RelicManager _relics;
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private float _attackTimer;
        private float _markUntil;
        private float _markMultiplier = 1f;
        private bool _dead;

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

        public void Initialize(
            PlayerStatus player,
            RelicManager relics,
            EnemyKind kind,
            float maxHp,
            float damage,
            SpriteRenderer renderer)
        {
            _player = player;
            _relics = relics;
            Kind = kind;
            MaxHP = Mathf.Max(1f, maxHp);
            HP = MaxHP;
            Damage = Mathf.Max(0f, damage);
            _renderer = renderer;

            if (_renderer != null)
            {
                _baseColor = _renderer.color;
            }

            _attackTimer = UnityEngine.Random.Range(0f, 0.4f);
            _markUntil = 0f;
            _markMultiplier = 1f;
            _dead = false;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            _attackTimer = Mathf.Max(0f, _attackTimer - Time.deltaTime);
            TickAI();
        }

        private void TickAI()
        {
            IDamageable target = SelectTarget(out Transform targetTransform);
            if (target == null || targetTransform == null)
            {
                return;
            }

            Vector2 position = transform.position;
            Vector2 delta = (Vector2)targetTransform.position - position;
            float distance = delta.magnitude;

            if (distance > AttackRange)
            {
                if (distance > 0.001f)
                {
                    Vector2 next = position + (delta / distance) * (MoveSpeed * Time.deltaTime);
                    transform.position = ArenaBounds.Clamp(next);
                }

                return;
            }

            if (_attackTimer > 0f)
            {
                return;
            }

            _attackTimer = AttackCooldown;

            bool shadowWasAlive = target is ShadowEcho shadow && shadow.IsAlive;
            target.TakeDamage(new DamageInfo(
                Damage,
                gameObject,
                false,
                DamageSource.Enemy,
                targetTransform.position));

            if (Kind == EnemyKind.ShadowHunter && shadowWasAlive && target is ShadowEcho killedShadow && !killedShadow.IsAlive)
            {
                _player?.Controller?.Energy.Drain(GameConfig.ShadowHunterEnergyPenalty);
            }
        }

        private IDamageable SelectTarget(out Transform targetTransform)
        {
            bool preferShadow = Kind == EnemyKind.ShadowHunter
                                || (_relics != null && _relics.Stats.EnemiesPreferShadows);

            if (preferShadow)
            {
                ShadowEcho shadow = ShadowEcho.GetNearest(transform.position);
                if (shadow != null && shadow.IsAlive)
                {
                    targetTransform = shadow.transform;
                    return shadow;
                }
            }

            if (_player != null && _player.IsAlive)
            {
                targetTransform = _player.transform;
                return _player;
            }

            targetTransform = null;
            return null;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0f)
            {
                return;
            }

            HP = Mathf.Max(0f, HP - damage.Amount);
            Flash();

            if (HP <= 0f)
            {
                Die();
            }
        }

        public void ApplyMark(float durationSeconds, float multiplier)
        {
            if (!IsAlive || durationSeconds <= 0f)
            {
                return;
            }

            _markUntil = Mathf.Max(_markUntil, Time.time + durationSeconds);
            _markMultiplier = Mathf.Max(_markMultiplier, multiplier);
        }

        private void Die()
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            HP = 0f;
            Active.Remove(this);
            Died?.Invoke(this);
            gameObject.SetActive(false);
            Destroy(gameObject, 0.05f);
        }

        private void Flash()
        {
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
            yield return new WaitForSeconds(0.06f);
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
