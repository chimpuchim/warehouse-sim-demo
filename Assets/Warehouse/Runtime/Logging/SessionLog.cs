using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Warehouse
{
    // ---------- line models (JSON Lines; one object per line, "type" tells them apart) ----------

    [Serializable]
    public class LogObject
    {
        public int id;
        public string name;
        public string item;
    }

    [Serializable]
    public class LogHeader
    {
        public string type = "header";
        public int version = 1;
        public string app;
        public string task;
        public string player;
        public float fixedDt;
        public string startedUtc;
        public LogObject[] objects;
    }

    /// <summary>
    /// One physics tick. Arrays are flat: ids[k] is the k-th written object, p has 3 floats per
    /// object, r has 4 (quaternion x y z w), v 3 (linear velocity), w 3 (angular velocity).
    /// Only objects whose pose changed since they were last written appear; tick 0 has all of them.
    /// </summary>
    [Serializable]
    public class LogTick
    {
        public string type = "tick";
        public int t;
        public float[] mv;   // move axis x,y
        public float[] lk;   // look delta yaw,pitch
        public int b;        // button bits, see InputSample
        public int g;        // id of the held object, -1 if none
        public int[] ids;
        public float[] p;
        public float[] r;
        public float[] v;
        public float[] w;
    }

    [Serializable]
    public class LogEvent
    {
        public string type = "event";
        public int t;
        public string name;
        public int obj;
        public string data;
    }

    [Serializable]
    public class LogResult
    {
        public string type = "result";
        public int t;
        public string outcome;
        public int score;
        public float durationSeconds;
    }

    /// <summary>Pose of one tracked object at one tick.</summary>
    public struct PoseSample
    {
        public int Id;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
    }

    // ---------- writer ----------

    /// <summary>Formats log lines. Numbers use 4 decimals (0.1 mm) and invariant culture.</summary>
    public static class LogFormat
    {
        public const int Version = 1;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Header(LogHeader h) => JsonUtility.ToJson(h);
        public static string Event(LogEvent e) => JsonUtility.ToJson(e);
        public static string Result(LogResult r) => JsonUtility.ToJson(r);

        public static string Tick(int t, InputSample input, int heldId, List<PoseSample> poses)
        {
            var sb = new StringBuilder(64 + poses.Count * 120);
            sb.Append("{\"type\":\"tick\",\"t\":").Append(t);
            sb.Append(",\"mv\":[").Num(input.Move.x).Append(',').Num(input.Move.y).Append(']');
            sb.Append(",\"lk\":[").Num(input.Look.x).Append(',').Num(input.Look.y).Append(']');
            sb.Append(",\"b\":").Append(input.Buttons).Append(",\"g\":").Append(heldId);
            Arr(sb, "ids", poses, (b, s) => b.Append(s.Id));
            Arr(sb, "p", poses, (b, s) => b.Num(s.Position.x).Append(',').Num(s.Position.y).Append(',').Num(s.Position.z));
            Arr(sb, "r", poses, (b, s) => b.Num(s.Rotation.x, 5).Append(',').Num(s.Rotation.y, 5).Append(',').Num(s.Rotation.z, 5).Append(',').Num(s.Rotation.w, 5));
            Arr(sb, "v", poses, (b, s) => b.Num(s.Velocity.x).Append(',').Num(s.Velocity.y).Append(',').Num(s.Velocity.z));
            Arr(sb, "w", poses, (b, s) => b.Num(s.AngularVelocity.x).Append(',').Num(s.AngularVelocity.y).Append(',').Num(s.AngularVelocity.z));
            sb.Append('}');
            return sb.ToString();
        }

        static void Arr(StringBuilder sb, string key, List<PoseSample> poses, Action<StringBuilder, PoseSample> one)
        {
            sb.Append(",\"").Append(key).Append("\":[");
            for (int i = 0; i < poses.Count; i++)
            {
                if (i > 0) sb.Append(',');
                one(sb, poses[i]);
            }
            sb.Append(']');
        }

        static StringBuilder Num(this StringBuilder sb, float value, int decimals = 4)
        {
            float rounded = (float)Math.Round(value, decimals);
            if (rounded == 0f) return sb.Append('0');
            return sb.Append(rounded.ToString(decimals == 4 ? "0.####" : "0.#####", Inv));
        }
    }

    // ---------- parsed log ----------

    /// <summary>A whole session log in memory, with per-tick full snapshots for scrubbing.</summary>
    public sealed class SessionLog
    {
        public LogHeader Header;
        public LogResult Result;
        public readonly List<LogTick> Ticks = new List<LogTick>();
        public readonly List<LogEvent> Events = new List<LogEvent>();

        public int ObjectCount => Header.objects.Length;
        public int TickCount => Ticks.Count;
        public float Duration => TickCount * Header.fixedDt;

        Vector3[] _pos;
        Quaternion[] _rot;

        public static SessionLog Parse(string text)
        {
            var log = new SessionLog();
            int lineNo = 0;
            foreach (var raw in text.Split('\n'))
            {
                lineNo++;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.Contains("\"type\":\"tick\"")) log.Ticks.Add(JsonUtility.FromJson<LogTick>(line));
                else if (line.Contains("\"type\":\"event\"")) log.Events.Add(JsonUtility.FromJson<LogEvent>(line));
                else if (line.Contains("\"type\":\"header\"")) log.Header = JsonUtility.FromJson<LogHeader>(line);
                else if (line.Contains("\"type\":\"result\"")) log.Result = JsonUtility.FromJson<LogResult>(line);
                else throw new FormatException($"line {lineNo}: unknown record");
            }
            if (log.Header == null) throw new FormatException("log has no header line");
            if (log.Header.version != LogFormat.Version) throw new FormatException("unsupported log version " + log.Header.version);
            log.BuildSnapshots();
            return log;
        }

        /// <summary>Expands the changed-only ticks into a full pose per object per tick.</summary>
        void BuildSnapshots()
        {
            int n = ObjectCount;
            _pos = new Vector3[TickCount * n];
            _rot = new Quaternion[TickCount * n];
            var lastPos = new Vector3[n];
            var lastRot = new Quaternion[n];
            for (int i = 0; i < n; i++) lastRot[i] = Quaternion.identity;

            for (int t = 0; t < TickCount; t++)
            {
                var tick = Ticks[t];
                if (tick.t != t) throw new FormatException($"tick {t} has index {tick.t}: log is not contiguous");
                for (int k = 0; k < tick.ids.Length; k++)
                {
                    int id = tick.ids[k];
                    lastPos[id] = new Vector3(tick.p[k * 3], tick.p[k * 3 + 1], tick.p[k * 3 + 2]);
                    lastRot[id] = new Quaternion(tick.r[k * 4], tick.r[k * 4 + 1], tick.r[k * 4 + 2], tick.r[k * 4 + 3]);
                }
                Array.Copy(lastPos, 0, _pos, t * n, n);
                Array.Copy(lastRot, 0, _rot, t * n, n);
            }
        }

        public Vector3 Position(int tick, int objectId) => _pos[tick * ObjectCount + objectId];
        public Quaternion Rotation(int tick, int objectId) => _rot[tick * ObjectCount + objectId];
    }
}
