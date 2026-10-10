using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warehouse
{
    /// <summary>Title screen: player name, task list from JSON, leaderboard, replay / load log.</summary>
    public class MenuScreen : MonoBehaviour
    {
        public string PlayerName => _name.text.Trim().Length > 0 ? _name.text.Trim() : "Player";

        InputField _name;
        RectTransform _taskList, _logList;
        Text _board, _status;
        Button _replayLast;

        public static MenuScreen Create(Action<TaskDefinition> onStart, Action onReplayLast, Action onLoadLog)
        {
            var canvas = UI.Canvas("Menu", 20);
            var m = canvas.gameObject.AddComponent<MenuScreen>();
            var root = canvas.transform;
            UI.Image(UI.Rect("Backdrop", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0.03f, 0.05f, 0.08f, 0.92f));

            UI.Label("Title", root, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1000, 50), "WAREHOUSE SIM DEMO", 40, UI.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Label("Sub", root, new Vector2(0.5f, 1), new Vector2(0, -86), new Vector2(1100, 30),
                "First-person physics prototype: grab, carry, stack  |  PICK / PACK / LOAD tasks from JSON  |  50 Hz session log + replay",
                16, UI.Muted, TextAnchor.MiddleCenter);

            var left = UI.Fixed("Left", root, new Vector2(0.5f, 1), new Vector2(-250, -130), new Vector2(520, 640));
            UI.Image(left, UI.Panel);
            UI.Label("NameLabel", left, new Vector2(0, 1), new Vector2(20, -16), new Vector2(200, 26), "Your name", 16, UI.Muted);
            m._name = UI.Input(left, new Vector2(0, 1), new Vector2(20, -44), new Vector2(300, 38), "Player");
            UI.Label("TasksLabel", left, new Vector2(0, 1), new Vector2(20, -100), new Vector2(400, 26), "Tasks (from StreamingAssets/Tasks/*.json)", 16, UI.Muted);
            m._taskList = UI.Fixed("Tasks", left, new Vector2(0, 1), new Vector2(20, -130), new Vector2(480, 330));
            m._status = UI.Label("Status", left, new Vector2(0, 0), new Vector2(20, 16), new Vector2(480, 120), "Loading tasks...", 14, UI.Bad, TextAnchor.LowerLeft);

            var right = UI.Fixed("Right", root, new Vector2(0.5f, 1), new Vector2(290, -130), new Vector2(520, 640));
            UI.Image(right, UI.Panel);
            UI.Label("BoardLabel", right, new Vector2(0, 1), new Vector2(20, -16), new Vector2(300, 26), "Leaderboard (local)", 16, UI.Muted);
            m._board = UI.Label("Board", right, new Vector2(0, 1), new Vector2(20, -44), new Vector2(480, 260), "", 17, UI.Text);
            UI.Label("ReplayLabel", right, new Vector2(0, 1), new Vector2(20, -320), new Vector2(300, 26), "Replay", 16, UI.Muted);
            m._replayLast = UI.Button(right, new Vector2(0, 1), new Vector2(20, -348), new Vector2(230, 40), "Replay last session", onReplayLast, UI.PanelLight, 17);
            m._replayLast.interactable = false;
            UI.Button(right, new Vector2(0, 1), new Vector2(260, -348), new Vector2(230, 40), Application.platform == RuntimePlatform.WebGLPlayer ? "Open log file..." : "Open log file...", onLoadLog, UI.PanelLight, 17);
            m._logList = UI.Fixed("Logs", right, new Vector2(0, 1), new Vector2(20, -400), new Vector2(480, 220));

            m.Build(onStart);
            return m;
        }

        void Build(Action<TaskDefinition> onStart) => _onStart = onStart;
        Action<TaskDefinition> _onStart;

        public void SetTasks(List<TaskDefinition> tasks, List<string> errors)
        {
            foreach (Transform c in _taskList) Destroy(c.gameObject);
            float y = 0;
            foreach (var t in tasks)
            {
                var def = t;
                UI.Button(_taskList, new Vector2(0, 1), new Vector2(0, y), new Vector2(480, 46),
                    $"{t.type,-5}  {t.title}   ({UI.Time(t.timeLimitSeconds)})", () => _onStart(def), UI.PanelLight, 17);
                y -= 52;
            }
            _status.text = errors.Count == 0 ? $"{tasks.Count} tasks loaded" : string.Join("\n", errors);
            _status.color = errors.Count == 0 ? UI.Muted : UI.Bad;
        }

        public void SetLeaderboard(Leaderboard board)
        {
            if (board.Entries.Count == 0) { _board.text = "No scores yet."; return; }
            var lines = new List<string>();
            int i = 1;
            foreach (var e in board.Entries)
                lines.Add($"{i++,2}. {e.name,-14} {e.score,5}   {e.taskId}   {UI.Time(e.elapsedSeconds)}");
            _board.text = string.Join("\n", lines);
        }

        public void SetReplayAvailable(bool on) => _replayLast.interactable = on;

        /// <summary>Desktop only: recent session files to open.</summary>
        public void SetLogFiles(IReadOnlyList<string> paths, Action<string> onOpen)
        {
            foreach (Transform c in _logList) Destroy(c.gameObject);
            float y = 0;
            foreach (var p in paths)
            {
                var path = p;
                UI.Button(_logList, new Vector2(0, 1), new Vector2(0, y), new Vector2(480, 34), System.IO.Path.GetFileName(path), () => onOpen(path), UI.PanelLight, 14);
                y -= 38;
            }
        }
    }
}
