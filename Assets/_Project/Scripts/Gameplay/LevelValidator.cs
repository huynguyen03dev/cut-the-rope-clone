using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level authoring validation (US-008). Pure static API — no editor-only dependencies — so
/// the same checks run from EditMode tests now and from a future editor menu / batch CLI
/// (the story's harness-delta option). Returns a <see cref="LevelReport"/> with a pass/fail
/// flag and a list of human-readable errors; failures are meant to be surfaced loud.
///
/// Per-level rules (product contract): exactly one candy (<see cref="CandyFollower"/>),
/// exactly one Om Nom (<see cref="OmNom"/>), at least one rope (<see cref="RopeAuthoring"/>),
/// a sim driver present, and a non-empty stable GUID. Catalog rules: no null entries and
/// unique level IDs across the whole catalog (saves key off the GUID, so duplicates would
/// collide persisted progress — US-012).
/// </summary>
public static class LevelValidator
{
    public readonly struct LevelReport
    {
        public readonly bool Ok;
        public readonly IReadOnlyList<string> Errors;
        public LevelReport(bool ok, List<string> errors) { Ok = ok; Errors = errors ?? new List<string>(); }
    }

    /// <summary>Validates a single level instance/prefab against the authoring rules.</summary>
    public static LevelReport ValidateLevel(Level level)
    {
        var errors = new List<string>();
        if (level == null) return new LevelReport(false, new List<string> { "level is null" });

        if (string.IsNullOrEmpty(level.Id))
            errors.Add("Level has no id — the stable GUID is missing (saves key off it).");

        if (level.Driver == null)
            errors.Add("Level has no RopeSimulationDriver — the candy won't simulate.");

        int candy = level.GetComponentsInChildren<CandyFollower>(true).Length;
        if (candy == 0) errors.Add("Level has no candy (missing CandyFollower).");
        else if (candy > 1) errors.Add($"Level has {candy} candies — expected exactly one CandyFollower.");

        int mouths = level.GetComponentsInChildren<OmNom>(true).Length;
        if (mouths == 0) errors.Add("Level has no Om Nom (missing OmNom marker).");
        else if (mouths > 1) errors.Add($"Level has {mouths} Om Nom markers — expected exactly one.");

        var ropes = level.GetComponentsInChildren<RopeAuthoring>(true);
        if (ropes.Length == 0)
            errors.Add("Level has no ropes (missing RopeAuthoring).");
        for (int i = 0; i < ropes.Length; i++)
        {
            // [Range] enforces 4–24 in the inspector; double-check in case of bad serialized data.
            if (ropes[i].SegmentCount < 4 || ropes[i].SegmentCount > 24)
                errors.Add($"Rope '{ropes[i].gameObject.name}' segment count {ropes[i].SegmentCount} is out of range [4,24].");
        }

        return new LevelReport(errors.Count == 0, errors);
    }

    /// <summary>Validates the whole catalog: no null entries, unique level IDs, and every
    /// referenced level prefab passes <see cref="ValidateLevel"/>. Iterates the raw
    /// <see cref="LevelCatalog.Boxes"/> slots (not the null-filtering AllLevels) so a null
    /// prefab reference is itself flagged.</summary>
    public static LevelReport ValidateCatalog(LevelCatalog catalog)
    {
        var errors = new List<string>();
        if (catalog == null) return new LevelReport(false, new List<string> { "catalog is null" });

        var seen = new Dictionary<string, string>(); // id → prefab name (for the duplicate message)
        int index = 0;
        for (int b = 0; b < catalog.Boxes.Count; b++)
        {
            Level[] levels = catalog.Boxes[b].levels;
            if (levels == null) continue;
            for (int i = 0; i < levels.Length; i++)
            {
                Level level = levels[i];
                index++;
                if (level == null) { errors.Add($"Catalog entry #{index} has a null level prefab reference."); continue; }

                LevelReport rep = ValidateLevel(level);
                if (!rep.Ok)
                    foreach (string e in rep.Errors) errors.Add($"Level '{level.Id}' [{level.name}]: {e}");

                if (string.IsNullOrEmpty(level.Id)) continue; // already flagged above
                if (seen.TryGetValue(level.Id, out string first))
                    errors.Add($"Duplicate level id '{level.Id}' (also used by '{first}') — saves would collide.");
                else
                    seen[level.Id] = level.name;
            }
        }

        return new LevelReport(errors.Count == 0, errors);
    }
}
