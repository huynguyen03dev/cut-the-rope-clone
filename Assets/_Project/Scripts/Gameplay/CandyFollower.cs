using Game.Core;
using UnityEngine;

/// <summary>
/// Visual follower for the candy point: mirrors the interpolated sim position
/// and derives a swing rotation from the attached rope direction (velocity
/// direction while free-falling). A Verlet point has no angular state — this
/// is the deliberate system from DESIGN §2. Squash-and-stretch lands in US-015.
/// </summary>
public sealed class CandyFollower : MonoBehaviour
{
    [SerializeField] RopeSimulationDriver driver;
    [SerializeField] float rotationLerpSpeed = 12f;

    void LateUpdate()
    {
        RopeSimulation sim = driver.Sim;
        Vector2 pos = sim.CandyInterpolated(driver.Alpha);
        transform.position = new Vector3(pos.x, pos.y, 0f);

        Vector2 up = ComputeUp(sim);
        float angle = Mathf.Atan2(up.y, up.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            rotationLerpSpeed * Time.deltaTime);
    }

    static Vector2 ComputeUp(RopeSimulation sim)
    {
        // hanging: average direction from the candy toward its attached ropes
        Vector2 sum = Vector2.zero;
        int attached = 0;
        for (int r = 0; r < sim.Ropes.Count; r++)
        {
            Rope rope = sim.Ropes[r];
            if (!rope.AttachedToCandy) continue;
            sum += rope.Points[rope.Points.Length - 1].Pos - sim.Candy.Pos;
            attached++;
        }
        if (attached > 0 && sum.sqrMagnitude > 1e-6f) return sum.normalized;

        // free-falling: lean into the velocity
        Vector2 vel = sim.Candy.Pos - sim.Candy.PrevPos;
        if (vel.sqrMagnitude > 1e-8f) return -vel.normalized;

        return Vector2.up;
    }
}
