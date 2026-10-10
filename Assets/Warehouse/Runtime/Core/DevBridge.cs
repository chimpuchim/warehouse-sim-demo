using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Automation entry point for developer mode. The browser calls
    /// unityInstance.SendMessage("Dev", "Cmd", "...") and the command drives the game through the
    /// same <see cref="ScriptedInput"/> the controllers read, so a scripted run exercises the real
    /// movement, grab and task code. Commands:
    ///   start &lt;taskId&gt; | walkto x z | look yaw pitch | lookat &lt;object&gt; | grab | release | throw
    ///   dist d | rotate yaw pitch | interact | stop | stat | result | replay | seek t | menu
    ///   stacktest | input kb|scripted | teleport x y z yaw
    /// </summary>
    public class DevBridge : MonoBehaviour
    {
        public GameFlow Flow;

        Vector3? _walkTarget;
        Transform _lookAt;
        float _stackTestUntil;
        Interactable[] _stack;
        Vector3[] _stackStart;

        public void Cmd(string line)
        {
            var a = line.Trim().Split(' ');
            if (a.Length == 0 || a[0].Length == 0) return;
            var sc = Flow.Scripted;
            switch (a[0])
            {
                case "input":
                    Flow.UseScriptedInput(a.Length > 1 && a[1] == "scripted");
                    break;
                case "start":
                    Flow.UseScriptedInput(true);
                    var task = Flow.Tasks.FirstOrDefault(t => t.id == a[1]);
                    if (task == null) { Debug.LogError("[DEV] no task " + a[1]); return; }
                    Flow.StartTask(task);
                    break;
                case "walkto":
                    _walkTarget = new Vector3(F(a[1]), 0, F(a[2]));
                    break;
                case "stop":
                    _walkTarget = null;
                    _lookAt = null;
                    sc.Move = Vector2.zero;
                    break;
                case "look":
                    _lookAt = null;
                    Flow.Player.SetView(F(a[1]), F(a[2]));
                    break;
                case "lookat":
                    var item = Flow.World.FindItem(a[1]);
                    _lookAt = item != null ? item.transform : GameObject.Find(a[1])?.transform;
                    if (_lookAt == null) Debug.LogError("[DEV] no object " + a[1]);
                    break;
                case "grab": sc.GrabHeld = true; break;
                case "release": sc.GrabHeld = false; break;
                case "throw": sc.PressThrow(); break;
                case "dist": sc.DistanceDelta = F(a[1]); break;
                case "rotate":
                    sc.RotateHeld = true;
                    sc.RotateDelta = new Vector2(F(a[1]), F(a[2]));
                    StartCoroutine(ReleaseRotateNextFrame());
                    break;
                case "interact": sc.PressInteract(); break;
                case "teleport": Flow.Player.Teleport(new Vector3(F(a[1]), F(a[2]), F(a[3])), a.Length > 4 ? F(a[4]) : 0f); break;
                case "stat": Stat(); break;
                case "result":
                    var r = Flow.LastResult;
                    Debug.Log(r == null ? "[DEV] result: none" : $"[DEV] result: {r.outcome} {r.taskId} score {r.score} elapsed {r.elapsedSeconds:0.00} ticks {Flow.LastLog.TickCount} bytes {Flow.LastLogText.Length}");
                    break;
                case "replay": Flow.StartReplay(Flow.LastLog); break;
                case "seek": Flow.Replay.Playing = false; Flow.Replay.Seek(F(a[1])); break;
                case "menu": Flow.ShowMenu(); break;
                case "stacktest": StackTest(); break;
                default:
                    Debug.LogError("[DEV] unknown command " + a[0]);
                    break;
            }
        }

        System.Collections.IEnumerator ReleaseRotateNextFrame()
        {
            yield return null;
            Flow.Scripted.RotateHeld = false;
        }

        void Stat()
        {
            var p = Flow.Player;
            var g = Flow.Grab;
            string held = g.Held != null ? $"{g.Held.name} at {g.Held.transform.position:F2}" : "none";
            string hover = g.Hovered != null ? g.Hovered.name : g.HoveredContainer != null ? g.HoveredContainer.Label : "none";
            string task = Flow.Runner.Running
                ? $"{Flow.Runner.Task.id} {Flow.Runner.Remaining:0}s " + string.Join(" ", Flow.Runner.Progress.Select(x => $"{x.Item}:{x.Have}/{x.Need}")) + (Flow.Runner.NeedsClose ? " needs-close" : "")
                : "none";
            Debug.Log($"[DEV] state {Flow.State} pos {p.transform.position:F2} yaw {p.Yaw:0} pitch {p.Pitch:0} held {held} hover {hover} task {task} rec {Flow.Recorder.Tick} ticks {(Flow.Recorder.Memory?.Bytes ?? 0)} B");
        }

        /// <summary>Stacks four medium boxes in front of the player and reports drift after 10 s.</summary>
        void StackTest()
        {
            var basePos = Flow.Player.transform.position + Flow.Player.transform.forward * 2.5f;
            basePos.y = 0f;
            _stack = Flow.World.Items.Where(i => i.Def.Id == "box_medium").Take(4).ToArray();
            _stackStart = new Vector3[_stack.Length];
            for (int i = 0; i < _stack.Length; i++)
            {
                var rb = _stack[i].Body;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = basePos + Vector3.up * (0.2f + i * 0.4f + 0.003f * i);
                rb.rotation = Quaternion.identity;
                _stack[i].transform.SetPositionAndRotation(rb.position, rb.rotation);
                _stackStart[i] = rb.position;
            }
            _stackTestUntil = Time.time + 10f;
            Debug.Log("[DEV] stacktest: 4 x box_medium stacked at " + basePos.ToString("F2"));
        }

        void Update()
        {
            var sc = Flow.Scripted;
            var p = Flow.Player;
            if (_lookAt != null)
            {
                var dir = _lookAt.position - p.Head.position;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;
                p.SetView(yaw, pitch);
            }
            if (_walkTarget.HasValue)
            {
                var to = _walkTarget.Value - p.transform.position;
                to.y = 0;
                if (to.magnitude < 0.25f)
                {
                    _walkTarget = null;
                    sc.Move = Vector2.zero;
                    Debug.Log("[DEV] arrived at " + p.transform.position.ToString("F2"));
                }
                else
                {
                    var local = p.transform.InverseTransformDirection(to.normalized);
                    sc.Move = new Vector2(local.x, local.z);
                }
            }
            if (_stack != null && Time.time >= _stackTestUntil)
            {
                float maxDrift = 0f;
                for (int i = 0; i < _stack.Length; i++) maxDrift = Mathf.Max(maxDrift, Vector3.Distance(_stack[i].Body.position, _stackStart[i]));
                bool allAsleep = _stack.All(s => s.Body.IsSleeping());
                Debug.Log($"[DEV] stacktest after 10 s: max drift {maxDrift * 1000f:0.0} mm, top box y {_stack[3].Body.position.y:F3}, all sleeping {allAsleep}");
                _stack = null;
            }
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    }
}
