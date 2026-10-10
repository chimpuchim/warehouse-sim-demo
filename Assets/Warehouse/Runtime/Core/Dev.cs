using UnityEngine;

namespace Warehouse
{
    /// <summary>Developer mode: `?dev=1` in the WebGL URL (always on in the Editor). Enables the scripted-input bridge.</summary>
    public static class Dev
    {
        public static bool Enabled { get; private set; }

        public static void Init()
        {
            Enabled = Application.isEditor || (Application.absoluteURL ?? "").Contains("dev=1");
            if (Enabled) Debug.Log("[DEV] developer mode on");
        }

        public static void Log(string message)
        {
            if (Enabled) Debug.Log("[DEV] " + message);
        }
    }
}
