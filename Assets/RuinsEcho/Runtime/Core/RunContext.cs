using RuinsEcho.Dungeon;

namespace RuinsEcho.Core
{
    /// <summary>
    /// 一局游戏的运行时上下文（局内数据）。死亡 / 胜利后必须整体丢弃，
    /// 严禁把这里的任何字段写进局外存档。
    /// </summary>
    public sealed class RunContext
    {
        public uint Seed { get; }
        public CharacterData Character { get; }
        public DungeonLayout Dungeon { get; }
        public RunStateMachine StateMachine { get; } = new RunStateMachine();

        public int CurrentLayerIndex { get; private set; }
        public int FragmentsEarned { get; set; }
        public int RelicsTaken { get; set; }
        public bool IsActive { get; private set; }

        private RunContext(uint seed, CharacterData character, DungeonLayout dungeon)
        {
            Seed = seed;
            Character = character;
            Dungeon = dungeon;
            IsActive = true;
            StateMachine.Reset(GameState.RunPreparation);
        }

        public static RunContext Create(uint seed, CharacterData character, DungeonLayout dungeon)
        {
            return new RunContext(seed, character, dungeon);
        }

        public bool AdvanceLayer()
        {
            if (Dungeon == null || CurrentLayerIndex + 1 >= Dungeon.Layers.Count)
            {
                return false;
            }

            CurrentLayerIndex++;
            return true;
        }

        /// <summary>结束本局：状态机进入 Victory/Death，局内上下文标记为失效。</summary>
        public void EndRun(bool victory)
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            StateMachine.TryTransition(victory ? GameState.Victory : GameState.Death);
        }
    }
}
