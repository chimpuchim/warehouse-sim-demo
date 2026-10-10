using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Warehouse;

namespace Warehouse.Tests
{
    public class SessionLogFormatTests
    {
        [Test]
        public void TickLineRoundTripsThroughJsonUtility()
        {
            var poses = new List<PoseSample>
            {
                new PoseSample { Id = 0, Position = new Vector3(1.23456f, -2.5f, 0.00004f), Rotation = Quaternion.Euler(10, 20, 30), Velocity = new Vector3(0.1f, 0, -0.1f), AngularVelocity = Vector3.one },
                new PoseSample { Id = 7, Position = new Vector3(-10, 0.3f, 8), Rotation = Quaternion.identity, Velocity = Vector3.zero, AngularVelocity = Vector3.zero },
            };
            var input = new InputSample { Move = new Vector2(0.5f, -1f), Look = new Vector2(1.25f, -0.5f), Buttons = InputSample.GrabBit | InputSample.SprintBit };
            var line = LogFormat.Tick(42, input, 7, poses);
            var tick = JsonUtility.FromJson<LogTick>(line);

            Assert.AreEqual(42, tick.t);
            Assert.AreEqual(7, tick.g);
            Assert.AreEqual(InputSample.GrabBit | InputSample.SprintBit, tick.b);
            Assert.AreEqual(new[] { 0, 7 }, tick.ids);
            Assert.AreEqual(0.5f, tick.mv[0]);
            Assert.AreEqual(-1f, tick.mv[1]);
            Assert.AreEqual(1.2346f, tick.p[0], 1e-5f);
            Assert.AreEqual(0f, tick.p[2]);          // 0.00004 rounds to 0
            Assert.AreEqual(-10f, tick.p[3]);
            var q = new Quaternion(tick.r[0], tick.r[1], tick.r[2], tick.r[3]);
            Assert.Less(Quaternion.Angle(q, Quaternion.Euler(10, 20, 30)), 0.01f);
            Assert.AreEqual(1f, tick.w[0]);
        }

        [Test]
        public void ParseExpandsChangedOnlyTicksIntoFullSnapshots()
        {
            var header = new LogHeader
            {
                app = "test", task = "t", player = "p", fixedDt = 0.02f, startedUtc = "2026-01-01T00:00:00Z",
                objects = new[] { new LogObject { id = 0, name = "Player", item = "player" }, new LogObject { id = 1, name = "box", item = "box_small" } },
            };
            var all = new List<PoseSample>
            {
                new PoseSample { Id = 0, Position = new Vector3(0, 1.65f, 0), Rotation = Quaternion.identity },
                new PoseSample { Id = 1, Position = new Vector3(2, 0.15f, 3), Rotation = Quaternion.Euler(0, 45, 0) },
            };
            var playerOnly = new List<PoseSample> { new PoseSample { Id = 0, Position = new Vector3(0.5f, 1.65f, 0), Rotation = Quaternion.Euler(0, 10, 0) } };
            var text = LogFormat.Header(header) + "\n" +
                       LogFormat.Tick(0, default, -1, all) + "\n" +
                       LogFormat.Event(new LogEvent { t = 1, name = "grab", obj = 1 }) + "\n" +
                       LogFormat.Tick(1, default, 1, playerOnly) + "\n" +
                       LogFormat.Result(new LogResult { t = 2, outcome = "success", score = 10, durationSeconds = 0.04f }) + "\n";

            var log = SessionLog.Parse(text);
            Assert.AreEqual(2, log.TickCount);
            Assert.AreEqual(2, log.ObjectCount);
            Assert.AreEqual(1, log.Events.Count);
            Assert.AreEqual("success", log.Result.outcome);
            // Tick 1 did not write the box, so it keeps the tick 0 pose.
            Assert.AreEqual(new Vector3(2, 0.15f, 3), log.Position(1, 1));
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 45, 0), log.Rotation(1, 1)), 0.01f);
            Assert.AreEqual(new Vector3(0.5f, 1.65f, 0), log.Position(1, 0));
        }

        [Test]
        public void ParseRejectsGapsAndUnknownVersions()
        {
            var header = LogFormat.Header(new LogHeader { fixedDt = 0.02f, objects = new[] { new LogObject { id = 0, name = "Player", item = "player" } } });
            var p = new List<PoseSample> { new PoseSample { Id = 0, Rotation = Quaternion.identity } };
            Assert.Throws<System.FormatException>(() => SessionLog.Parse(header + "\n" + LogFormat.Tick(0, default, -1, p) + "\n" + LogFormat.Tick(2, default, -1, p)));
            Assert.Throws<System.FormatException>(() => SessionLog.Parse(header.Replace("\"version\":1", "\"version\":9")));
        }
    }
}
