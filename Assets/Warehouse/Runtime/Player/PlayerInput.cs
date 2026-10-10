using UnityEngine;

namespace Warehouse
{
    /// <summary>Locomotion and interaction input. One implementation per input device family.</summary>
    public interface IPlayerInput
    {
        /// <summary>Planar move axis, x = strafe, y = forward, each in -1..1.</summary>
        Vector2 Move { get; }
        /// <summary>Look delta for this frame in degrees (yaw, pitch).</summary>
        Vector2 Look { get; }
        bool Sprint { get; }
        bool JumpPressed { get; }
        bool InteractPressed { get; }
        bool MenuPressed { get; }
    }

    /// <summary>
    /// Grab input. The mouse/keyboard version is the only implementation shipped; a VR controller
    /// implementation would map trigger/grip to GrabHeld and the controller pose to the hold point.
    /// </summary>
    public interface IGrabInput
    {
        bool GrabHeld { get; }
        bool ThrowPressed { get; }
        /// <summary>Hold-distance change for this frame in metres.</summary>
        float DistanceDelta { get; }
        bool RotateHeld { get; }
        /// <summary>Rotation delta while RotateHeld, in degrees (yaw, pitch).</summary>
        Vector2 RotateDelta { get; }
    }

    /// <summary>Compact snapshot of one tick of input, written to the session log.</summary>
    public struct InputSample
    {
        public Vector2 Move;
        public Vector2 Look;
        public int Buttons;

        public const int SprintBit = 1;
        public const int JumpBit = 2;
        public const int GrabBit = 4;
        public const int ThrowBit = 8;
        public const int InteractBit = 16;
        public const int RotateBit = 32;
    }
}
