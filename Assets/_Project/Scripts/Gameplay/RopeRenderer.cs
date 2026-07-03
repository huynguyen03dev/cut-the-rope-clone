using System.Collections.Generic;
using Game.Core;
using UnityEngine;

/// <summary>
/// Draws every rope in the simulation with one LineRenderer per rope, fed
/// interpolated positions. LineRenderers are created on demand (a cut adds a
/// rope) and per-frame work reuses them — no per-frame allocations (WebGL).
/// </summary>
[RequireComponent(typeof(RopeSimulationDriver))]
public sealed class RopeRenderer : MonoBehaviour
{
    [SerializeField] float width = 0.07f;
    [SerializeField] Color color = new Color(0.78f, 0.64f, 0.41f); // gray-box rope tan
    [SerializeField] string sortingLayer = "Rope";

    RopeSimulationDriver _driver;
    Material _material;
    readonly List<LineRenderer> _lines = new List<LineRenderer>();

    void Awake()
    {
        _driver = GetComponent<RopeSimulationDriver>();
        _material = new Material(Shader.Find("Sprites/Default"));
    }

    void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    void LateUpdate()
    {
        RopeSimulation sim = _driver.Sim;
        float alpha = _driver.Alpha;

        while (_lines.Count < sim.Ropes.Count) _lines.Add(CreateLine(_lines.Count));

        for (int r = 0; r < sim.Ropes.Count; r++)
        {
            Rope rope = sim.Ropes[r];
            LineRenderer line = _lines[r];
            int count = rope.Points.Length + (rope.AttachedToCandy ? 1 : 0);
            if (count < 2)
            {
                line.enabled = false; // bare anchor point — nothing to draw
                continue;
            }

            line.enabled = true;
            line.positionCount = count;
            for (int i = 0; i < rope.Points.Length; i++)
            {
                line.SetPosition(i, RopeSimulation.Interpolate(in rope.Points[i], alpha));
            }
            if (rope.AttachedToCandy)
            {
                line.SetPosition(count - 1, sim.CandyInterpolated(alpha));
            }
        }
    }

    LineRenderer CreateLine(int index)
    {
        var go = new GameObject($"RopeLine{index}");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = _material;
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.sortingLayerName = sortingLayer;
        line.useWorldSpace = true;
        return line;
    }
}
