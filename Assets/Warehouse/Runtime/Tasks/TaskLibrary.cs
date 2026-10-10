using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Warehouse
{
    /// <summary>Loads the task manifest and task files from StreamingAssets on every platform.</summary>
    public static class TaskLibrary
    {
        public const string Folder = "Tasks";
        public const string ManifestFile = "tasks.json";

        public static TaskDefinition Parse(string json)
        {
            var def = JsonUtility.FromJson<TaskDefinition>(json);
            if (def == null) throw new FormatException("not a task object");
            return def;
        }

        public static TaskManifest ParseManifest(string json) => JsonUtility.FromJson<TaskManifest>(json);

        /// <summary>Coroutine: loads every task listed in the manifest; invalid files are reported, not loaded.</summary>
        public static IEnumerator LoadAll(Action<List<TaskDefinition>, List<string>> done)
        {
            var tasks = new List<TaskDefinition>();
            var errors = new List<string>();
            string manifestText = null;
            yield return ReadText(Path.Combine(Folder, ManifestFile), t => manifestText = t, e => errors.Add(e));
            if (manifestText == null)
            {
                done(tasks, errors);
                yield break;
            }

            var manifest = ParseManifest(manifestText);
            foreach (var file in manifest.tasks ?? Array.Empty<string>())
            {
                string text = null;
                yield return ReadText(Path.Combine(Folder, file), t => text = t, e => errors.Add(e));
                if (text == null) continue;
                try
                {
                    var def = Parse(text);
                    var problems = def.Validate();
                    if (problems.Count > 0) errors.Add($"{file}: {string.Join("; ", problems)}");
                    else tasks.Add(def);
                }
                catch (Exception ex)
                {
                    errors.Add($"{file}: {ex.Message}");
                }
            }
            done(tasks, errors);
        }

        static IEnumerator ReadText(string relative, Action<string> ok, Action<string> fail)
        {
            string path = Path.Combine(Application.streamingAssetsPath, relative);
            string url = path.Contains("://") ? path : "file://" + path;
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success) ok(req.downloadHandler.text);
            else fail($"{relative}: {req.error}");
        }
    }
}
