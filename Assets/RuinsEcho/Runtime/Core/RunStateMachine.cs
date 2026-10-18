using System;
using System.Collections.Generic;

namespace RuinsEcho.Core
{
    /// <summary>
    /// 局内状态机。只负责状态合法性，不拥有任何场景对象。
    /// 规格 2.2：敌人清空 -> RoomReward；死亡 / 胜利 -> 结算并清空局内数据。
    /// </summary>
    public sealed class RunStateMachine
    {
        private static readonly IReadOnlyDictionary<GameState, GameState[]> AllowedTransitions =
            new Dictionary<GameState, GameState[]>
            {
                [GameState.MainMenu] = new[] { GameState.RunPreparation },
                [GameState.RunPreparation] = new[] { GameState.InRoomIdle, GameState.Death },
                [GameState.InRoomIdle] = new[]
                {
                    GameState.InCombat, GameState.EventDialog, GameState.BossFight,
                    GameState.RoomReward, GameState.Victory, GameState.Death, GameState.MainMenu
                },
                [GameState.InCombat] = new[] { GameState.RoomReward, GameState.Death, GameState.Victory },
                [GameState.RoomReward] = new[] { GameState.InRoomIdle, GameState.Death },
                [GameState.EventDialog] = new[] { GameState.InRoomIdle, GameState.Death },
                [GameState.BossFight] = new[] { GameState.RoomReward, GameState.Victory, GameState.Death },
                [GameState.Victory] = new[] { GameState.MainMenu, GameState.RunPreparation },
                [GameState.Death] = new[] { GameState.MainMenu, GameState.RunPreparation }
            };

        public GameState Current { get; private set; } = GameState.MainMenu;

        public event Action<GameState, GameState> StateChanged;

        public bool CanTransition(GameState next)
        {
            if (next == Current)
            {
                return false;
            }

            return AllowedTransitions.TryGetValue(Current, out GameState[] allowed)
                   && Array.IndexOf(allowed, next) >= 0;
        }

        public bool TryTransition(GameState next)
        {
            if (!CanTransition(next))
            {
                return false;
            }

            GameState previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }

        public void Reset(GameState state = GameState.MainMenu)
        {
            GameState previous = Current;
            Current = state;
            if (previous != state)
            {
                StateChanged?.Invoke(previous, state);
            }
        }
    }
}
