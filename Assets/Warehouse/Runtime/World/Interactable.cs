using UnityEngine;

namespace Warehouse
{
    /// <summary>A grabbable physics object. One per spawned item; the registry assigns the tracking id.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Interactable : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public ItemDef Def { get; private set; }
        public Rigidbody Body { get; private set; }
        public Collider Collider { get; private set; }
        /// <summary>Index in the session log's object table.</summary>
        public int TrackId { get; set; } = -1;
        public bool IsHeld { get; set; }

        Renderer _renderer;
        MaterialPropertyBlock _block;
        Vector3 _startPos;
        Quaternion _startRot;

        public void Init(ItemDef def)
        {
            Def = def;
            Body = GetComponent<Rigidbody>();
            Collider = GetComponent<Collider>();
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            _startPos = transform.position;
            _startRot = transform.rotation;
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            _block.Clear();
            if (on) _block.SetColor(BaseColor, Color.Lerp(Def.Color, Color.white, 0.45f));
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>Puts the object back where the map placed it, at rest.</summary>
        public void ResetPose()
        {
            IsHeld = false;
            Body.isKinematic = false;
            Body.useGravity = true;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = _startPos;
            Body.rotation = _startRot;
            transform.SetPositionAndRotation(_startPos, _startRot);
            SetHighlight(false);
        }
    }
}
