using System.Collections.Generic;
using UnityEngine;

namespace Warehouse
{
    public enum ItemShape { Box, Cylinder, Capsule }
    public enum Surface { Cardboard, Wood, Plastic, Metal }

    /// <summary>Physical description of one item type. Sizes in metres, mass in kilograms.</summary>
    public sealed class ItemDef
    {
        public string Id;
        public string Label;
        public ItemShape Shape;
        public Vector3 Size;
        public float Mass;
        public Surface Surface;
        public Color Color;
    }

    /// <summary>All item types in the demo. Task JSON refers to items by <see cref="ItemDef.Id"/>.</summary>
    public static class ItemCatalog
    {
        static readonly Dictionary<string, ItemDef> _byId = new Dictionary<string, ItemDef>();
        static readonly Dictionary<Surface, PhysicsMaterial> _materials = new Dictionary<Surface, PhysicsMaterial>();

        public static readonly ItemDef[] All =
        {
            Def("box_small", "Small box", ItemShape.Box, new Vector3(0.30f, 0.30f, 0.30f), 2f, Surface.Cardboard, Hex("#C9A978")),
            Def("box_medium", "Medium box", ItemShape.Box, new Vector3(0.50f, 0.40f, 0.50f), 6f, Surface.Cardboard, Hex("#B8925E")),
            Def("box_large", "Large box", ItemShape.Box, new Vector3(0.80f, 0.60f, 0.80f), 15f, Surface.Cardboard, Hex("#A67F4B")),
            Def("crate", "Wooden crate", ItemShape.Box, new Vector3(0.60f, 0.50f, 0.60f), 20f, Surface.Wood, Hex("#7A5A3A")),
            Def("pallet", "Pallet", ItemShape.Box, new Vector3(1.20f, 0.14f, 0.80f), 25f, Surface.Wood, Hex("#9C7B55")),
            Def("can", "Paint can", ItemShape.Cylinder, new Vector3(0.14f, 0.18f, 0.14f), 0.9f, Surface.Metal, Hex("#8FA3B5")),
            Def("part", "Spare part", ItemShape.Box, new Vector3(0.16f, 0.16f, 0.16f), 0.5f, Surface.Plastic, Hex("#4F86C6")),
            Def("bottle", "Bottle", ItemShape.Capsule, new Vector3(0.10f, 0.30f, 0.10f), 0.8f, Surface.Plastic, Hex("#5FBF8F")),
        };

        public static ItemDef Get(string id) => _byId.TryGetValue(id, out var d) ? d : null;

        public static PhysicsMaterial Material(Surface s)
        {
            if (_materials.TryGetValue(s, out var m)) return m;
            m = s switch
            {
                Surface.Cardboard => Make("Cardboard", 0.65f, 0.60f, 0.05f),
                Surface.Wood => Make("Wood", 0.55f, 0.50f, 0.10f),
                Surface.Plastic => Make("Plastic", 0.40f, 0.35f, 0.25f),
                _ => Make("Metal", 0.35f, 0.30f, 0.15f),
            };
            _materials[s] = m;
            return m;
        }

        static PhysicsMaterial Make(string name, float staticFriction, float dynamicFriction, float bounce) =>
            new PhysicsMaterial(name)
            {
                staticFriction = staticFriction,
                dynamicFriction = dynamicFriction,
                bounciness = bounce,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

        static ItemDef Def(string id, string label, ItemShape shape, Vector3 size, float mass, Surface surface, Color color)
        {
            var d = new ItemDef { Id = id, Label = label, Shape = shape, Size = size, Mass = mass, Surface = surface, Color = color };
            _byId[id] = d;
            return d;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
