using NUnit.Framework;
using RuinsEcho.Core;
using RuinsEcho.Dungeon;

namespace RuinsEcho.Tests
{
    public sealed class RunStateMachineTests
    {
        [Test]
        public void InitialState_IsMainMenu()
        {
            var machine = new RunStateMachine();
            Assert.AreEqual(GameState.MainMenu, machine.Current);
        }

        [Test]
        public void InvalidTransition_IsRejected()
        {
            var machine = new RunStateMachine();
            Assert.IsFalse(machine.CanTransition(GameState.InCombat));
            Assert.IsFalse(machine.TryTransition(GameState.InCombat));
            Assert.AreEqual(GameState.MainMenu, machine.Current);
        }

        [Test]
        public void HappyPath_CombatRewardIdle()
        {
            var machine = new RunStateMachine();

            Assert.IsTrue(machine.TryTransition(GameState.RunPreparation));
            Assert.IsTrue(machine.TryTransition(GameState.InRoomIdle));
            Assert.IsTrue(machine.TryTransition(GameState.InCombat));
            Assert.IsTrue(machine.TryTransition(GameState.RoomReward));
            Assert.IsTrue(machine.TryTransition(GameState.InRoomIdle));

            Assert.AreEqual(GameState.InRoomIdle, machine.Current);
        }

        [Test]
        public void Death_IsReachableFromCombatAndCanRestart()
        {
            var machine = new RunStateMachine();
            machine.TryTransition(GameState.RunPreparation);
            machine.TryTransition(GameState.InRoomIdle);
            machine.TryTransition(GameState.InCombat);

            Assert.IsTrue(machine.TryTransition(GameState.Death));
            Assert.IsTrue(machine.TryTransition(GameState.RunPreparation));
        }

        [Test]
        public void StateChanged_FiresWithPreviousAndNext()
        {
            var machine = new RunStateMachine();
            GameState previous = GameState.Death;
            GameState next = GameState.Death;

            machine.StateChanged += (from, to) =>
            {
                previous = from;
                next = to;
            };

            machine.TryTransition(GameState.RunPreparation);

            Assert.AreEqual(GameState.MainMenu, previous);
            Assert.AreEqual(GameState.RunPreparation, next);
        }

        [Test]
        public void RunContext_EndRunInvalidatesInRunData()
        {
            var layout = new DungeonLayout();
            RunContext context = RunContext.Create(123u, CharacterCatalog.Hunter, layout);

            Assert.IsTrue(context.IsActive);
            Assert.AreEqual(GameState.RunPreparation, context.StateMachine.Current);

            context.EndRun(victory: false);

            Assert.IsFalse(context.IsActive);
            Assert.AreEqual(GameState.Death, context.StateMachine.Current);
            Assert.IsFalse(context.AdvanceLayer());
        }
    }
}
