using UnityEngine;

namespace Warehouse
{
    /// <summary>A named axis-aligned volume that tasks count items in (tote, pack box, truck bed).</summary>
    public class Zone : MonoBehaviour
    {
        public string Id { get; private set; }
        public Bounds Bounds { get; private set; }

        public void Init(string id, Vector3 center, Vector3 size)
        {
            Id = id;
            Bounds = new Bounds(center, size);
        }

        public bool Contains(Interactable item) => Bounds.Contains(item.transform.position);
    }

    /// <summary>A zone with a lid: PACK tasks need it closed. Toggled with the interact key.</summary>
    public class Container : MonoBehaviour
    {
        public Zone Zone { get; private set; }
        public bool Closed { get; private set; }
        public string Label { get; private set; }

        Transform _lid;
        float _lidT;

        public void Init(Zone zone, Transform lid, string label)
        {
            Zone = zone;
            _lid = lid;
            Label = label;
        }

        public void Toggle() => Closed = !Closed;

        public void ResetLid() => Closed = false;

        void Update()
        {
            float target = Closed ? 1f : 0f;
            if (Mathf.Approximately(_lidT, target)) return;
            _lidT = Mathf.MoveTowards(_lidT, target, Time.deltaTime * 3f);
            _lid.localRotation = Quaternion.Euler(Mathf.Lerp(-110f, 0f, _lidT), 0f, 0f);
        }
    }
}
