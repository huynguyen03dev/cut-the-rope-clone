using UnityEngine;

/// <summary>
/// The logical playfield rect (US-001 rendering baseline). The camera keeps a
/// fixed logical height (orthographic size); width expands per aspect. Leaving
/// this rect is US-002's lose condition.
/// </summary>
public sealed class Playfield : MonoBehaviour
{
    [Tooltip("World-space size of the playfield, centered on this transform.")]
    [SerializeField] Vector2 size = new Vector2(14f, 11f);

    public Rect WorldRect
    {
        get
        {
            var center = (Vector2)transform.position;
            return new Rect(center - size * 0.5f, size);
        }
    }

    public bool Contains(Vector2 point) => WorldRect.Contains(point);

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
