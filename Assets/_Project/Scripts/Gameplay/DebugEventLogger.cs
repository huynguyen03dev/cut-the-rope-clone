using Game.Core;
using UnityEngine;

/// <summary>
/// Gray-box observer: logs every gameplay event exactly as raised. This is the
/// integration-proof surface for US-002 (each interaction fires exactly one
/// event); real subscribers replace it from US-003 on.
/// </summary>
public sealed class DebugEventLogger : MonoBehaviour
{
    [SerializeField] RopeSimulationDriver driver;

    void Start()
    {
        GameEvents events = driver.Events;
        events.RopeCut += at => Debug.Log($"[GameEvents] RopeCut at {at}");
        events.RopeAttached += at => Debug.Log($"[GameEvents] RopeAttached (auto-grab) at {at}");
        events.CandyBubbled += at => Debug.Log($"[GameEvents] CandyBubbled at {at}");
        events.BubblePopped += at => Debug.Log($"[GameEvents] BubblePopped at {at}");
        events.StarCollected += star => Debug.Log($"[GameEvents] StarCollected: {star.name}");
        events.CandyEaten += () => Debug.Log("[GameEvents] CandyEaten (win)");
        events.CandyLost += () => Debug.Log("[GameEvents] CandyLost (lose)");
        events.LevelCompleted += r => Debug.Log($"[GameEvents] LevelCompleted stars={r.Stars} score={r.Score}");
    }
}
