using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal HUD for US-003: star counter, win/lose overlay with score, restart button.
/// (DESIGN §3 — full UI flow / level select is US-013.) Subscribes to the per-level
/// <see cref="GameEvents"/> bus the session owns; <see cref="Bind"/> is called after
/// every rebuild so stale subscriptions never survive a restart.
/// </summary>
public sealed class HudController : MonoBehaviour
{
    [SerializeField] Text starCounterText;
    [SerializeField] GameObject winOverlay;
    [SerializeField] Text winScoreText;
    [SerializeField] GameObject loseOverlay;
    [SerializeField] Text loseText;
    [SerializeField] Button restartButton;

    [Header("Beat tweens")]
    [SerializeField] float overlayFadeDuration = 0.35f;
    [SerializeField] float starPunchScale = 1.25f;
    [SerializeField] float starPunchDuration = 0.2f;

    int _totalStars;
    Vector3 _starCounterRestScale;

    /// <summary>Resolve inspector references by name when unset, so the gray-box scene
    /// works with zero manual wiring. Uses a recursive name search because the score text
    /// is nested under its overlay (transform.Find only checks direct children).</summary>
    void ResolveRefs()
    {
        if (starCounterText == null) starCounterText = FindDeep("StarCounter")?.GetComponent<Text>();
        if (winOverlay == null) winOverlay = FindDeep("WinOverlay")?.gameObject;
        if (winScoreText == null) winScoreText = FindDeep("WinScoreText")?.GetComponent<Text>();
        if (loseOverlay == null) loseOverlay = FindDeep("LoseOverlay")?.gameObject;
        if (loseText == null) loseText = FindDeep("LoseText")?.GetComponent<Text>();
        if (restartButton == null) restartButton = FindDeep("RestartButton")?.GetComponent<Button>();
    }

    /// <summary>Recursive descendant search by name (includes inactive children).</summary>
    Transform FindDeep(string name)
    {
        foreach (var t in transform.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    void SetRestartVisible(bool visible)
    {
        if (restartButton != null) restartButton.gameObject.SetActive(visible);
    }

    public void Bind(GameEvents events, int totalStars)
    {
        ResolveRefs();
        _totalStars = Mathf.Max(1, totalStars);
        _starCounterRestScale = starCounterText != null ? starCounterText.transform.localScale : Vector3.one;
        SetStarCount(0);
        HideOverlays();
        // The restart button only makes sense on the win/lose overlays — hide it during play.
        SetRestartVisible(false);
        if (restartButton != null) restartButton.onClick.RemoveAllListeners();
        // The button's listener is wired by GameSession when it holds the events bus.
    }

    /// <summary>Attach the restart button to the session (called by GameSession).</summary>
    public void WireRestartButton(UnityEngine.Events.UnityAction onRestart)
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(onRestart);
        }
    }

    public void SetStarCount(int collected)
    {
        if (starCounterText != null)
            starCounterText.text = $"{collected} / {_totalStars}";
        // Star-collect punch — a quick scale pop. PrimeTween is awaitable but this is fire-and-forget.
        if (collected > 0 && starCounterText != null)
            Tween.PunchScale(starCounterText.transform, Vector3.one * (starPunchScale - 1f), starPunchDuration, 4);
    }

    public void OnIntroComplete()
    {
        // Reserved for a "tap/swipe to start" hint in US-013; nothing for the gray box.
    }

    public async UniTask ShowWinAsync(LevelResult result, CancellationToken ct)
    {
        if (winScoreText != null)
            winScoreText.text = $"LEVEL CLEAR!\n\nStars {result.Stars}\nScore {result.Score}";
        await ShowOverlayAsync(winOverlay, ct);
    }

    public async UniTask ShowLoseAsync(CancellationToken ct)
    {
        if (loseText != null) loseText.text = "CANDY LOST!\n\nTap Restart";
        await ShowOverlayAsync(loseOverlay, ct);
    }

    public void HideOverlays()
    {
        if (winOverlay != null) winOverlay.SetActive(false);
        if (loseOverlay != null) loseOverlay.SetActive(false);
        SetRestartVisible(false);
        if (starCounterText != null) starCounterText.gameObject.SetActive(true);
    }

    async UniTask ShowOverlayAsync(GameObject overlay, CancellationToken ct)
    {
        if (overlay == null) return;
        overlay.SetActive(true);
        var cg = overlay.GetComponent<CanvasGroup>();
        if (cg == null) { cg = overlay.AddComponent<CanvasGroup>(); }
        cg.alpha = 0f;
        // Cancellation stops the fade tween; the awaiter resumes normally.
        using var reg = ct.Register(() => Tween.StopAll(cg));
        await Tween.Alpha(cg, 1f, overlayFadeDuration, Ease.OutCubic);
        cg.alpha = 1f;
        // Reveal the restart button once the overlay is up so the player can continue.
        SetRestartVisible(true);
    }
}