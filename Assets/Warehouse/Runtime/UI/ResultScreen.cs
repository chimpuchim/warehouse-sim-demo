using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warehouse
{
    /// <summary>Shown when a task ends: outcome, score, log statistics and what to do next.</summary>
    public class ResultScreen : MonoBehaviour
    {
        Text _title, _body;
        Button _download;

        public static ResultScreen Create(Action onReplay, Action onMenu, Action onDownload)
        {
            var canvas = UI.Canvas("Result", 30);
            var r = canvas.gameObject.AddComponent<ResultScreen>();
            var root = canvas.transform;
            UI.Image(UI.Rect("Backdrop", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0, 0, 0, 0.55f));
            var panel = UI.Fixed("Panel", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 420));
            UI.Image(panel, UI.Panel);
            r._title = UI.Label("Title", panel, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(580, 44), "", 34, UI.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            r._body = UI.Label("Body", panel, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(560, 240), "", 18, UI.Text, TextAnchor.UpperLeft);
            UI.Button(panel, new Vector2(0, 0), new Vector2(20, 20), new Vector2(180, 44), "Replay session", onReplay, UI.PanelLight, 18);
            r._download = UI.Button(panel, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(180, 44), "Download log", onDownload, UI.PanelLight, 18);
            UI.Button(panel, new Vector2(1, 0), new Vector2(-20, 20), new Vector2(180, 44), "Menu", onMenu, UI.PanelLight, 18);
            return r;
        }

        public void Show(TaskDefinition task, TaskResult result, int rank, SessionRecorder rec, string savedPath)
        {
            _title.text = result.outcome switch { "success" => "TASK COMPLETE", "timeout" => "TIME OUT", _ => "ABORTED" };
            _title.color = result.outcome == "success" ? UI.Good : UI.Bad;
            string rankText = rank > 0 ? $"#{rank} on the local leaderboard" : "not on the leaderboard";
            string where = savedPath != null ? "Saved to: " + savedPath : "Log kept in memory; use Download log to save it.";
            _body.text =
                $"{task.type}  {task.title}\n" +
                $"Time: {result.elapsedSeconds:0.0} s of {task.timeLimitSeconds:0} s\n" +
                $"Score: {result.score}   ({rankText})\n\n" +
                $"Session log: {rec.Tick} ticks at {1f / Time.fixedDeltaTime:0} Hz, {rec.LinesWritten} lines, {rec.Memory.Bytes / 1024f:0.0} KB\n" +
                $"({rec.Memory.Bytes / 1024f / Mathf.Max(1f, rec.Tick * Time.fixedDeltaTime / 60f):0} KB per minute at this activity level)\n" +
                where;
            _download.gameObject.SetActive(Application.platform == RuntimePlatform.WebGLPlayer || Application.isEditor);
        }
    }
}
