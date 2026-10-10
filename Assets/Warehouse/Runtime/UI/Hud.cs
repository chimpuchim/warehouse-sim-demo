using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Warehouse
{
    /// <summary>In-game overlay: crosshair, look-at label, order panel, timer, recorder status, hints.</summary>
    public class Hud : MonoBehaviour
    {
        Text _hover, _taskTitle, _taskBody, _timer, _rec, _message, _pause;
        RectTransform _pausePanel;
        float _messageUntil;
        readonly StringBuilder _sb = new StringBuilder();

        public static Hud Create()
        {
            var canvas = UI.Canvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<Hud>();
            var root = canvas.transform;

            var cross = UI.Fixed("Crosshair", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
            UI.Image(cross, new Color(1, 1, 1, 0.9f));
            hud._hover = UI.Label("Hover", root, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(600, 30), "", 18, UI.Text, TextAnchor.MiddleCenter);

            var panel = UI.Fixed("Task", root, new Vector2(0, 1), new Vector2(20, -20), new Vector2(360, 230));
            UI.Image(panel, UI.Panel);
            hud._taskTitle = UI.Label("Title", panel, new Vector2(0, 1), new Vector2(14, -10), new Vector2(330, 56), "", 22, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            hud._taskBody = UI.Label("Body", panel, new Vector2(0, 1), new Vector2(14, -70), new Vector2(330, 150), "", 18, UI.Text);
            hud._timer = UI.Label("Timer", panel, new Vector2(1, 1), new Vector2(-14, -10), new Vector2(120, 30), "", 24, UI.Text, TextAnchor.UpperRight, FontStyle.Bold);

            hud._rec = UI.Label("Rec", root, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(420, 30), "", 16, UI.Bad, TextAnchor.UpperRight);
            hud._message = UI.Label("Message", root, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 50), "", 30, UI.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            var hint = UI.Fixed("Hint", root, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(1200, 28));
            UI.Image(hint, UI.Panel);
            UI.Label(UI.Rect("Text", hint, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                "WASD move   Shift sprint   Space jump   LMB hold: grab / release to place   RMB: throw   Wheel: distance   R + mouse: rotate   F: lid   Esc: pause",
                15, UI.Muted, TextAnchor.MiddleCenter);

            hud._pausePanel = UI.Fixed("Pause", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 150));
            UI.Image(hud._pausePanel, UI.Panel);
            hud._pause = UI.Label("Text", hud._pausePanel, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(400, 60), "PAUSED\nClick in the game to resume", 20, UI.Text, TextAnchor.UpperCenter);
            hud._pausePanel.gameObject.SetActive(false);
            return hud;
        }

        public void SetPauseAction(string label, System.Action onClick)
        {
            UI.Button(_pausePanel, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(220, 40), label, onClick, UI.Bad);
        }

        public void ShowPause(bool on) => _pausePanel.gameObject.SetActive(on);

        public void SetHover(string text) => _hover.text = text;

        public void Message(string text, float seconds = 2.5f)
        {
            _message.text = text;
            _messageUntil = Time.unscaledTime + seconds;
        }

        public void SetTask(TaskRunner runner)
        {
            if (runner == null || runner.Task == null)
            {
                _taskTitle.text = "";
                _taskBody.text = "";
                _timer.text = "";
                return;
            }
            var t = runner.Task;
            _taskTitle.text = $"{t.type}  {t.title}";
            _sb.Clear();
            _sb.Append(t.description).Append("\n\n");
            foreach (var p in runner.Progress)
                _sb.Append(p.Done ? "[x] " : "[ ] ").Append(p.Have).Append('/').Append(p.Need).Append("  ").Append(p.Label).Append('\n');
            if (t.requireClosed) _sb.Append(runner.NeedsClose ? "[ ] close the lid (F)" : "[x] lid closed");
            _taskBody.text = _sb.ToString();
            _timer.text = UI.Time(runner.Remaining);
            _timer.color = runner.Remaining < 15f ? UI.Bad : UI.Text;
        }

        public void SetRecorder(SessionRecorder rec)
        {
            if (rec == null || !rec.Recording) { _rec.text = ""; return; }
            _rec.text = $"REC  {UI.Time(rec.Tick * Time.fixedDeltaTime)}   tick {rec.Tick}   {rec.Memory.Bytes / 1024} KB";
        }

        void Update()
        {
            if (_message.text.Length > 0 && Time.unscaledTime > _messageUntil) _message.text = "";
        }
    }
}
