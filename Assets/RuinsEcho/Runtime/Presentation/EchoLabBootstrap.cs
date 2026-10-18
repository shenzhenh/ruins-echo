using System;
using System.Collections.Generic;
using System.Linq;
using RuinsEcho.Core;
using RuinsEcho.Dungeon;
using RuinsEcho.Enemies;
using RuinsEcho.Player;
using RuinsEcho.Recording;
using RuinsEcho.Relics;
using RuinsEcho.Save;
using RuinsEcho.Spawning;
using UnityEngine;

namespace RuinsEcho.Presentation
{
    /// <summary>
    /// Echo Lab：无需场景 / Prefab 即可运行的技术切片。
    /// 验证“录制 3 秒 → 召唤影子 → 影子原样回放 → 遗物改变影子行为”的核心链路。
    /// 完整 5 层房间流程见 docs/roadmap.md M3。
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class EchoLabBootstrap : MonoBehaviour
    {
        public RelicManager Relics { get; private set; }
        public ShadowPool Pool { get; private set; }
        public PlayerStatus Player { get; private set; }
        public PlayerController Controller { get; private set; }
        public PlayerRecorder Recorder { get; private set; }
        public EchoLabHud Hud { get; private set; }
        public RunStateMachine State { get; private set; }
        public DungeonLayout Layout { get; private set; }
        public MetaProgress Meta { get; private set; }
        public CharacterData Character { get; private set; }
        public uint Seed { get; private set; }

        public int WaveIndex { get; private set; }
        public bool IsDead { get; private set; }
        public IReadOnlyList<RelicData> Choices => _choices;

        private readonly List<RelicData> _choices = new List<RelicData>();
        private bool _waveActive;
        private bool _restartQueued;
        private float _waveStartTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (FindObjectOfType<EchoLabBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("EchoLab");
            go.AddComponent<EchoLabBootstrap>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            Time.timeScale = 1f;
            Physics2D.queriesHitTriggers = true;

            State = new RunStateMachine();
            Character = CharacterCatalog.Hunter;
            Meta = MetaProgress.Load();

            GreyboxFactory.CreateCamera();
            GreyboxFactory.CreateArena(transform);

            Relics = gameObject.AddComponent<RelicManager>();
            Pool = gameObject.AddComponent<ShadowPool>();
            Pool.Factory = () => GreyboxFactory.CreateShadow(Pool.transform);

            BuildPlayer();
            BuildHud();
            StartRun();
        }

        private void BuildPlayer()
        {
            GameObject playerGo = GreyboxFactory.CreatePlayer(new Vector2(0f, -2f));

            Player = playerGo.AddComponent<PlayerStatus>();
            Recorder = playerGo.AddComponent<PlayerRecorder>();
            Controller = playerGo.AddComponent<PlayerController>();
            Controller.AimPivot = playerGo.transform.Find("AimPivot");

            Player.Initialize(Relics);
            Player.Died += OnPlayerDied;

            Recorder.Initialize(Controller, Player, Character);
            Controller.Initialize(Player, Recorder, Relics, Pool, Character);
            Controller.ToastRequested += ShowToast;
        }

        private void BuildHud()
        {
            Hud = gameObject.AddComponent<EchoLabHud>();
            Hud.Bind(this);
        }

        private void StartRun()
        {
            Seed = unchecked((uint)DateTime.UtcNow.Ticks);
            // Echo Lab 故意开放全部 9 件遗物用于验证；正式流程只从 Meta 解锁池抽取。
            Layout = DungeonGenerator.Generate(
                Seed,
                RelicCatalog.All.Select(relic => relic.relicId).ToList());

            State.Reset(GameState.MainMenu);
            State.TryTransition(GameState.RunPreparation);
            State.TryTransition(GameState.InRoomIdle);

            WaveIndex = 0;
            IsDead = false;
            _restartQueued = false;
            _waveActive = false;

            Player.transform.position = new Vector3(0f, -2f, 0f);
            Controller.ResetForNewRun();
            Recorder.SetFrozen(false);
            Controller.InputEnabled = true;

            SpawnWave();
        }

        private void Update()
        {
            if (IsDead)
            {
                if (!_restartQueued && Input.GetKeyDown(KeyCode.Return))
                {
                    _restartQueued = true;
                    RestartRun();
                }

                return;
            }

            if (_choices.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SelectRelic(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectRelic(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectRelic(2);
                return;
            }

            if (State.Current == GameState.InCombat && _waveActive && EnemyBase.AliveCount == 0)
            {
                OpenReward();
            }
        }

        private void SpawnWave()
        {
            ClearEnemies();

            if (State.Current == GameState.RoomReward)
            {
                State.TryTransition(GameState.InRoomIdle);
            }

            if (State.Current == GameState.InRoomIdle)
            {
                State.TryTransition(GameState.InCombat);
            }

            _choices.Clear();
            _waveActive = true;
            _waveStartTime = Time.time;
            Recorder.SetFrozen(false);
            Controller.InputEnabled = true;

            var rng = new DeterministicRandom(Seed ^ (uint)((WaveIndex + 1) * 2654435761u));
            int count = 3 + WaveIndex;

            for (int i = 0; i < count; i++)
            {
                float angle = ((Mathf.PI * 2f * i) / count) + rng.Range(-0.35f, 0.35f);
                float radiusX = rng.Range(3.5f, 7.5f);
                float radiusY = rng.Range(2.2f, 4.6f);
                Vector2 position = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);

                EnemyKind kind = WaveIndex > 0 && rng.Chance(0.25f)
                    ? EnemyKind.ShadowHunter
                    : EnemyKind.Melee;

                EnemyBase enemy = GreyboxFactory.CreateEnemy(kind, position, transform);
                float hp = 28f + (WaveIndex * 8f);
                float damage = 5f + (WaveIndex * 1.2f);
                enemy.Initialize(Player, Relics, kind, hp, damage, enemy.GetComponent<SpriteRenderer>());
            }

            ShowToast($"第 {WaveIndex + 1} 波：{count} 个敌人");
        }

        private void OpenReward()
        {
            if (!State.TryTransition(GameState.RoomReward))
            {
                return;
            }

            _waveActive = false;
            Recorder.SetFrozen(true);
            Controller.InputEnabled = false;

            if (!Relics.CanAddRelic)
            {
                ShowToast("遗物已达上限，无法继续拾取");
                StartNextWave();
                return;
            }

            List<string> owned = Relics.ActiveRelics
                .Where(relic => relic != null)
                .Select(relic => relic.relicId)
                .ToList();

            List<RelicData> drawn = RelicCatalog.GetRandomChoices(
                new DeterministicRandom(Seed ^ (uint)(WaveIndex + 101)),
                RelicCatalog.All.Select(relic => relic.relicId),
                owned,
                3);

            _choices.Clear();
            _choices.AddRange(drawn);

            if (_choices.Count == 0)
            {
                StartNextWave();
                return;
            }

            ShowToast("战斗结束：选择一件回响");
        }

        public void SelectRelic(int index)
        {
            if (index < 0 || index >= _choices.Count)
            {
                return;
            }

            RelicData relic = _choices[index];
            if (!Relics.AddRelic(relic))
            {
                ShowToast("无法拾取该遗物");
                return;
            }

            ShowToast($"获得回响：{relic.displayName}");
            _choices.Clear();
            State.TryTransition(GameState.InRoomIdle);
            Recorder.SetFrozen(false);
            Controller.InputEnabled = true;

            Invoke(nameof(StartNextWave), 0.25f);
        }

        private void StartNextWave()
        {
            if (IsDead)
            {
                return;
            }

            WaveIndex++;
            SpawnWave();
        }

        private void OnPlayerDied()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            State.TryTransition(GameState.Death);
            Recorder.SetFrozen(true);
            Controller.InputEnabled = false;
            Pool.ReleaseAll();
            _choices.Clear();
            ShowToast("本局结束：所有局内数据已销毁");
        }

        public void RestartRun()
        {
            ClearEnemies();
            Relics.ClearAllRelics();
            Pool.ReleaseAll();

            IsDead = false;
            _restartQueued = false;
            _choices.Clear();
            _waveActive = false;
            WaveIndex = 0;

            Player.transform.position = new Vector3(0f, -2f, 0f);
            Player.ResetForNewRun();
            Controller.ResetForNewRun();
            Recorder.ClearBuffer();

            State.Reset(GameState.MainMenu);
            State.TryTransition(GameState.RunPreparation);
            State.TryTransition(GameState.InRoomIdle);
            SpawnWave();
        }

        private void ClearEnemies()
        {
            EnemyBase[] enemies = EnemyBase.Active.ToArray();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null)
                {
                    continue;
                }

                enemies[i].gameObject.SetActive(false);
                Destroy(enemies[i].gameObject);
            }

            _waveActive = false;
        }

        public void ShowToast(string message)
        {
            Hud?.ShowToast(message);
        }

        public float WaveAge => _waveStartTime > 0f ? Time.time - _waveStartTime : 0f;

        private void OnDestroy()
        {
            if (Player != null)
            {
                Player.Died -= OnPlayerDied;
            }

            if (Controller != null)
            {
                Controller.ToastRequested -= ShowToast;
            }

            EnemyBase.Active.Clear();
            ShadowEcho.Active.Clear();
        }
    }
}
