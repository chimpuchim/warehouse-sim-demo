using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Warehouse
{
    /// <summary>The only object in the scene. Sets up input and starts the game flow; everything else is built in code.</summary>
    public class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = -1;
            Time.fixedDeltaTime = 0.02f; // 50 Hz physics and logging
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Dev.Init();
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
            if (Dev.Enabled) new GameObject("Dev").AddComponent<DevBridge>().Flow = flow;
        }
    }
}
