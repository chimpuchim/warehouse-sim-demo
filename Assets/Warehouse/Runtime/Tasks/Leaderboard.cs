using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public string taskId;
        public int score;
        public float elapsedSeconds;
        public string dateUtc;
    }

    /// <summary>Local top-10 kept in PlayerPrefs as JSON (IndexedDB on WebGL).</summary>
    public class Leaderboard
    {
        const string Key = "warehouse.leaderboard";
        const int Max = 10;

        [Serializable]
        class Store { public List<LeaderboardEntry> entries = new List<LeaderboardEntry>(); }

        Store _store;

        public IReadOnlyList<LeaderboardEntry> Entries => _store.entries;

        public Leaderboard()
        {
            try { _store = JsonUtility.FromJson<Store>(PlayerPrefs.GetString(Key, "")) ?? new Store(); }
            catch { _store = new Store(); }
        }

        /// <summary>Adds the entry and returns its 1-based rank, or 0 if it did not make the table.</summary>
        public int Add(LeaderboardEntry e)
        {
            _store.entries.Add(e);
            _store.entries = _store.entries.OrderByDescending(x => x.score).ThenBy(x => x.elapsedSeconds).Take(Max).ToList();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_store));
            PlayerPrefs.Save();
            int i = _store.entries.IndexOf(e);
            return i < 0 ? 0 : i + 1;
        }
    }
}
