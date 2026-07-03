namespace Game.Core
{
    /// <summary>Game flow states (DESIGN §3 / docs/product/progression.md):
    /// Loading → Intro → Playing → Won | Lost → (restart/next).</summary>
    public enum SessionState { Loading, Intro, Playing, Won, Lost }

    /// <summary>Inputs that drive <see cref="SessionStateMachine"/> transitions.</summary>
    public enum SessionEvent { Loaded, IntroDone, Win, Lose, Restart, Next }

    /// <summary>
    /// Pure transition table for the game session state machine. Lives in Game.Core so
    /// the transition rules are unit-testable independent of Unity driver timing.
    /// Invalid inputs (e.g. Win from Intro) are ignored — the current state is returned.
    /// </summary>
    public static class SessionStateMachine
    {
        public static SessionState Transition(SessionState current, SessionEvent ev)
        {
            switch (current)
            {
                case SessionState.Loading:
                    return ev == SessionEvent.Loaded ? SessionState.Intro : current;
                case SessionState.Intro:
                    return ev == SessionEvent.IntroDone ? SessionState.Playing : current;
                case SessionState.Playing:
                    if (ev == SessionEvent.Win) return SessionState.Won;
                    if (ev == SessionEvent.Lose) return SessionState.Lost;
                    return current;
                case SessionState.Won:
                case SessionState.Lost:
                    return (ev == SessionEvent.Restart || ev == SessionEvent.Next) ? SessionState.Loading : current;
                default:
                    return current;
            }
        }
    }
}
