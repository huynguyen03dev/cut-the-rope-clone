using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Every active pointer is an independent cutter with its own previous-position
/// state (gameplay.md input rules). The first frame of a touch has no previous
/// position, so no segment — the cut test is skipped that frame. Cuts are
/// tested against the interpolated rope the player sees.
/// </summary>
public sealed class SwipeCutter : MonoBehaviour
{
    [SerializeField] RopeSimulationDriver driver;
    [SerializeField] Camera worldCamera;

    const int MousePointerId = -1;
    readonly Dictionary<int, Vector2> _prevByPointer = new Dictionary<int, Vector2>();

    void OnEnable() => EnhancedTouchSupport.Enable();

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        _prevByPointer.Clear();
    }

    void Update()
    {
        float alpha = driver.Alpha;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.isPressed)
            {
                AdvancePointer(MousePointerId, ToWorld(mouse.position.ReadValue()), alpha);
            }
            else
            {
                _prevByPointer.Remove(MousePointerId);
            }
        }

        foreach (Touch touch in Touch.activeTouches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    AdvancePointer(touch.touchId, ToWorld(touch.screenPosition), alpha);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    _prevByPointer.Remove(touch.touchId);
                    break;
            }
        }
    }

    void AdvancePointer(int pointerId, Vector2 world, float alpha)
    {
        // Per-pointer UI occlusion (gameplay.md input rules): a swipe crossing HUD/UI
        // must not cut ropes. If the pointer is currently over a UI element, skip the
        // cut test this frame and drop its previous position so it can't carry a stale
        // segment across the UI region.
        EventSystem es = EventSystem.current;
        if (es != null && es.IsPointerOverGameObject(pointerId))
        {
            _prevByPointer.Remove(pointerId);
            return;
        }
        if (_prevByPointer.TryGetValue(pointerId, out Vector2 prev) && prev != world)
        {
            driver.CutAt(prev, world, alpha);
        }
        _prevByPointer[pointerId] = world;
    }

    Vector2 ToWorld(Vector2 screen)
    {
        return worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
    }
}
