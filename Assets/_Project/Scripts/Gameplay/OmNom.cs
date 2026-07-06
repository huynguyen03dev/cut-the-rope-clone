using UnityEngine;

/// <summary>
/// Authoring marker for Om Nom's mouth (US-008 level pipeline). Identifies the single mouth
/// entity per level so the validation pass can assert "exactly one Om Nom", and draws an
/// editor gizmo so the mouth is visible while authoring. Runtime eat detection still runs via
/// <see cref="CandyInteractor"/>'s mouth layer mask (unchanged) — this component is the
/// authoring/validation identity and does not drive gameplay behavior in M2a. (M4's "mouth
/// opens / eat + gulp" game-feel can expand this component later.)
/// </summary>
public sealed class OmNom : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = new Color(1f, 0.3f, 0.6f, 0.35f);
        Gizmos.DrawSphere(p, 0.25f);
        Gizmos.color = new Color(1f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(p, 0.25f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(p + Vector3.up * 0.4f, "Om Nom");
#endif
    }
}
