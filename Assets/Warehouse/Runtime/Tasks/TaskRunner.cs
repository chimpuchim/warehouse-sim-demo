using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warehouse
{
    public struct ItemProgress
    {
        public string Item;
        public string Label;
        public int Have;
        public int Need;
        public bool Done => Have >= Need;
    }

    [Serializable]
    public class TaskResult
    {
        public string taskId;
        public string outcome;   // success | timeout | aborted
        public float elapsedSeconds;
        public int score;
    }

    /// <summary>
    /// Runs one task: counts the required items inside the target zone, checks the container lid
    /// for PACK, keeps the timer and raises Completed / Failed. All three task types share this
    /// evaluation; the differences live in the JSON (zone, items, requireClosed).
    /// </summary>
    public class TaskRunner : MonoBehaviour
    {
        public TaskDefinition Task { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, Task.timeLimitSeconds - Elapsed);
        public bool Running { get; private set; }
        public bool NeedsClose => Task.requireClosed && _container != null && !_container.Closed;
        public IReadOnlyList<ItemProgress> Progress => _progress;

        public event Action<TaskResult> Finished;
        public event Action ProgressChanged;

        World _world;
        Zone _zone;
        Container _container;
        readonly List<ItemProgress> _progress = new List<ItemProgress>();
        float _nextEval;

        public void Begin(TaskDefinition task, World world)
        {
            Task = task;
            _world = world;
            _zone = world.FindZone(task.targetZone);
            if (_zone == null) throw new InvalidOperationException($"Task {task.id}: zone '{task.targetZone}' is not in the map");
            _container = world.FindContainer(_zone);
            _progress.Clear();
            foreach (var r in task.requiredItems)
                _progress.Add(new ItemProgress { Item = r.item, Label = ItemCatalog.Get(r.item).Label, Need = r.count });
            Elapsed = 0f;
            _nextEval = 0f;
            Running = true;
            Evaluate();
        }

        public void Abort() => Finish("aborted");

        void Update()
        {
            if (!Running) return;
            Elapsed += Time.deltaTime;
            if (Elapsed >= Task.timeLimitSeconds)
            {
                Finish("timeout");
                return;
            }
            if (Time.time < _nextEval) return;
            _nextEval = Time.time + 0.2f;
            Evaluate();
        }

        void Evaluate()
        {
            bool changed = false, all = true;
            for (int i = 0; i < _progress.Count; i++)
            {
                var p = _progress[i];
                int have = Mathf.Min(_world.CountInZone(_zone, p.Item), p.Need);
                if (have != p.Have)
                {
                    p.Have = have;
                    _progress[i] = p;
                    changed = true;
                }
                if (!p.Done) all = false;
            }
            if (changed) ProgressChanged?.Invoke();
            if (all && !NeedsClose) Finish("success");
        }

        void Finish(string outcome)
        {
            if (!Running) return;
            Running = false;
            var result = new TaskResult
            {
                taskId = Task.id,
                outcome = outcome,
                elapsedSeconds = Elapsed,
                score = outcome == "success" ? Scoring.Compute(Task, Elapsed) : 0,
            };
            Finished?.Invoke(result);
        }
    }

    public static class Scoring
    {
        /// <summary>Base score plus a bonus for every full second left on the clock.</summary>
        public static int Compute(TaskDefinition task, float elapsedSeconds)
        {
            int secondsLeft = Mathf.FloorToInt(Mathf.Max(0f, task.timeLimitSeconds - elapsedSeconds));
            return task.baseScore + secondsLeft * task.timeBonusPerSecond;
        }
    }
}
