using UnityEngine;

namespace FireRescue
{
    [RequireComponent(typeof(Camera))]
    public sealed class RescueCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        Vector3 focus;
        Vector3 velocity;
        public void Initialize(Transform player) { target = player; Snap(); }
        void Awake() => Snap();
        void Snap() { if (target) focus = target.position + Vector3.up * .7f; Apply(); }
        void LateUpdate()
        {
            if (!target) return;
            focus = Vector3.SmoothDamp(focus, target.position + Vector3.up * .7f, ref velocity, .18f);
            Apply();
        }
        void Apply()
        {
            Quaternion rotation = Quaternion.Euler(50, 40, 0);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * 22, rotation);
        }
    }
}
