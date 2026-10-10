using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Warehouse
{
    /// <summary>Replay transport: timeline scrub, play/pause, stepping, speed, event list, camera toggle.</summary>
    public class ReplayScreen : MonoBehaviour
    {
        ReplayPlayer _player;
        Slider _timeline;
        Text _info, _events, _playLabel;
        bool _dragging;
        readonly StringBuilder _sb = new StringBuilder();

        public static ReplayScreen Create(ReplayPlayer player, Action onExit, Action onToggleCamera)
        {
            var canvas = UI.Canvas("Replay", 25);
            var s = canvas.gameObject.AddComponent<ReplayScreen>();
            s._player = player;
            var root = canvas.transform;

            var top = UI.Rect("Top", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -44), Vector2.zero);
            UI.Image(top, new Color(0.6f, 0.1f, 0.1f, 0.85f));
            s._info = UI.Label(UI.Rect("Info", top, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-200, 0)), "", 18, UI.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Button(top, new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(120, 32), "Exit replay", onExit, UI.Panel, 15);

            var bottom = UI.Rect("Bottom", root, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 96));
            UI.Image(bottom, UI.Panel);
            s._timeline = UI.Slider(bottom, new Vector2(0, 1), new Vector2(20, -10), new Vector2(1560, 30));
            s._timeline.onValueChanged.AddListener(v => { if (s._dragging || !s._player.Playing) s._player.Seek(v); });
            var drag = s._timeline.gameObject.AddComponent<DragFlag>();
            drag.Screen = s;

            float x = 20;
            UI.Button(bottom, new Vector2(0, 0), new Vector2(x, 12), new Vector2(60, 36), "|<", () => s._player.Seek(0), UI.PanelLight, 16); x += 66;
            UI.Button(bottom, new Vector2(0, 0), new Vector2(x, 12), new Vector2(60, 36), "-1", () => s._player.Step(-1), UI.PanelLight, 16); x += 66;
            var play = UI.Button(bottom, new Vector2(0, 0), new Vector2(x, 12), new Vector2(100, 36), "Play", () => s.TogglePlay(), UI.Accent, 16); x += 106;
            s._playLabel = play.GetComponentInChildren<Text>();
            s._playLabel.color = Color.black;
            UI.Button(bottom, new Vector2(0, 0), new Vector2(x, 12), new Vector2(60, 36), "+1", () => s._player.Step(1), UI.PanelLight, 16); x += 66;
            UI.Button(bottom, new Vector2(0, 0), new Vector2(x, 12), new Vector2(60, 36), ">|", () => s._player.Seek(s._player.TickCount - 1), UI.PanelLight, 16); x += 86;
            foreach (var sp in new[] { 0.25f, 0.5f, 1f, 2f, 4f })
            {
                float speed = sp;
                UI.Button(bottom, new Vector2(0, 0), new Vector2(x, 12), new Vector2(64, 36), sp + "x", () => s._player.Speed = speed, UI.PanelLight, 15);
                x += 70;
            }
            UI.Button(bottom, new Vector2(0, 0), new Vector2(x + 20, 12), new Vector2(150, 36), "Camera: POV/3rd", onToggleCamera, UI.PanelLight, 15);
            UI.Label("Keys", bottom, new Vector2(1, 0), new Vector2(-20, 12), new Vector2(560, 36), "Space play/pause   Left/Right step   Drag the timeline to scrub", 14, UI.Muted, TextAnchor.MiddleRight);

            var evPanel = UI.Fixed("Events", root, new Vector2(1, 1), new Vector2(-20, -60), new Vector2(340, 300));
            UI.Image(evPanel, UI.Panel);
            s._events = UI.Label(UI.Rect("Text", evPanel, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, UI.Text, TextAnchor.UpperLeft);
            return s;
        }

        void TogglePlay()
        {
            if (!_player.Playing && _player.Time >= _player.TickCount - 1) _player.Seek(0);
            _player.Playing = !_player.Playing;
        }

        public void BeginDrag() { _dragging = true; _player.Playing = false; }
        public void EndDrag() => _dragging = false;

        void Update()
        {
            if (!_player.Loaded) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) TogglePlay();
                if (kb.leftArrowKey.wasPressedThisFrame) _player.Step(-1);
                if (kb.rightArrowKey.wasPressedThisFrame) _player.Step(1);
            }
            if (!_dragging)
            {
                _timeline.minValue = 0;
                _timeline.maxValue = _player.TickCount - 1;
                _timeline.SetValueWithoutNotify(_player.Time);
            }
            var h = _player.Log.Header;
            int tick = _player.Tick;
            _info.text = $"REPLAY   {h.task}   {h.player}   {h.startedUtc.Substring(0, 19).Replace('T', ' ')} UTC      tick {tick}/{_player.TickCount - 1}   {UI.Time(tick * h.fixedDt)} / {UI.Time(_player.Log.Duration)}   {_player.Speed}x";
            _playLabel.text = _player.Playing ? "Pause" : "Play";

            _sb.Clear();
            _sb.Append("Events up to this tick\n");
            int shown = 0;
            for (int i = _player.Log.Events.Count - 1; i >= 0 && shown < 14; i--)
            {
                var e = _player.Log.Events[i];
                if (e.t > tick) continue;
                string obj = e.obj >= 0 && e.obj < h.objects.Length ? h.objects[e.obj].name : "";
                _sb.Append(UI.Time(e.t * h.fixedDt)).Append("  ").Append(e.name).Append("  ").Append(obj).Append(' ').Append(e.data ?? "").Append('\n');
                shown++;
            }
            _events.text = _sb.ToString();
        }

        class DragFlag : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
        {
            public ReplayScreen Screen;
            public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) => Screen.BeginDrag();
            public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) => Screen.EndDrag();
        }
    }
}
