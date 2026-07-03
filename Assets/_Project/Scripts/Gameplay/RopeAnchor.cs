using UnityEngine;

/// <summary>Marks a transform as a rope anchor; the driver spawns one rope per
/// anchor toward the candy at load.</summary>
public sealed class RopeAnchor : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
