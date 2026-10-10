using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Builds the single warehouse map from primitives: floor, walls, three racks with stock,
    /// a pick station with totes, a pack station with lidded boxes, a loading dock with a truck bed,
    /// and a staging pile. Static geometry uses box colliders; every item is a dynamic rigidbody
    /// with a box or capsule collider (no mesh colliders on moving bodies).
    /// </summary>
    public static class WarehouseMap
    {
        public const float Width = 30f;   // x
        public const float Depth = 20f;   // z
        const float WallHeight = 4f;

        static Transform _root;
        static World _world;
        static Material _cardboardLabel;

        public static World Build()
        {
            _world = new World();
            _root = new GameObject("Warehouse").transform;

            BuildShell();
            BuildRacks();
            BuildPickStation();
            BuildPackStation();
            BuildLoadingDock();
            BuildStagingPile();

            _world.PlayerStart = new Vector3(-6f, 0f, -8.5f);
            _world.PlayerStartYaw = 0f;
            return _world;
        }

        // ---------- shell ----------

        static void BuildShell()
        {
            var floorMat = Mats.Checker(new Color(0.42f, 0.43f, 0.45f), new Color(0.36f, 0.37f, 0.39f), 1f, new Vector2(Width, Depth));
            var floor = Static("Floor", new Vector3(0, -0.05f, 0), new Vector3(Width, 0.1f, Depth), floorMat);
            floor.GetComponent<Collider>().sharedMaterial = ItemCatalog.Material(Surface.Wood);

            var wallMat = Mats.Solid(new Color(0.78f, 0.80f, 0.82f), 0.05f);
            Static("Wall N", new Vector3(0, WallHeight / 2, Depth / 2 + 0.25f), new Vector3(Width + 1f, WallHeight, 0.5f), wallMat);
            Static("Wall S", new Vector3(0, WallHeight / 2, -Depth / 2 - 0.25f), new Vector3(Width + 1f, WallHeight, 0.5f), wallMat);
            Static("Wall E", new Vector3(Width / 2 + 0.25f, WallHeight / 2, 0), new Vector3(0.5f, WallHeight, Depth), wallMat);
            Static("Wall W", new Vector3(-Width / 2 - 0.25f, WallHeight / 2, 0), new Vector3(0.5f, WallHeight, Depth), wallMat);

            var stripe = Mats.Solid(new Color(0.95f, 0.78f, 0.15f), 0.3f);
            Static("Lane line", new Vector3(3.0f, 0.005f, 0), new Vector3(0.12f, 0.01f, Depth), stripe).GetComponent<Collider>().enabled = false;
            Static("Dock line", new Vector3(9.0f, 0.005f, 0), new Vector3(0.12f, 0.01f, Depth), stripe).GetComponent<Collider>().enabled = false;

            // Simple ceiling beams for a sense of scale (no collider).
            var beamMat = Mats.Solid(new Color(0.30f, 0.32f, 0.36f), 0.2f);
            for (int i = -2; i <= 2; i++)
                Static("Beam", new Vector3(i * 6f, WallHeight - 0.2f, 0), new Vector3(0.3f, 0.4f, Depth), beamMat).GetComponent<Collider>().enabled = false;
        }

        // ---------- racks with stock ----------

        static void BuildRacks()
        {
            var steel = Mats.Solid(new Color(0.20f, 0.45f, 0.75f), 0.4f);
            var plate = Mats.Solid(new Color(0.55f, 0.56f, 0.58f), 0.3f);
            float[] racksX = { -11f, -6f, -1f };
            string[] names = { "A", "B", "C" };
            const float length = 6f, depth = 1.0f;
            float[] levels = { 0.06f, 0.95f, 1.80f };

            for (int r = 0; r < racksX.Length; r++)
            {
                float x = racksX[r];
                var rack = new GameObject("Rack " + names[r]).transform;
                rack.SetParent(_root, false);
                foreach (float sx in new[] { -depth / 2, depth / 2 })
                foreach (float sz in new[] { -length / 2, length / 2 })
                    Static("Upright", new Vector3(x + sx, 1.2f, sz), new Vector3(0.08f, 2.4f, 0.08f), steel, rack);
                foreach (float y in levels)
                    Static("Shelf", new Vector3(x, y - 0.03f, 0), new Vector3(depth, 0.06f, length), plate, rack);
                Static("Sign", new Vector3(x, 2.55f, 0), new Vector3(depth * 0.8f, 0.3f, 0.05f), Mats.Solid(new Color(0.9f, 0.3f, 0.2f)), rack).GetComponent<Collider>().enabled = false;
            }

            // Rack A: heavy stock low, boxes above.
            RowOnShelf("box_large", -11f, levels[0], new[] { -2.2f, -1.1f, 0f }, 0f);
            RowOnShelf("crate", -11f, levels[0], new[] { 1.3f, 2.3f }, 0f);
            RowOnShelf("box_medium", -11f, levels[1], new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f }, 0f);
            RowOnShelf("box_small", -11f, levels[2], new[] { -2.5f, -1.5f, -0.5f, 0.5f, 1.5f, 2.5f }, 0f);

            // Rack B: mixed.
            RowOnShelf("crate", -6f, levels[0], new[] { -2.3f, -1.4f }, 0f);
            RowOnShelf("box_large", -6f, levels[0], new[] { 0.3f, 1.6f }, 0f);
            RowOnShelf("box_medium", -6f, levels[1], new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f }, 0f);
            RowOnShelf("can", -6f, levels[2], new[] { -2.6f, -2.2f, -1.8f, -1.4f }, -0.25f);
            RowOnShelf("part", -6f, levels[2], new[] { -0.6f, -0.2f, 0.2f, 0.6f }, 0f);
            RowOnShelf("bottle", -6f, levels[2], new[] { 1.6f, 1.9f, 2.2f, 2.5f }, 0.25f);

            // Rack C: small goods.
            RowOnShelf("box_medium", -1f, levels[0], new[] { -2.3f, -1.2f, -0.1f }, 0f);
            RowOnShelf("box_small", -1f, levels[0], new[] { 1.2f, 1.8f, 2.4f }, 0f);
            RowOnShelf("box_small", -1f, levels[1], new[] { -2.5f, -1.5f, -0.5f, 0.5f, 1.5f, 2.5f }, 0f);
            RowOnShelf("can", -1f, levels[2], new[] { -2.6f, -2.2f, -1.8f, -1.4f, -1.0f }, 0f);
            RowOnShelf("part", -1f, levels[2], new[] { 0.4f, 0.8f, 1.2f, 1.6f, 2.0f, 2.4f }, 0f);
        }

        static void RowOnShelf(string itemId, float x, float shelfY, float[] zs, float xOffset)
        {
            foreach (float z in zs) Spawn(itemId, new Vector3(x + xOffset, shelfY, z), 0f);
        }

        // ---------- stations ----------

        static void BuildPickStation()
        {
            var tableMat = Mats.Solid(new Color(0.35f, 0.30f, 0.26f), 0.3f);
            Table(new Vector3(-8f, 0, -7f), tableMat);
            Table(new Vector3(-4f, 0, -7f), tableMat);
            Tote("tote_a", "Tote A", new Vector3(-8f, 0.8f, -7f), new Color(0.15f, 0.55f, 0.85f));
            Tote("tote_b", "Tote B", new Vector3(-4f, 0.8f, -7f), new Color(0.85f, 0.45f, 0.15f));
        }

        static void BuildPackStation()
        {
            var tableMat = Mats.Solid(new Color(0.35f, 0.30f, 0.26f), 0.3f);
            Table(new Vector3(5f, 0, -7f), tableMat);
            PackBox("pack_box_a", "Pack box A", new Vector3(5f, 0.8f, -7f));
            PackBox("pack_box_b", "Pack box B", new Vector3(5f, 0f, -4.5f));
        }

        static void BuildLoadingDock()
        {
            var bedMat = Mats.Solid(new Color(0.25f, 0.26f, 0.28f), 0.2f);
            var sideMat = Mats.Solid(new Color(0.85f, 0.85f, 0.88f), 0.3f);
            const float bedY = 0.6f, bedW = 2.4f, bedL = 5f, wallH = 2.3f; // 2.3 m clearance: the 1.8 m player walks in upright
            var center = new Vector3(12.5f, 0, 2f);
            var truck = new GameObject("Truck").transform;
            truck.SetParent(_root, false);
            Static("Bed", center + new Vector3(0, bedY - 0.1f, 0), new Vector3(bedW, 0.2f, bedL), bedMat, truck);
            Static("Side L", center + new Vector3(-bedW / 2 - 0.05f, bedY + wallH / 2, 0), new Vector3(0.1f, wallH, bedL), sideMat, truck);
            Static("Side R", center + new Vector3(bedW / 2 + 0.05f, bedY + wallH / 2, 0), new Vector3(0.1f, wallH, bedL), sideMat, truck);
            Static("Front", center + new Vector3(0, bedY + wallH / 2, bedL / 2 + 0.05f), new Vector3(bedW + 0.2f, wallH, 0.1f), sideMat, truck);
            Static("Roof", center + new Vector3(0, bedY + wallH + 0.05f, 0), new Vector3(bedW + 0.2f, 0.1f, bedL), sideMat, truck);
            Static("Cab", center + new Vector3(0, 1.1f, bedL / 2 + 1.0f), new Vector3(bedW, 2.2f, 1.8f), Mats.Solid(new Color(0.75f, 0.15f, 0.15f), 0.5f), truck);
            foreach (float sx in new[] { -0.9f, 0.9f })
            foreach (float sz in new[] { -1.6f, 1.6f, 3.0f })
                Static("Wheel", center + new Vector3(sx, 0.35f, sz), new Vector3(0.3f, 0.7f, 0.7f), Mats.Solid(Color.black, 0.2f), truck).GetComponent<Collider>().enabled = false;

            // Ramp up to the tail of the bed.
            const float rampL = 3.0f;
            float angle = Mathf.Atan2(bedY, rampL) * Mathf.Rad2Deg;
            var ramp = Static("Ramp", center + new Vector3(0, bedY / 2, -bedL / 2 - rampL / 2), new Vector3(bedW, 0.08f, Mathf.Sqrt(rampL * rampL + bedY * bedY)), bedMat, truck);
            ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);

            var zone = new GameObject("Zone truck_bed").AddComponent<Zone>();
            zone.transform.SetParent(truck, false);
            zone.Init("truck_bed", center + new Vector3(0, bedY + 0.9f, 0), new Vector3(bedW, 1.8f, bedL));
            _world.Zones.Add(zone);
            Label("TRUCK", center + new Vector3(-bedW / 2 - 0.2f, bedY + wallH + 0.3f, 0), Quaternion.Euler(0, 90, 0));
        }

        static void BuildStagingPile()
        {
            var pallet = Spawn("pallet", new Vector3(7.5f, 0f, 3f), 0f);
            float top = pallet.Def.Size.y;
            Spawn("box_large", new Vector3(7.3f, top, 2.8f), 0f);
            Spawn("box_large", new Vector3(7.3f, top + 0.6f, 2.8f), 0f);
            Spawn("crate", new Vector3(8.1f, top, 3.2f), 0f);
            Spawn("crate", new Vector3(8.1f, top + 0.5f, 3.2f), 0f);
            Spawn("pallet", new Vector3(7.5f, 0f, 6f), 0f);
            Spawn("box_large", new Vector3(7.5f, top, 6f), 0f);
            Spawn("box_medium", new Vector3(7.3f, top + 0.6f, 5.8f), 0f);
            Spawn("box_medium", new Vector3(7.8f, top + 0.6f, 6.3f), 0f);
            Spawn("box_medium", new Vector3(9.5f, 0f, 4.5f), 20f);
            Spawn("box_small", new Vector3(9.5f, 0.4f, 4.5f), 35f);
            Spawn("crate", new Vector3(6.0f, 0f, 8.0f), 10f);
            Spawn("box_large", new Vector3(6.0f, 0.5f, 8.0f), 10f);
        }

        // ---------- helpers ----------

        static void Table(Vector3 at, Material mat)
        {
            var t = new GameObject("Table").transform;
            t.SetParent(_root, false);
            Static("Top", at + new Vector3(0, 0.78f, 0), new Vector3(1.6f, 0.04f, 0.9f), mat, t);
            foreach (float sx in new[] { -0.7f, 0.7f })
            foreach (float sz in new[] { -0.35f, 0.35f })
                Static("Leg", at + new Vector3(sx, 0.38f, sz), new Vector3(0.06f, 0.76f, 0.06f), mat, t);
        }

        /// <summary>Open-top bin made of five static plates; the zone is the inside volume.</summary>
        static void Tote(string id, string label, Vector3 at, Color color)
        {
            var size = new Vector3(0.8f, 0.4f, 0.6f);
            var mat = Mats.Solid(color, 0.35f);
            var t = OpenBox(label, at, size, 0.025f, mat);
            // The zone reaches 35 cm above the rim so items stacked in a full tote still count.
            var zone = t.gameObject.AddComponent<Zone>();
            zone.Init(id, at + new Vector3(0, (size.y + 0.35f) / 2, 0), new Vector3(size.x - 0.05f, size.y + 0.35f, size.z - 0.05f));
            _world.Zones.Add(zone);
            // Stations sit along the south wall, so the player reads the sign from the north side.
            Label(label.ToUpperInvariant(), at + new Vector3(0, size.y * 0.5f, size.z / 2 + 0.01f), Quaternion.Euler(0, 180, 0), 0.16f, Color.white);
        }

        /// <summary>Open box with a hinged lid plate; PACK tasks need it closed.</summary>
        static void PackBox(string id, string label, Vector3 at)
        {
            var size = new Vector3(0.7f, 0.5f, 0.6f);
            var mat = Mats.Solid(new Color(0.72f, 0.58f, 0.40f), 0.2f);
            var t = OpenBox(label, at, size, 0.03f, mat);
            var zone = t.gameObject.AddComponent<Zone>();
            zone.Init(id, at + new Vector3(0, (size.y + 0.25f) / 2, 0), new Vector3(size.x - 0.06f, size.y + 0.25f, size.z - 0.06f));
            _world.Zones.Add(zone);

            var hinge = new GameObject("Lid hinge").transform;
            hinge.SetParent(t, false);
            hinge.position = at + new Vector3(0, size.y, size.z / 2);
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Lid";
            lid.transform.SetParent(hinge, false);
            lid.transform.localPosition = new Vector3(0, 0.015f, -size.z / 2);
            lid.transform.localScale = new Vector3(size.x, 0.03f, size.z);
            lid.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(lid.GetComponent<Collider>());
            hinge.localRotation = Quaternion.Euler(-110f, 0, 0);

            var container = t.gameObject.AddComponent<Container>();
            container.Init(zone, hinge, label);
            _world.Containers.Add(container);
            Label(label.ToUpperInvariant(), at + new Vector3(0, size.y * 0.5f, size.z / 2 + 0.01f), Quaternion.Euler(0, 180, 0), 0.16f, Color.white);
        }

        static Transform OpenBox(string name, Vector3 at, Vector3 size, float wall, Material mat)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_root, false);
            t.position = at;
            Static("Bottom", at + new Vector3(0, wall / 2, 0), new Vector3(size.x, wall, size.z), mat, t);
            Static("Wall", at + new Vector3(-size.x / 2 + wall / 2, size.y / 2, 0), new Vector3(wall, size.y, size.z), mat, t);
            Static("Wall", at + new Vector3(size.x / 2 - wall / 2, size.y / 2, 0), new Vector3(wall, size.y, size.z), mat, t);
            Static("Wall", at + new Vector3(0, size.y / 2, -size.z / 2 + wall / 2), new Vector3(size.x, size.y, wall), mat, t);
            Static("Wall", at + new Vector3(0, size.y / 2, size.z / 2 - wall / 2), new Vector3(size.x, size.y, wall), mat, t);
            return t;
        }

        static GameObject Static(string name, Vector3 center, Vector3 size, Material mat, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : _root, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>World-space sign using a small uGUI canvas (no extra shaders needed).</summary>
        static void Label(string text, Vector3 at, Quaternion rot, float size = 0.4f, Color? color = null)
        {
            var go = new GameObject("Label " + text, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(at, rot);
            go.transform.localScale = Vector3.one * (size / 40f);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 60);
            var child = new GameObject("Text", typeof(RectTransform));
            child.transform.SetParent(go.transform, false);
            var crt = child.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var t = child.AddComponent<UnityEngine.UI.Text>();
            t.font = UI.Font;
            t.fontSize = 44;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color ?? new Color(0.08f, 0.08f, 0.08f);
            t.text = text;
        }

        /// <summary>Spawns an item resting on the given surface point (y = bottom of the item).</summary>
        public static Interactable Spawn(string itemId, Vector3 bottomCenter, float yawDegrees)
        {
            var def = ItemCatalog.Get(itemId);
            if (def == null) throw new System.ArgumentException("Unknown item " + itemId);
            int n = _world.Items.FindAll(i => i.Def == def).Count + 1;

            var go = GameObject.CreatePrimitive(def.Shape switch
            {
                ItemShape.Cylinder => PrimitiveType.Cylinder,
                ItemShape.Capsule => PrimitiveType.Capsule,
                _ => PrimitiveType.Cube,
            });
            go.name = $"{def.Id}_{n:00}";
            go.transform.SetParent(_root, false);
            go.transform.position = bottomCenter + new Vector3(0, def.Size.y / 2 + 0.002f, 0);
            go.transform.rotation = Quaternion.Euler(0, yawDegrees, 0);
            // Unity's cylinder and capsule primitives are 2 units tall at scale 1.
            float yScale = def.Shape == ItemShape.Box ? def.Size.y : def.Size.y / 2f;
            go.transform.localScale = new Vector3(def.Size.x, yScale, def.Size.z);
            go.GetComponent<Renderer>().sharedMaterial = ItemMaterial(def);

            // Cylinders get a box collider: the capsule the primitive ships with would roll on its end caps.
            // Replace the primitive's collider with one added explicitly: code stripping removes
            // collider classes that are only ever created through CreatePrimitive.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Collider col;
            if (def.Shape == ItemShape.Capsule)
            {
                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                col = capsule;
            }
            else col = go.AddComponent<BoxCollider>();
            col.sharedMaterial = ItemCatalog.Material(def.Surface);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = def.Mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = def.Mass < 3f ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            rb.maxAngularVelocity = 20f;

            var item = go.AddComponent<Interactable>();
            item.Init(def);
            item.TrackId = _world.Items.Count + 1; // 0 is the player
            _world.Items.Add(item);
            return item;
        }

        static readonly System.Collections.Generic.Dictionary<string, Material> _itemMats = new System.Collections.Generic.Dictionary<string, Material>();

        static Material ItemMaterial(ItemDef def)
        {
            if (_itemMats.TryGetValue(def.Id, out var m)) return m;
            m = Mats.Solid(def.Color, def.Surface == Surface.Metal ? 0.7f : def.Surface == Surface.Plastic ? 0.5f : 0.15f);
            _itemMats[def.Id] = m;
            return m;
        }
    }
}
