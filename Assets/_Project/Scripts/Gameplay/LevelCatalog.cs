using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level catalog (docs/product/progression.md): boxes → level prefab references + star-unlock
/// thresholds. A single catalog asset is the source of truth for the level list. Saves
/// (US-012) key off each prefab's <see cref="Level.Id"/> GUID, never off catalog position, so
/// levels can be reordered/inserted without breaking persisted progress. The box/level-select
/// menu UI is US-013; this is the data + lookup API the validator (US-008) and future menu
/// consume.
/// </summary>
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Cut The Rope/Level Catalog", order = 0)]
public sealed class LevelCatalog : ScriptableObject
{
    [Tooltip("Boxes (worlds) in unlock order. Box 0 is unlocked from the start.")]
    [SerializeField] Box[] boxes = System.Array.Empty<Box>();

    /// <summary>Boxes in unlock order.</summary>
    public IReadOnlyList<Box> Boxes => boxes;

    /// <summary>Every non-null level prefab in catalog order (box 0 first). Used by the validator.</summary>
    public IEnumerable<Level> AllLevels()
    {
        if (boxes == null) yield break;
        for (int b = 0; b < boxes.Length; b++)
        {
            Level[] levels = boxes[b].levels;
            if (levels == null) continue;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null) yield return levels[i];
            }
        }
    }

    /// <summary>Find a level prefab by its stable GUID. Returns false on missing/empty id.</summary>
    public bool TryGetLevel(string levelId, out Level level)
    {
        level = null;
        if (string.IsNullOrEmpty(levelId)) return false;
        foreach (Level l in AllLevels())
        {
            if (l.Id == levelId) { level = l; return true; }
        }
        return false;
    }

    /// <summary>Find the box containing a given level id (for showing "Box N" in the menu).</summary>
    public bool TryGetBox(string levelId, out Box box)
    {
        box = default;
        if (boxes == null || string.IsNullOrEmpty(levelId)) return false;
        for (int b = 0; b < boxes.Length; b++)
        {
            Level[] levels = boxes[b].levels;
            if (levels == null) continue;
            for (int i = 0; i < levels.Length; i++)
            {
                Level l = levels[i];
                if (l != null && l.Id == levelId) { box = boxes[b]; return true; }
            }
        }
        return false;
    }

    /// <summary>A box (world): a set of levels unlocked together, with a star threshold.</summary>
    [System.Serializable]
    public struct Box
    {
        [Tooltip("Stable box id, e.g. 'box-01'.")]
        public string id;
        [Tooltip("Display name shown in the box-select menu (US-013).")]
        public string displayName;
        [Tooltip("Levels in this box, in play order.")]
        public Level[] levels;
        [Tooltip("Total stars required to unlock this box. 0 = unlocked from the start.")]
        public int starsToUnlock;
    }
}
