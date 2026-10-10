using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Input driven by code instead of a device. Used by the developer bridge to run the game from
    /// automated tests and by the walk-to helper. Values persist until changed; presses are one-shot.
    /// </summary>
    public class ScriptedInput : IPlayerInput, IGrabInput
    {
        public Vector2 Move { get; set; }
        public Vector2 Look { get; set; }
        public bool Sprint { get; set; }
        public bool GrabHeld { get; set; }
        public bool RotateHeld { get; set; }
        public Vector2 RotateDelta { get; set; }
        public float DistanceDelta { get; set; }

        bool _jump, _interact, _menu, _throw;

        public bool JumpPressed => Consume(ref _jump);
        public bool InteractPressed => Consume(ref _interact);
        public bool MenuPressed => Consume(ref _menu);
        public bool ThrowPressed => Consume(ref _throw);

        public void PressJump() => _jump = true;
        public void PressInteract() => _interact = true;
        public void PressMenu() => _menu = true;
        public void PressThrow() => _throw = true;

        /// <summary>Clears the per-frame deltas; call once per frame after consumers have read them.</summary>
        public void EndFrame()
        {
            Look = Vector2.zero;
            RotateDelta = Vector2.zero;
            DistanceDelta = 0f;
        }

        static bool Consume(ref bool flag)
        {
            if (!flag) return false;
            flag = false;
            return true;
        }
    }
}
