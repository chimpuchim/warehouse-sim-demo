using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Warehouse;

namespace Warehouse.Tests
{
    /// <summary>Loads the real scene: the map builds without errors, the tasks load, a task can start and record.</summary>
    public class SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneBootsBuildsMapLoadsTasksAndRecords()
        {
            SceneManager.LoadScene("Warehouse");
            yield return null;
            yield return null;
            var flow = Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "GameFlow was not created");
            Assert.GreaterOrEqual(flow.World.Items.Count, 50, "expected about 50 interactables");
            Assert.AreEqual(5, flow.World.Zones.Count);
            Assert.AreEqual(2, flow.World.Containers.Count);

            float deadline = Time.realtimeSinceStartup + 10f;
            while (flow.Tasks.Count == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(4, flow.Tasks.Count, "tasks from StreamingAssets");

            flow.UseScriptedInput(true);
            flow.StartTask(flow.Tasks[0]);
            Assert.AreEqual(GameState.Playing, flow.State);
            flow.Scripted.Move = new Vector2(0, 1);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            flow.Scripted.Move = Vector2.zero;
            Assert.GreaterOrEqual(flow.Recorder.Tick, 25, "recorder should have logged ~30 ticks");
            Assert.Greater(flow.Player.transform.position.z, flow.World.PlayerStart.z + 0.1f, "player should have walked forward");

            flow.Runner.Abort();
            yield return null;
            Assert.AreEqual(GameState.Result, flow.State);
            Assert.IsNotNull(flow.LastLog);
            Assert.AreEqual("aborted", flow.LastLog.Result.outcome);

            flow.StartReplay(flow.LastLog);
            Assert.AreEqual(GameState.Replay, flow.State);
            flow.Replay.Seek(flow.LastLog.TickCount - 1);
            yield return null;
            flow.ShowMenu();
            Assert.AreEqual(GameState.Menu, flow.State);
        }
    }
}
