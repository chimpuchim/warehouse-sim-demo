using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Character-controller based first-person locomotion. Reads an <see cref="IPlayerInput"/>;
    /// the camera is a child pitched by the look input, the body is yawed.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] float _walkSpeed = 4f;
        [SerializeField] float _sprintSpeed = 6.5f;
        [SerializeField] float _jumpHeight = 1.0f;
        [SerializeField] float _gravity = -20f;
        [SerializeField] float _pushForce = 2.5f;

        public IPlayerInput Input { get; set; }
        public Transform Head { get; private set; }
        public Camera Camera { get; private set; }
        public CharacterController Controller { get; private set; }
        /// <summary>Set by the grab system while carrying something heavy.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public Vector3 Velocity => _velocity;
        public float Yaw => transform.eulerAngles.y;
        public float Pitch => _pitch;
        public bool Frozen { get; set; }

        Vector3 _velocity;
        float _pitch;
        InputSample _sample;

        public static FirstPersonController Create(Vector3 position, float yaw)
        {
            var go = new GameObject("Player");
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.04f;
            var fpc = go.AddComponent<FirstPersonController>();
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));

            var head = new GameObject("Head");
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 1.65f, 0);
            var cam = head.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            cam.fieldOfView = 70f;
            cam.tag = "MainCamera";
            head.AddComponent<AudioListener>();
            fpc.Head = head.transform;
            fpc.Camera = cam;
            return fpc;
        }

        void Awake() => Controller = GetComponent<CharacterController>();

        public void Teleport(Vector3 position, float yaw, float pitch = 0f)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            _pitch = pitch;
            Head.localRotation = Quaternion.Euler(_pitch, 0, 0);
            _velocity = Vector3.zero;
            Controller.enabled = true;
        }

        /// <summary>Sets the view angles without moving (used by the developer bridge).</summary>
        public void SetView(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            _pitch = Mathf.Clamp(pitch, -85f, 85f);
            Head.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        /// <summary>Input as sampled on the last frame, for the session log.</summary>
        public InputSample LastSample => _sample;

        void Update()
        {
            if (Input == null || Frozen) return;

            var look = Input.Look;
            transform.Rotate(0, look.x, 0, Space.World);
            _pitch = Mathf.Clamp(_pitch - look.y, -85f, 85f);
            Head.localRotation = Quaternion.Euler(_pitch, 0, 0);

            var move = Input.Move;
            float speed = (Input.Sprint ? _sprintSpeed : _walkSpeed) * SpeedMultiplier;
            var planar = (transform.right * move.x + transform.forward * move.y) * speed;

            bool jump = Input.JumpPressed;
            if (Controller.isGrounded)
            {
                _velocity.y = jump ? Mathf.Sqrt(2f * -_gravity * _jumpHeight) : -1f;
            }
            _velocity.y += _gravity * Time.deltaTime;
            _velocity.x = planar.x;
            _velocity.z = planar.z;
            Controller.Move(_velocity * Time.deltaTime);

            _sample.Move = move;
            _sample.Look = look;
            _sample.Buttons = (Input.Sprint ? InputSample.SprintBit : 0) | (jump ? InputSample.JumpBit : 0);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.rigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            if (rb.TryGetComponent<Interactable>(out var item) && item.IsHeld) return;
            var push = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            rb.AddForceAtPosition(push * _pushForce * Mathf.Clamp(10f / rb.mass, 0.2f, 1f), hit.point, ForceMode.Impulse);
        }
    }
}
