using UnityEngine;
using UnityEngine.InputSystem;

namespace Warehouse
{
    /// <summary>
    /// Mouse + keyboard implementation of both input interfaces (Input System package).
    /// WASD move, Shift sprint, Space jump, mouse look, LMB hold grab, RMB throw, wheel distance,
    /// R + mouse rotate, F interact, Esc menu.
    /// </summary>
    public class KeyboardMouseInput : MonoBehaviour, IPlayerInput, IGrabInput
    {
        [SerializeField] float _lookSensitivity = 0.08f;
        [SerializeField] float _distancePerNotch = 0.25f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Sprint { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool MenuPressed { get; private set; }
        public bool GrabHeld { get; private set; }
        public bool ThrowPressed { get; private set; }
        public float DistanceDelta { get; private set; }
        public bool RotateHeld { get; private set; }
        public Vector2 RotateDelta { get; private set; }

        /// <summary>When false, look/grab input is ignored (cursor not locked, menu open).</summary>
        public bool Active { get; set; } = true;

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null || !Active)
            {
                Clear();
                MenuPressed = kb != null && kb.escapeKey.wasPressedThisFrame;
                return;
            }

            float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            float y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            Move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);

            var delta = mouse.delta.ReadValue() * _lookSensitivity;
            RotateHeld = kb.rKey.isPressed;
            Look = RotateHeld ? Vector2.zero : delta;
            RotateDelta = RotateHeld ? delta : Vector2.zero;

            Sprint = kb.leftShiftKey.isPressed;
            JumpPressed = kb.spaceKey.wasPressedThisFrame;
            InteractPressed = kb.fKey.wasPressedThisFrame;
            MenuPressed = kb.escapeKey.wasPressedThisFrame;
            GrabHeld = mouse.leftButton.isPressed;
            ThrowPressed = mouse.rightButton.wasPressedThisFrame;
            float scroll = mouse.scroll.ReadValue().y;
            DistanceDelta = Mathf.Abs(scroll) > 0.01f ? Mathf.Sign(scroll) * _distancePerNotch : 0f;
        }

        void Clear()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            Sprint = false;
            JumpPressed = false;
            InteractPressed = false;
            GrabHeld = false;
            ThrowPressed = false;
            DistanceDelta = 0f;
            RotateHeld = false;
            RotateDelta = Vector2.zero;
        }
    }
}
