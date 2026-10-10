using System.Runtime.InteropServices;
using UnityEngine;

namespace Warehouse
{
    /// <summary>Browser helpers for the WebGL build (download a text file, open a text file).</summary>
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void WarehouseDownloadText(string name, string text);
        [DllImport("__Internal")] static extern void WarehouseOpenTextFile(string gameObject, string method);
        [DllImport("__Internal")] static extern void WarehouseNotifyReady();
#endif

        public static void DownloadText(string fileName, string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WarehouseDownloadText(fileName, text);
#else
            Debug.Log("[WebBridge] download is only wired on WebGL: " + fileName + " (" + text.Length + " chars)");
#endif
        }

        /// <summary>Opens the browser file picker; the file's text is sent to gameObject.method.</summary>
        public static void OpenTextFile(string gameObject, string method)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WarehouseOpenTextFile(gameObject, method);
#else
            Debug.Log("[WebBridge] file picker is only wired on WebGL");
#endif
        }

        public static void NotifyReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WarehouseNotifyReady();
#endif
        }
    }
}
