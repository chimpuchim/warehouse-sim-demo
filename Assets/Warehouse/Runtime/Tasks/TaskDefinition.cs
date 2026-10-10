using System;
using System.Collections.Generic;

namespace Warehouse
{
    /// <summary>One required line of an order: item type id (see <see cref="ItemCatalog"/>) and count.</summary>
    [Serializable]
    public class RequiredItem
    {
        public string item;
        public int count;
    }

    /// <summary>
    /// A task as loaded from StreamingAssets/Tasks/*.json. Fields map 1:1 to the JSON keys, so a new
    /// task is a new file plus one line in tasks.json; no code changes.
    /// </summary>
    [Serializable]
    public class TaskDefinition
    {
        public const string Pick = "PICK";
        public const string Pack = "PACK";
        public const string Load = "LOAD";

        public string id;
        public string type;
        public string title;
        public string description;
        public float timeLimitSeconds;
        public int baseScore;
        public int timeBonusPerSecond;
        /// <summary>Zone id the items must end up in (tote_a, pack_box_a, truck_bed ...).</summary>
        public string targetZone;
        /// <summary>PACK only: the container must be closed after the items are inside.</summary>
        public bool requireClosed;
        public RequiredItem[] requiredItems;

        public bool IsValidType => type == Pick || type == Pack || type == Load;

        /// <summary>Structural checks that need no scene: types, item ids, counts, zone name.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(id)) errors.Add("id is missing");
            if (!IsValidType) errors.Add($"type must be PICK, PACK or LOAD (got '{type}')");
            if (string.IsNullOrWhiteSpace(targetZone)) errors.Add("targetZone is missing");
            if (timeLimitSeconds <= 0) errors.Add("timeLimitSeconds must be > 0");
            if (requiredItems == null || requiredItems.Length == 0) errors.Add("requiredItems is empty");
            else
                foreach (var r in requiredItems)
                {
                    if (ItemCatalog.Get(r.item) == null) errors.Add($"unknown item '{r.item}'");
                    if (r.count <= 0) errors.Add($"count for '{r.item}' must be > 0");
                }
            return errors;
        }
    }

    [Serializable]
    public class TaskManifest
    {
        public string[] tasks;
    }
}
