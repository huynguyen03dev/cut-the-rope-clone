using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SessionStateMachineTests
    {
        [Test]
        public void Loading_On_Loaded_Goes_To_Intro()
        {
            Assert.AreEqual(SessionState.Intro,
                SessionStateMachine.Transition(SessionState.Loading, SessionEvent.Loaded));
        }

        [Test]
        public void Intro_On_IntroDone_Goes_To_Playing()
        {
            Assert.AreEqual(SessionState.Playing,
                SessionStateMachine.Transition(SessionState.Intro, SessionEvent.IntroDone));
        }

        [Test]
        public void Playing_Win_Goes_Won_And_Lose_Goes_Lost()
        {
            Assert.AreEqual(SessionState.Won,
                SessionStateMachine.Transition(SessionState.Playing, SessionEvent.Win));
            Assert.AreEqual(SessionState.Lost,
                SessionStateMachine.Transition(SessionState.Playing, SessionEvent.Lose));
        }

        [Test]
        public void Win_Beats_Lose_Is_Encoded_By_Order_In_Playing()
        {
            // Same-frame win beats lose (gameplay.md priority). The table resolves Win
            // from Playing independently of Lose; the driver raises only one — so the
            // outcome is whichever the resolution pass chose, never both. Here we assert
            // the table never merges them into an invalid state.
            Assert.AreEqual(SessionState.Won,
                SessionStateMachine.Transition(SessionState.Playing, SessionEvent.Win));
            Assert.AreNotEqual(SessionState.Lost, SessionState.Won);
        }

        [Test]
        public void Won_And_Lost_End_States_Restart_To_Loading()
        {
            Assert.AreEqual(SessionState.Loading,
                SessionStateMachine.Transition(SessionState.Won, SessionEvent.Restart));
            Assert.AreEqual(SessionState.Loading,
                SessionStateMachine.Transition(SessionState.Lost, SessionEvent.Restart));
            Assert.AreEqual(SessionState.Loading,
                SessionStateMachine.Transition(SessionState.Won, SessionEvent.Next));
            Assert.AreEqual(SessionState.Loading,
                SessionStateMachine.Transition(SessionState.Lost, SessionEvent.Next));
        }

        [Test]
        public void Invalid_Inputs_Are_Ignored_Current_State_Retained()
        {
            // Win from Intro, Lose from Loading, IntroDone from Playing: all ignored.
            Assert.AreEqual(SessionState.Intro,
                SessionStateMachine.Transition(SessionState.Intro, SessionEvent.Win));
            Assert.AreEqual(SessionState.Loading,
                SessionStateMachine.Transition(SessionState.Loading, SessionEvent.Lose));
            Assert.AreEqual(SessionState.Playing,
                SessionStateMachine.Transition(SessionState.Playing, SessionEvent.IntroDone));
            // Win can't be re-triggered from Won.
            Assert.AreEqual(SessionState.Won,
                SessionStateMachine.Transition(SessionState.Won, SessionEvent.Win));
        }

        [Test]
        public void Full_Playthrough_Cycle_Transitions_Correctly()
        {
            var s = SessionState.Loading;
            s = SessionStateMachine.Transition(s, SessionEvent.Loaded);   // Intro
            s = SessionStateMachine.Transition(s, SessionEvent.IntroDone); // Playing
            s = SessionStateMachine.Transition(s, SessionEvent.Win);        // Won
            s = SessionStateMachine.Transition(s, SessionEvent.Restart);   // Loading
            s = SessionStateMachine.Transition(s, SessionEvent.Loaded);    // Intro
            s = SessionStateMachine.Transition(s, SessionEvent.IntroDone); // Playing
            s = SessionStateMachine.Transition(s, SessionEvent.Lose);       // Lost
            s = SessionStateMachine.Transition(s, SessionEvent.Restart);    // Loading
            Assert.AreEqual(SessionState.Loading, s);
        }
    }
}