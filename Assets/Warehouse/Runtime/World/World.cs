using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    /// <summary>Everything the map builder spawned: items (tracked in the log), zones and containers.</summary>
    public sealed class World
    {
        public readonly List<Interactable> Items = new List<Interactable>();
        public readonly List<Zone> Zones = new List<Zone>();
        public readonly List<Container> Containers = new List<Container>();
        public Vector3 PlayerStart;
        public float PlayerStartYaw;

        public Zone FindZone(string id) => Zones.FirstOrDefault(z => z.Id == id);
        public Container FindContainer(Zone zone) => Containers.FirstOrDefault(c => c.Zone == zone);
        public Interactable FindItem(string name) => Items.FirstOrDefault(i => i.name == name);

        public int CountInZone(Zone zone, string itemId)
        {
            int n = 0;
            foreach (var item in Items)
                if (item.Def.Id == itemId && !item.IsHeld && zone.Contains(item)) n++;
            return n;
        }

        public void ResetItems()
        {
            foreach (var item in Items) item.ResetPose();
            foreach (var c in Containers) c.ResetLid();
        }
    }
}
