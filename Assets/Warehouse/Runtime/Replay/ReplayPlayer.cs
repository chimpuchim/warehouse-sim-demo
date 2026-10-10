using System.Collections.Generic;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Plays a <see cref="SessionLog"/> back by driving transforms. Every tracked rigidbody is made
    /// kinematic and placed at the logged pose; nothing is re-simulated. Between ticks the render
    /// pose is interpolated; at a tick boundary it is exactly the logged pose.
    /// </summary>
    public class ReplayPlayer : MonoBehaviour
    {
        public SessionLog Log { get; private set; }
        public bool Loaded => Log != null;
        public bool Playing { get; set; }
        public float Speed { get; set; } = 1f;
        /// <summary>Fractional tick position on the timeline.</summary>
        public float Time { get; private set; }
        public int Tick => Mathf.Clamp(Mathf.FloorToInt(Time), 0, Log.TickCount - 1);
        public int TickCount => Log.TickCount;

        readonly List<Transform> _targets = new List<Transform>();
        readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        Transform _head;

        /// <summary>Maps the log's object table onto scene objects by name. Unmatched ids are skipped.</summary>
        public void Load(SessionLog log, World world, Transform head)
        {
            Log = log;
            _head = head;
            _targets.Clear();
            _bodies.Clear();
            foreach (var o in log.Header.objects)
            {
                Transform t = null;
                if (o.id == 0) t = head;
                else
                {
                    var item = world.FindItem(o.name);
                    if (item != null)
                    {
                        t = item.transform;
                        item.Body.isKinematic = true;
                        item.Body.interpolation = RigidbodyInterpolation.None;
                        _bodies.Add(item.Body);
                    }
                }
                _targets.Add(t);
            }
            Time = 0f;
            Playing = false;
            Apply(0f);
        }

        /// <summary>Restores physics on the tracked bodies; the caller resets the map afterwards.</summary>
        public void Unload()
        {
            foreach (var rb in _bodies)
            {
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            _bodies.Clear();
            _targets.Clear();
            Log = null;
        }

        public void Seek(float tick)
        {
            Time = Mathf.Clamp(tick, 0f, Log.TickCount - 1);
            Apply(Time);
        }

        public void Step(int ticks) => Seek(Mathf.Floor(Time) + ticks);

        /// <summary>Exact pose of tick t on every target, no interpolation (used by the tests).</summary>
        public void ApplyTick(int t)
        {
            Time = t;
            for (int id = 0; id < _targets.Count; id++)
            {
                var tr = _targets[id];
                if (tr == null) continue;
                tr.SetPositionAndRotation(Log.Position(t, id), Log.Rotation(t, id));
            }
        }

        void Apply(float time)
        {
            int a = Mathf.Clamp(Mathf.FloorToInt(time), 0, Log.TickCount - 1);
            int b = Mathf.Min(a + 1, Log.TickCount - 1);
            float f = Mathf.Clamp01(time - a);
            if (f < 1e-4f || a == b)
            {
                ApplyTick(a);
                return;
            }
            for (int id = 0; id < _targets.Count; id++)
            {
                var tr = _targets[id];
                if (tr == null) continue;
                tr.SetPositionAndRotation(
                    Vector3.Lerp(Log.Position(a, id), Log.Position(b, id), f),
                    Quaternion.Slerp(Log.Rotation(a, id), Log.Rotation(b, id), f));
            }
        }

        void Update()
        {
            if (!Loaded || !Playing) return;
            Time += UnityEngine.Time.deltaTime * Speed / Log.Header.fixedDt;
            if (Time >= Log.TickCount - 1)
            {
                Time = Log.TickCount - 1;
                Playing = false;
            }
            Apply(Time);
        }
    }
}
