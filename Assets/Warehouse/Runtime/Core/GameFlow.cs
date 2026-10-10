using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Warehouse
{
    public enum GameState { Menu, Playing, Result, Replay }

    /// <summary>
    /// Owns the world, the player, the task runner, the recorder and the replay player, and switches
    /// between menu / playing / result / replay. Session logs are written to
    /// persistentDataPath/Sessions on desktop and kept in memory (downloadable) on WebGL.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public GameState State { get; private set; }
        public World World { get; private set; }
        public FirstPersonController Player { get; private set; }
        public GrabController Grab { get; private set; }
        public TaskRunner Runner { get; private set; }
        public SessionRecorder Recorder { get; private set; }
        public ReplayPlayer Replay { get; private set; }
        public Hud Hud { get; private set; }
        public List<TaskDefinition> Tasks { get; } = new List<TaskDefinition>();
        public SessionLog LastLog { get; private set; }
        public string LastLogText { get; private set; }
        public TaskResult LastResult { get; private set; }

        KeyboardMouseInput _keyboard;
        ScriptedInput _scripted;
        bool _useScripted;
        MenuScreen _menu;
        ResultScreen _result;
        ReplayScreen _replayScreen;
        Leaderboard _board;
        TaskDefinition _currentTask;
        string _savedPath;
        Camera _thirdPerson;
        Light _sun;

        public ScriptedInput Scripted => _scripted;

        void Awake()
        {
            SetupLighting();
            World = WarehouseMap.Build();
            Player = FirstPersonController.Create(World.PlayerStart, World.PlayerStartYaw);
            Grab = GrabController.Attach(Player);
            Grab.ContainerOf = item => World.Containers.FirstOrDefault(c => c.Zone.Contains(item));
            _keyboard = Player.gameObject.AddComponent<KeyboardMouseInput>();
            _scripted = new ScriptedInput();
            UseScriptedInput(false);

            Runner = gameObject.AddComponent<TaskRunner>();
            Recorder = gameObject.AddComponent<SessionRecorder>();
            Replay = gameObject.AddComponent<ReplayPlayer>();
            _board = new Leaderboard();

            Hud = Hud.Create();
            Hud.SetPauseAction("Abort task", AbortTask);
            _menu = MenuScreen.Create(StartTask, () => StartReplay(LastLog), OpenLogFile);
            _result = ResultScreen.Create(() => StartReplay(LastLog), ShowMenu, DownloadLastLog);
            _replayScreen = ReplayScreen.Create(Replay, ExitReplay, ToggleReplayCamera);
            _thirdPerson = new GameObject("Replay camera").AddComponent<Camera>();
            _thirdPerson.fieldOfView = 60f;
            _thirdPerson.enabled = false;

            Grab.Grabbed += item => { Recorder.LogEvent("grab", item.TrackId); Hud.Message(""); };
            Grab.Released += (item, thrown) => Recorder.LogEvent(thrown ? "throw" : "drop", item.TrackId);
            Grab.ContainerToggled += c => Recorder.LogEvent(c.Closed ? "lid_closed" : "lid_opened", -1, c.Zone.Id);
            Runner.ProgressChanged += OnProgress;
            Runner.Finished += OnTaskFinished;

            Debug.Log($"[Warehouse] map built: {World.Items.Count} items, {World.Zones.Count} zones");
            StartCoroutine(TaskLibrary.LoadAll((tasks, errors) =>
            {
                Tasks.Clear();
                Tasks.AddRange(tasks);
                _menu.SetTasks(tasks, errors);
                foreach (var e in errors) Debug.LogWarning("[Tasks] " + e);
                Debug.Log($"[Tasks] loaded {tasks.Count}: {string.Join(", ", tasks.Select(t => t.id))}");
                WebBridge.NotifyReady();
            }));
            ShowMenu();
        }

        void SetupLighting()
        {
            _sun = new GameObject("Sun").AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            _sun.intensity = 1.1f;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.7f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);
            RenderSettings.fog = false;
        }

        public void UseScriptedInput(bool on)
        {
            _useScripted = on;
            Player.Input = on ? _scripted : _keyboard;
            Grab.Input = on ? _scripted : _keyboard;
        }

        // ---------- states ----------

        void SetState(GameState s)
        {
            State = s;
            _menu.gameObject.SetActive(s == GameState.Menu);
            Hud.gameObject.SetActive(s == GameState.Playing);
            _result.gameObject.SetActive(s == GameState.Result);
            _replayScreen.gameObject.SetActive(s == GameState.Replay);
            bool play = s == GameState.Playing;
            Player.Frozen = !play;
            Grab.Frozen = !play;
            if (!play) SetCursorLocked(false);
            _thirdPerson.enabled = false;
            Player.Camera.enabled = true;
            Player.BodyVisual.SetActive(false);
        }

        public void ShowMenu()
        {
            if (Runner.Running) Runner.Abort(); // closes the log with outcome "aborted"
            if (Replay.Loaded) Replay.Unload();
            World.ResetItems();
            Player.Teleport(World.PlayerStart, World.PlayerStartYaw);
            _menu.SetLeaderboard(_board);
            _menu.SetReplayAvailable(LastLog != null);
            _menu.SetLogFiles(RecentLogFiles(), path => LoadLogText(File.ReadAllText(path)));
            SetState(GameState.Menu);
        }

        public void StartTask(TaskDefinition task)
        {
            if (State == GameState.Playing) return;
            _currentTask = task;
            World.ResetItems();
            Player.Teleport(World.PlayerStart, World.PlayerStartYaw);
            Grab.Release(false);

            _savedPath = null;
            var sinks = new List<ILogSink>();
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                _savedPath = Path.Combine(SessionsFolder, $"{task.id}_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl");
                sinks.Add(new FileSink(_savedPath));
            }
            Recorder.Begin(World.Items, Player, Grab, task.id, _menu.PlayerName, sinks.ToArray());
            Runner.Begin(task, World);
            Recorder.LogEvent("task_start", -1, task.id);
            Hud.SetTask(Runner);
            Hud.Message(task.description, 5f);
            SetState(GameState.Playing);
            SetCursorLocked(true);
            Debug.Log($"[Task] start {task.id} ({task.type}) -> zone {task.targetZone}");
        }

        void AbortTask()
        {
            if (State == GameState.Playing) Runner.Abort();
        }

        void OnProgress()
        {
            Hud.SetTask(Runner);
            var summary = string.Join(" ", Runner.Progress.Select(p => $"{p.Item}:{p.Have}/{p.Need}"));
            Recorder.LogEvent("progress", -1, summary);
            Debug.Log("[Task] progress " + summary);
            if (Runner.Progress.All(p => p.Done) && Runner.NeedsClose) Hud.Message("All items packed. Close the lid: look at the box and press F.", 4f);
        }

        void OnTaskFinished(TaskResult result)
        {
            Grab.Release(false);
            Recorder.LogEvent("task_" + result.outcome, -1, result.score.ToString());
            LastLogText = Recorder.End(result.outcome, result.score);
            LastLog = SessionLog.Parse(LastLogText);
            LastResult = result;
            int rank = 0;
            if (result.outcome == "success")
                rank = _board.Add(new LeaderboardEntry { name = _menu.PlayerName, taskId = result.taskId, score = result.score, elapsedSeconds = result.elapsedSeconds, dateUtc = DateTime.UtcNow.ToString("o") });
            _result.Show(_currentTask, result, rank, Recorder, _savedPath);
            SetState(GameState.Result);
            Debug.Log($"[Task] {result.outcome} {result.taskId} score {result.score} in {result.elapsedSeconds:0.0}s; log {Recorder.Tick} ticks {Recorder.Memory.Bytes} bytes" + (_savedPath != null ? " -> " + _savedPath : ""));
        }

        public void StartReplay(SessionLog log)
        {
            if (log == null) return;
            World.ResetItems();
            Grab.Release(false);
            Replay.Load(log, World, Player.Head);
            SetState(GameState.Replay);
            Replay.Playing = true;
            Debug.Log($"[Replay] {log.Header.task} {log.TickCount} ticks, {log.Events.Count} events");
        }

        void ExitReplay() => ShowMenu();

        void ToggleReplayCamera()
        {
            if (State != GameState.Replay) return;
            _thirdPerson.enabled = !_thirdPerson.enabled;
            Player.Camera.enabled = !_thirdPerson.enabled;
            Player.BodyVisual.SetActive(_thirdPerson.enabled);
        }

        void DownloadLastLog()
        {
            if (LastLogText == null) return;
            WebBridge.DownloadText($"{LastLog.Header.task}_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl", LastLogText);
        }

        void OpenLogFile()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) WebBridge.OpenTextFile(gameObject.name, nameof(OnLogFileLoaded));
            else
            {
                var files = RecentLogFiles();
                if (files.Count > 0) LoadLogText(File.ReadAllText(files[0]));
                else Debug.Log("[Replay] no session files in " + SessionsFolder);
            }
        }

        /// <summary>Called from the browser (SendMessage) with the text of a chosen log file.</summary>
        public void OnLogFileLoaded(string text) => LoadLogText(text);

        public void LoadLogText(string text)
        {
            try
            {
                var log = SessionLog.Parse(text);
                LastLog = log;
                LastLogText = text;
                StartReplay(log);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Replay] could not load log: " + ex.Message);
            }
        }

        static string SessionsFolder => Path.Combine(Application.persistentDataPath, "Sessions");

        List<string> RecentLogFiles()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer || !Directory.Exists(SessionsFolder)) return new List<string>();
            return Directory.GetFiles(SessionsFolder, "*.jsonl").OrderByDescending(File.GetLastWriteTime).Take(5).ToList();
        }

        // ---------- per-frame ----------

        void Update()
        {
            switch (State)
            {
                case GameState.Playing:
                    UpdatePlaying();
                    break;
                case GameState.Replay:
                    if (_thirdPerson.enabled) UpdateThirdPerson();
                    break;
            }
        }

        void UpdatePlaying()
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (!locked && !_useScripted)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
            }
            _keyboard.Active = locked;
            Hud.ShowPause(!locked && !_useScripted);
            Player.Frozen = !locked && !_useScripted;
            Grab.Frozen = Player.Frozen;

            if (Player.Input.MenuPressed) SetCursorLocked(false);
            if (Player.Input.InteractPressed) Grab.Interact();

            Hud.SetTask(Runner);
            Hud.SetRecorder(Recorder);
            if (Grab.Held != null) Hud.SetHover($"{Grab.Held.Def.Label}  {Grab.Held.Def.Mass:0.#} kg   {Grab.HoldDistance:0.0} m");
            else if (Grab.Hovered != null) Hud.SetHover($"{Grab.Hovered.Def.Label}  {Grab.Hovered.Def.Mass:0.#} kg   [LMB] grab");
            else if (Grab.HoveredContainer != null) Hud.SetHover($"{Grab.HoveredContainer.Label}   [F] {(Grab.HoveredContainer.Closed ? "open" : "close")} lid");
            else Hud.SetHover("");
        }

        void UpdateThirdPerson()
        {
            var head = Player.Head;
            var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            var pos = head.position - fwd * 3.5f + Vector3.up * 1.4f;
            _thirdPerson.transform.position = pos;
            _thirdPerson.transform.LookAt(head.position + fwd * 2f - Vector3.up * 0.4f);
        }

        void LateUpdate()
        {
            _scripted.EndFrame();
            if (State == GameState.Replay) FollowReplayHead();
        }

        /// <summary>The log drives the head; keep the body (capsule shown in third person) under it.</summary>
        void FollowReplayHead()
        {
            var head = Player.Head;
            var pos = head.position;
            var rot = head.rotation;
            float yaw = rot.eulerAngles.y;
            Player.transform.SetPositionAndRotation(pos - FirstPersonController.HeadOffset, Quaternion.Euler(0, yaw, 0));
            head.SetPositionAndRotation(pos, rot);
        }

        void SetCursorLocked(bool on)
        {
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }
    }
}
