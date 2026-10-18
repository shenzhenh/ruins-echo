using System;
using System.Collections.Generic;
using RuinsEcho.Combat;
using RuinsEcho.Core;
using RuinsEcho.Enemies;
using RuinsEcho.Recording;
using RuinsEcho.Relics;
using RuinsEcho.Spawning;
using UnityEngine;

namespace RuinsEcho.Player
{
    /// <summary>
    /// 玩家输入与本体行为（规格 3）。移动使用 Transform，普攻 / 闪避为确定性时间逻辑；
    /// 影子回放绝不使用 Rigidbody2D 驱动。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("移动 / 瞄准")]
        public float MoveSpeed = GameConfig.BaseMoveSpeed;
        public Transform AimPivot;

        [Header("普攻")]
        public float AttackDamage = GameConfig.BaseAttackDamage;
        public float AttackWindup = GameConfig.AttackWindup;
        public float AttackRecovery = GameConfig.AttackRecovery;
        public float AttackRadius = GameConfig.AttackRadius;
        public float AttackReach = GameConfig.AttackReach;

        [Header("闪避")]
        public float DodgeDistance = GameConfig.DodgeDistance;
        public float DodgeDuration = GameConfig.DodgeDuration;
        public float DodgeInvincibleTime = GameConfig.DodgeInvincibleTime;
        public float DodgeCooldown = GameConfig.DodgeCooldown;

        public EchoEnergy Energy { get; private set; }
        public PlayerStatus Status { get; private set; }
        public PlayerRecorder Recorder { get; private set; }
        public RelicManager Relics { get; private set; }
        public ShadowPool ShadowPool { get; private set; }
        public CharacterData Character { get; private set; }

        public bool InputEnabled { get; set; } = true;

        public bool AttackTriggeredThisFrame { get; private set; }
        public bool DodgeTriggeredThisFrame { get; private set; }

        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public float AimAngle { get; private set; }

        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public event Action<string> ToastRequested;
        public event Action<ShadowEcho> ShadowSummoned;

        private readonly Collider2D[] _hitBuffer = new Collider2D[32];
        private readonly HashSet<EnemyBase> _hitEnemies = new HashSet<EnemyBase>();

        private float _dodgeTimer;
        private float _dodgeCooldownTimer;
        private float _invulnerableUntil;
        private Vector2 _dodgeDirection;

        private float _attackTimer = -1f;
        private bool _attackHitDone;
        private float _attackCooldownTimer;

        public void Initialize(
            PlayerStatus status,
            PlayerRecorder recorder,
            RelicManager relics,
            ShadowPool shadowPool,
            CharacterData character)
        {
            Status = status;
            Recorder = recorder;
            Relics = relics;
            ShadowPool = shadowPool;
            Character = character;

            Energy = new EchoEnergy();
            Status.Controller = this;

            if (Relics != null)
            {
                Relics.RelicsChanged += RefreshModifiers;
            }

            CombatEvents.ShadowHitEnemy += OnShadowHitEnemy;
            RefreshModifiers();
        }

        private void OnDestroy()
        {
            if (Relics != null)
            {
                Relics.RelicsChanged -= RefreshModifiers;
            }

            CombatEvents.ShadowHitEnemy -= OnShadowHitEnemy;
        }

        private void Update()
        {
            AttackTriggeredThisFrame = false;
            DodgeTriggeredThisFrame = false;

            if (!InputEnabled || Status == null || !Status.IsAlive)
            {
                return;
            }

            float dt = Time.deltaTime;
            Energy.Tick(dt);
            RefreshModifiers();

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - dt);
            _dodgeCooldownTimer = Mathf.Max(0f, _dodgeCooldownTimer - dt);

            HandleAim();
            TickAttack(dt);
            HandleAttackInput();
            HandleDodgeInput();
            TickDodge(dt);
            HandleSummonInput();
            HandleClearInput();
        }

        private void RefreshModifiers()
        {
            if (Energy == null)
            {
                return;
            }

            float relicRegen = Relics != null ? Relics.Stats.EnergyRegenMultiplier : 1f;
            float characterRegen = Character != null ? Character.energyRegenMultiplier : 1f;
            Energy.RegenMultiplier = relicRegen * characterRegen;
            Energy.CostMultiplier = Relics != null ? Relics.Stats.SummonCostMultiplier : 1f;
        }

        private void HandleAim()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 screen = Input.mousePosition;
            screen.z = -camera.transform.position.z;
            Vector2 world = camera.ScreenToWorldPoint(screen);
            Vector2 delta = world - (Vector2)transform.position;

            if (delta.sqrMagnitude < 0.0001f)
            {
                return;
            }

            AimDirection = delta.normalized;
            AimAngle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;

            if (AimPivot != null)
            {
                AimPivot.rotation = Quaternion.Euler(0f, 0f, AimAngle);
            }
        }

        private void HandleAttackInput()
        {
            if (!Input.GetMouseButtonDown(0) || _attackCooldownTimer > 0f || _attackTimer >= 0f)
            {
                return;
            }

            _attackTimer = 0f;
            _attackHitDone = false;
            _attackCooldownTimer = AttackWindup + AttackRecovery;
            AttackTriggeredThisFrame = true;
        }

        private void TickAttack(float dt)
        {
            if (_attackTimer < 0f)
            {
                return;
            }

            _attackTimer += dt;

            if (!_attackHitDone && _attackTimer >= AttackWindup)
            {
                _attackHitDone = true;
                PerformAttack();
            }

            if (_attackTimer >= AttackWindup + AttackRecovery)
            {
                _attackTimer = -1f;
            }
        }

        private void PerformAttack()
        {
            Physics2D.SyncTransforms();

            Vector2 center = (Vector2)transform.position + (AimDirection * AttackReach);
            int count = Physics2D.OverlapCircleNonAlloc(center, AttackRadius, _hitBuffer);

            _hitEnemies.Clear();
            float critChance = Relics != null ? Relics.Stats.CritChance : GameConfig.BaseCritChance;

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
                float damage = AttackDamage
                               * (critical ? GameConfig.CritMultiplier : 1f)
                               * enemy.MarkDamageMultiplier;

                enemy.TakeDamage(new DamageInfo(
                    damage,
                    gameObject,
                    critical,
                    DamageSource.Player,
                    enemy.transform.position));
            }
        }

        private void HandleDodgeInput()
        {
            if (!Input.GetKeyDown(KeyCode.Space) || _dodgeCooldownTimer > 0f || _dodgeTimer > 0f)
            {
                return;
            }

            Vector2 input = ReadMoveInput();
            _dodgeDirection = input.sqrMagnitude > 0.01f ? input : AimDirection;
            _dodgeTimer = DodgeDuration;
            _dodgeCooldownTimer = DodgeCooldown;
            _invulnerableUntil = Time.time + DodgeInvincibleTime;
            DodgeTriggeredThisFrame = true;
        }

        private void TickDodge(float dt)
        {
            if (_dodgeTimer <= 0f)
            {
                Vector2 input = ReadMoveInput();
                if (input.sqrMagnitude > 0.01f)
                {
                    transform.position = ArenaBounds.Clamp(
                        (Vector2)transform.position + (input * (MoveSpeed * dt)));
                }

                return;
            }

            float speed = DodgeDistance / Mathf.Max(0.01f, DodgeDuration);
            transform.position = ArenaBounds.Clamp(
                (Vector2)transform.position + (_dodgeDirection * (speed * dt)));

            _dodgeTimer = Mathf.Max(0f, _dodgeTimer - dt);
        }

        private static Vector2 ReadMoveInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            Vector2 input = new Vector2(x, y);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void HandleSummonInput()
        {
            if (!Input.GetKeyDown(KeyCode.Q))
            {
                return;
            }

            TrySummonShadow();
        }

        public bool TrySummonShadow()
        {
            if (Recorder == null || !Recorder.HasValidBuffer)
            {
                ToastRequested?.Invoke("没有录制动作，无法召唤影子");
                return false;
            }

            if (ShadowPool == null)
            {
                ToastRequested?.Invoke("影子对象池不可用");
                return false;
            }

            float cost = Energy.SummonCost;
            if (!Energy.TryConsume(cost))
            {
                ToastRequested?.Invoke("回响能量不足");
                return false;
            }

            int count = Relics != null ? Relics.Stats.ShadowsPerSummon : 1;
            List<RecordFrame> frames = Recorder.GetBufferCopy();
            float shadowHp = GameConfig.BaseShadowHP
                             * (Relics != null ? Relics.Stats.ShadowMaxHpMultiplier : 1f)
                             * (Character != null ? Character.shadowHpMultiplier : 1f);
            float inheritedDamage = AttackDamage
                                    * (Relics != null ? Relics.Stats.ShadowDamageMultiplier : 1f);

            Vector2 perpendicular = new Vector2(-AimDirection.y, AimDirection.x);
            int summoned = 0;

            for (int i = 0; i < count; i++)
            {
                ShadowEcho shadow = ShadowPool.Get();
                if (shadow == null)
                {
                    break;
                }

                float spread = (i - ((count - 1) * 0.5f)) * 0.45f;
                shadow.Setup(
                    frames,
                    inheritedDamage,
                    shadowHp,
                    Relics,
                    Status,
                    ShadowPool,
                    perpendicular * spread,
                    Character);
                shadow.StartPlayback();
                summoned++;

                ShadowSummoned?.Invoke(shadow);
            }

            if (summoned == 0)
            {
                Energy.Add(cost);
                ToastRequested?.Invoke("影子对象池已满");
                return false;
            }

            return true;
        }

        private void HandleClearInput()
        {
            if (!Input.GetKeyDown(KeyCode.R))
            {
                return;
            }

            Recorder?.ClearBuffer();
            ToastRequested?.Invoke("动作缓存已清空");
        }

        private void OnShadowHitEnemy(ShadowEcho shadow, EnemyBase enemy)
        {
            if (Relics == null || Relics.Stats.AttackCooldownRefund <= 0f)
            {
                return;
            }

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Relics.Stats.AttackCooldownRefund);
        }

        public void ResetForNewRun()
        {
            _dodgeTimer = 0f;
            _dodgeCooldownTimer = 0f;
            _invulnerableUntil = 0f;
            _attackTimer = -1f;
            _attackHitDone = false;
            _attackCooldownTimer = 0f;

            Energy.ResetForNewRun();
            Status.ResetForNewRun();
            Recorder?.ClearBuffer();
            RefreshModifiers();
        }
    }
}
