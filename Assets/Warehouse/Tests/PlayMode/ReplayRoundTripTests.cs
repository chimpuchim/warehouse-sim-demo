using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Warehouse;

namespace Warehouse.Tests
{
    /// <summary>
    /// Records real physics (boxes dropped onto a floor and onto each other), then replays the log
    /// kinematically and checks that every tracked transform at every tick matches what was logged.
    /// Physics is stepped manually so the test does not depend on frame timing.
    /// </summary>
    public class ReplayRoundTripTests
    {
        const int Ticks = 150;
        const float Tolerance = 0.0006f; // log precision is 0.0001 m / 0.00001 per quaternion component

        readonly List<GameObject> _spawned = new List<GameObject>();
        SimulationMode _previousMode;

        [SetUp]
        public void SetUp()
        {
            _previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = _previousMode;
            foreach (var go in _spawned) Object.Destroy(go);
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator ReplayReproducesEveryLoggedTransform()
        {
            var world = BuildTestWorld();
            var recorder = NewObject("Recorder").AddComponent<SessionRecorder>();
            recorder.Begin(world.Items, null, null, "test", "tester");

            for (int t = 0; t < Ticks; t++)
            {
                recorder.RecordTick();
                Physics.Simulate(Time.fixedDeltaTime);
            }
            var text = recorder.End("success", 1);
            yield return null;

            var log = SessionLog.Parse(text);
            Assert.AreEqual(Ticks, log.TickCount);
            Assert.AreEqual(world.Items.Count + 1, log.ObjectCount);
            Assert.Less(log.Ticks[Ticks - 1].ids.Length, log.ObjectCount, "boxes at rest should stop being written");

            // The boxes really moved: a dropped box must be lower at the end than at the start.
            Assert.Less(log.Position(Ticks - 1, 1).y, log.Position(0, 1).y - 0.5f);

            // Scramble the scene, then replay and compare at every tick.
            foreach (var item in world.Items) item.transform.position += new Vector3(5, 5, 5);
            var player = NewObject("Replay").AddComponent<ReplayPlayer>();
            player.Load(log, world, null);
            float worst = 0f;
            for (int t = 0; t < Ticks; t++)
            {
                player.ApplyTick(t);
                foreach (var item in world.Items)
                {
                    int id = item.TrackId;
                    float dp = Vector3.Distance(item.transform.position, log.Position(t, id));
                    float dr = Quaternion.Angle(item.transform.rotation, log.Rotation(t, id));
                    worst = Mathf.Max(worst, dp);
                    Assert.LessOrEqual(dp, Tolerance, $"tick {t} {item.name} position");
                    Assert.LessOrEqual(dr, 0.05f, $"tick {t} {item.name} rotation (deg)");
                }
            }
            Debug.Log($"replay round trip: {Ticks} ticks, {world.Items.Count} objects, worst position error {worst * 1000f:0.000} mm, log {text.Length} bytes");
            player.Unload();
        }

        [UnityTest]
        public IEnumerator StackOfFourBoxesStaysStableForTenSeconds()
        {
            var world = BuildTestWorld(stackOnly: true);
            var start = new Vector3[world.Items.Count];
            for (int i = 0; i < start.Length; i++) start[i] = world.Items[i].Body.position;

            int steps = Mathf.RoundToInt(10f / Time.fixedDeltaTime);
            for (int t = 0; t < steps; t++) Physics.Simulate(Time.fixedDeltaTime);
            yield return null;

            float maxDrift = 0f;
            for (int i = 0; i < start.Length; i++)
                maxDrift = Mathf.Max(maxDrift, Vector3.Distance(world.Items[i].Body.position, start[i]));
            Debug.Log($"stack of 4 after 10 s: max drift {maxDrift * 1000f:0.0} mm");
            Assert.Less(maxDrift, 0.02f, "a box moved more than 2 cm");
            Assert.Greater(world.Items[3].Body.position.y, 1.3f, "top box fell off");
        }

        World BuildTestWorld(bool stackOnly = false)
        {
            var world = new World();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(20, 0.2f, 20);
            floor.transform.position = new Vector3(0, -0.1f, 0);
            _spawned.Add(floor);

            // Four medium boxes stacked exactly on each other.
            var def = ItemCatalog.Get("box_medium");
            for (int i = 0; i < 4; i++) world.Items.Add(Spawn(def, new Vector3(0, i * def.Size.y + def.Size.y / 2, 0), Quaternion.identity));
            if (stackOnly) return world;

            // Plus boxes dropped from height with random orientations so there is real motion to log.
            var small = ItemCatalog.Get("box_small");
            Random.InitState(7);
            for (int i = 0; i < 6; i++)
                world.Items.Add(Spawn(small, new Vector3(Random.Range(-1f, 1f), 2f + i * 0.4f, Random.Range(-1f, 1f)), Random.rotation));
            return world;
        }

        GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        Interactable Spawn(ItemDef def, Vector3 pos, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = def.Id + "_" + _spawned.Count;
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = def.Size;
            go.GetComponent<Collider>().sharedMaterial = ItemCatalog.Material(def.Surface);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = def.Mass;
            var item = go.AddComponent<Interactable>();
            item.Init(def);
            _spawned.Add(go);
            return item;
        }
    }
}
