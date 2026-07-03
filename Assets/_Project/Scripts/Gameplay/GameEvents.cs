using System;
using UnityEngine;

/// <summary>
/// Plain C# event channels (DESIGN §3): gameplay raises, UI/audio/save
/// subscribe. Instance-owned, never static — today the driver owns one per
/// scene load; GameSession takes ownership and rebuilds it per level in US-003.
/// </summary>
public sealed class GameEvents
{
    /// <summary>Raised at the cut position (audio/VFX subscribe in US-014/US-015).</summary>
    public event Action<Vector2> RopeCut;

    public event Action<GameObject> StarCollected;
    public event Action CandyEaten;
    public event Action CandyLost;

    public void RaiseRopeCut(Vector2 at) => RopeCut?.Invoke(at);
    public void RaiseStarCollected(GameObject star) => StarCollected?.Invoke(star);
    public void RaiseCandyEaten() => CandyEaten?.Invoke();
    public void RaiseCandyLost() => CandyLost?.Invoke();
}
