using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Fixed-timestep logger. Every FixedUpdate (50 Hz) it writes the player's input and head pose
    /// and the pose + velocity of every tracked rigidbody that moved since it was last written.
    /// Events (grab, release, lid, task progress) are stamped with the next tick index.
    /// </summary>
    public class SessionRecorder : MonoBehaviour
    {
        public const string AppName = "WarehouseSimDemo";

        public bool Recording { get; private set; }
        public int Tick { get; private set; }
        public int LinesWritten { get; private set; }
        public MemorySink Memory { get; private set; }

        readonly List<ILogSink> _sinks = new List<ILogSink>();
        readonly List<PoseSample> _buffer = new List<PoseSample>(64);
        List<Interactable> _items;
        Vector3[] _lastPos;
        Quaternion[] _lastRot;
        FirstPersonController _player;
        GrabController _grab;

        /// <summary>Starts a log over the given items. The player and grab controller may be null in tests.</summary>
        public void Begin(List<Interactable> items, FirstPersonController player, GrabController grab, string taskId, string playerName, params ILogSink[] extraSinks)
        {
            _items = items;
            _player = player;
            _grab = grab;
            Memory = new MemorySink();
            _sinks.Clear();
            _sinks.Add(Memory);
            _sinks.AddRange(extraSinks);
            _lastPos = new Vector3[items.Count + 1];
            _lastRot = new Quaternion[items.Count + 1];
            Tick = 0;
            LinesWritten = 0;

            var objects = new LogObject[items.Count + 1];
            objects[0] = new LogObject { id = 0, name = "Player", item = "player" };
            for (int i = 0; i < items.Count; i++)
            {
                items[i].TrackId = i + 1;
                objects[i + 1] = new LogObject { id = i + 1, name = items[i].name, item = items[i].Def.Id };
            }
            Write(LogFormat.Header(new LogHeader
            {
                app = AppName + " " + Application.version,
                task = taskId,
                player = playerName,
                fixedDt = Time.fixedDeltaTime,
                startedUtc = DateTime.UtcNow.ToString("o"),
                objects = objects,
            }));
            Recording = true;
        }

        public void LogEvent(string name, int objectId = -1, string data = null)
        {
            if (!Recording) return;
            Write(LogFormat.Event(new LogEvent { t = Tick, name = name, obj = objectId, data = data }));
        }

        /// <summary>Writes the result line, closes the sinks and returns the in-memory log text.</summary>
        public string End(string outcome, int score)
        {
            if (!Recording) return Memory?.Text;
            Recording = false;
            Write(LogFormat.Result(new LogResult { t = Tick, outcome = outcome, score = score, durationSeconds = Tick * Time.fixedDeltaTime }));
            foreach (var s in _sinks) s.Close();
            return Memory.Text;
        }

        void FixedUpdate()
        {
            if (!Recording) return;
            RecordTick();
        }

        /// <summary>One tick of recording; public so tests can drive it with manual physics stepping.</summary>
        public void RecordTick()
        {
            _buffer.Clear();
            bool first = Tick == 0;

            // Object 0: the player's head (camera) pose and body velocity.
            var head = _player != null ? _player.Head : null;
            AddIfChanged(0, head != null ? head.position : Vector3.zero, head != null ? head.rotation : Quaternion.identity,
                _player != null ? _player.Velocity : Vector3.zero, Vector3.zero, first);

            for (int i = 0; i < _items.Count; i++)
            {
                var rb = _items[i].Body;
                AddIfChanged(i + 1, rb.position, rb.rotation, rb.linearVelocity, rb.angularVelocity, first);
            }

            var input = _player != null ? _player.LastSample : default;
            if (_grab != null) input.Buttons |= _grab.LastButtons;
            int held = _grab != null && _grab.Held != null ? _grab.Held.TrackId : -1;
            Write(LogFormat.Tick(Tick, input, held, _buffer));
            Tick++;
        }

        void AddIfChanged(int id, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 ang, bool force)
        {
            bool moved = force
                || (pos - _lastPos[id]).sqrMagnitude > 1e-8f
                || Quaternion.Angle(rot, _lastRot[id]) > 0.005f;
            if (!moved) return;
            _lastPos[id] = pos;
            _lastRot[id] = rot;
            _buffer.Add(new PoseSample { Id = id, Position = pos, Rotation = rot, Velocity = vel, AngularVelocity = ang });
        }

        void Write(string line)
        {
            LinesWritten++;
            foreach (var s in _sinks) s.WriteLine(line);
        }
    }
}
