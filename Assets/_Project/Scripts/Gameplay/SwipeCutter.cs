using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Every active pointer is an independent gesture with its own state (gameplay.md input
/// rules). Tap vs swipe is resolved per pointer (DESIGN §3): a pointer stays a "tap
/// candidate" until it travels past the distance threshold, at which point it commits to
/// being a cutter and can only cut for the rest of its life; a pointer released while still
/// a candidate — quick and local — is a tap that pops a bubble (US-006). One touch is
/// therefore EITHER a cut OR a pop, never both. The first frame of a touch has no previous
/// position, so no cut segment. Cuts are tested against the interpolated rope the player
/// sees; tap/swipe classification uses screen pixels (the DPI-natural unit for touch).
/// </summary>
public sealed class SwipeCutter : MonoBehaviour
{
    [SerializeField] RopeSimulationDriver driver;
    [SerializeField] Camera worldCamera;

    [Header("Tap vs swipe (gameplay.md input rule #1)")]
    [Tooltip("Screen-pixel travel past which a pointer commits to a swipe (cutter). Below " +
             "this, a quick release is a tap that pops a bubble instead of cutting. Re-verify " +
             "on device at M2a exit — touch thresholds differ from mouse.")]
    [SerializeField] float swipeDistancePixels = 24f;
    [Tooltip("Max seconds a sub-threshold touch may last and still count as a tap.")]
    [SerializeField] float tapMaxDuration = 0.3f;

    const int MousePointerId = -1;
    readonly Dictionary<int, Pointer> _pointers = new Dictionary<int, Pointer>();

    /// <summary>Per-pointer gesture state. StartScreen/StartTime classify tap vs swipe;
    /// PrevWorld carries the cut segment once the pointer has committed to swiping.</summary>
    struct Pointer
    {
        public Vector2 StartScreen;
        public Vector2 PrevWorld;
        public float StartTime;
        public bool Swiping; // latched true once committed to cutting
    }

    void OnEnable() => EnhancedTouchSupport.Enable();

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        _pointers.Clear();
    }

    void Update()
    {
        float alpha = driver.Alpha;
        float now = Time.time;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 screen = mouse.position.ReadValue();
            if (mouse.leftButton.isPressed) Advance(MousePointerId, screen, alpha, now);
            else End(MousePointerId, screen, now);
        }

        foreach (Touch touch in Touch.activeTouches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    Advance(touch.touchId, touch.screenPosition, alpha, now);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    End(touch.touchId, touch.screenPosition, now);
                    break;
            }
        }
    }

    void Advance(int pointerId, Vector2 screen, float alpha, float now)
    {
        // Per-pointer UI occlusion (gameplay.md input rule #2): a pointer over UI neither
        // cuts nor taps. Drop its state so it can't carry a stale segment across the UI region.
        EventSystem es = EventSystem.current;
        if (es != null && es.IsPointerOverGameObject(pointerId))
        {
            _pointers.Remove(pointerId);
            return;
        }

        Vector2 world = ToWorld(screen);
        if (!_pointers.TryGetValue(pointerId, out Pointer p))
        {
            // First frame of this pointer: record the press origin. No segment yet — a tap
            // must not cut, and a swipe's first frame has no previous position either.
            _pointers[pointerId] = new Pointer
            {
                StartScreen = screen,
                PrevWorld = world,
                StartTime = now,
                Swiping = false,
            };
            return;
        }

        if (!p.Swiping && PointerGesture.HasCommittedToSwipe(p.StartScreen, screen, swipeDistancePixels))
        {
            p.Swiping = true; // latched: this pointer is now a cutter and can no longer pop
        }

        // Only a committed swipe cuts, so the small initial travel of a tap never severs a rope.
        if (p.Swiping && p.PrevWorld != world) driver.CutAt(p.PrevWorld, world, alpha);

        p.PrevWorld = world;
        _pointers[pointerId] = p;
    }

    void End(int pointerId, Vector2 screen, float now)
    {
        if (!_pointers.TryGetValue(pointerId, out Pointer p)) return;
        _pointers.Remove(pointerId);

        // A pointer that never committed to a swipe pops a bubble if the touch was a quick,
        // local tap — the guarantee that one touch is either a cut or a pop, never both.
        if (!p.Swiping &&
            PointerGesture.IsTap(p.StartScreen, screen, now - p.StartTime, swipeDistancePixels, tapMaxDuration))
        {
            Vector2 world = ToWorld(screen);
            // US-007: a tap is EITHER a cut OR a pop OR a puff — try the bubble first, then a
            // cushion. At most one fires (TryPopBubble returns false on a miss / no active
            // bubble, then TryPuffAirCushion checks the cushion mask). One touch, one effect.
            //
            // Priority when an active bubble and a cushion overlap at the tap point: the bubble
            // wins (it is attached to the candy and is the more salient target). gameplay.md does
            // not specify this ordering, but bubble-first matches the salience heuristic and keeps
            // the candy-held bubble poppable even when it overlaps a cushion.
            if (!driver.TryPopBubble(world)) driver.TryPuffAirCushion(world);
        }
    }

    Vector2 ToWorld(Vector2 screen)
    {
        return worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
    }
}
