using System;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Hold-to-grab physics carry. The held body stays a dynamic rigidbody: every physics tick its
    /// velocity is set toward a hold point in front of the camera, capped by a mass-dependent
    /// speed, so heavy objects lag and light ones snap. Objects collide with the world while
    /// carried, which is what lets the player stack them.
    /// </summary>
    public class GrabController : MonoBehaviour
    {
        [SerializeField] float _reach = 3.0f;
        [SerializeField] float _minDistance = 0.8f;
        [SerializeField] float _maxDistance = 2.6f;
        [SerializeField] float _spring = 14f;
        [SerializeField] float _breakDistance = 1.2f;
        [SerializeField] float _breakSeconds = 0.6f;

        public IGrabInput Input { get; set; }
        /// <summary>Resolves the container an item is sitting in, so looking at the contents still targets the lid.</summary>
        public Func<Interactable, Container> ContainerOf { get; set; }
        /// <summary>Name of the first collider under the crosshair (diagnostics).</summary>
        public string HitName { get; private set; } = "";
        public Interactable Held { get; private set; }
        public Interactable Hovered { get; private set; }
        public Container HoveredContainer { get; private set; }
        public float HoldDistance => _holdDistance;
        public bool Frozen { get; set; }

        public event Action<Interactable> Grabbed;
        /// <summary>Raised on drop and throw; the bool is true for a throw.</summary>
        public event Action<Interactable, bool> Released;
        public event Action<Container> ContainerToggled;

        FirstPersonController _player;
        Transform _cam;
        float _holdDistance;
        Quaternion _holdLocalRot;
        int _buttons;
        float _stuckSince = -1f;

        public static GrabController Attach(FirstPersonController player)
        {
            var g = player.gameObject.AddComponent<GrabController>();
            g._player = player;
            g._cam = player.Head;
            return g;
        }

        /// <summary>Buttons sampled this frame (grab/throw/interact/rotate bits), for the log.</summary>
        public int LastButtons => _buttons;

        void Update()
        {
            if (Input == null || Frozen) return;
            UpdateHover();

            bool grab = Input.GrabHeld;
            bool throwPressed = Input.ThrowPressed;
            bool rotate = Input.RotateHeld;
            _buttons = (grab ? InputSample.GrabBit : 0) | (throwPressed ? InputSample.ThrowBit : 0) | (rotate ? InputSample.RotateBit : 0);

            if (Held == null)
            {
                if (grab && Hovered != null && !_wasGrabHeld) Grab(Hovered);
            }
            else
            {
                if (throwPressed) Release(true);
                else if (!grab) Release(false);
                else
                {
                    _holdDistance = Mathf.Clamp(_holdDistance + Input.DistanceDelta, _minDistance, _maxDistance);
                    if (rotate)
                    {
                        var d = Input.RotateDelta;
                        var yawFrame = Quaternion.Euler(0, _player.Yaw, 0);
                        var rightInYaw = Quaternion.Inverse(yawFrame) * _cam.right;
                        _holdLocalRot = Quaternion.AngleAxis(-d.x, Vector3.up) * Quaternion.AngleAxis(d.y, rightInYaw) * _holdLocalRot;
                    }
                }
            }
            _wasGrabHeld = grab;
        }

        bool _wasGrabHeld;

        /// <summary>Distance from the held object to its hold point (0 when nothing is held).</summary>
        public float Lag { get; private set; }

        public void SetHoldDistance(float d) => _holdDistance = Mathf.Clamp(d, _minDistance, _maxDistance);

        /// <summary>Called by the game flow when the interact key is pressed.</summary>
        public void Interact()
        {
            if (HoveredContainer == null) return;
            HoveredContainer.Toggle();
            _buttons |= InputSample.InteractBit;
            ContainerToggled?.Invoke(HoveredContainer);
        }

        void UpdateHover()
        {
            Interactable item = null;
            Container container = null;
            HitName = "";
            // The player is on the Ignore Raycast layer: the ray starts inside its own capsule and
            // would otherwise hit it when looking down.
            var hits = Physics.RaycastAll(_cam.position, _cam.forward, _reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                var first = hits[0];
                HitName = first.collider.name + (first.collider.transform.parent != null ? " < " + first.collider.transform.parent.name : "");
                if (first.rigidbody != null) first.rigidbody.TryGetComponent(out item);
                // The lid target: the first container along the ray, or the container the hovered item sits in.
                foreach (var h in hits)
                {
                    container = h.collider.GetComponentInParent<Container>();
                    if (container != null) break;
                }
                if (container == null && item != null && ContainerOf != null) container = ContainerOf(item);
            }
            if (item != Hovered)
            {
                if (Hovered != null) Hovered.SetHighlight(false);
                Hovered = item;
                if (Hovered != null && Held == null) Hovered.SetHighlight(true);
            }
            HoveredContainer = container;
        }

        void Grab(Interactable item)
        {
            Held = item;
            Held.IsHeld = true;
            Held.SetHighlight(false);
            var rb = Held.Body;
            rb.useGravity = false;
            rb.linearDamping = 4f;
            rb.angularDamping = 4f;
            _holdDistance = Mathf.Clamp(Vector3.Distance(_cam.position, rb.position), _minDistance, _maxDistance);
            _holdLocalRot = Quaternion.Inverse(Quaternion.Euler(0, _player.Yaw, 0)) * rb.rotation;
            _stuckSince = -1f;
            Physics.IgnoreCollision(Held.Collider, _player.Controller, true);
            _player.SpeedMultiplier = Mathf.Lerp(1f, 0.55f, Mathf.InverseLerp(5f, 30f, rb.mass));
            Grabbed?.Invoke(Held);
        }

        public void Release(bool thrown)
        {
            if (Held == null) return;
            var item = Held;
            var rb = item.Body;
            Held = null;
            Lag = 0f;
            item.IsHeld = false;
            rb.useGravity = true;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            Physics.IgnoreCollision(item.Collider, _player.Controller, false);
            _player.SpeedMultiplier = 1f;

            if (thrown)
            {
                float speed = Mathf.Lerp(12f, 2f, Mathf.InverseLerp(0.5f, 30f, rb.mass));
                rb.linearVelocity = _cam.forward * speed + _player.Velocity;
            }
            else if (rb.linearVelocity.magnitude < 1.5f)
            {
                // Placing: settle instead of carrying the follow velocity into the stack.
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Released?.Invoke(item, thrown);
        }

        void FixedUpdate()
        {
            if (Held == null) return;
            var rb = Held.Body;
            var target = _cam.position + _cam.forward * _holdDistance;
            float halfHeight = Held.Def.Size.y * 0.5f;
            if (target.y < halfHeight + 0.02f) target.y = halfHeight + 0.02f;

            var toTarget = target - rb.position;
            Lag = toTarget.magnitude;
            float maxSpeed = Mathf.Lerp(10f, 2.5f, Mathf.InverseLerp(1f, 30f, rb.mass));

            // Drop only when the object cannot follow for a while (blocked by geometry), not on a
            // fast turn: a 90 degree flick moves the hold point further than the break distance
            // for a few ticks and a heavy object needs up to a second to catch up.
            if (toTarget.magnitude > _breakDistance)
            {
                if (_stuckSince < 0f) _stuckSince = Time.time;
                else if (Time.time - _stuckSince > _breakSeconds + _breakDistance / maxSpeed)
                {
                    Release(false);
                    return;
                }
            }
            else _stuckSince = -1f;
            rb.linearVelocity = Vector3.ClampMagnitude(toTarget * _spring, maxSpeed);

            var desired = Quaternion.Euler(0, _player.Yaw, 0) * _holdLocalRot;
            var delta = desired * Quaternion.Inverse(rb.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (float.IsNaN(axis.x)) return;
            float maxTurn = Mathf.Lerp(12f, 3f, Mathf.InverseLerp(1f, 30f, rb.mass));
            rb.angularVelocity = Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad * 8f), maxTurn);
        }
    }
}
